using UnityEngine;

namespace Code.Scripts.Restaurant.Kitchen
{
    /// <summary>
    /// BackCounter 조리기구 하나의 진행 상태와 완성 결과물을 관리합니다.
    /// 자동 진행과 수동 홀드 진행을 같은 세션 모델로 처리합니다.
    /// </summary>
    public sealed class ToolCookingSession
    {
        /// <summary>
        /// 지정한 조리기구 타입에 대한 세션을 생성합니다.
        /// </summary>
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

        /// <summary>
        /// 새 조리 작업을 시작하고 완료 시 반환할 결과물을 세션에 보관합니다.
        /// </summary>
        public void Start(KitchenProgressMode mode, float seconds, KitchenCarryItem output)
        {
            ProgressMode = mode;
            DurationSeconds = Mathf.Max(0.1f, seconds);
            ProgressSeconds = 0f;
            OutputItem = output != null ? output.Clone() : null;
            IsCooking = true;
        }

        /// <summary>
        /// 시간 경과와 수동 입력 유지 여부를 반영하고 이번 틱에서 완료됐는지 반환합니다.
        /// </summary>
        public bool Tick(float deltaSeconds, bool manualHeld)
        {
            if (!IsCooking)
            {
                return false;
            }

            // 도마 같은 수동 기구는 상호작용 키를 누른 동안만 진행됩니다.
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

        /// <summary>
        /// 완료된 결과물을 꺼내고 세션을 빈 상태로 되돌립니다.
        /// </summary>
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
