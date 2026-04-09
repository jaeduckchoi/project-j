#if UNITY_EDITOR
using System;
using Exploration.World;
using TMPro;
using UI;
using UI.Controllers;
using UI.Layout;
using UI.Style;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

// ProjectEditor 네임스페이스
namespace Editor
{
    public static partial class JongguMinimalPrototypeBuilder
    {
        private static void CreateUiCanvas(bool isHubScene)
        {
            GameObject canvasObject = new("Canvas");
            ApplySceneTransformOverride(canvasObject.transform, canvasObject.name, Vector3.zero, Quaternion.identity, Vector3.one, useLocalSpace: false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            ApplySceneComponentOverride(canvas, canvasObject.name);

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            ApplySceneComponentOverride(scaler, canvasObject.name);

            GraphicRaycaster raycaster = canvasObject.AddComponent<GraphicRaycaster>();
            ApplySceneComponentOverride(raycaster, canvasObject.name);

            EnsureUiEventSystem();

            Color chromeDark = new(0.96f, 0.97f, 0.99f, 1f);
            Color chromeSurface = new(0.98f, 0.98f, 0.99f, 1f);
            Color chromeGlass = new(0.93f, 0.95f, 0.98f, 1f);
            Color chromeOverlay = new(0f, 0f, 0f, 0.52f);
            Color chromeAmber = new(0.94f, 0.74f, 0.10f, 1f);
            Color chromeText = new(0.23f, 0.27f, 0.34f, 1f);
            Color chromeDock = new(0.22f, 0.60f, 0.87f, 1f);
            string hudActionGroupName = PrototypeUISceneLayoutCatalog.ResolveObjectName("HUDActionGroup");
            string hudPanelButtonGroupName = PrototypeUISceneLayoutCatalog.ResolveObjectName("HUDPanelButtonGroup");

            RectTransform hudRoot = CreateCanvasGroupRoot("HUDRoot", canvasObject.transform, 0);
            RectTransform popupRoot = CreateCanvasGroupRoot("PopupRoot", canvasObject.transform, 1);
            RectTransform hudStatusGroup = CreateCanvasGroupRoot("HUDStatusGroup", hudRoot, 0);
            RectTransform hudActionGroup = CreateCanvasGroupRoot(hudActionGroupName, hudRoot, 1);
            RectTransform hudBottomGroup = CreateCanvasGroupRoot("HUDBottomGroup", hudRoot, 2);
            RectTransform hudOverlayGroup = CreateCanvasGroupRoot("HUDOverlayGroup", hudRoot, 4);
            RectTransform popupShellGroup = CreateCanvasGroupRoot("PopupShellGroup", popupRoot, 0);
            CreateCanvasGroupRoot("PopupFrameHeader", popupRoot, 2);
            RectTransform popupFrameGroup = null;
            RectTransform popupFrameLeftGroup = null;
            RectTransform popupFrameRightGroup = null;
            RectTransform actionDock = null;
            RectTransform hudPanelButtonGroup = null;

            CreatePanel("TopLeftPanel", hudStatusGroup, PrototypeUILayout.TopLeftPanel, chromeDark);
            CreatePanel("InteractionPromptBackdrop", hudRoot, PrototypeUILayout.PromptBackdrop(isHubScene), Color.white);
            CreatePanel("GuideBackdrop", hudOverlayGroup, PrototypeUILayout.GuideBackdrop(isHubScene), chromeSurface);
            CreatePanel("ResultBackdrop", hudOverlayGroup, PrototypeUILayout.ResultBackdrop(isHubScene), chromeSurface);
            if (isHubScene)
            {
                CreatePanel(hudPanelButtonGroupName, hudRoot, PrototypeUILayout.HubPanelButtonGroup, chromeSurface);
                hudPanelButtonGroup = FindChildRecursive(hudRoot, hudPanelButtonGroupName) as RectTransform;
                CreatePanel("PopupOverlay", popupShellGroup, PrototypeUILayout.HubPopupOverlay, chromeOverlay);
                CreatePanel("ActionDock", hudActionGroup, PrototypeUILayout.HubActionDock, chromeDock);
                actionDock = FindChildRecursive(hudActionGroup, "ActionDock") as RectTransform;
                CreatePanel("ActionAccent", hudActionGroup, PrototypeUILayout.HubActionAccent, chromeAmber);
                CreatePanel("PopupFrame", popupRoot, PrototypeUILayout.HubPopupFrame, new Color(1f, 1f, 1f, 0f));
                popupFrameGroup = FindChildRecursive(popupRoot, "PopupFrame") as RectTransform;

                if (popupFrameGroup != null)
                {
                    CreatePanel("PopupFrameLeft", popupFrameGroup, PrototypeUILayout.HubPopupFrameLeft, Color.white);
                    CreatePanel("PopupFrameRight", popupFrameGroup, PrototypeUILayout.HubPopupFrameRight, new Color(0.92f, 0.95f, 0.99f, 1f));

                    popupFrameLeftGroup = FindChildRecursive(popupFrameGroup, "PopupFrameLeft") as RectTransform;
                    popupFrameRightGroup = FindChildRecursive(popupFrameGroup, "PopupFrameRight") as RectTransform;

                    if (popupFrameLeftGroup != null)
                    {
                        CreatePanel("PopupLeftBody", popupFrameLeftGroup, PrototypeUILayout.HubPopupFrameBody, Color.white);
                        CreatePopupBodyItemBoxes("PopupLeftBody", "PopupLeftItemBox", "PopupLeftItemIcon", "PopupLeftItemText", popupFrameLeftGroup, chromeText, true);
                    }

                    if (popupFrameRightGroup != null)
                    {
                        CreatePanel("PopupRightBody", popupFrameRightGroup, PrototypeUILayout.HubPopupFrameBody, Color.white);
                    }
                }
            }

            UIManager uiManager = canvasObject.AddComponent<UIManager>();
            PrototypeUIDesignController designController = canvasObject.AddComponent<PrototypeUIDesignController>();
            designController.Configure(uiManager);

            TextMeshProUGUI storageCaption = null;
            TextMeshProUGUI recipeCaption = null;
            TextMeshProUGUI upgradeCaption = null;
            TextMeshProUGUI actionCaption = null;

            if (isHubScene)
            {
                Transform popupFrameTextRoot = popupFrameGroup != null ? popupFrameGroup : popupRoot;
                Transform popupFrameLeftTextRoot = popupFrameLeftGroup != null ? popupFrameLeftGroup : popupFrameTextRoot;
                Transform popupFrameRightTextRoot = popupFrameRightGroup != null ? popupFrameRightGroup : popupFrameTextRoot;

                actionCaption = CreateScreenText(
                    "ActionCaption",
                    actionDock != null ? actionDock : hudActionGroup,
                    PrototypeUILayout.HubActionCaption,
                    15,
                    TextAlignmentOptions.TopRight,
                    new Color(0.88f, 0.88f, 0.88f, 1f));
                CreatePopupHeadingText(PrototypeUIObjectNames.PopupTitle, popupFrameLeftTextRoot, PrototypeUILayout.HubPopupTitle, 40f, 24f, "\uC694\uB9AC \uBA54\uB274", chromeText, false);
                CreatePopupHeadingText(PrototypeUIObjectNames.PopupLeftCaption, popupFrameLeftTextRoot, PrototypeUILayout.HubPopupLeftCaption, 32f, 20f, "\uBA54\uB274 \uBAA9\uB85D", chromeText, false);
                CreatePopupHeadingText(PrototypeUIObjectNames.PopupRightCaption, popupFrameRightTextRoot, PrototypeUILayout.HubPopupFrameCaption, 32f, 20f, "\uBA54\uB274 \uC0C1\uC138", chromeText, false);
            }

            TextMeshProUGUI goldText = CreateScreenText("GoldText", hudStatusGroup, PrototypeUILayout.GoldText, 20, TextAlignmentOptions.TopLeft, chromeText);
            TextMeshProUGUI inventoryText = isHubScene
                ? CreateScreenText("InventoryText", popupFrameLeftGroup != null ? popupFrameLeftGroup : popupRoot, PrototypeUILayout.HubPopupFrameText, 19, TextAlignmentOptions.TopLeft, chromeText)
                : null;
            TextMeshProUGUI storageText = isHubScene
                ? CreateScreenText("StorageText", popupFrameRightGroup != null ? popupFrameRightGroup : popupRoot, PrototypeUILayout.HubPopupRightDetailText, 18, TextAlignmentOptions.TopLeft, chromeText)
                : null;
            TextMeshProUGUI promptText = CreateScreenText("InteractionPromptText", hudRoot, PrototypeUILayout.PromptText(isHubScene), 21, TextAlignmentOptions.Center, chromeText);
            TextMeshProUGUI guideText = CreateScreenText("GuideText", hudOverlayGroup, PrototypeUILayout.GuideText(isHubScene), 18, TextAlignmentOptions.Center, chromeText);
            TextMeshProUGUI resultText = CreateScreenText("RestaurantResultText", hudOverlayGroup, PrototypeUILayout.ResultText(isHubScene), 18, TextAlignmentOptions.Center, chromeText);
            TextMeshProUGUI selectedRecipeText = isHubScene
                ? CreateScreenText("SelectedRecipeText", popupFrameRightGroup != null ? popupFrameRightGroup : popupRoot, PrototypeUILayout.HubPopupRightDetailText, 18, TextAlignmentOptions.TopLeft, chromeText)
                : null;
            TextMeshProUGUI upgradeText = isHubScene
                ? CreateScreenText("UpgradeText", popupFrameRightGroup != null ? popupFrameRightGroup : popupRoot, PrototypeUILayout.HubPopupRightDetailText, 18, TextAlignmentOptions.TopLeft, chromeText)
                : null;
            ApplyPopupInventoryTextPresentation(inventoryText);
            ApplyPopupDetailTextPresentation(storageText);
            ApplyPopupDetailTextPresentation(selectedRecipeText);
            ApplyPopupDetailTextPresentation(upgradeText);

            Button recipePanelButton = isHubScene ? CreateUiButton("RecipePanelButton", hudPanelButtonGroup != null ? hudPanelButtonGroup : hudRoot, PrototypeUILayout.HubRecipePanelButton, "\uC694\uB9AC \uBA54\uB274") : null;
            Button upgradePanelButton = isHubScene ? CreateUiButton("UpgradePanelButton", hudPanelButtonGroup != null ? hudPanelButtonGroup : hudRoot, PrototypeUILayout.HubUpgradePanelButton, "\uC5C5\uADF8\uB808\uC774\uB4DC") : null;
            Button materialPanelButton = isHubScene ? CreateUiButton("MaterialPanelButton", hudPanelButtonGroup != null ? hudPanelButtonGroup : hudRoot, PrototypeUILayout.HubMaterialPanelButton, "\uC7AC\uB8CC") : null;
            Button guideHelpButton = CreateUiButton("GuideHelpButton", hudOverlayGroup, PrototypeUILayout.GuideHelpButton(isHubScene), string.Empty);
            Button popupCloseButton = isHubScene ? CreateUiButton("PopupCloseButton", popupFrameRightGroup != null ? popupFrameRightGroup : (popupFrameGroup != null ? popupFrameGroup : popupRoot), PrototypeUILayout.HubPopupCloseButton, string.Empty) : null;
            HideGeneratedButtonLabel(guideHelpButton);
            HideGeneratedButtonLabel(popupCloseButton);

            if (storageCaption != null) storageCaption.text = "\uCC3D\uACE0";
            if (recipeCaption != null) recipeCaption.text = "\uC694\uB9AC \uBA54\uB274";
            if (upgradeCaption != null) upgradeCaption.text = "\uC5C5\uADF8\uB808\uC774\uB4DC";
            if (actionCaption != null) actionCaption.text = "\uC9C4\uD589";

            if (storageCaption != null) storageCaption.fontStyle = FontStyles.Bold;
            if (recipeCaption != null) recipeCaption.fontStyle = FontStyles.Bold;
            if (upgradeCaption != null) upgradeCaption.fontStyle = FontStyles.Bold;
            if (actionCaption != null) actionCaption.fontStyle = FontStyles.Bold;
            if (storageCaption != null) storageCaption.characterSpacing = 0.5f;
            if (recipeCaption != null) recipeCaption.characterSpacing = 0.5f;
            if (upgradeCaption != null) upgradeCaption.characterSpacing = 0.5f;
            if (actionCaption != null) actionCaption.characterSpacing = 0.5f;
            if (storageCaption != null) storageCaption.margin = Vector4.zero;
            if (recipeCaption != null) recipeCaption.margin = Vector4.zero;
            if (upgradeCaption != null) upgradeCaption.margin = Vector4.zero;
            if (actionCaption != null) actionCaption.margin = Vector4.zero;

            if (inventoryText != null) inventoryText.textWrappingMode = TextWrappingModes.Normal;
            if (inventoryText != null) inventoryText.overflowMode = TextOverflowModes.Masking;
            if (storageText != null) storageText.textWrappingMode = TextWrappingModes.Normal;
            if (storageText != null) storageText.overflowMode = TextOverflowModes.Masking;
            if (selectedRecipeText != null) selectedRecipeText.textWrappingMode = TextWrappingModes.Normal;
            if (selectedRecipeText != null) selectedRecipeText.overflowMode = TextOverflowModes.Masking;
            if (upgradeText != null) upgradeText.textWrappingMode = TextWrappingModes.Normal;
            if (upgradeText != null) upgradeText.overflowMode = TextOverflowModes.Masking;

            if (goldText != null) goldText.text = "\uCF54\uC778: 0   \uD3C9\uD310: 0";
            if (inventoryText != null) inventoryText.text = "\uC778\uBCA4\uD1A0\uB9AC 0/8\uCE78\n- \uBE44\uC5B4 \uC788\uC74C";
            if (storageText != null) storageText.text = "- \uBE44\uC5B4 \uC788\uC74C";
            if (promptText != null) promptText.text = "\uC774\uB3D9: WASD / \uBC29\uD5A5\uD0A4   \uC0C1\uD638\uC791\uC6A9: E";
            if (guideText != null) guideText.text = string.Empty;
            if (resultText != null) resultText.text = string.Empty;
            if (selectedRecipeText != null) selectedRecipeText.text = "\uC120\uD0DD \uBA54\uB274: \uC5C6\uC74C";
            if (upgradeText != null) upgradeText.text = "- \uC778\uBCA4\uD1A0\uB9AC 8\uCE78 -> 12\uCE78";

            ApplySceneOverridesToHierarchy(canvasObject.transform);

            if (isHubScene)
            {
                SetChildActive(canvasObject.transform, hudPanelButtonGroupName, false);
                SetChildActive(canvasObject.transform, "PopupOverlay", false);
                SetChildActive(canvasObject.transform, "PopupFrame", false);
                SetChildActive(canvasObject.transform, "PopupFrameLeft", false);
                SetChildActive(canvasObject.transform, "PopupFrameRight", false);
                SetChildActive(canvasObject.transform, "PopupLeftBody", false);
                SetChildActive(canvasObject.transform, "PopupRightBody", false);
                SetChildActive(canvasObject.transform, "StorageCard", false);
                SetChildActive(canvasObject.transform, "RecipeCard", false);
                SetChildActive(canvasObject.transform, "UpgradeCard", false);
                SetChildActive(canvasObject.transform, "ActionDock", false);
                SetChildActive(canvasObject.transform, "StorageAccent", false);
                SetChildActive(canvasObject.transform, "RecipeAccent", false);
                SetChildActive(canvasObject.transform, "UpgradeAccent", false);
                SetChildActive(canvasObject.transform, "ActionAccent", false);
            }

            SetChildActive(canvasObject.transform, "GuideBackdrop", false);
            SetChildActive(canvasObject.transform, "ResultBackdrop", false);
            if (storageCaption != null) storageCaption.gameObject.SetActive(false);
            if (recipeCaption != null) recipeCaption.gameObject.SetActive(false);
            if (upgradeCaption != null) upgradeCaption.gameObject.SetActive(false);
            if (actionCaption != null) actionCaption.gameObject.SetActive(false);
            SetChildActive(canvasObject.transform, PrototypeUIObjectNames.PopupTitle, false);
            SetChildActive(canvasObject.transform, PrototypeUIObjectNames.PopupLeftCaption, false);
            SetChildActive(canvasObject.transform, PrototypeUIObjectNames.PopupRightCaption, false);
            if (inventoryText != null) inventoryText.gameObject.SetActive(false);
            if (storageText != null) storageText.gameObject.SetActive(false);
            if (guideText != null) guideText.gameObject.SetActive(false);
            if (resultText != null) resultText.gameObject.SetActive(false);
            if (selectedRecipeText != null) selectedRecipeText.gameObject.SetActive(false);
            if (upgradeText != null) upgradeText.gameObject.SetActive(false);
            if (recipePanelButton != null) recipePanelButton.gameObject.SetActive(false);
            if (upgradePanelButton != null) upgradePanelButton.gameObject.SetActive(false);
            if (materialPanelButton != null) materialPanelButton.gameObject.SetActive(false);
            if (popupCloseButton != null) popupCloseButton.gameObject.SetActive(false);

            SerializedObject so = new(uiManager);
            so.FindProperty("interactionPromptText").objectReferenceValue = promptText;
            so.FindProperty("inventoryText").objectReferenceValue = inventoryText;
            so.FindProperty("storageText").objectReferenceValue = storageText;
            so.FindProperty("upgradeText").objectReferenceValue = upgradeText;
            so.FindProperty("goldText").objectReferenceValue = goldText;
            so.FindProperty("selectedRecipeText").objectReferenceValue = selectedRecipeText;
            so.FindProperty("guideText").objectReferenceValue = guideText;
            so.FindProperty("resultText").objectReferenceValue = resultText;
            so.FindProperty("bodyFontAsset").objectReferenceValue = _generatedKoreanFont;
            so.FindProperty("headingFontAsset").objectReferenceValue = _generatedHeadingFont;
            so.FindProperty("recipePanelButton").objectReferenceValue = recipePanelButton;
            so.FindProperty("upgradePanelButton").objectReferenceValue = upgradePanelButton;
            so.FindProperty("materialPanelButton").objectReferenceValue = materialPanelButton;
            so.FindProperty("guideHelpButton").objectReferenceValue = guideHelpButton;
            so.FindProperty("popupCloseButton").objectReferenceValue = popupCloseButton;
            so.FindProperty("defaultPromptText").stringValue = "\uC774\uB3D9: WASD / \uBC29\uD5A5\uD0A4   \uC0C1\uD638\uC791\uC6A9: E";
            so.ApplyModifiedPropertiesWithoutUndo();
            ApplySceneComponentOverride(uiManager, canvasObject.name);
            ApplySceneActiveOverride(canvasObject, canvasObject.name);
        }

        private static void EnsureUiEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            ConfigureInputSystemUiModule(inputModule);
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// Input System 기본 액션 생성 경로가 에디터에서 예외를 내는 환경이 있어
        /// 프로젝트 내부 UI 액션 에셋을 직접 만들고 모듈에 연결합니다.
        /// </summary>
        private static void ConfigureInputSystemUiModule(InputSystemUIInputModule inputModule)
        {
            InputActionAsset asset = EnsureUiInputActionsAsset();

            inputModule.actionsAsset = asset;
            inputModule.point = InputActionReference.Create(asset.FindAction("UI/Point", true));
            inputModule.leftClick = InputActionReference.Create(asset.FindAction("UI/LeftClick", true));
            inputModule.rightClick = InputActionReference.Create(asset.FindAction("UI/RightClick", true));
            inputModule.middleClick = InputActionReference.Create(asset.FindAction("UI/MiddleClick", true));
            inputModule.scrollWheel = InputActionReference.Create(asset.FindAction("UI/ScrollWheel", true));
            inputModule.move = InputActionReference.Create(asset.FindAction("UI/Move", true));
            inputModule.submit = InputActionReference.Create(asset.FindAction("UI/Submit", true));
            inputModule.cancel = InputActionReference.Create(asset.FindAction("UI/Cancel", true));
            inputModule.trackedDevicePosition = InputActionReference.Create(asset.FindAction("UI/TrackedDevicePosition", true));
            inputModule.trackedDeviceOrientation = InputActionReference.Create(asset.FindAction("UI/TrackedDeviceOrientation", true));
        }

        private static InputActionAsset EnsureUiInputActionsAsset()
        {
            string assetPath = InputDataRoot + "/generated-ui-input-actions.asset";
            InputActionAsset existingAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
            EnsureMainObjectNameMatchesFileName(existingAsset, assetPath);
            if (existingAsset != null && HasRequiredUiActions(existingAsset))
            {
                return existingAsset;
            }

            if (existingAsset != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "generated-ui-input-actions";
            InputActionMap uiMap = new("UI");
            asset.AddActionMap(uiMap);

            InputAction pointAction = uiMap.AddAction("Point", InputActionType.PassThrough);
            pointAction.expectedControlType = "Vector2";
            pointAction.AddBinding("<Mouse>/position");
            pointAction.AddBinding("<Pen>/position");
            pointAction.AddBinding("<Touchscreen>/primaryTouch/position");

            InputAction leftClickAction = uiMap.AddAction("LeftClick", InputActionType.PassThrough);
            leftClickAction.expectedControlType = "Button";
            leftClickAction.AddBinding("<Mouse>/leftButton");
            leftClickAction.AddBinding("<Pen>/tip");
            leftClickAction.AddBinding("<Touchscreen>/primaryTouch/press");

            InputAction rightClickAction = uiMap.AddAction("RightClick", InputActionType.PassThrough);
            rightClickAction.expectedControlType = "Button";
            rightClickAction.AddBinding("<Mouse>/rightButton");

            InputAction middleClickAction = uiMap.AddAction("MiddleClick", InputActionType.PassThrough);
            middleClickAction.expectedControlType = "Button";
            middleClickAction.AddBinding("<Mouse>/middleButton");

            InputAction scrollWheelAction = uiMap.AddAction("ScrollWheel", InputActionType.PassThrough);
            scrollWheelAction.expectedControlType = "Vector2";
            scrollWheelAction.AddBinding("<Mouse>/scroll");

            InputAction moveAction = uiMap.AddAction("Move", InputActionType.PassThrough);
            moveAction.expectedControlType = "Vector2";
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick");
            moveAction.AddBinding("<Gamepad>/dpad");

            InputAction submitAction = uiMap.AddAction("Submit", InputActionType.Button);
            submitAction.expectedControlType = "Button";
            submitAction.AddBinding("<Keyboard>/enter");
            submitAction.AddBinding("<Keyboard>/numpadEnter");
            submitAction.AddBinding("<Keyboard>/space");
            submitAction.AddBinding("<Gamepad>/buttonSouth");

            InputAction cancelAction = uiMap.AddAction("Cancel", InputActionType.Button);
            cancelAction.expectedControlType = "Button";
            cancelAction.AddBinding("<Keyboard>/escape");
            cancelAction.AddBinding("<Gamepad>/buttonEast");

            InputAction trackedPositionAction = uiMap.AddAction("TrackedDevicePosition", InputActionType.PassThrough);
            trackedPositionAction.expectedControlType = "Vector3";
            trackedPositionAction.AddBinding("<XRController>/devicePosition");

            InputAction trackedOrientationAction = uiMap.AddAction("TrackedDeviceOrientation", InputActionType.PassThrough);
            trackedOrientationAction.expectedControlType = "Quaternion";
            trackedOrientationAction.AddBinding("<XRController>/deviceRotation");

            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static bool HasRequiredUiActions(InputActionAsset asset)
        {
            return asset.FindAction("UI/Point") != null
                   && asset.FindAction("UI/LeftClick") != null
                   && asset.FindAction("UI/RightClick") != null
                   && asset.FindAction("UI/MiddleClick") != null
                   && asset.FindAction("UI/ScrollWheel") != null
                   && asset.FindAction("UI/Move") != null
                   && asset.FindAction("UI/Submit") != null
                   && asset.FindAction("UI/Cancel") != null
                   && asset.FindAction("UI/TrackedDevicePosition") != null
                   && asset.FindAction("UI/TrackedDeviceOrientation") != null;
        }
#endif

        /// <summary>
        /// 화면 고정 UI 텍스트를 만들고 generated 한글 폰트와 기본 여백을 같이 적용합니다.
        /// </summary>
        private static bool ShouldSkipCanvasObjectCreation(string objectName, Transform parent)
        {
            return parent == null
                   || string.IsNullOrWhiteSpace(objectName)
                   || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName);
        }

        private static TextMeshProUGUI CreateScreenText(
            string objectName,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color)
        {
            if (ShouldSkipCanvasObjectCreation(objectName, parent))
            {
                return null;
            }

            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta));

            GameObject go = new(objectName);
            ApplyHubPopupObjectIdentity(go);
            go.transform.SetParent(parent, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = resolvedLayout.AnchorMin;
            rect.anchorMax = resolvedLayout.AnchorMax;
            rect.pivot = resolvedLayout.Pivot;
            rect.anchoredPosition = resolvedLayout.AnchoredPosition;
            rect.sizeDelta = resolvedLayout.SizeDelta;

            TMP_FontAsset preferredFont = EnsurePreferredTmpFontAsset();
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.text = string.Empty;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.margin = new Vector4(10f, 8f, 10f, 8f);
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(12f, fontSize - 6f);
            text.fontSizeMax = fontSize;
            text.overflowMode = TextOverflowModes.Truncate;

            if (preferredFont != null)
            {
                text.font = preferredFont;
            }
            else if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            ApplySceneTextOverride(text);
            return text;
        }

        /// <summary>
        /// 공용 레이아웃 프리셋을 바로 넘겨 HUD 텍스트 생성 중복을 줄입니다.
        /// </summary>
        private static TextMeshProUGUI CreateScreenText(
            string objectName,
            Transform parent,
            PrototypeUIRect layout,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color)
        {
            return CreateScreenText(
                objectName,
                parent,
                layout.AnchorMin,
                layout.AnchorMax,
                layout.Pivot,
                layout.AnchoredPosition,
                layout.SizeDelta,
                fontSize,
                alignment,
                color);
        }

        /// <summary>
        /// 씬에서 저장한 TMP 표시 오버라이드가 있으면 기본 스타일 적용 뒤 마지막에 다시 덮어씁니다.
        /// </summary>
        private static void ApplySceneTextOverride(TextMeshProUGUI text)
        {
            if (text == null)
            {
                return;
            }

            PrototypeUISceneLayoutCatalog.TryApplyTextOverride(text, text.name);
        }

        /// <summary>
        /// 씬에서 저장한 버튼과 라벨 오버라이드가 있으면 기본 스타일 적용 뒤 마지막에 다시 덮어씁니다.
        /// </summary>
        private static void ApplySceneButtonOverride(Button button)
        {
            if (button == null)
            {
                return;
            }

            PrototypeUISceneLayoutCatalog.TryApplyButtonOverride(button, button.name);

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                PrototypeUISceneLayoutCatalog.TryApplyTextOverride(label, label.name);
            }
        }

