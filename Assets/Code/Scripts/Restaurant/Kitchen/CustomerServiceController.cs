using System;
using System.Collections.Generic;
using UnityEngine;

namespace Restaurant.Kitchen
{
    /// <summary>
    /// 단일 테이블에 귀속된 주문 티켓이다.
    /// </summary>
    public sealed class OrderTicket
    {
        public OrderTicket(string ticketId, string tableId, KitchenDishData dish, float patienceSeconds)
        {
            TicketId = string.IsNullOrWhiteSpace(ticketId) ? Guid.NewGuid().ToString("N") : ticketId;
            TableId = string.IsNullOrWhiteSpace(tableId) ? string.Empty : tableId;
            Dish = dish;
            PatienceSeconds = Mathf.Max(0.1f, patienceSeconds);
            RemainingSeconds = PatienceSeconds;
        }

        public string TicketId { get; }
        public string TableId { get; }
        public KitchenDishData Dish { get; }
        public float PatienceSeconds { get; }
        public float RemainingSeconds { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool IsExpired => !IsCompleted && RemainingSeconds <= 0f;

        /// <summary>
        /// 주문 인내심을 감소시킨다.
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            if (IsCompleted)
            {
                return;
            }

            RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Mathf.Max(0f, deltaSeconds));
        }

