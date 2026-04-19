#if UNITY_EDITOR
using Shared;
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
            ProjectAssetPaths.CodeRoot,
            ProjectAssetPaths.ScriptsRoot,
            ProjectAssetPaths.ScriptsRoot + "/CoreLoop",
            ProjectAssetPaths.ScriptsRoot + "/CoreLoop/Core",
            ProjectAssetPaths.ScriptsRoot + "/CoreLoop/Flow",
            ProjectAssetPaths.ScriptsRoot + "/Exploration",
            ProjectAssetPaths.ScriptsRoot + "/Exploration/Camera",
            ProjectAssetPaths.ScriptsRoot + "/Exploration/Gathering",
            ProjectAssetPaths.ScriptsRoot + "/Exploration/Interaction",
            ProjectAssetPaths.ScriptsRoot + "/Exploration/Player",
            ProjectAssetPaths.ScriptsRoot + "/Exploration/World",
            ProjectAssetPaths.ScriptsRoot + "/Management",
            ProjectAssetPaths.ScriptsRoot + "/Management/Economy",
            ProjectAssetPaths.ScriptsRoot + "/Management/Inventory",
            ProjectAssetPaths.ScriptsRoot + "/Management/Storage",
            ProjectAssetPaths.ScriptsRoot + "/Management/Tools",
            ProjectAssetPaths.ScriptsRoot + "/Management/Upgrade",
            ProjectAssetPaths.ScriptsRoot + "/Restaurant",
            ProjectAssetPaths.ScriptsRoot + "/Shared",
            ProjectAssetPaths.ScriptsRoot + "/Shared/Data",
            ProjectAssetPaths.ScriptsRoot + "/UI",
            ProjectAssetPaths.EditorRoot,
            ProjectAssetPaths.TestsRoot,
            ProjectAssetPaths.EditModeTestsRoot,
            ProjectAssetPaths.PlayModeTestsRoot,
            ProjectAssetPaths.DataRoot,
            ProjectAssetPaths.GameDataSourceRoot,
            ProjectAssetPaths.LevelRoot,
            ProjectAssetPaths.ScenesRoot,
            ProjectAssetPaths.ResourcesRoot,
            ProjectAssetPaths.GeneratedResourcesRoot,
            ProjectAssetPaths.SettingsRoot
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
