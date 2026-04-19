using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// Flow 네임스페이스
namespace Code.Scripts.CoreLoop.Flow
{
    /// <summary>
    /// 날짜/단계 루틴 없이 현재 플레이 상황에 맞는 안내 문구와 1회성 힌트만 관리한다.
    /// </summary>
    [MovedFrom(false, sourceNamespace: "Flow", sourceAssembly: "Assembly-CSharp", sourceClassName: "DayCycleManager")]
    public class DayCycleManager : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float defaultHintDuration = 5f;

        private readonly HashSet<string> shownHintIds = new();
        private bool initialized;
        private string baseGuideText = string.Empty;
        private string temporaryGuideText = string.Empty;
        private float temporaryGuideExpireTime;

        public event Action StateChanged;

        public string CurrentGuideText => HasTemporaryGuide ? temporaryGuideText : baseGuideText;

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
        /// 기본 안내 문구를 한 번만 초기화합니다.
        /// </summary>
        public void InitializeIfNeeded()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            baseGuideText = "식당과 탐험 지역을 자유롭게 오가며 재료를 모으고 메뉴를 판매하세요.";
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

            SetBaseGuide(sceneGuide);
            ShowHintOnce($"scene_enter_{sceneName}", sceneGuide, 6f);
        }

        /// <summary>
        /// 씬 이동에 맞춰 단계 전환 없이 현재 위치 안내만 갱신합니다.
        /// </summary>
        public void HandleSceneTravel(string currentSceneName, string targetSceneName, string hubSceneName)
        {
            InitializeIfNeeded();

            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
                return;
            }

            bool isTravelingToHub = string.Equals(targetSceneName, hubSceneName, StringComparison.Ordinal);
            bool isLeavingHub = string.Equals(currentSceneName, hubSceneName, StringComparison.Ordinal) && !isTravelingToHub;
            bool isReturningToHub = !string.Equals(currentSceneName, hubSceneName, StringComparison.Ordinal) && isTravelingToHub;

            if (isLeavingHub)
            {
                SetBaseGuide("탐험 지역으로 이동합니다. 재료를 모은 뒤 언제든 식당으로 돌아올 수 있습니다.");
                return;
            }

            if (isReturningToHub)
            {
                SetBaseGuide("식당으로 돌아왔습니다. 메뉴 선택, 창고, 영업은 Hub에서 진행할 수 있습니다.");
            }
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
        /// 현재 위치의 기본 안내 문구를 갱신합니다.
        /// </summary>
        private void SetBaseGuide(string guideText)
        {
            baseGuideText = string.IsNullOrWhiteSpace(guideText) ? string.Empty : guideText;
            RaiseStateChanged();
        }

        /// <summary>
        /// 지역 진입 시 보여줄 안내 문구를 씬 이름 기준으로 반환합니다.
        /// </summary>
        private static string GetSceneEntryGuide(string sceneName)
        {
            return sceneName switch
            {
                "Hub" => "여기는 종구의 식당입니다. 메뉴판, 창고, 작업대를 돌며 준비하고 바로 영업할 수 있습니다.",
                "Beach" => "바닷가는 첫 탐험 지역입니다. 식당 복귀 지점과 배, 등대를 확인하세요.",
                "DeepForest" => "깊은 숲은 갈림길과 늪지대가 있는 지역입니다. 귀환 동선을 보며 재료를 모으세요.",
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
}