        /// <summary>
        /// 지정한 완성 요리가 이 주문과 일치하면 완료로 표시한다.
        /// </summary>
        public bool TryComplete(KitchenCarryItem servedItem)
        {
            if (servedItem == null || Dish == null)
            {
                return false;
            }

            if (!string.Equals(servedItem.RecipeId, Dish.RecipeId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            IsCompleted = true;
            return true;
        }
    }

    /// <summary>
    /// 허브 테이블별 0/1 활성 주문과 서빙 결과를 관리한다.
    /// </summary>
    public sealed class CustomerServiceController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float orderDelaySeconds = 2f;
        [SerializeField, Min(0.1f)] private float patienceSeconds = 10f;

        private readonly Dictionary<string, OrderTicket> ticketsByTableId = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<OrderTicket> ticketSnapshot = new();

        private RestaurantFlowController flowController;
        private RestaurantManager restaurantManager;
        private RestaurantManager subscribedRestaurant;
        private float nextOrderTimer;

        public event Action TicketsChanged;

        public IReadOnlyList<OrderTicket> Tickets => ticketSnapshot;
        public int ActiveTicketCount => ticketSnapshot.Count;
        public bool HasActiveTickets => ActiveTicketCount > 0;

        private void Awake()
        {
            BindDependencies();
            ResetOrderTimer();
            RebuildTicketSnapshot();
        }

        private void OnEnable()
        {
            BindDependencies();
        }

        private void OnDisable()
        {
            UnbindRestaurant();
        }

        /// <summary>
        /// 지정한 테이블에 활성 주문이 있으면 반환한다.
        /// </summary>
        public OrderTicket GetTicketForTable(DiningTableStation table)
        {
            return table != null
                && !string.IsNullOrWhiteSpace(table.TableId)
                && ticketsByTableId.TryGetValue(table.TableId, out OrderTicket ticket)
                ? ticket
                : null;
        }

        /// <summary>
        /// 지정한 테이블에 활성 주문이 있는지 반환한다.
        /// </summary>
        public bool HasActiveTicketForTable(DiningTableStation table)
        {
            return GetTicketForTable(table) != null;
        }

        /// <summary>
        /// 지정한 테이블 주문과 손에 든 완성 요리가 일치하면 서빙을 완료한다.
        /// </summary>
        public bool TryServeHeldDish(DiningTableStation table)
        {
            BindDependencies();
            if (flowController == null || table == null || string.IsNullOrWhiteSpace(table.TableId))
            {
                return false;
            }

            if (!ticketsByTableId.TryGetValue(table.TableId, out OrderTicket ticket) || ticket == null)
            {
                return false;
            }

            if (!flowController.TryServeHeldDish(ticket))
            {
                return false;
            }

            restaurantManager?.TryRecordCompletedOrder(ticket.Dish != null ? ticket.Dish.RecipeId : string.Empty);
            ticketsByTableId.Remove(table.TableId);
            RaiseTicketsChanged();
            return true;
        }

        private void Update()
        {
            BindDependencies();
            if (flowController == null || !flowController.IsOpen)
            {
                return;
            }

            float deltaSeconds = Time.unscaledDeltaTime;
            bool removedAnyTicket = false;
            List<string> expiredTableIds = null;

            foreach (KeyValuePair<string, OrderTicket> pair in ticketsByTableId)
            {
                OrderTicket ticket = pair.Value;
                if (ticket == null)
                {
                    expiredTableIds ??= new List<string>();
                    expiredTableIds.Add(pair.Key);
                    removedAnyTicket = true;
                    continue;
                }

                ticket.Tick(deltaSeconds);
                if (!ticket.IsCompleted && !ticket.IsExpired)
                {
                    continue;
                }

                expiredTableIds ??= new List<string>();
                expiredTableIds.Add(pair.Key);
                removedAnyTicket = true;
            }

            if (expiredTableIds != null)
            {
                for (int index = 0; index < expiredTableIds.Count; index++)
                {
                    ticketsByTableId.Remove(expiredTableIds[index]);
                }
            }

            if (removedAnyTicket)
            {
                RaiseTicketsChanged();
            }

            IReadOnlyList<DiningTableStation> tables = ResolveTables();
            nextOrderTimer -= deltaSeconds;
            if (nextOrderTimer > 0f || tables.Count == 0 || ActiveTicketCount >= tables.Count)
            {
                return;
            }

            DiningTableStation availableTable = PickRandomAvailableTable(tables);
            KitchenDishData dish = availableTable != null ? flowController.GetRandomTodayDish() : null;
            if (availableTable != null && dish != null)
            {
                ticketsByTableId[availableTable.TableId] = new OrderTicket(Guid.NewGuid().ToString("N"), availableTable.TableId, dish, patienceSeconds);
                RaiseTicketsChanged();
            }

            ResetOrderTimer();
        }

        private void BindDependencies()
        {
            HubRuntimeContext hubContext = HubRuntimeContext.Active;

            if (flowController == null)
            {
                flowController = hubContext != null
                    ? hubContext.RestaurantFlowController
                    : GetComponent<RestaurantFlowController>();
            }

            RestaurantManager targetRestaurant = hubContext != null
                ? hubContext.RestaurantManager
                : restaurantManager != null
                    ? restaurantManager
                    : GetComponent<RestaurantManager>();

            if (targetRestaurant == subscribedRestaurant)
            {
                restaurantManager = targetRestaurant;
                return;
            }

            UnbindRestaurant();
            restaurantManager = targetRestaurant;
            subscribedRestaurant = targetRestaurant;

            if (subscribedRestaurant != null)
            {
                subscribedRestaurant.ServiceStateChanged += HandleServiceStateChanged;
            }
        }

        private IReadOnlyList<DiningTableStation> ResolveTables()
        {
            if (HubRuntimeContext.Active != null && HubRuntimeContext.Active.DiningTables != null)
            {
                return HubRuntimeContext.Active.DiningTables;
            }

            return Array.Empty<DiningTableStation>();
        }

        private DiningTableStation PickRandomAvailableTable(IReadOnlyList<DiningTableStation> tables)
        {
            List<DiningTableStation> availableTables = new();
            for (int index = 0; index < tables.Count; index++)
            {
                DiningTableStation table = tables[index];
                if (table == null || string.IsNullOrWhiteSpace(table.TableId) || ticketsByTableId.ContainsKey(table.TableId))
                {
                    continue;
                }

                availableTables.Add(table);
            }

            return availableTables.Count == 0
                ? null
                : availableTables[UnityEngine.Random.Range(0, availableTables.Count)];
        }

        private void UnbindRestaurant()
        {
            if (subscribedRestaurant != null)
            {
                subscribedRestaurant.ServiceStateChanged -= HandleServiceStateChanged;
            }

            subscribedRestaurant = null;
        }

        private void HandleServiceStateChanged(bool isOpen)
        {
            ResetOrderTimer();
            if (!isOpen && ticketsByTableId.Count > 0)
            {
                ticketsByTableId.Clear();
                RaiseTicketsChanged();
                return;
            }

            if (!isOpen && ticketsByTableId.Count == 0)
            {
                RaiseTicketsChanged();
            }
        }

        private void ResetOrderTimer()
        {
            nextOrderTimer = orderDelaySeconds;
        }

        private void RebuildTicketSnapshot()
        {
            ticketSnapshot.Clear();

            IReadOnlyList<DiningTableStation> tables = ResolveTables();
            if (tables.Count > 0)
            {
                for (int index = 0; index < tables.Count; index++)
                {
                    DiningTableStation table = tables[index];
                    if (table != null
                        && !string.IsNullOrWhiteSpace(table.TableId)
                        && ticketsByTableId.TryGetValue(table.TableId, out OrderTicket ticket)
                        && ticket != null)
                    {
                        ticketSnapshot.Add(ticket);
                    }
                }

                return;
            }

            foreach (OrderTicket ticket in ticketsByTableId.Values)
            {
                if (ticket != null)
                {
                    ticketSnapshot.Add(ticket);
                }
            }
        }

        private void RaiseTicketsChanged()
        {
            RebuildTicketSnapshot();
            TicketsChanged?.Invoke();
        }
    }
}
