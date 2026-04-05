#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// ProjectEditor.UI 네임스페이스
namespace Editor.UI
{
    /// <summary>
    /// 지원하는 Canvas 씬을 저장하면 공용 UI 오버라이드 자산을 다시 동기화하고,
    /// 같은 관리 대상 Canvas 변경을 다른 지원 씬 Canvas 파일에도 자동으로 반영합니다.
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
