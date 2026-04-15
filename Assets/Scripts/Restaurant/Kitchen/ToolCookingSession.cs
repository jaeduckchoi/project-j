using UnityEngine;

namespace Restaurant.Kitchen
{
    public sealed class ToolCookingSession
    {
        public ToolCookingSession(KitchenToolType toolType)
        {
            ToolType = toolType;
        }

        public KitchenToolType ToolType { get; }
        public KitchenProgressMode ProgressMode { get; private set; }
        public float DurationSeconds { get; private set; } = 1f;
        public float ProgressSeconds { get; private set; }
        public KitchenCarryItem OutputItem { get; private set; }
        public bool IsCooking { get; private set; }
        public bool HasOutput => OutputItem != null && !IsCooking;
        public float NormalizedProgress => DurationSeconds <= 0f ? 1f : Mathf.Clamp01(ProgressSeconds / DurationSeconds);

        public void Start(KitchenProgressMode mode, float seconds, KitchenCarryItem output)
        {
            ProgressMode = mode;
            DurationSeconds = Mathf.Max(0.1f, seconds);
            ProgressSeconds = 0f;
            OutputItem = output != null ? output.Clone() : null;
            IsCooking = true;
        }

        public bool Tick(float deltaSeconds, bool manualHeld)
        {
            if (!IsCooking)
            {
                return false;
            }

            if (ProgressMode == KitchenProgressMode.AutoProgress || manualHeld)
            {
                ProgressSeconds += Mathf.Max(0f, deltaSeconds);
            }

            if (ProgressSeconds < DurationSeconds)
            {
                return false;
            }

            ProgressSeconds = DurationSeconds;
            IsCooking = false;
            return true;
        }

        public KitchenCarryItem TakeOutput()
        {
            if (!HasOutput)
            {
                return null;
            }

            KitchenCarryItem item = OutputItem;
            OutputItem = null;
            ProgressSeconds = 0f;
            return item;
        }
    }
}