        private static void CreatePopupHeadingText(
            string objectName,
            Transform parent,
            PrototypeUIRect layout,
            float fontSize,
            float sceneFontSizeMax,
            string content,
            Color color,
            bool enableAutoSizing)
        {
            TextMeshProUGUI text = CreateScreenText(objectName, parent, layout, fontSize, TextAlignmentOptions.TopLeft, color);
            if (text == null)
            {
                return;
            }

            text.text = content;
            TMP_FontAsset headingFont = EnsureHeadingTmpFontAsset();
            if (headingFont != null)
            {
                text.font = headingFont;
                if (headingFont.material != null)
                {
                    text.fontSharedMaterial = headingFont.material;
                }
            }

            ApplyPopupHeadingPresentation(text, fontSize, sceneFontSizeMax, enableAutoSizing);
            ApplySceneTextOverride(text);
        }

        private static void ApplyPopupHeadingPresentation(TextMeshProUGUI text, float fontSize, float sceneFontSizeMax, bool enableAutoSizing)
        {
            if (text == null)
            {
                return;
            }

            text.enableAutoSizing = enableAutoSizing;
            text.fontSize = fontSize;
            text.fontSizeMin = 12f;
            text.fontSizeMax = sceneFontSizeMax;
            text.fontStyle = FontStyles.Normal;
            text.characterSpacing = 0f;
            text.margin = new Vector4(10f, 8f, 10f, 8f);
            text.overflowMode = TextOverflowModes.Truncate;
        }

