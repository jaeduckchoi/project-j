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
        private float nextOrderTimer;

        public IReadOnlyList<OrderTicket> Tickets => tickets;

        public bool TryServeHeldDish()
        {
            if (flowController == null)
            {
                flowController = RestaurantFlowController.GetOrCreate();
            }

            for (int i = 0; i < tickets.Count; i++)
            {
                if (tickets[i] != null && flowController.TryServeHeldDish(tickets[i]))
                {
                    tickets.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        private void Awake()
        {
            flowController = RestaurantFlowController.GetOrCreate();
            nextOrderTimer = orderDelaySeconds;
        }

        private void Update()
        {
            if (flowController == null || !flowController.IsOpen)
            {
                return;
            }

            float delta = Time.unscaledDeltaTime;
            for (int i = tickets.Count - 1; i >= 0; i--)
            {
                tickets[i].Tick(delta);
                if (tickets[i].IsCompleted || tickets[i].IsExpired)
                {
                    tickets.RemoveAt(i);
                }
            }

            nextOrderTimer -= delta;
            if (nextOrderTimer <= 0f && tickets.Count < 3)
            {
                KitchenDishData dish = flowController.GetRandomTodayDish();
                if (dish != null)
                {
                    tickets.Add(new OrderTicket(Guid.NewGuid().ToString("N"), dish, patienceSeconds));
                }

                nextOrderTimer = orderDelaySeconds;
            }
        }
    }
}
