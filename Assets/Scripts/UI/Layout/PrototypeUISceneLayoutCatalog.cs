using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
        private static readonly string[] SharedHudManagedObjectNames =
        {
            "HUDRoot",
            "PopupRoot",
            "HUDStatusGroup",
            "HUDActionGroup",
            "HUDBottomGroup",
            "HUDOverlayGroup",
            "PopupShellGroup",
            "PopupFrameHeader",
            "InteractionPromptBackdrop",
            "InteractionPromptText",
            "GuideBackdrop",
            "GuideText",
            "GuideHelpButton",
            "ResultBackdrop",
            "RestaurantResultText"
        };

        private static readonly string[] ExploreHudManagedObjectNames =
        {
            "TopLeftPanel",
            "GoldText"
        };

        private static readonly string[] HubHudManagedObjectNames =
        {
            "ResourcePanel",
            "ResourceAmountText",
            "HUDPanelButtonGroup",
            "ActionDock",
            "ActionAccent",
            "ActionCaption",
            "RecipePanelButton",
            "UpgradePanelButton",
            "MaterialPanelButton"
        };

        private static readonly HashSet<string> PopupCanvasObjectNames = BuildPopupCanvasObjectNames();

#if !UNITY_EDITOR
        private static PrototypeUISceneLayoutSettings _cachedSettings;
#endif

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

        /// <summary>
        /// HUD 그룹 재배치와 에디터 캡처가 함께 쓰는 HUD 관리 이름 목록입니다.
        /// 이름 오버라이드가 있으면 현재 씬 이름으로 풀어 반환합니다.
        /// </summary>
        public static IEnumerable<string> EnumerateHudCanvasObjectNames(bool isHubScene)
        {
            return EnumerateHudCanvasObjectNames(isHubScene, null);
        }

        /// <summary>
        /// 허브 팝업 루트 아래에서 관리하는 오브젝트 이름 목록입니다.
        /// </summary>
        public static IEnumerable<string> EnumeratePopupCanvasObjectNames()
        {
            return PopupCanvasObjectNames;
        }

        public static bool IsPopupCanvasObjectName(string objectName)
        {
            return !string.IsNullOrWhiteSpace(objectName)
                && PopupCanvasObjectNames.Contains(objectName);
        }

        public static HashSet<string> GetManagedCanvasObjectNames(bool isHubScene)
        {
            return GetManagedCanvasObjectNames(isHubScene, null);
        }

        public static HashSet<string> GetManagedCanvasObjectNames(bool isHubScene, IReadOnlyDictionary<string, string> sceneNameOverrides)
        {
            HashSet<string> objectNames = new(StringComparer.Ordinal);
            AddObjectNames(objectNames, EnumerateHudCanvasObjectNames(isHubScene, sceneNameOverrides));

            if (!isHubScene)
            {
                return objectNames;
            }

            objectNames.Add("InventoryText");
            AddObjectNames(objectNames, PopupCanvasObjectNames);
            return objectNames;
        }

        private static IEnumerable<string> EnumerateHudCanvasObjectNames(bool isHubScene, IReadOnlyDictionary<string, string> sceneNameOverrides)
        {
            foreach (string objectName in SharedHudManagedObjectNames)
            {
                yield return ResolveManagedObjectName(objectName, sceneNameOverrides);
            }

            string[] sceneSpecificObjectNames = isHubScene ? HubHudManagedObjectNames : ExploreHudManagedObjectNames;
            foreach (string objectName in sceneSpecificObjectNames)
            {
                yield return ResolveManagedObjectName(objectName, sceneNameOverrides);
            }
        }

        private static HashSet<string> BuildPopupCanvasObjectNames()
        {
            HashSet<string> objectNames = new(StringComparer.Ordinal)
            {
                "PopupOverlay",
                "PopupFrame",
                "PopupFrameLeft",
                "PopupFrameRight",
                PrototypeUIObjectNames.PopupTitle,
                "PopupCloseButton",
                "PopupLeftBody",
                "PopupRightBody",
                PrototypeUIObjectNames.PopupLeftCaption,
                PrototypeUIObjectNames.PopupRightCaption,
                "StorageText",
                "SelectedRecipeText",
                "UpgradeText",
                PrototypeUIObjectNames.RefrigeratorStorage,
                PrototypeUIObjectNames.RefrigeratorSelectedSlot,
                PrototypeUIObjectNames.RefrigeratorRemoveZone,
                PrototypeUIObjectNames.RefrigeratorRemoveIcon,
                PrototypeUIObjectNames.RefrigeratorRemoveText,
                PrototypeUIObjectNames.RefrigeratorDragGhost
            };

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

            for (int index = 0; index < PrototypeUILayout.RefrigeratorSlotCount; index++)
            {
                int displayIndex = index + 1;
                objectNames.Add($"{PrototypeUIObjectNames.RefrigeratorSlotPrefix}{displayIndex:00}");
                objectNames.Add($"{PrototypeUIObjectNames.RefrigeratorSlotIconPrefix}{displayIndex:00}");
                objectNames.Add($"{PrototypeUIObjectNames.RefrigeratorSlotAmountPrefix}{displayIndex:00}");
            }

            return objectNames;
        }

        private static void AddObjectNames(ISet<string> target, IEnumerable<string> objectNames)
        {
            if (target == null || objectNames == null)
            {
                return;
            }

            foreach (string objectName in objectNames)
            {
                if (!string.IsNullOrWhiteSpace(objectName))
                {
                    target.Add(objectName);
                }
            }
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
            settings = AssetDatabase.LoadAssetAtPath<PrototypeUISceneLayoutSettings>(PrototypeUISceneLayoutSettings.AssetPath);
            if (settings == null)
            {
                settings = Resources.Load<PrototypeUISceneLayoutSettings>(PrototypeUISceneLayoutSettings.ResourcesLoadPath);
            }
            return settings != null;
#else
            if (_cachedSettings != null)
            {
                settings = _cachedSettings;
                return true;
            }

            settings = Resources.Load<PrototypeUISceneLayoutSettings>(PrototypeUISceneLayoutSettings.ResourcesLoadPath);
            _cachedSettings = settings;
            return settings != null;
#endif
        }
    }
}
