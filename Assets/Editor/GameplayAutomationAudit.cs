#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using CoreLoop.Core;
using Management.Economy;
using CoreLoop.Flow;
using Management.Tools;
using UI;
using Exploration.World;
using UnityEditor;
using UnityEngine;

// ProjectEditor 네임스페이스
namespace Editor
{
    /// <summary>
    /// 생성 씬 구조 감사에 더해 핵심 게임플레이 규칙이 크게 깨지지 않았는지 빠르게 점검합니다.
    /// 하루 루프, 팝업 일시정지, 포털 잠금 규칙처럼 회귀가 잦은 기준을 에디터와 배치 모드에서 같이 확인합니다.
    /// </summary>
    public static class GameplayAutomationAudit
    {
        private const string LightAuditMenuName = "Tools/Jonggu Restaurant/경량 자동 감사 실행";
        private static readonly BindingFlags InstanceFieldFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem(LightAuditMenuName, true, 2220)]
        private static bool ValidateLightAutomationAuditMenu()
        {
            return !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem(LightAuditMenuName, false, 2220)]
        public static void RunLightAutomationAuditMenu()
        {
            if (!ValidateLightAutomationAuditMenu())
            {
                Debug.LogWarning("경량 자동 감사 실행은 플레이 모드를 종료한 뒤 실행해 주세요.");
                return;
            }

            RunLightAutomationAudit();
            EditorUtility.DisplayDialog("종구의 식당", "경량 자동 감사를 완료했습니다.", "OK");
        }

        /// <summary>
        /// 배치 실행과 메뉴 실행이 같은 경로를 공유하도록 권장 자동 감사를 한 번에 실행합니다.
        /// </summary>
        public static void RunLightAutomationAudit()
        {
            List<string> issues = new();

            try
            {
                PrototypeSceneAudit.AuditGeneratedScenes();
            }
            catch (Exception exception)
            {
                issues.Add($"생성 씬 감사 실패: {exception.Message}");
            }

            issues.AddRange(ValidateDayCycleFlow());
            issues.AddRange(ValidatePopupPauseStateUtility());
            issues.AddRange(ValidateScenePortalRules());
            issues.AddRange(ValidateUnavailableSceneLoadGuide());

            if (issues.Count == 0)
            {
                Debug.Log("경량 자동 감사가 통과했습니다.");
                return;
            }

            foreach (string issue in issues)
            {
                Debug.LogError(issue);
            }

            throw new InvalidOperationException($"경량 자동 감사가 {issues.Count}개 문제로 실패했습니다.");
        }

