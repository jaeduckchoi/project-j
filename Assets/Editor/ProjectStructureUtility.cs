#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Editor
{
    /// <summary>
    /// 기능 기반 기본 폴더 구조를 한 번에 맞춰 이후 작업 경로를 예측 가능하게 유지한다.
    /// </summary>
    public static class ProjectStructureUtility
    {
        private static readonly string[] RequiredFolders =
        {
            "Assets/Scripts/CoreLoop",
            "Assets/Scripts/CoreLoop/Core",
            "Assets/Scripts/CoreLoop/Flow",
            "Assets/Scripts/Exploration",
            "Assets/Scripts/Exploration/Camera",
            "Assets/Scripts/Exploration/Gathering",
            "Assets/Scripts/Exploration/Interaction",
            "Assets/Scripts/Exploration/Player",
            "Assets/Scripts/Exploration/World",
            "Assets/Scripts/Management",
            "Assets/Scripts/Management/Economy",
            "Assets/Scripts/Management/Inventory",
            "Assets/Scripts/Management/Storage",
            "Assets/Scripts/Management/Tools",
            "Assets/Scripts/Management/Upgrade",
            "Assets/Scripts/Restaurant",
            "Assets/Scripts/Shared",
            "Assets/Scripts/Shared/Data",
            "Assets/Scripts/UI"
        };

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0051", Justification = "숨겨진 유지보수 경로로 보존합니다.")]
        private static void EnsureBaseProjectFoldersForMaintenance()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("프로젝트 구조 정리는 플레이 모드가 아닐 때만 실행할 수 있습니다.");
                return;
            }

            EnsureBaseProjectFolders();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("종구네 식당", "기능 기반 기본 폴더 구조를 맞췄습니다.", "OK");
        }

        /// <summary>
        /// 빌더와 내부 유지보수 경로가 같은 기준 구조를 재사용하도록 공용 폴더를 보장한다.
        /// </summary>
        public static void EnsureBaseProjectFolders()
        {
            foreach (string folderPath in RequiredFolders)
            {
                EnsureFolderRecursive(folderPath);
            }
        }

        private static void EnsureFolderRecursive(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] segments = folderPath.Split('/');
            string currentPath = segments[0];

            for (int index = 1; index < segments.Length; index++)
            {
                string nextPath = currentPath + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, segments[index]);
                }

                currentPath = nextPath;
            }
        }
    }
}
#endif