        /// <summary>
        /// 왼쪽 본문 인벤토리 텍스트는 Hub.unity 직렬화 값과 같은 크기/여백으로
        /// 맞춰서 빌더 미리보기와 실제 씬 표시 밀도가 달라지지 않게 합니다.
        /// </summary>
        private static void ApplyPopupInventoryTextPresentation(TextMeshProUGUI text)
        {
            if (text == null)
            {
                return;
            }

            text.fontSize = 19f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 13f;
            text.fontSizeMax = 19f;
            text.fontStyle = FontStyles.Normal;
            text.characterSpacing = 0f;
            text.lineSpacing = 0f;
            text.paragraphSpacing = 0f;
            text.margin = new Vector4(10f, 8f, 10f, 8f);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Masking;
            ApplySceneTextOverride(text);
        }

        private static void ApplyPopupDetailTextPresentation(TextMeshProUGUI text)
        {
            if (text == null)
            {
                return;
            }

            text.fontSize = 18f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = 18f;
            text.fontStyle = FontStyles.Normal;
            text.characterSpacing = 0f;
            text.lineSpacing = 0f;
            text.paragraphSpacing = 0f;
            text.margin = new Vector4(10f, 8f, 10f, 8f);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Masking;
            ApplySceneTextOverride(text);
        }

