using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// Flow 네임스페이스
namespace CoreLoop.Flow
{
    /// <summary>
    /// 오전 탐험, 오후 장사, 결과 정산, 다음 날 전환과 안내 문구를 관리한다.
    /// </summary>
    [MovedFrom(false, sourceNamespace: "Flow", sourceAssembly: "Assembly-CSharp", sourceClassName: "DayCycleManager")]
    public class DayCycleManager : MonoBehaviour
    {
        [SerializeField, Min(1)] private int startingDay = 1;
        [SerializeField] private DayPhase startingPhase = DayPhase.MorningExplore;
        [SerializeField, Min(1f)] private float defaultHintDuration = 5f;

        private readonly HashSet<string> shownHintIds = new();
        private bool initialized;
        private string baseGuideText = string.Empty;
        private string temporaryGuideText = string.Empty;
        private float temporaryGuideExpireTime;

        public event Action StateChanged;

        public int CurrentDay { get; private set; }
        public DayPhase CurrentPhase { get; private set; }
        public string CurrentGuideText => HasTemporaryGuide ? temporaryGuideText : baseGuideText;
        public string LastSettlementSummary { get; private set; } = string.Empty;

        private bool HasTemporaryGuide =>
            !string.IsNullOrWhiteSpace(temporaryGuideText) && Time.unscaledTime < temporaryGuideExpireTime;

        /// <summary>
        /// 임시 안내 문구의 만료 시간을 감시하고 기본 안내로 되돌립니다.
        /// </summary>
        private void Update()
        {
            if (!HasTemporaryGuide && !string.IsNullOrWhiteSpace(temporaryGuideText))
            {
                temporaryGuideText = string.Empty;
                RaiseStateChanged();
            }
        }

        /// <summary>
        /// 하루 상태와 기본 안내 문구를 한 번만 초기화합니다.
        /// </summary>
        public void InitializeIfNeeded()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            CurrentDay = Mathf.Max(1, startingDay);
            CurrentPhase = startingPhase;
            baseGuideText = GetDefaultGuide(CurrentPhase);
            LastSettlementSummary = "오늘 하루를 시작하세요.";
            RaiseStateChanged();
        }

        /// <summary>
        /// 씬 진입 시 지역별 1회성 안내 문구를 노출합니다.
        /// </summary>
        public void HandleSceneEntered(string sceneName)
        {
            InitializeIfNeeded();

            string sceneGuide = GetSceneEntryGuide(sceneName);
            if (string.IsNullOrWhiteSpace(sceneGuide))
            {
                return;
            }

            ShowHintOnce($"scene_enter_{sceneName}", sceneGuide, 6f);
        }

        /// <summary>
        /// 허브 출발과 허브 복귀에 맞춰 하루 단계를 전환합니다.
        /// </summary>
        public void HandleSceneTravel(string currentSceneName, string targetSceneName, string hubSceneName)
        {
            InitializeIfNeeded();

            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
                return;
            }

            bool isTravelingToHub = targetSceneName == hubSceneName;
            bool isLeavingHub = currentSceneName == hubSceneName && !isTravelingToHub;
            bool isReturningToHub = currentSceneName != hubSceneName && isTravelingToHub;

            if (isLeavingHub && CurrentPhase == DayPhase.MorningExplore)
            {
                SetBaseGuide("오전 탐험 중입니다. 재료를 모으고 식당으로 돌아가세요.");
                return;
            }

