using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
// UI.Layout 네임스페이스
namespace UI.Layout
{
    /// <summary>
    /// Canvas UI 레이아웃과 표시 값을 공용 자산으로 읽고 쓰는 도우미입니다.
    /// 런타임 read API와 GetManagedCanvasObjectNames 정본을 보관합니다.
    /// 에디터 sync/overlay/capture 기능은 .Editor.cs / .Editor.Capture.cs 파셜에 분리되어 있습니다.
    /// </summary>
    public static partial class PrototypeUISceneLayoutCatalog
    {
        // 런타임은 이 파일을 읽기 API로만 사용하고, 에디터 저장/캡처는 sibling partial이 맡습니다.
        private static PrototypeUISceneLayoutSettings _cachedSettings;

        /// <summary>
        /// 저장된 레이아웃이 있으면 그 값을, 없으면 전달한 기본값을 반환합니다.
        /// </summary>
        public static PrototypeUIRect ResolveLayout(string objectName, PrototypeUIRect fallback)
        {
            if (TryGetSettings(out PrototypeUISceneLayoutSettings settings)
                && settings.TryGetLayout(objectName, out PrototypeUIRect layout))
            {
                return layout;
            }

            return fallback;
        }

        /// <summary>
        /// 저장된 Canvas Image 표시 값이 있으면 현재 Image에 다시 적용합니다.
        /// </summary>
        public static bool TryApplyImageOverride(Image image, string objectName)
        {
            if (image == null || string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            if (TryGetSettings(out PrototypeUISceneLayoutSettings settings)
                && settings.TryGetImageEntry(objectName, out PrototypeUISceneImageEntry imageEntry))
            {
                imageEntry.ApplyTo(image);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 저장된 TMP 텍스트 표시 값이 있으면 현재 TextMeshProUGUI에 다시 적용합니다.
        /// </summary>
        public static bool TryApplyTextOverride(TextMeshProUGUI text, string objectName)
        {
            if (text == null || string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            if (TryGetSettings(out PrototypeUISceneLayoutSettings settings)
                && settings.TryGetTextEntry(objectName, out PrototypeUISceneTextEntry textEntry))
            {
                textEntry.ApplyTo(text);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 저장된 Button 표시 값이 있으면 현재 버튼에 다시 적용합니다.
        /// </summary>
        public static bool TryApplyButtonOverride(Button button, string objectName)
        {
            if (button == null || string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            if (TryGetSettings(out PrototypeUISceneLayoutSettings settings)
                && settings.TryGetButtonEntry(objectName, out PrototypeUISceneButtonEntry buttonEntry))
            {
                buttonEntry.ApplyTo(button);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 현재 씬 기준 이름 오버라이드가 있으면 그 값을, 없으면 기준 이름을 그대로 반환합니다.
        /// </summary>
        public static bool TryGetHierarchyOverride(string objectName, out string parentName, out int siblingIndex)
        {
            parentName = null;
            siblingIndex = 0;

            if (string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            if (TryGetSettings(out PrototypeUISceneLayoutSettings settings)
                && settings.TryGetHierarchyEntry(objectName, out PrototypeUISceneHierarchyEntry hierarchyEntry))
            {
                parentName = hierarchyEntry.ParentName;
                siblingIndex = hierarchyEntry.SiblingIndex;
                return !string.IsNullOrWhiteSpace(parentName);
            }

            return false;
        }

        public static bool IsObjectRemoved(string objectName)
        {
#if UNITY_EDITOR
            if (_ignoreRemovedObjectChecksDepth > 0)
            {
                return false;
            }
#endif
            return !string.IsNullOrWhiteSpace(objectName)
                && TryGetSettings(out PrototypeUISceneLayoutSettings settings)
                && settings.IsObjectRemoved(objectName);
        }

        public static HashSet<string> GetManagedCanvasObjectNames(bool isHubScene)
        {
            return GetManagedCanvasObjectNames(isHubScene, null);
        }

        public static HashSet<string> GetManagedCanvasObjectNames(bool isHubScene, IReadOnlyDictionary<string, string> sceneNameOverrides)
        {
            HashSet<string> objectNames = new(System.StringComparer.Ordinal)
            {
                "HUDRoot",
                "PopupRoot",
                "HUDStatusGroup",
                ResolveManagedObjectName("HUDActionGroup", sceneNameOverrides),
                "HUDBottomGroup",
                "HUDOverlayGroup",
                "PopupShellGroup",
                "PopupFrameHeader",
                "TopLeftPanel",
                "GoldText",
                "InteractionPromptBackdrop",
                "InteractionPromptText",
                "GuideBackdrop",
                "GuideText",
                "GuideHelpButton",
                "ResultBackdrop",
                "RestaurantResultText",
            };

            if (!isHubScene)
            {
                return objectNames;
            }

            objectNames.Add(ResolveManagedObjectName("HUDPanelButtonGroup", sceneNameOverrides));
            objectNames.Add("PopupOverlay");
            objectNames.Add("PopupFrame");
            objectNames.Add("PopupFrameLeft");
            objectNames.Add("PopupFrameRight");
            objectNames.Add("PopupLeftBody");
            objectNames.Add("PopupRightBody");
            objectNames.Add(PrototypeUIObjectNames.PopupTitle);
            objectNames.Add(PrototypeUIObjectNames.PopupLeftCaption);
            objectNames.Add(PrototypeUIObjectNames.PopupRightCaption);
            objectNames.Add("PopupCloseButton");
            objectNames.Add("InventoryText");
            objectNames.Add("StorageText");
            objectNames.Add("SelectedRecipeText");
            objectNames.Add("UpgradeText");
            objectNames.Add("ActionDock");
            objectNames.Add("ActionAccent");
            objectNames.Add("ActionCaption");
            objectNames.Add("RecipePanelButton");
            objectNames.Add("UpgradePanelButton");
            objectNames.Add("MaterialPanelButton");

            for (int index = 0; index < PrototypeUILayout.HubPopupBodyItemBoxCount; index++)
            {
                int displayIndex = index + 1;
                objectNames.Add($"PopupLeftItemBox{displayIndex:00}");
                objectNames.Add($"PopupLeftItemIcon{displayIndex:00}");
                objectNames.Add($"PopupLeftItemText{displayIndex:00}");
                objectNames.Add($"PopupRightItemBox{displayIndex:00}");
                objectNames.Add($"PopupRightItemIcon{displayIndex:00}");
                objectNames.Add($"PopupRightItemText{displayIndex:00}");
            }

            return objectNames;
        }

        private static string ResolveManagedObjectName(string canonicalName, IReadOnlyDictionary<string, string> sceneNameOverrides)
        {
            if (sceneNameOverrides != null
                && sceneNameOverrides.TryGetValue(canonicalName, out string sceneName)
                && !string.IsNullOrWhiteSpace(sceneName))
            {
                return sceneName;
            }

            return ResolveObjectName(canonicalName);
        }

        public static string ResolveObjectName(string canonicalName)
        {
            if (string.IsNullOrWhiteSpace(canonicalName))
            {
                return canonicalName;
            }

            if (TryGetSettings(out PrototypeUISceneLayoutSettings settings)
                && settings.TryGetSceneName(canonicalName, out string sceneName))
            {
                return sceneName;
            }

            return canonicalName;
        }

        private static bool TryGetSettings(out PrototypeUISceneLayoutSettings settings)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                settings = AssetDatabase.LoadAssetAtPath<PrototypeUISceneLayoutSettings>(PrototypeUISceneLayoutSettings.AssetPath);
                if (settings == null)
                {
                    settings = Resources.Load<PrototypeUISceneLayoutSettings>(PrototypeUISceneLayoutSettings.ResourcesLoadPath);
                }

                _cachedSettings = settings;
                return settings != null;
            }
#endif

            if (_cachedSettings != null)
            {
                settings = _cachedSettings;
                return true;
            }

            settings = Resources.Load<PrototypeUISceneLayoutSettings>(PrototypeUISceneLayoutSettings.ResourcesLoadPath);
            _cachedSettings = settings;
            return settings != null;
        }
    }
}