        /// <summary>
        /// 카드 배경이나 포인트 바 같은 평면 UI 블록을 그림자와 함께 생성합니다.
        /// </summary>
        private static void CreatePanel(
            string objectName,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color)
        {
            if (ShouldSkipCanvasObjectCreation(objectName, parent))
            {
                return;
            }

            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta));

            GameObject panelObject = new(objectName);
            ApplyHubPopupObjectIdentity(panelObject);
            panelObject.transform.SetParent(parent, false);

            RectTransform rect = panelObject.AddComponent<RectTransform>();
            rect.anchorMin = resolvedLayout.AnchorMin;
            rect.anchorMax = resolvedLayout.AnchorMax;
            rect.pivot = resolvedLayout.Pivot;
            rect.anchoredPosition = resolvedLayout.AnchoredPosition;
            rect.sizeDelta = resolvedLayout.SizeDelta;

            Image image = panelObject.AddComponent<Image>();
            ApplyScenePanelImagePresentation(image, objectName, color);

            image.raycastTarget = false;

            if (!objectName.EndsWith("Accent"))
            {
                Shadow shadow = panelObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
                shadow.effectDistance = new Vector2(0f, -4f);
                shadow.useGraphicAlpha = true;
            }
        }

        /// <summary>
        /// 공용 레이아웃 프리셋으로 배경 패널을 생성합니다.
        /// </summary>
        private static void CreatePanel(
            string objectName,
            Transform parent,
            PrototypeUIRect layout,
            Color color)
        {
            CreatePanel(
                objectName,
                parent,
                layout.AnchorMin,
                layout.AnchorMax,
                layout.Pivot,
                layout.AnchoredPosition,
                layout.SizeDelta,
                color);
        }

        /// <summary>
        /// 허브 팝업 본문 안에 반복 아이템 박스를 미리 만들어 두면 에디터 프리뷰와 새 씬 기본 구조가 같은 기준을 쓸 수 있습니다.
        /// </summary>
        private static void CreatePopupBodyItemBoxes(
            string bodyName,
            string boxPrefix,
            string iconPrefix,
            string textPrefix,
            Transform popupRoot,
            Color textColor,
            bool isInteractive)
        {
            Transform bodyTransform = FindChildRecursive(popupRoot, bodyName);
            if (bodyTransform == null)
            {
                return;
            }

            Color boxColor = new(0.96f, 0.92f, 0.78f, 1f);
            for (int i = 0; i < PrototypeUILayout.HubPopupBodyItemBoxCount; i++)
            {
                string boxName = $"{boxPrefix}{i + 1:00}";
                string iconName = $"{iconPrefix}{i + 1:00}";
                string textName = $"{textPrefix}{i + 1:00}";
                CreatePanel(boxName, bodyTransform, PrototypeUILayout.HubPopupBodyItemBox(i), boxColor);

                Transform boxTransform = FindChildRecursive(bodyTransform, boxName);
                if (boxTransform == null)
                {
                    continue;
                }

                Image boxImage = boxTransform.GetComponent<Image>();
                if (boxImage != null)
                {
                    boxImage.raycastTarget = isInteractive;
                }

                if (isInteractive)
                {
                    Button button = boxTransform.GetComponent<Button>();
                    if (button == null)
                    {
                        button = boxTransform.gameObject.AddComponent<Button>();
                    }

                    button.targetGraphic = boxImage;
                    button.transition = Selectable.Transition.ColorTint;
                    Navigation navigation = button.navigation;
                    navigation.mode = Navigation.Mode.None;
                    button.navigation = navigation;
                    ApplySceneButtonOverride(button);
                }

                CreatePopupBodyItemIcon(iconName, boxTransform);
                TextMeshProUGUI text = CreateScreenText(
                    textName,
                    boxTransform,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    17f,
                    TextAlignmentOptions.TopLeft,
                    textColor);
                if (text == null)
                {
                    continue;
                }

                text.textWrappingMode = TextWrappingModes.Normal;
                text.overflowMode = TextOverflowModes.Ellipsis;
                text.margin = Vector4.zero;
                text.lineSpacing = 0f;
                text.enableAutoSizing = true;
                text.fontSizeMin = 12f;
                text.fontSizeMax = 17f;
                text.rectTransform.offsetMin = new Vector2(74f, 10f);
                text.rectTransform.offsetMax = new Vector2(-14f, -10f);
                text.text = string.Empty;
                ApplySceneTextOverride(text);
            }
        }

        private static void CreatePopupBodyItemIcon(string objectName, Transform parent)
        {
            if (ShouldSkipCanvasObjectCreation(objectName, parent))
            {
                return;
            }

            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(40f, 0f),
                    new Vector2(44f, 44f)));

            GameObject iconObject = new(objectName);
            ApplyHubPopupObjectIdentity(iconObject);
            iconObject.transform.SetParent(parent, false);

            RectTransform rect = iconObject.AddComponent<RectTransform>();
            rect.anchorMin = resolvedLayout.AnchorMin;
            rect.anchorMax = resolvedLayout.AnchorMax;
            rect.pivot = resolvedLayout.Pivot;
            rect.anchoredPosition = resolvedLayout.AnchoredPosition;
            rect.sizeDelta = resolvedLayout.SizeDelta;

            Image image = iconObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = false;
            PrototypeUISceneLayoutCatalog.TryApplyImageOverride(image, objectName);
        }

        private static RectTransform CreateCanvasGroupRoot(string objectName, Transform parent, int siblingIndex)
        {
            if (ShouldSkipCanvasObjectCreation(objectName, parent))
            {
                return null;
            }

            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero));

            GameObject groupObject = new(objectName);
            ApplyHubPopupObjectIdentity(groupObject);
            groupObject.transform.SetParent(parent, false);

            RectTransform rect = groupObject.AddComponent<RectTransform>();
            rect.anchorMin = resolvedLayout.AnchorMin;
            rect.anchorMax = resolvedLayout.AnchorMax;
            rect.pivot = resolvedLayout.Pivot;
            rect.anchoredPosition = resolvedLayout.AnchoredPosition;
            rect.sizeDelta = resolvedLayout.SizeDelta;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetSiblingIndex(siblingIndex);
            return rect;
        }

        private static void SetChildActive(Transform parent, string objectName, bool isActive)
        {
            if (parent == null)
            {
                return;
            }

            Transform child = FindChildRecursive(parent, objectName);
            if (child != null)
            {
                child.gameObject.SetActive(isActive);
            }
        }

        private static Transform FindChildRecursive(Transform parent, string objectName)
        {
            if (parent == null || string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            foreach (Transform child in parent)
            {
                if (child.name == objectName)
                {
                    return child;
                }

                Transform nested = FindChildRecursive(child, objectName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static Transform FindNamedTransformInScene(Scene scene, string objectName)
        {
            if (!scene.IsValid() || string.IsNullOrEmpty(objectName))
            {
                return null;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root == null)
                {
                    continue;
                }

                if (root.name == objectName)
                {
                    return root.transform;
                }

                Transform nested = FindChildRecursive(root.transform, objectName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        /// <summary>
        /// 빠른 행동 버튼을 만들고 텍스트와 그림자까지 기본 스타일로 맞춥니다.
        /// </summary>
        private static Button CreateUiButton(
            string objectName,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            string label)
        {
            if (ShouldSkipCanvasObjectCreation(objectName, parent))
            {
                return null;
            }

            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta));

            GameObject buttonObject = new(objectName);
            ApplyHubPopupObjectIdentity(buttonObject);
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = resolvedLayout.AnchorMin;
            rect.anchorMax = resolvedLayout.AnchorMax;
            rect.pivot = resolvedLayout.Pivot;
            rect.anchoredPosition = resolvedLayout.AnchoredPosition;
            rect.sizeDelta = resolvedLayout.SizeDelta;

            Image image = buttonObject.AddComponent<Image>();
            ApplySceneButtonImagePresentation(image, objectName, new Color(0.18f, 0.18f, 0.18f, 0.82f));

            Shadow shadow = buttonObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
            shadow.effectDistance = new Vector2(0f, -3f);
            shadow.useGraphicAlpha = true;

            Button button = buttonObject.AddComponent<Button>();

            TextMeshProUGUI labelText = CreateScreenText(
                objectName + "_Label",
                buttonObject.transform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                20,
                TextAlignmentOptions.Center,
                Color.white);
            labelText.text = label;
            if (_generatedHeadingFont != null)
            {
                labelText.font = _generatedHeadingFont;
            }

            labelText.fontStyle = FontStyles.Bold;
            labelText.margin = new Vector4(8f, 6f, 8f, 6f);

            ApplySceneTextOverride(labelText);
            ApplySceneButtonOverride(button);
            return button;
        }

        /// <summary>
        /// 공용 레이아웃 프리셋으로 버튼을 생성해 허브 메뉴/액션 배치를 통일합니다.
        /// </summary>
        private static Button CreateUiButton(
            string objectName,
            Transform parent,
            PrototypeUIRect layout,
            string label)
        {
            return CreateUiButton(
                objectName,
                parent,
                layout.AnchorMin,
                layout.AnchorMax,
                layout.Pivot,
                layout.AnchoredPosition,
                layout.SizeDelta,
                label);
        }

        private static void HideGeneratedButtonLabel(Button button)
        {
            if (button == null)
            {
                return;
            }

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null)
            {
                return;
            }

            label.text = string.Empty;
            label.gameObject.SetActive(false);
        }

        private static void ApplyHubPopupObjectIdentity(GameObject target)
        {
            if (target == null || !IsHubPopupDisplayObject(target.name))
            {
                return;
            }

            int uiLayer = LayerMask.NameToLayer("UI");
            target.layer = uiLayer >= 0 ? uiLayer : 5;
            target.tag = "Player";
        }

        /// <summary>
        /// 허브 팝업은 기존 Hub.unity에 저장된 Image 설정을 우선 적용해
        /// generated 스킨보다 씬에 직접 지정한 소스 이미지를 기준으로 맞춥니다.
        /// </summary>
        private static bool TryApplyHubPopupSceneImage(Image image, string objectName)
        {
            if (image == null || string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            if (PrototypeUISkinCatalog.UsesGeneratedUiDesignPanel(objectName)
                || PrototypeUISkinCatalog.UsesGeneratedUiDesignButton(objectName))
            {
                return false;
            }

            if (!CachedHubPopupSceneImages.TryGetValue(objectName, out SceneImageSnapshot snapshot))
            {
                return false;
            }

            image.sprite = snapshot.Sprite;
            image.type = snapshot.Type;
            image.color = snapshot.Color;
            image.preserveAspect = snapshot.PreserveAspect;
            return true;
        }

        /// <summary>
        /// 패널 기본 스킨을 적용한 뒤, 씬에서 동기화한 Image 오버라이드가 있으면 마지막에 다시 덮어씁니다.
        /// </summary>
        private static void ApplyScenePanelImagePresentation(Image image, string objectName, Color fallbackColor)
        {
            if (image == null)
            {
                return;
            }

            if (!TryApplyHubPopupSceneImage(image, objectName))
            {
                PrototypeUISkin.ApplyPanel(image, objectName, fallbackColor);
            }

            // generated UI 스킨은 기본값으로 적용하고, 씬 저장으로 동기화한 오버라이드가 있으면 마지막에 우선 반영한다.
            PrototypeUISceneLayoutCatalog.TryApplyImageOverride(image, objectName);
        }

        /// <summary>
        /// 버튼 기본 스킨을 적용한 뒤, 씬에서 동기화한 Image 오버라이드가 있으면 마지막에 다시 덮어씁니다.
        /// </summary>
        private static void ApplySceneButtonImagePresentation(Image image, string objectName, Color fallbackColor)
        {
            if (image == null)
            {
                return;
            }

            if (!TryApplyHubPopupSceneImage(image, objectName))
            {
                PrototypeUISkin.ApplyButton(image, objectName, fallbackColor);
            }

            // generated UI 스킨은 기본값으로 적용하고, 씬 저장으로 동기화한 오버라이드가 있으면 마지막에 우선 반영한다.
            PrototypeUISceneLayoutCatalog.TryApplyImageOverride(image, objectName);
        }

        private static bool IsHubPopupDisplayObject(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            if (objectName is "PopupRoot" or "PopupShellGroup" or "PopupFrameHeader" or "PopupOverlay")
            {
                return false;
            }

            return objectName.StartsWith("Popup", StringComparison.Ordinal)
                   || objectName is "InventoryText" or "StorageText" or "SelectedRecipeText" or "UpgradeText";
        }

        private static void CreateWorldLabel(string objectName, Transform parent, Vector3 localPosition, string content, Color color, float fontSize, int sortingOrder)
        {
            CreateWorldTextObject(objectName, parent, localPosition, content, color, fontSize, sortingOrder);
        }

        /// <summary>
        /// 월드 배치용 TextMeshPro 오브젝트를 만들고 참조를 바로 돌려준다.
        /// 메뉴 보드처럼 후속 구성에서 텍스트 컴포넌트가 필요할 때 사용한다.
        /// </summary>
        private static TextMeshPro CreateWorldTextObject(
            string objectName,
            Transform parent,
            Vector3 localPosition,
            string content,
            Color color,
            float fontSize,
            int sortingOrder,
            float? labelScale = null,
            FontStyles? fontStyle = null,
            float? characterSpacing = null)
        {
            bool defaultLargeLabel = fontSize >= 3.4f;
            bool defaultPrimaryLabel = fontSize >= 2.5f;
            GameObject labelObject = new(objectName);
            if (parent != null)
            {
                labelObject.transform.SetParent(parent, false);
                ApplySceneTransformOverride(labelObject.transform, objectName, localPosition, Quaternion.identity, Vector3.one, useLocalSpace: true);
            }
            else
            {
                ApplySceneTransformOverride(labelObject.transform, objectName, localPosition, Quaternion.identity, Vector3.one, useLocalSpace: false);
            }

            TextMeshPro text = labelObject.AddComponent<TextMeshPro>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.characterSpacing = characterSpacing ?? (defaultLargeLabel ? 0.22f : defaultPrimaryLabel ? 0.08f : 0.02f);
            text.wordSpacing = 0f;
            text.lineSpacing = 0f;
            text.fontStyle = fontStyle ?? (fontSize >= 2.5f ? FontStyles.Bold : FontStyles.Normal);
            ApplySceneComponentOverride(text, objectName);

            bool isLargeLabel = text.fontSize >= 3.4f;
            bool isPrimaryLabel = text.fontSize >= 2.5f;
            TMP_FontAsset preferredFont = isLargeLabel ? EnsureHeadingTmpFontAsset() : EnsurePreferredTmpFontAsset();
            if (text.font == null)
            {
                if (preferredFont != null)
                {
                    text.font = preferredFont;
                }
                else if (TMP_Settings.defaultFontAsset != null)
                {
                    text.font = TMP_Settings.defaultFontAsset;
                }
            }

            float resolvedLabelScale = labelScale ?? (isLargeLabel ? 0.39f : isPrimaryLabel ? 0.36f : 0.33f);
            labelObject.transform.localScale = ResolveSceneObjectScale(objectName, Vector3.one * resolvedLabelScale);
            Material worldTextMaterial = EnsureWorldTextSharedMaterial(text.font, isLargeLabel || isPrimaryLabel);
            if (worldTextMaterial != null)
            {
                text.fontSharedMaterial = worldTextMaterial;
            }

            ApplyWorldTextReadability(text);

            MeshRenderer meshRenderer = text.GetComponent<MeshRenderer>();
            meshRenderer.sortingOrder = sortingOrder;
            ApplySceneComponentOverride(meshRenderer, objectName);
            ApplySceneActiveOverride(labelObject, objectName);
            return text;
        }

        /// <summary>
        /// 허브 월드 텍스트는 배경 그림 위에서도 읽히도록 외곽선과 패딩을 기본 적용한다.
        /// </summary>
        private static void ApplyWorldTextReadability(TextMeshPro text)
        {
            if (text == null)
            {
                return;
            }

            text.extraPadding = true;
        }

        /// <summary>
        /// 에디터 빌드에서는 TMP setter가 renderer.material을 열지 않도록
        /// 공유 머티리얼 에셋을 만들어 폰트별/강도별로 재사용한다.
        /// </summary>
        private static Material EnsureWorldTextSharedMaterial(TMP_FontAsset fontAsset, bool useStrongOutline)
        {
            if (fontAsset == null || fontAsset.material == null)
            {
                return null;
            }

            string materialName = fontAsset.name + (useStrongOutline ? "WorldTextStrong" : "WorldTextNormal");
            if (CachedWorldTextMaterials.TryGetValue(materialName, out Material cachedMaterial) && cachedMaterial != null)
            {
                return cachedMaterial;
            }

            string materialPath = $"{FontRoot}/{materialName}.mat";
            Material materialAsset = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (materialAsset == null)
            {
                materialAsset = new Material(fontAsset.material)
                {
                    name = materialName
                };
                AssetDatabase.CreateAsset(materialAsset, materialPath);
            }

            materialAsset.SetColor(ShaderUtilities.ID_OutlineColor, HubRoomLayout.WorldTextOutlineColor);
            materialAsset.SetFloat(
                ShaderUtilities.ID_OutlineWidth,
                useStrongOutline
                    ? HubRoomLayout.WorldTextStrongOutlineWidth
                    : HubRoomLayout.WorldTextNormalOutlineWidth);
            EditorUtility.SetDirty(materialAsset);

            CachedWorldTextMaterials[materialName] = materialAsset;
            return materialAsset;
        }

        /// <summary>
        /// 허브 바닥 표시는 별도 이미지 대신 얇은 바닥 패널과 텍스트 조합으로 다시 만든다.
        /// </summary>
        private static void CreateHubFloorSign(HubRoomLayout.HubFloorSignPlacement placement, Sprite floorSprite, Transform parent)
        {
            GameObject sign = CreateDecorBlock(
                placement.ObjectName,
                placement.Position,
                placement.BackdropScale,
                floorSprite,
                HubRoomLayout.SignBackdropColor,
                HubRoomLayout.SignSortingOrder,
                parent);

            CreateWorldTextObject(
                placement.ObjectName + "Label",
                sign.transform,
                placement.TextLocalPosition,
                placement.Content,
                HubRoomLayout.SignTextColor,
                placement.FontSize,
                HubRoomLayout.SignTextSortingOrder,
                labelScale: placement.TextScale,
                fontStyle: FontStyles.Bold,
                characterSpacing: placement.CharacterSpacing);
        }

        private static GameObject CreateFloorZone(string objectName, Vector3 position, Vector3 scale, Sprite sprite, Color color, int sortingOrder)
        {
            return CreateDecorBlock(objectName, position, scale, sprite, color, sortingOrder);
        }

        private static void CreateFeaturePad(string objectName, Vector3 position, Vector3 scale, Sprite sprite, Color color)
        {
            CreateDecorBlock(objectName, position, scale, sprite, color, 3);
        }

        /// <summary>
        /// 허브 전용 배경 아트를 레이어별 자식 오브젝트로 배치한다.
        /// </summary>
        private static GameObject CreateHubArtSprite(string objectName, Vector3 position, Sprite sprite, int sortingOrder, Transform parent)
        {
            if (sprite == null)
            {
                return null;
            }

            return CreateDecorBlock(objectName, position, Vector3.one, sprite, Color.white, sortingOrder, parent);
        }

        private static void CreateHubTiledArtSprite(
            string objectName,
            Vector3 position,
            Vector2 worldSize,
            Vector3 localScale,
            Sprite sprite,
            int sortingOrder,
            Transform parent)
        {
            if (sprite == null)
            {
                return;
            }

            Vector2 tiledSize = new(
                Mathf.Approximately(localScale.x, 0f) ? worldSize.x : worldSize.x / localScale.x,
                Mathf.Approximately(localScale.y, 0f) ? worldSize.y : worldSize.y / localScale.y);

            CreateDecorBlock(objectName, position, localScale, sprite, Color.white, sortingOrder, parent, SpriteDrawMode.Tiled, tiledSize);
        }

        private static void CreateHubSplitBarArt(
            string objectName,
            Vector3 position,
            Sprite leftSprite,
            Sprite rightSprite,
            int sortingOrder,
            Transform parent)
        {
            GameObject root = new(objectName);
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
                ApplySceneTransformOverride(root.transform, objectName, position, Quaternion.identity, Vector3.one, useLocalSpace: true);
            }
            else
            {
                ApplySceneTransformOverride(root.transform, objectName, position, Quaternion.identity, Vector3.one, useLocalSpace: false);
            }

            CreateDecorBlock(
                HubRoomLayout.BarLeftVisualObjectName,
                HubRoomLayout.BarLeftVisualLocalPosition,
                Vector3.one,
                leftSprite,
                Color.white,
                sortingOrder,
                root.transform,
                SpriteDrawMode.Sliced,
                HubRoomLayout.BarLeftVisualSize);

            if (rightSprite != null)
            {
                CreateDecorBlock(
                    HubRoomLayout.BarRightVisualObjectName,
                    HubRoomLayout.BarRightVisualLocalPosition,
                    Vector3.one,
                    rightSprite,
                    Color.white,
                    sortingOrder,
                    root.transform,
                    SpriteDrawMode.Sliced,
                    HubRoomLayout.BarRightVisualSize);
            }

            ApplySceneActiveOverride(root, objectName);
        }

        private static GameObject CreateDecorBlock(
            string objectName,
            Vector3 position,
            Vector3 scale,
            Sprite sprite,
            Color color,
            int sortingOrder,
            Transform parent = null,
            SpriteDrawMode drawMode = SpriteDrawMode.Simple,
            Vector2? tiledSize = null)
        {
            GameObject go = new(objectName);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
                ApplySceneTransformOverride(go.transform, objectName, position, Quaternion.identity, scale, useLocalSpace: true);
            }
            else
            {
                ApplySceneTransformOverride(go.transform, objectName, position, Quaternion.identity, scale, useLocalSpace: false);
            }

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            renderer.drawMode = drawMode;
            if (tiledSize.HasValue)
            {
                renderer.size = tiledSize.Value;
            }

            ApplySceneComponentOverride(renderer, objectName);
            ApplySceneActiveOverride(go, objectName);
            return go;
        }

        /// <summary>
        /// 배경 아트 위에 상호작용만 남기고 싶은 허브 오브젝트는 렌더러와 월드 라벨을 숨긴다.
        /// 콜라이더와 상호작용 컴포넌트는 유지하므로 프롬프트와 기능은 그대로 동작한다.
        /// </summary>
        private static void HideWorldInteractionPresentation(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.enabled = false;
            }

            foreach (TextMeshPro label in root.GetComponentsInChildren<TextMeshPro>(true))
            {
                label.gameObject.SetActive(false);
            }
        }
    }
}
#endif
