using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

namespace Code.Scripts.UI.Layout
{
    /// <summary>
    /// Canvas 관리 대상 이름 목록을 보관하고 명시적 에디터 레이아웃 binding을 적용합니다.
    /// </summary>
    public static class PrototypeUISceneLayoutCatalog
    {
        private static PrototypeUILayoutBindingSettings _cachedBindingSettings;
        private static bool _didTryLoadBindingSettings;

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
            "HUDPanelButtonGroup",
            "ActionDock",
            "ActionAccent",
            "ActionCaption",
            "StorageCard",
            "RecipeCard",
            "UpgradeCard",
            "StorageAccent",
            "RecipeAccent",
            "UpgradeAccent",
            "StorageCaption",
            "RecipeCaption",
            "UpgradeCaption",
            "RecipePanelButton",
            "UpgradePanelButton",
            "MaterialPanelButton",
            "OpenRestaurantButton",
            "CloseRestaurantButton",
            "ResourcePanel",
            "ResourceAmountText"
        };

        private static readonly HashSet<string> PopupCanvasObjectNames = BuildPopupCanvasObjectNames();
        private static readonly HashSet<string> RuntimeOnlyCanvasObjectNames = new(StringComparer.Ordinal)
        {
            PrototypeUIObjectNames.RefrigeratorDragGhost,
            "StorageCard",
            "RecipeCard",
            "UpgradeCard",
            "StorageAccent",
            "RecipeAccent",
            "UpgradeAccent",
            "StorageCaption",
            "RecipeCaption",
            "UpgradeCaption",
            "OpenRestaurantButton",
            "CloseRestaurantButton"
        };

        private static readonly string[] RuntimeOnlyCanvasObjectPrefixes =
        {
            "PopupLeftItemBox",
            "PopupLeftItemIcon",
            "PopupLeftItemText",
            "PopupRightItemBox",
            "PopupRightItemIcon",
            "PopupRightItemText"
        };

        /// <summary>
        /// 활성화된 editor binding 레이아웃이 있으면 적용하고, 없으면 코드 기본값을 반환합니다.
        /// </summary>
        public static PrototypeUIRect ResolveLayout(string objectName, PrototypeUIRect fallback)
        {
            if (TryGetBindingEntry(objectName, out PrototypeUILayoutBindingEntry binding)
                && binding.TryGetLayout(out PrototypeUIRect layout))
            {
                return layout;
            }

            return fallback;
        }

        /// <summary>
        /// 명시적 editor binding에 저장된 Image 표시 값을 적용합니다.
        /// </summary>
        public static bool TryApplyImageOverride(Image image, string objectName)
        {
            return TryGetBindingEntry(objectName, out PrototypeUILayoutBindingEntry binding)
                   && binding.TryApplyImage(image);
        }

        /// <summary>
        /// 명시적 editor binding에 저장된 TMP 표시 값을 적용합니다.
        /// </summary>
        public static bool TryApplyTextOverride(TextMeshProUGUI text, string objectName)
        {
            return TryGetBindingEntry(objectName, out PrototypeUILayoutBindingEntry binding)
                   && binding.TryApplyText(text);
        }

        /// <summary>
        /// 명시적 editor binding에 저장된 Button 표시 값을 적용합니다.
        /// </summary>
        public static bool TryApplyButtonOverride(Button button, string objectName)
        {
            return TryGetBindingEntry(objectName, out PrototypeUILayoutBindingEntry binding)
                   && binding.TryApplyButton(button);
        }

        public static bool HasLayoutOverride(string objectName)
        {
            return TryGetBindingEntry(objectName, out PrototypeUILayoutBindingEntry binding) && binding.ApplyRect;
        }

        public static bool HasImageOverride(string objectName)
        {
            return TryGetBindingEntry(objectName, out PrototypeUILayoutBindingEntry binding) && binding.ApplyImage;
        }

        public static bool HasTextOverride(string objectName)
        {
            return TryGetBindingEntry(objectName, out PrototypeUILayoutBindingEntry binding) && binding.ApplyText;
        }

        public static bool HasButtonOverride(string objectName)
        {
            return TryGetBindingEntry(objectName, out PrototypeUILayoutBindingEntry binding) && binding.ApplyButton;
        }

        public static bool HasHierarchyOverride(string objectName)
        {
            if (IsRuntimeOnlyObjectName(objectName))
            {
                return false;
            }

            return TryGetBindingEntry(objectName, out PrototypeUILayoutBindingEntry binding) && binding.ApplyHierarchy;
        }

        public static bool HasAnyOverride(string objectName)
        {
            return TryGetBindingEntry(objectName, out PrototypeUILayoutBindingEntry binding)
                   && (binding.ApplyHierarchy || binding.ApplyRect || binding.ApplyImage || binding.ApplyText || binding.ApplyButton);
        }

