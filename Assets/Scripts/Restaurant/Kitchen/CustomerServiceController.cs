using System;
using System.Collections.Generic;
using UnityEngine;

namespace Restaurant.Kitchen
{
    public sealed class OrderTicket
    {
        public OrderTicket(string ticketId, KitchenDishData dish, float patienceSeconds)
        {
            TicketId = string.IsNullOrWhiteSpace(ticketId) ? Guid.NewGuid().ToString("N") : ticketId;
            Dish = dish;
            PatienceSeconds = Mathf.Max(0.1f, patienceSeconds);
            RemainingSeconds = PatienceSeconds;
        }

        public string TicketId { get; }
        public KitchenDishData Dish { get; }
        public float PatienceSeconds { get; }
        public float RemainingSeconds { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool IsExpired => !IsCompleted && RemainingSeconds <= 0f;

        public void Tick(float deltaSeconds)
        {
            if (IsCompleted)
            {
                return;
            }

            RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Mathf.Max(0f, deltaSeconds));
        }

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

    public sealed class CustomerServiceController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float orderDelaySeconds = 2f;
        [SerializeField, Min(0.1f)] private float patienceSeconds = 10f;

        private readonly List<OrderTicket> tickets = new();

        private RestaurantFlowController flowController;
        private RestaurantManager restaurantManager;
        private RestaurantManager subscribedRestaurant;
        private float nextOrderTimer;

        public event Action TicketsChanged;

        public IReadOnlyList<OrderTicket> Tickets => tickets;
        public int ActiveTicketCount => tickets.Count;
        public bool HasActiveTickets => ActiveTicketCount > 0;

        private void Awake()
        {
            BindDependencies();
            ResetOrderTimer();
        }

        private void OnEnable()
        {
            BindDependencies();
        }

        private void OnDisable()
        {
            UnbindRestaurant();
        }

        public bool TryServeHeldDish()
        {
            BindDependencies();
            if (flowController == null)
            {
                return false;
            }

            for (int index = 0; index < tickets.Count; index++)
            {
                OrderTicket ticket = tickets[index];
                if (ticket == null || !flowController.TryServeHeldDish(ticket))
                {
                    continue;
                }

                restaurantManager?.TryRecordCompletedOrder(ticket.Dish != null ? ticket.Dish.RecipeId : string.Empty);
                tickets.RemoveAt(index);
                RaiseTicketsChanged();
                return true;
            }

            return false;
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

            for (int index = tickets.Count - 1; index >= 0; index--)
            {
                OrderTicket ticket = tickets[index];
                if (ticket == null)
                {
                    tickets.RemoveAt(index);
                    removedAnyTicket = true;
                    continue;
                }

                ticket.Tick(deltaSeconds);
                if (!ticket.IsCompleted && !ticket.IsExpired)
                {
                    continue;
                }

                tickets.RemoveAt(index);
                removedAnyTicket = true;
            }

            if (removedAnyTicket)
            {
                RaiseTicketsChanged();
            }

            nextOrderTimer -= deltaSeconds;
            if (nextOrderTimer > 0f || tickets.Count >= 3)
            {
                return;
            }

            KitchenDishData dish = flowController.GetRandomTodayDish();
            if (dish != null)
            {
                tickets.Add(new OrderTicket(Guid.NewGuid().ToString("N"), dish, patienceSeconds));
                RaiseTicketsChanged();
            }

            ResetOrderTimer();
        }

        private void BindDependencies()
        {
            if (flowController == null)
            {
                flowController = RestaurantFlowController.GetOrCreate();
            }

            RestaurantManager targetRestaurant = restaurantManager != null
                ? restaurantManager
                : FindFirstObjectByType<RestaurantManager>();

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
            if (!isOpen && tickets.Count == 0)
            {
                RaiseTicketsChanged();
            }
        }

        private void ResetOrderTimer()
        {
            nextOrderTimer = orderDelaySeconds;
        }

        private void RaiseTicketsChanged()
        {
            TicketsChanged?.Invoke();
        }
    }
}