            if (isReturningToHub && CurrentPhase == DayPhase.MorningExplore)
            {
                CurrentPhase = DayPhase.AfternoonService;
                SetBaseGuide("오후 장사 준비 시간입니다. 메뉴를 고르고 영업을 시작하세요.");
            }
        }

        /// <summary>
        /// 오전 탐험 단계를 건너뛰고 바로 오후 장사 준비로 넘깁니다.
        /// </summary>
        public void SkipExploration()
        {
            InitializeIfNeeded();

            if (CurrentPhase != DayPhase.MorningExplore)
            {
                return;
            }

            CurrentPhase = DayPhase.AfternoonService;
            SetBaseGuide("탐험을 건너뛰었습니다. 바로 장사를 준비할 수 있습니다.");
        }

        /// <summary>
        /// 장사 결과를 정산 단계로 넘기고 결과 문자열을 저장합니다.
        /// </summary>
        public void CompleteService(string settlementSummary)
        {
            InitializeIfNeeded();

            if (CurrentPhase != DayPhase.AfternoonService)
            {
                return;
            }

            LastSettlementSummary = string.IsNullOrWhiteSpace(settlementSummary)
                ? "오늘 장사 결과가 정산되었습니다."
                : settlementSummary;

            CurrentPhase = DayPhase.Settlement;
            SetBaseGuide("결과를 확인하고 다음 날로 넘어가세요.");
        }

        /// <summary>
        /// 장사 결과를 스킵 처리용 기본 문구로 정산합니다.
        /// </summary>
        public void SkipService()
        {
            CompleteService("오늘 영업 결과\n- 장사를 건너뛰었습니다.");
        }

        /// <summary>
        /// 정산이 끝난 뒤 날짜를 하루 넘기고 오전 탐험 단계로 되돌립니다.
        /// </summary>
        public void AdvanceToNextDay()
        {
            InitializeIfNeeded();

            if (CurrentPhase != DayPhase.Settlement)
            {
                return;
            }

            CurrentDay += 1;
            CurrentPhase = DayPhase.MorningExplore;
            LastSettlementSummary = "새로운 하루가 시작되었습니다.";
            SetBaseGuide("오전 탐험 준비 시간입니다. 오늘 갈 지역을 정하고 출발하세요.");
        }

        /// <summary>
        /// 일정 시간 동안만 보이는 임시 안내 문구를 설정합니다.
        /// </summary>
        public void ShowTemporaryGuide(string guideText, float duration = -1f)
        {
            InitializeIfNeeded();

            if (string.IsNullOrWhiteSpace(guideText))
            {
                return;
            }

            temporaryGuideText = guideText;
            temporaryGuideExpireTime = Time.unscaledTime + (duration > 0f ? duration : defaultHintDuration);
            RaiseStateChanged();
        }

        /// <summary>
        /// 같은 힌트 id 에 대해서는 한 번만 보이는 안내 문구를 설정합니다.
        /// </summary>
        public void ShowHintOnce(string hintId, string guideText, float duration = -1f)
        {
            if (string.IsNullOrWhiteSpace(hintId))
            {
                ShowTemporaryGuide(guideText, duration);
                return;
            }

            InitializeIfNeeded();
            if (!shownHintIds.Add(hintId))
            {
                return;
            }

            ShowTemporaryGuide(guideText, duration);
        }

        /// <summary>
        /// UI 에 표시할 하루 단계 이름을 반환합니다.
        /// </summary>
        public static string GetPhaseDisplayName(DayPhase phase)
        {
            return phase switch
            {
                DayPhase.MorningExplore => "오전 탐험",
                DayPhase.AfternoonService => "오후 장사",
                DayPhase.Settlement => "결과 정산",
                _ => "상태 없음"
            };
        }

        /// <summary>
        /// 현재 단계에서 기본으로 유지할 안내 문구를 갱신합니다.
        /// </summary>
        private void SetBaseGuide(string guideText)
        {
            baseGuideText = guideText;
            RaiseStateChanged();
        }

        /// <summary>
        /// 단계별 기본 안내 문구를 반환합니다.
        /// </summary>
        private static string GetDefaultGuide(DayPhase phase)
        {
            return phase switch
            {
                DayPhase.MorningExplore => "오전 탐험 준비 시간입니다. 오늘 갈 지역을 정하고 출발하세요.",
                DayPhase.AfternoonService => "오후 장사 준비 시간입니다. 메뉴를 고르고 영업을 시작하세요.",
                DayPhase.Settlement => "결과를 확인하고 다음 날로 넘어가세요.",
                _ => string.Empty
            };
        }

        /// <summary>
        /// 지역 진입 시 보여줄 안내 문구를 씬 이름 기준으로 반환합니다.
        /// </summary>
        private static string GetSceneEntryGuide(string sceneName)
        {
            return sceneName switch
            {
                "Hub" => "여기는 종구의 식당입니다. 메뉴판, 창고, 작업대를 돌며 오늘 일과를 준비하세요.",
                "Beach" => "바닷가는 입문 지역입니다. E로 채집하고 배를 통해 식당으로 돌아가면 오전 탐험이 끝납니다.",
                "DeepForest" => "깊은 숲은 갈림길과 늪지대가 있는 지역입니다. 욕심내기보다 귀환 타이밍을 보세요.",
                "AbandonedMine" => "폐광산은 어두운 지역입니다. 랜턴 준비 여부와 귀환 동선을 먼저 확인하세요.",
                "WindHill" => "바람 언덕은 강풍 주기에 맞춰 움직여야 안전합니다. 바람이 멎는 순간을 노리세요.",
                _ => string.Empty
            };
        }

        /// <summary>
        /// UI 와 외부 구독자에게 상태 갱신 이벤트를 전달합니다.
        /// </summary>
        private void RaiseStateChanged()
        {
            StateChanged?.Invoke();
        }
    }

    public enum DayPhase
    {
        MorningExplore,
        AfternoonService,
        Settlement
    }
}