        public static bool TryGetSceneObjectPath(string objectName, out string sceneObjectPath)
        {
            if (IsRuntimeOnlyObjectName(objectName))
            {
                sceneObjectPath = string.Empty;
                return false;
            }

            if (TryGetBindingEntry(objectName, out PrototypeUILayoutBindingEntry binding)
                && !string.IsNullOrWhiteSpace(binding.SceneObjectPath))
            {
                sceneObjectPath = binding.SceneObjectPath;
                return true;
            }

            sceneObjectPath = string.Empty;
            return false;
        }

        public static bool TryGetHierarchyOverride(string objectName, out string parentName, out int siblingIndex)
        {
            return TryGetHierarchyOverride(objectName, out parentName, out siblingIndex, out _);
        }

        public static bool TryGetHierarchyOverride(
            string objectName,
            out string parentScenePath,
            out int siblingIndex,
            out bool initialActiveSelf)
        {
            if (IsRuntimeOnlyObjectName(objectName))
            {
                parentScenePath = string.Empty;
                siblingIndex = 0;
                initialActiveSelf = false;
                return false;
            }

            if (TryGetBindingEntry(objectName, out PrototypeUILayoutBindingEntry binding)
                && binding.TryGetHierarchyOverride(out parentScenePath, out siblingIndex, out initialActiveSelf))
            {
                return true;
            }

            parentScenePath = string.Empty;
            siblingIndex = 0;
            initialActiveSelf = false;
            return false;
        }

        public static bool TryGetHierarchyInitialActiveSelf(string objectName, out bool initialActiveSelf)
        {
            if (TryGetHierarchyOverride(objectName, out _, out _, out initialActiveSelf))
            {
                return true;
            }

            initialActiveSelf = false;
            return false;
        }

        public static bool IsRuntimeOnlyObjectName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return false;
            }

            if (RuntimeOnlyCanvasObjectNames.Contains(objectName))
            {
                return true;
            }

            for (int index = 0; index < RuntimeOnlyCanvasObjectPrefixes.Length; index++)
            {
                if (objectName.StartsWith(RuntimeOnlyCanvasObjectPrefixes[index], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsObjectRemoved(string objectName) => false;

#if UNITY_EDITOR
        public static void ReloadBindingSettingsForEditor()
        {
            _cachedBindingSettings = null;
            _didTryLoadBindingSettings = false;
        }
#endif

        /// <summary>
        /// HUD 그룹 재배치에 쓰는 관리 대상 이름 목록입니다.
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

        public static HashSet<string> GetManagedCanvasObjectNames(
            bool isHubScene,
            IReadOnlyDictionary<string, string> sceneNameOverrides)
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

        public static string ResolveObjectName(string canonicalName) => canonicalName;

        private static IEnumerable<string> EnumerateHudCanvasObjectNames(
            bool isHubScene,
            IReadOnlyDictionary<string, string> sceneNameOverrides)
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
                "PopupLeftBody",
                "PopupRightBody",
                PrototypeUIObjectNames.PopupTitle,
                PrototypeUIObjectNames.PopupLeftCaption,
                PrototypeUIObjectNames.PopupRightCaption,
                "PopupCloseButton",
                PrototypeUIObjectNames.RefrigeratorStorage,
                PrototypeUIObjectNames.RefrigeratorInfoPanel,
                PrototypeUIObjectNames.RefrigeratorInfoIcon,
                PrototypeUIObjectNames.RefrigeratorItemNameText,
                PrototypeUIObjectNames.RefrigeratorItemDescriptionText,
                PrototypeUIObjectNames.RefrigeratorSelectedSlot,
                PrototypeUIObjectNames.RefrigeratorRemoveZone,
                PrototypeUIObjectNames.RefrigeratorRemoveIcon,
                PrototypeUIObjectNames.RefrigeratorRemoveText,
                PrototypeUIObjectNames.RefrigeratorDragGhost
            };

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

        private static string ResolveManagedObjectName(
            string canonicalName,
            IReadOnlyDictionary<string, string> sceneNameOverrides)
        {
            if (sceneNameOverrides != null
                && sceneNameOverrides.TryGetValue(canonicalName, out string sceneName)
                && !string.IsNullOrWhiteSpace(sceneName))
            {
                return sceneName;
            }

            return ResolveObjectName(canonicalName);
        }

        private static bool TryGetBindingEntry(string objectName, out PrototypeUILayoutBindingEntry binding)
        {
            binding = null;
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return false;
            }

            PrototypeUILayoutBindingSettings settings = GetBindingSettings();
            return settings != null && settings.TryGetEntry(objectName, out binding);
        }

        private static PrototypeUILayoutBindingSettings GetBindingSettings()
        {
            if (!_didTryLoadBindingSettings)
            {
                _cachedBindingSettings = UnityEngine.Resources.Load<PrototypeUILayoutBindingSettings>(
                    PrototypeUILayoutBindingSettings.ResourcesLoadPath);
                _didTryLoadBindingSettings = true;
            }

            return _cachedBindingSettings;
        }
    }
}
