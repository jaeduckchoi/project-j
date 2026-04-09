using Exploration.Gathering;
using Exploration.Player;
using Exploration.World;
using Management.Storage;
using Management.Upgrade;
using Restaurant;
using Shared;
using TMPro;
using UI.Content;
using UI.Controllers;
using UI.Layout;
using UI.Style;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    public partial class UIManager
    {
        // HUD/Popup 시각 요소 생성과 테마 적용은 Chrome partial에 모아 탐색 경로를 고정합니다.

        /// <summary>
        /// 현재 UI 구조에 맞춰 패널, 강조선, 텍스트, 버튼 스타일을 한 번에 다시 적용합니다.
        /// </summary>
        private void ApplyTextPresentation()
        {
            bool isHubScene = IsHubScene();
            TMP_FontAsset runtimeDefaultFont = TmpFontAssetResolver.EnsureDefaultFontAsset();
            TMP_FontAsset preferredFont = TmpFontAssetResolver.ResolveFontOrDefault(
                bodyFontAsset != null
                    ? bodyFontAsset
                    : interactionPromptText != null
                        ? interactionPromptText.font
                        : runtimeDefaultFont);
            TMP_FontAsset headingFont = TmpFontAssetResolver.ResolveHeadingFontOrDefault(headingFontAsset, preferredFont);

            ApplyCanvasScaleSettings();
            EnsureCanvasGroups();
            ResolveOptionalUiReferences();
            goldText = EnsureHudStatusText(goldText, "GoldText");
            guideText = EnsureOverlayText(guideText, "GuideText");
            resultText = EnsureOverlayText(resultText, "RestaurantResultText");
            PrototypeUITheme theme = PrototypeUIThemePalette.GetForScene(SceneManager.GetActiveScene().name);

            EnsureCommonHudChrome(isHubScene, preferredFont, theme.Parchment, theme.Paper, theme.Glass);
            if (isHubScene)
            {
                EnsureHubHudChrome(preferredFont, theme.Paper, theme.Dock, theme.AmberAccent, theme.ActionText);
            }

            // 실제 HUD 배치와 텍스트 스타일은 아래 공용 레이아웃 메서드에서 한 번만 적용합니다.
            ApplyCompactHudLayout(preferredFont, headingFont, theme.Text, theme.OceanAccent, theme.AmberAccent, theme.CoralAccent, theme.GoldAccent);
            ApplyMenuPanelState();
            RefreshStoragePanelVisibility();
            ApplyWorldTextPresentation(preferredFont, headingFont);
        }

        /// <summary>
        /// 허브와 탐험 씬이 공통으로 쓰는 HUD 바탕과 캡션을 생성합니다.
        /// </summary>
        private void EnsureCommonHudChrome(
            bool isHubScene,
            TMP_FontAsset preferredFont,
            Color parchment,
            Color paper,
            Color glass)
        {
            EnsureUiBackdrop("TopLeftPanel", PrototypeUILayout.TopLeftPanel(isHubScene), parchment);
            EnsureUiBackdrop("InteractionPromptBackdrop", PrototypeUILayout.PromptBackdrop(isHubScene), Color.white);
            EnsureUiBackdrop("GuideBackdrop", PrototypeUILayout.GuideBackdrop(isHubScene), paper);
            EnsureUiBackdrop("ResultBackdrop", PrototypeUILayout.ResultBackdrop(isHubScene), paper);
            EnsureGuideHelpButton(preferredFont, isHubScene);
        }

        /// <summary>
        /// 허브에서만 필요한 팝업 카드와 진행 버튼 독을 생성합니다.
        /// </summary>
        private void EnsureHubHudChrome(
            TMP_FontAsset preferredFont,
            Color paper,
            Color nightDock,
            Color amberAccent,
            Color actionTextColor)
        {
            PrototypeUIPopupDefinition recipePopupDefinition = PrototypeUIPopupCatalog.GetDefinition(PrototypeUIPreviewPanel.Recipe);
            Color popupShell = new(1f, 1f, 1f, 0f);
            Color popupFrameLeft = Color.white;
            Color popupFrameRight = new(0.92f, 0.95f, 0.99f, 1f);
            Color popupBody = Color.white;

            EnsureUiBackdrop(HudPanelButtonGroupObjectName, PrototypeUILayout.HubPanelButtonGroup, paper);
            EnsureUiBackdrop("PopupOverlay", PrototypeUILayout.HubPopupOverlay, new Color(0f, 0f, 0f, 0.52f));
            EnsureUiBackdrop("ActionDock", PrototypeUILayout.HubActionDock, nightDock);
            EnsureUiBackdrop("PopupFrame", PrototypeUILayout.HubPopupFrame, popupShell);
            EnsureUiBackdrop("PopupFrameLeft", PrototypeUILayout.HubPopupFrameLeft, popupFrameLeft);
            EnsureUiBackdrop("PopupFrameRight", PrototypeUILayout.HubPopupFrameRight, popupFrameRight);
            EnsureUiBackdrop("PopupLeftBody", PrototypeUILayout.HubPopupFrameBody, popupBody);
            EnsureUiBackdrop("PopupRightBody", PrototypeUILayout.HubPopupFrameBody, popupBody);

            EnsureHubCoinBadgeChrome(preferredFont);
            EnsureUiAccentBar("ActionAccent", PrototypeUILayout.HubActionAccent, amberAccent);
            EnsureUiCaption("ActionCaption", "진행", PrototypeUILayout.HubActionCaption, preferredFont, actionTextColor, TextAlignmentOptions.TopRight);
            EnsureHubPopupHeadings(recipePopupDefinition, preferredFont, actionTextColor);
            EnsurePopupCloseButton(preferredFont);
        }

        /// <summary>
        /// 이름으로 찾은 패널 오브젝트에 카드형 배경과 그림자를 적용합니다.
        /// </summary>
        private void EnsureUiBackdrop(
            string objectName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color)
        {
            if (transform == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return;
            }

            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta));

            Transform existing = FindNamedUiTransform(objectName);
            Transform targetParent = existing == null ? GetCanvasGroupParent(objectName) : null;
            if (existing == null && targetParent == null)
            {
                return;
            }

            GameObject backdropObject = existing != null ? existing.gameObject : new GameObject(objectName);
            ApplyHubPopupObjectIdentity(backdropObject);
            if (existing == null)
            {
                backdropObject.transform.SetParent(targetParent, false);
            }
            else
            {
                AssignCanvasGroupParent(backdropObject.transform, objectName);
            }

            RectTransform rect = backdropObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = backdropObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = resolvedLayout.AnchorMin;
            rect.anchorMax = resolvedLayout.AnchorMax;
            rect.pivot = resolvedLayout.Pivot;
            rect.anchoredPosition = resolvedLayout.AnchoredPosition;
            rect.sizeDelta = resolvedLayout.SizeDelta;
            rect.SetSiblingIndex(0);

            Image image = backdropObject.GetComponent<Image>();
            if (image == null)
            {
                image = backdropObject.AddComponent<Image>();
            }

            bool shouldUseGeneratedUiDesign = PrototypeUISkinCatalog.UsesGeneratedUiDesignPanel(objectName);
            bool preserveSceneSprite = existing != null
                                       && IsHubPopupDisplayObject(objectName)
                                       && image.sprite != null
                                       && !shouldUseGeneratedUiDesign;
            if (!preserveSceneSprite)
            {
                PrototypeUISkin.ApplyPanel(image, objectName, color);
            }

            // generated UI 스킨은 기본값으로 적용하고, 씬에서 저장한 이미지 오버라이드가 있으면 마지막에 우선 반영한다.
            PrototypeUISceneLayoutCatalog.TryApplyImageOverride(image, objectName);

            image.raycastTarget = false;

            Shadow shadow = backdropObject.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = backdropObject.AddComponent<Shadow>();
            }

            shadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
            shadow.effectDistance = new Vector2(0f, -4f);
            shadow.useGraphicAlpha = true;
        }

        private void EnsureUiBackdrop(string objectName, PrototypeUIRect layout, Color color)
        {
            EnsureUiBackdrop(
                objectName,
                layout.AnchorMin,
                layout.AnchorMax,
                layout.Pivot,
                layout.AnchoredPosition,
                layout.SizeDelta,
                color);
        }

        /// <summary>
        /// 카드 상단 강조선을 추가해 HUD와 팝업 구획을 더 또렷하게 나눕니다.
        /// </summary>
        private void EnsureUiAccentBar(
            string objectName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color)
        {
            if (transform == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return;
            }

            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta));

            Transform existing = FindNamedUiTransform(objectName);
            Transform targetParent = existing == null ? GetCanvasGroupParent(objectName) : null;
            if (existing == null && targetParent == null)
            {
                return;
            }

            GameObject accentObject = existing != null ? existing.gameObject : new GameObject(objectName);
            if (existing == null)
            {
                accentObject.transform.SetParent(targetParent, false);
            }
            else
            {
                AssignCanvasGroupParent(accentObject.transform, objectName);
            }

            RectTransform rect = accentObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = accentObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = resolvedLayout.AnchorMin;
            rect.anchorMax = resolvedLayout.AnchorMax;
            rect.pivot = resolvedLayout.Pivot;
            rect.anchoredPosition = resolvedLayout.AnchoredPosition;
            rect.sizeDelta = resolvedLayout.SizeDelta;
            rect.SetSiblingIndex(1);

            Image image = accentObject.GetComponent<Image>();
            if (image == null)
            {
                image = accentObject.AddComponent<Image>();
            }

            image.sprite = null;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = color;
            PrototypeUISceneLayoutCatalog.TryApplyImageOverride(image, objectName);
            image.raycastTarget = false;
        }

        private void EnsureUiAccentBar(string objectName, PrototypeUIRect layout, Color color)
        {
            EnsureUiAccentBar(
                objectName,
                layout.AnchorMin,
                layout.AnchorMax,
                layout.Pivot,
                layout.AnchoredPosition,
                layout.SizeDelta,
                color);
        }

        /// <summary>
        /// 카드 제목 캡션을 생성하거나 갱신해 각 정보 구역 이름을 표시합니다.
        /// </summary>
        private void EnsureUiCaption(
            string objectName,
            string content,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            TMP_FontAsset font,
            Color color,
            TextAlignmentOptions alignment)
        {
            if (transform == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return;
            }

            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta));

            Transform existing = FindNamedUiTransform(objectName);
            Transform targetParent = existing == null ? GetCanvasGroupParent(objectName) : null;
            if (existing == null && targetParent == null)
            {
                return;
            }

            GameObject captionObject = existing != null ? existing.gameObject : new GameObject(objectName);
            ApplyHubPopupObjectIdentity(captionObject);
            TextMeshProUGUI existingText = captionObject.GetComponent<TextMeshProUGUI>();
            bool preserveExistingPopupHeading = existing != null && existingText != null && IsFixedPopupHeading(objectName);
            if (existing == null)
            {
                captionObject.transform.SetParent(targetParent, false);
            }
            else if (!preserveExistingPopupHeading)
            {
                AssignCanvasGroupParent(captionObject.transform, objectName);
            }

            RectTransform rect = captionObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = captionObject.AddComponent<RectTransform>();
            }

            if (!preserveExistingPopupHeading)
            {
                rect.anchorMin = resolvedLayout.AnchorMin;
                rect.anchorMax = resolvedLayout.AnchorMax;
                rect.pivot = resolvedLayout.Pivot;
                rect.anchoredPosition = resolvedLayout.AnchoredPosition;
                rect.sizeDelta = resolvedLayout.SizeDelta;
                rect.SetSiblingIndex(2);
            }

            TextMeshProUGUI text = existingText;
            if (text == null)
            {
                text = captionObject.AddComponent<TextMeshProUGUI>();
            }

            CaptionPresentationPreset presentation = ResolveCaptionPresentation(objectName);
            TMP_FontAsset resolvedFont = ResolveCaptionFont(objectName, font);
            text.text = content;
            if (!preserveExistingPopupHeading)
            {
                ApplyScreenTextStyle(text, resolvedFont, presentation.FontSize, color, alignment, false, 0f, presentation.Margin, presentation.EnableAutoSizing);
                text.enableAutoSizing = presentation.EnableAutoSizing;
                text.fontSizeMin = presentation.FontSizeMin;
                text.fontSizeMax = presentation.FontSizeMax;
                text.fontSize = presentation.FontSize;
                text.characterSpacing = presentation.CharacterSpacing;
            }
            else if (resolvedFont != null && text.font != resolvedFont)
            {
                text.font = resolvedFont;
                if (resolvedFont.material != null)
                {
                    text.fontSharedMaterial = resolvedFont.material;
                }
            }

            text.raycastTarget = false;
            ApplySceneTextOverride(text);
        }

        private void EnsureUiCaption(
            string objectName,
            string content,
            PrototypeUIRect layout,
            TMP_FontAsset font,
            Color color,
            TextAlignmentOptions alignment)
        {
            EnsureUiCaption(
                objectName,
                content,
                layout.AnchorMin,
                layout.AnchorMax,
                layout.Pivot,
                layout.AnchoredPosition,
                layout.SizeDelta,
                font,
                color,
                alignment);
        }

        private TextMeshProUGUI EnsureOverlayText(TextMeshProUGUI current, string objectName)
        {
            if (PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return null;
            }

            if (current != null)
            {
                AssignCanvasGroupParent(current.transform, objectName);
                return current;
            }

            Transform existing = FindNamedUiTransform(objectName);
            Transform targetParent = existing == null ? GetCanvasGroupParent(objectName) : null;
            if (existing == null && targetParent == null)
            {
                return null;
            }

            GameObject textObject = existing != null ? existing.gameObject : new GameObject(objectName);
            ApplyHubPopupObjectIdentity(textObject);
            if (existing == null)
            {
                textObject.transform.SetParent(targetParent, false);
            }
            else
            {
                AssignCanvasGroupParent(textObject.transform, objectName);
            }

            RectTransform rect = textObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = textObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(920f, 60f);
            rect.SetAsLastSibling();

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = textObject.AddComponent<TextMeshProUGUI>();
            }

            text.raycastTarget = false;
            return text;
        }

        private TextMeshProUGUI EnsureHudStatusText(TextMeshProUGUI current, string objectName)
        {
            if (PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return null;
            }

            if (current != null)
            {
                AssignCanvasGroupParent(current.transform, objectName);
                return current;
            }

            Transform existing = FindNamedUiTransform(objectName);
            Transform targetParent = existing == null ? GetCanvasGroupParent(objectName) : null;
            if (existing == null && targetParent == null)
            {
                return null;
            }

            GameObject textObject = existing != null ? existing.gameObject : new GameObject(objectName);
            if (existing == null)
            {
                textObject.transform.SetParent(targetParent, false);
            }
            else
            {
                AssignCanvasGroupParent(textObject.transform, objectName);
            }

            RectTransform rect = textObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = textObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-34f, -22f);
            rect.sizeDelta = new Vector2(116f, 34f);
            rect.SetAsLastSibling();

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = textObject.AddComponent<TextMeshProUGUI>();
            }

            text.raycastTarget = false;
            return text;
        }

        private void EnsureHubCoinBadgeChrome(TMP_FontAsset font)
        {
            Transform badgeTransform = FindNamedUiTransform("TopLeftPanel");
            if (badgeTransform == null)
            {
                return;
            }

            Image badgeImage = badgeTransform.GetComponent<Image>();
            if (badgeImage != null)
            {
                ApplyGeneratedPanelSprite(badgeImage, "dark-thin-outline-panel", HubCoinBadgeOuterColor);
                badgeImage.raycastTarget = false;
            }

            Shadow badgeShadow = badgeTransform.GetComponent<Shadow>();
            if (badgeShadow != null)
            {
                badgeShadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
                badgeShadow.effectDistance = new Vector2(0f, -3f);
                badgeShadow.useGraphicAlpha = true;
            }

            EnsureHubCoinBadgeInnerFill(badgeTransform);
            EnsureHubCoinBadgeIcon(badgeTransform, font);
            SetHubCoinBadgeVisualState(true);
        }

        private void EnsureHubCoinBadgeInnerFill(Transform badgeTransform)
        {
            Transform existing = badgeTransform.Find(TopLeftPanelInnerObjectName);
            GameObject fillObject = existing != null ? existing.gameObject : new GameObject(TopLeftPanelInnerObjectName);
            if (existing == null)
            {
                fillObject.transform.SetParent(badgeTransform, false);
            }

            RectTransform rect = fillObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = fillObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = new Vector2(4f, 4f);
            rect.offsetMax = new Vector2(-4f, -4f);
            rect.SetSiblingIndex(0);

            Image image = fillObject.GetComponent<Image>();
            if (image == null)
            {
                image = fillObject.AddComponent<Image>();
            }

            ApplyGeneratedPanelSprite(image, "dark-solid-panel", HubCoinBadgeInnerColor);
            image.raycastTarget = false;
        }

        private void EnsureHubCoinBadgeIcon(Transform badgeTransform, TMP_FontAsset font)
        {
            Transform existing = badgeTransform.Find(TopLeftPanelCoinIconObjectName);
            GameObject iconObject = existing != null ? existing.gameObject : new GameObject(TopLeftPanelCoinIconObjectName);
            if (existing == null)
            {
                iconObject.transform.SetParent(badgeTransform, false);
            }

            RectTransform rect = iconObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = iconObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(28f, 0f);
            rect.sizeDelta = new Vector2(34f, 34f);
            rect.SetAsLastSibling();

            TextMeshProUGUI iconText = iconObject.GetComponent<TextMeshProUGUI>();
            if (iconText == null)
            {
                iconText = iconObject.AddComponent<TextMeshProUGUI>();
            }

            ApplyScreenTextStyle(iconText, font, 26f, HubCoinIconColor, TextAlignmentOptions.Center, false, 0f, Vector4.zero, false);
            iconText.text = "◉";
            iconText.enableAutoSizing = false;
            iconText.raycastTarget = false;
            iconText.overflowMode = TextOverflowModes.Overflow;
            iconText.fontStyle = FontStyles.Bold;

            Shadow iconShadow = iconObject.GetComponent<Shadow>();
            if (iconShadow == null)
            {
                iconShadow = iconObject.gameObject.AddComponent<Shadow>();
            }

            iconShadow.effectColor = new Color(0.33f, 0.18f, 0.04f, 0.92f);
            iconShadow.effectDistance = new Vector2(1f, -1f);
            iconShadow.useGraphicAlpha = true;
        }

        private void SetHubCoinBadgeVisualState(bool isActive)
        {
            Transform innerFill = FindNamedUiTransform(TopLeftPanelInnerObjectName);
            if (innerFill != null)
            {
                innerFill.gameObject.SetActive(isActive);
            }

            Transform icon = FindNamedUiTransform(TopLeftPanelCoinIconObjectName);
            if (icon != null)
            {
                icon.gameObject.SetActive(isActive);
            }
        }

        private static void ApplyGeneratedPanelSprite(Image image, string spriteName, Color color)
        {
            if (image == null)
            {
                return;
            }

            string resourcePath = $"{PrototypeGeneratedAssetSettings.GetCurrent().GeneratedUiPanelResourceRoot}/{spriteName}";
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                sprite = Resources.Load<Sprite>($"Generated/Sprites/UI/Panels/{spriteName}");
            }

            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.preserveAspect = false;
            }
            else
            {
                image.sprite = null;
                image.type = Image.Type.Simple;
            }

            image.color = color;
        }

        /// <summary>
        /// 허브 팝업 우측 상단에 닫기 버튼을 생성하거나 다시 스타일링합니다.
        /// </summary>
        private void EnsurePopupCloseButton(TMP_FontAsset font)
        {
            if (transform == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved("PopupCloseButton"))
            {
                return;
            }

            Transform existing = FindNamedUiTransform("PopupCloseButton");
            Transform targetParent = existing == null ? GetCanvasGroupParent("PopupCloseButton") : null;
            if (existing == null && targetParent == null)
            {
                popupCloseButton = null;
                return;
            }

            GameObject buttonObject = existing != null ? existing.gameObject : new GameObject("PopupCloseButton");
            ApplyHubPopupObjectIdentity(buttonObject);
            if (existing == null)
            {
                buttonObject.transform.SetParent(targetParent, false);
            }
            else
            {
                AssignCanvasGroupParent(buttonObject.transform, "PopupCloseButton");
            }

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = buttonObject.AddComponent<RectTransform>();
            }

            ApplyRectLayout(rect, PrototypeUILayout.HubPopupCloseButton);
            rect.SetAsLastSibling();

            Image image = buttonObject.GetComponent<Image>();
            if (image == null)
            {
                image = buttonObject.AddComponent<Image>();
            }

            Button button = buttonObject.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObject.AddComponent<Button>();
            }

            button.targetGraphic = image;
            popupCloseButton = button;
            ApplyButtonPresentation(button, font, Color.white);

            TextMeshProUGUI label = buttonObject.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null)
            {
                GameObject labelObject = new("PopupCloseButton_Label");
                ApplyHubPopupObjectIdentity(labelObject);
                labelObject.transform.SetParent(buttonObject.transform, false);

                RectTransform labelRect = labelObject.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition = Vector2.zero;
                labelRect.sizeDelta = Vector2.zero;

                label = labelObject.AddComponent<TextMeshProUGUI>();
            }

            label.text = string.Empty;
            label.gameObject.SetActive(false);
        }

        private void EnsureGuideHelpButton(TMP_FontAsset font, bool isHubScene)
        {
            if (transform == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved("GuideHelpButton"))
            {
                return;
            }

            Transform existing = FindNamedUiTransform("GuideHelpButton");
            Transform targetParent = existing == null ? GetCanvasGroupParent("GuideHelpButton") : null;
            if (existing == null && targetParent == null)
            {
                guideHelpButton = null;
                return;
            }

            GameObject buttonObject = existing != null ? existing.gameObject : new GameObject("GuideHelpButton");
            if (existing == null)
            {
                buttonObject.transform.SetParent(targetParent, false);
            }
            else
            {
                AssignCanvasGroupParent(buttonObject.transform, "GuideHelpButton");
            }

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = buttonObject.AddComponent<RectTransform>();
            }

            ApplyRectLayout(rect, PrototypeUILayout.GuideHelpButton(isHubScene));
            rect.SetAsLastSibling();

            Image image = buttonObject.GetComponent<Image>();
            if (image == null)
            {
                image = buttonObject.AddComponent<Image>();
            }

            Button button = buttonObject.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObject.AddComponent<Button>();
            }

            button.targetGraphic = image;
            guideHelpButton = button;
            ApplyButtonPresentation(button, font, Color.white);

            TextMeshProUGUI label = buttonObject.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = string.Empty;
                label.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 텍스트나 버튼의 위치와 크기를 지정된 값으로 다시 맞춥니다.
        /// </summary>
        private static void ApplyRectLayout(RectTransform rect, PrototypeUIRect layout)
        {
            ApplyRectLayout(
                rect,
                layout.AnchorMin,
                layout.AnchorMax,
                layout.Pivot,
                layout.AnchoredPosition,
                layout.SizeDelta);
        }

        private static void ApplyRectLayout(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        /// <summary>
        /// UI 텍스트의 공통 폰트, 줄바꿈, 여백, 굵기 규칙을 한곳에서 통일합니다.
        /// </summary>
        private static void ApplyScreenTextStyle(
            TextMeshProUGUI text,
            TMP_FontAsset font,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment,
            bool allowWrap,
            float lineSpacing,
            Vector4 margin,
            bool bold)
        {
            if (text == null)
            {
                return;
            }

            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.margin = margin;
            text.lineSpacing = lineSpacing;
            text.isRightToLeftText = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = allowWrap ? Mathf.Max(12f, fontSize - 7f) : Mathf.Max(13f, fontSize - 6f);
            text.fontSizeMax = fontSize;
            text.textWrappingMode = allowWrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.overflowMode = allowWrap ? TextOverflowModes.Masking : TextOverflowModes.Truncate;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.characterSpacing = bold ? 0.35f : 0f;
            text.wordSpacing = 0f;

            if (allowWrap)
            {
                text.paragraphSpacing = 2f;
            }
            else
            {
                text.paragraphSpacing = 0f;
            }
        }

        /// <summary>
        /// 씬에서 저장한 TMP 표시 값이 있으면 기본 HUD 스타일 적용 뒤 마지막에 다시 덮어씁니다.
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
        /// 씬에서 저장한 버튼과 라벨 표시 값이 있으면 기본 스타일 적용 뒤 마지막에 다시 덮어씁니다.
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

        /// <summary>
        /// 버튼 RectTransform을 조정해 허브 하단과 우측 액션 영역 배치를 맞춥니다.
        /// </summary>
        private static void ApplyButtonLayout(Button button, PrototypeUIRect layout)
        {
            if (button == null)
            {
                return;
            }

            RectTransform rect = button.GetComponent<RectTransform>();
            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(button.name, layout);
            ApplyRectLayout(rect, resolvedLayout);
        }

        /// <summary>
        /// 버튼 배경색과 라벨 스타일을 현재 HUD 톤에 맞게 다시 입힙니다.
        /// </summary>
        private void ApplyButtonPresentation(Button button, TMP_FontAsset font, Color accentColor)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            bool hasKenneySkin = false;
            if (image != null)
            {
                hasKenneySkin = PrototypeUISkin.ApplyButton(image, button.name, accentColor);
                if (PrototypeUISceneLayoutCatalog.TryApplyImageOverride(image, button.name))
                {
                    hasKenneySkin = true;
                }
            }

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            ApplyScreenTextStyle(label, font, 20f, Color.white, TextAlignmentOptions.Center, false, 0f, new Vector4(8f, 6f, 8f, 6f), true);

            Shadow shadow = button.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = button.gameObject.AddComponent<Shadow>();
            }

            shadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
            shadow.effectDistance = new Vector2(0f, -3f);
            shadow.useGraphicAlpha = true;

            ColorBlock colors = button.colors;
            if (hasKenneySkin)
            {
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 1f, 1f, 0.96f);
                colors.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
                colors.selectedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
                colors.disabledColor = new Color(1f, 1f, 1f, 0.42f);
            }
            else
            {
                colors.normalColor = accentColor;
                colors.highlightedColor = Color.Lerp(accentColor, Color.white, 0.14f);
                colors.pressedColor = Color.Lerp(accentColor, Color.black, 0.18f);
                colors.selectedColor = Color.Lerp(accentColor, Color.white, 0.10f);
                colors.disabledColor = new Color(accentColor.r * 0.55f, accentColor.g * 0.55f, accentColor.b * 0.55f, 0.45f);
            }

            colors.fadeDuration = 0.08f;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = colors;
            ApplySceneButtonOverride(button);
        }

        /// <summary>
        /// 월드 라벨은 한글 폰트, 굵기, 외곽선 기준을 통일해 장면마다 읽기 쉽게 만듭니다.
        /// </summary>
        private static void ApplyWorldTextPresentation(TMP_FontAsset bodyFont, TMP_FontAsset headingFont)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            TextMeshPro[] worldTexts = FindObjectsByType<TextMeshPro>(FindObjectsSortMode.None);
            foreach (TextMeshPro worldText in worldTexts)
            {
                if (worldText == null)
                {
                    continue;
                }

                bool isLargeLabel = worldText.fontSize >= 3.4f;
                TMP_FontAsset font = isLargeLabel ? headingFont : bodyFont;
                if (font != null)
                {
                    worldText.font = font;
                }

                worldText.enableAutoSizing = false;
                bool isPrimaryLabel = worldText.fontSize >= 2.5f;
                worldText.characterSpacing = isLargeLabel ? 0.22f : isPrimaryLabel ? 0.08f : 0.02f;
                worldText.wordSpacing = 0f;
                worldText.lineSpacing = 0f;
                worldText.fontStyle = isLargeLabel || isPrimaryLabel ? FontStyles.Bold : FontStyles.Normal;
                float labelScale = isLargeLabel ? 0.30f : isPrimaryLabel ? 0.27f : 0.25f;
                worldText.transform.localScale = Vector3.one * labelScale;
                ApplyCompactWorldLabelOffset(worldText);

                float luminance = (worldText.color.r * 0.299f) + (worldText.color.g * 0.587f) + (worldText.color.b * 0.114f);
                worldText.outlineWidth = isLargeLabel ? 0.10f : isPrimaryLabel ? 0.08f : 0.06f;
                worldText.outlineColor = luminance < 0.45f
                    ? new Color(1f, 1f, 1f, 0.90f)
                    : new Color(0f, 0f, 0f, 0.88f);
            }
        }

        private static void ApplyCompactWorldLabelOffset(TextMeshPro worldText)
        {
            if (worldText == null || worldText.transform.parent == null)
            {
                return;
            }

            Transform parent = worldText.transform.parent;
            float? compactY = null;

            if (parent.GetComponent<ScenePortal>() != null)
            {
                compactY = 0.82f;
            }
            else if (parent.GetComponent<RecipeSelectorStation>() != null || parent.GetComponent<ServiceCounterStation>() != null)
            {
                compactY = 0.80f;
            }
            else if (parent.GetComponent<StorageStation>() != null)
            {
                compactY = 0.72f;
            }
            else if (parent.GetComponent<UpgradeStation>() != null)
            {
                compactY = 0.68f;
            }
            else if (parent.GetComponent<GatherableResource>() != null)
            {
                compactY = 0.64f;
            }
            else if (parent.GetComponent<PlayerController>() != null)
            {
                compactY = 0.46f;
            }

            if (!compactY.HasValue)
            {
                return;
            }

            Vector3 localPosition = worldText.transform.localPosition;
            worldText.transform.localPosition = new Vector3(localPosition.x, compactY.Value, localPosition.z);
        }
    }
}
