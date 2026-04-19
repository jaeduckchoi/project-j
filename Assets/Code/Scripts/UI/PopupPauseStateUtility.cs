namespace UI
{
    /// <summary>
    /// 허브 팝업 열림 상태에 맞춰 시간 정지 적용 여부와 복구 값을 계산합니다.
    /// UIManager는 계산 결과만 받아 실제 Time.timeScale에 반영합니다.
    /// </summary>
    public static class PopupPauseStateUtility
    {
        public readonly struct Snapshot
        {
            public Snapshot(bool isPauseApplied, float previousTimeScale, float nextTimeScale)
            {
                IsPauseApplied = isPauseApplied;
                PreviousTimeScale = previousTimeScale;
                NextTimeScale = nextTimeScale;
            }

            public bool IsPauseApplied { get; }
            public float PreviousTimeScale { get; }
            public float NextTimeScale { get; }
        }

        /// <summary>
        /// 팝업 표시 여부에 따라 다음 시간 정지 상태를 계산합니다.
        /// 이미 정지가 적용된 상태면 기존 복구 값을 유지합니다.
        /// </summary>
        public static Snapshot Apply(bool shouldPause, bool isPauseApplied, float previousTimeScale, float currentTimeScale)
        {
            if (shouldPause)
            {
                return isPauseApplied
                    ? new Snapshot(true, previousTimeScale, currentTimeScale)
                    : new Snapshot(true, currentTimeScale, 0f);
            }

            return Restore(isPauseApplied, previousTimeScale, currentTimeScale);
        }

        /// <summary>
        /// 정지 상태가 적용돼 있었다면 직전 시간 배속으로 복구합니다.
        /// </summary>
        public static Snapshot Restore(bool isPauseApplied, float previousTimeScale, float currentTimeScale)
        {
            return isPauseApplied
                ? new Snapshot(false, previousTimeScale, previousTimeScale)
                : new Snapshot(false, previousTimeScale, currentTimeScale);
        }
    }
}