        private static IEnumerable<string> ValidateDayCycleFlow()
        {
            List<string> issues = new();
            GameObject root = new("GameplayAutomationAudit_DayCycle");

            try
            {
                DayCycleManager dayCycle = root.AddComponent<DayCycleManager>();
                dayCycle.InitializeIfNeeded();

                AssertCondition(issues, dayCycle.CurrentDay == 1, "DayCycle 초기 일차가 1일차가 아닙니다.");
                AssertCondition(issues, dayCycle.CurrentPhase == DayPhase.MorningExplore, "DayCycle 초기 단계가 오전 탐험이 아닙니다.");

                dayCycle.SkipExploration();
                AssertCondition(issues, dayCycle.CurrentPhase == DayPhase.AfternoonService, "탐험 스킵 뒤 오후 장사 단계로 전환되지 않았습니다.");

                dayCycle.SkipService();
                AssertCondition(issues, dayCycle.CurrentPhase == DayPhase.Settlement, "장사 스킵 뒤 결과 정산 단계로 전환되지 않았습니다.");
                AssertCondition(issues, dayCycle.LastSettlementSummary.Contains("장사를 건너뛰었습니다.", StringComparison.Ordinal), "장사 스킵 정산 문구가 예상과 다릅니다.");

                dayCycle.AdvanceToNextDay();
                AssertCondition(issues, dayCycle.CurrentDay == 2, "다음 날 전환 뒤 일차가 2일차가 되지 않았습니다.");
                AssertCondition(issues, dayCycle.CurrentPhase == DayPhase.MorningExplore, "다음 날 전환 뒤 오전 탐험 단계로 복귀하지 않았습니다.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            return issues;
        }

        private static IEnumerable<string> ValidatePopupPauseStateUtility()
        {
            List<string> issues = new();

            PopupPauseStateUtility.Snapshot pausedSnapshot = PopupPauseStateUtility.Apply(
                shouldPause: true,
                isPauseApplied: false,
                previousTimeScale: 1f,
                currentTimeScale: 1.5f);
            AssertCondition(issues, pausedSnapshot.IsPauseApplied, "팝업 정지 적용 상태가 true가 되지 않았습니다.");
            AssertCondition(issues, Mathf.Approximately(pausedSnapshot.PreviousTimeScale, 1.5f), "팝업 정지 복구용 이전 배속을 현재 배속으로 기억하지 못했습니다.");
            AssertCondition(issues, Mathf.Approximately(pausedSnapshot.NextTimeScale, 0f), "팝업 정지 적용 시 다음 배속이 0이 아닙니다.");

            PopupPauseStateUtility.Snapshot repeatedPauseSnapshot = PopupPauseStateUtility.Apply(
                shouldPause: true,
                isPauseApplied: true,
                previousTimeScale: pausedSnapshot.PreviousTimeScale,
                currentTimeScale: 0f);
            AssertCondition(issues, Mathf.Approximately(repeatedPauseSnapshot.PreviousTimeScale, 1.5f), "이미 정지된 팝업의 복구 배속이 덮어써졌습니다.");
            AssertCondition(issues, Mathf.Approximately(repeatedPauseSnapshot.NextTimeScale, 0f), "이미 정지된 팝업의 다음 배속이 유지되지 않았습니다.");

            PopupPauseStateUtility.Snapshot restoredSnapshot = PopupPauseStateUtility.Restore(
                isPauseApplied: true,
                previousTimeScale: repeatedPauseSnapshot.PreviousTimeScale,
                currentTimeScale: repeatedPauseSnapshot.NextTimeScale);
            AssertCondition(issues, !restoredSnapshot.IsPauseApplied, "팝업 정지 해제 뒤 적용 상태가 false가 되지 않았습니다.");
            AssertCondition(issues, Mathf.Approximately(restoredSnapshot.NextTimeScale, 1.5f), "팝업 정지 해제 뒤 원래 배속으로 복구되지 않았습니다.");

            return issues;
        }

        private static IEnumerable<string> ValidateScenePortalRules()
        {
            List<string> issues = new();
            using TemporaryGameManagerScope gameManagerScope = new();
            ScenePortal portal = gameManagerScope.Root.AddComponent<ScenePortal>();

            gameManagerScope.DayCycle.SkipExploration();
            portal.Configure("DeepForest", string.Empty, "숲 이동", morningOnly: true);

            AssertCondition(
                issues,
                !portal.InteractionPrompt.StartsWith("[E]", StringComparison.Ordinal),
                "오후 장사 단계인데 오전 전용 포털이 열려 있습니다.");

            portal.Configure(gameManagerScope.GameManager.HubSceneName, string.Empty, "허브 복귀", morningOnly: true);
            AssertCondition(
                issues,
                portal.InteractionPrompt.StartsWith("[E]", StringComparison.Ordinal),
                "허브 복귀 포털은 오후 장사 단계에도 열려 있어야 합니다.");

            portal.Configure("AbandonedMine", string.Empty, "폐광산 이동", morningOnly: true, toolType: ToolType.Lantern);
            AssertCondition(
                issues,
                !portal.InteractionPrompt.StartsWith("[E]", StringComparison.Ordinal),
                "랜턴 없이 폐광산 포털이 열려 있습니다.");

            gameManagerScope.ToolManager.UnlockTool(ToolType.Lantern);
            gameManagerScope.DayCycle.SkipService();
            gameManagerScope.DayCycle.AdvanceToNextDay();
            portal.Configure("AbandonedMine", string.Empty, "폐광산 이동", morningOnly: true, toolType: ToolType.Lantern, reputation: 1);
            AssertCondition(
                issues,
                !portal.InteractionPrompt.StartsWith("[E]", StringComparison.Ordinal),
                "평판 조건이 모자란데 포털이 열려 있습니다.");

            gameManagerScope.EconomyManager.AddReputation(1);
            AssertCondition(
                issues,
                portal.InteractionPrompt.StartsWith("[E]", StringComparison.Ordinal),
                "도구와 평판 조건을 만족했는데 포털이 열리지 않았습니다.");

            return issues;
        }

        private static IEnumerable<string> ValidateUnavailableSceneLoadGuide()
        {
            List<string> issues = new();
            using TemporaryGameManagerScope gameManagerScope = new();
            const string missingSceneName = "GameplayAutomationAuditMissingScene";

            gameManagerScope.GameManager.LoadScene(missingSceneName);

            AssertCondition(
                issues,
                gameManagerScope.DayCycle.CurrentGuideText.Contains(missingSceneName, StringComparison.Ordinal),
                "없는 씬을 열려고 할 때 안내 문구에 씬 이름이 포함되지 않았습니다.");

            return issues;
        }

        private static void AssertCondition(ICollection<string> issues, bool condition, string message)
        {
            if (!condition)
            {
                issues.Add(message);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, InstanceFieldFlags);
            if (fieldInfo == null)
            {
                throw new InvalidOperationException($"{target.GetType().Name}에서 '{fieldName}' 필드를 찾지 못했습니다.");
            }

            fieldInfo.SetValue(target, value);
        }

        private static void SetGameManagerInstance(GameManager instance)
        {
            PropertyInfo instanceProperty = typeof(GameManager).GetProperty(nameof(GameManager.Instance), BindingFlags.Public | BindingFlags.Static);
            MethodInfo setter = instanceProperty != null ? instanceProperty.GetSetMethod(true) : null;
            if (setter == null)
            {
                throw new InvalidOperationException("GameManager.Instance setter를 찾지 못했습니다.");
            }

            setter.Invoke(null, new object[] { instance });
        }

        private sealed class TemporaryGameManagerScope : IDisposable
        {
            public TemporaryGameManagerScope()
            {
                Root = new GameObject("GameplayAutomationAudit_GameManager");
                GameManager = Root.AddComponent<GameManager>();
                DayCycle = Root.AddComponent<DayCycleManager>();
                EconomyManager = Root.AddComponent<EconomyManager>();
                ToolManager = Root.AddComponent<ToolManager>();

                DayCycle.InitializeIfNeeded();
                EconomyManager.InitializeIfNeeded();
                ToolManager.InitializeIfNeeded();

                SetPrivateField(GameManager, "dayCycleManager", DayCycle);
                SetPrivateField(GameManager, "economyManager", EconomyManager);
                SetPrivateField(GameManager, "toolManager", ToolManager);
                SetGameManagerInstance(GameManager);
            }

            public GameObject Root { get; }
            public GameManager GameManager { get; }
            public DayCycleManager DayCycle { get; }
            public EconomyManager EconomyManager { get; }
            public ToolManager ToolManager { get; }

            public void Dispose()
            {
                SetGameManagerInstance(null);
                UnityEngine.Object.DestroyImmediate(Root);
            }
        }
    }
}
#endif
