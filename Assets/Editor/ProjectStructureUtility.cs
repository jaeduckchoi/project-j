#if UNITY_EDITOR
using UnityEditor;

namespace Editor
{
    /// <summary>
    /// 기능 기반 기본 폴더 구조를 유지하기 위한 에디터 유틸리티입니다.
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

        /// <summary>
        /// 빌더와 이후 유지보수 경로가 같은 기준 구조를 재사용하도록 공용 폴더를 보장합니다.
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
