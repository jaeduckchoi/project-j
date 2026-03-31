#if UNITY_EDITOR
using ProjectEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// ProjectEditor.UI 네임스페이스
namespace ProjectEditor.UI
{
    /// <summary>
    /// 지원하는 Canvas 씬을 저장하면 공용 UI 오버라이드 자산을 자동으로 다시 동기화합니다.
    /// Hub는 공용 기준과 탐험 HUD 전체를 갱신하고, 탐험 씬은 현재 씬 값만 오버레이합니다.
    /// </summary>
    [InitializeOnLoad]
    internal static class PrototypeUICanvasAutoSync
    {
        private static bool _isAutoSyncInProgress;

        static PrototypeUICanvasAutoSync()
        {
            EditorSceneManager.sceneSaved += HandleSceneSaved;
        }

        private static void HandleSceneSaved(Scene scene)
        {
            if (_isAutoSyncInProgress
                || EditorApplication.isPlaying
                || EditorApplication.isPlayingOrWillChangePlaymode
                || EditorApplication.isCompiling
                || !JongguMinimalPrototypeBuilder.ShouldAutoSyncCanvasOnSceneSave(scene))
            {
                return;
            }

            try
            {
                _isAutoSyncInProgress = true;

                if (JongguMinimalPrototypeBuilder.TryAutoSyncCanvasOnSceneSaved(scene, out string message))
                {
                    UnityEngine.Debug.Log(message);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(message))
                {
                    UnityEngine.Debug.LogWarning(message);
                }
            }
            finally
            {
                _isAutoSyncInProgress = false;
            }
        }
    }
}
#endif
