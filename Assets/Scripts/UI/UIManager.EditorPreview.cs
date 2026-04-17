#if UNITY_EDITOR
using System.Collections.Generic;
using UI.Content.Catalog;
using UI.Controllers;
using UI.Style;
using UnityEngine;

namespace UI
{
    public partial class UIManager
    {
        public void ApplyEditorDesignPreview(bool showPopupPreview, PrototypeUIPreviewPanel previewPanel)
        {
            if (Application.isPlaying)
            {
                return;
            }

            PrototypeUISkin.ClearGeneratedCache();
            bool previousPreserveEditorLayoutState = preserveExistingEditorLayoutDuringPreview;
            preserveExistingEditorLayoutDuringPreview = true;
            EnsureEditorPreviewCanvasHierarchy();
            bool previousSuppressState = suppressCanvasGroupingInEditorPreview;
            suppressCanvasGroupingInEditorPreview = true;

            try
            {
                ResolveOptionalUiReferences();
                ApplyTextPresentation();
                ApplyEditorPreviewState(showPopupPreview, previewPanel);
            }
            finally
            {
                suppressCanvasGroupingInEditorPreview = previousSuppressState;
                preserveExistingEditorLayoutDuringPreview = previousPreserveEditorLayoutState;
            }
        }

        public void ClearEditorDesignPreview()
        {
            ApplyEditorDesignPreview(false, PrototypeUIPreviewPanel.None);
        }

        public void OrganizeCanvasHierarchyInEditor()
        {
            if (Application.isPlaying)
            {
                return;
            }

            bool previousPreserveEditorLayoutState = preserveExistingEditorLayoutDuringPreview;
            preserveExistingEditorLayoutDuringPreview = true;

            try
            {
                EnsureCanvasGroups();
                ResolveOptionalUiReferences();
            }
            finally
            {
                preserveExistingEditorLayoutDuringPreview = previousPreserveEditorLayoutState;
            }
        }

        private void EnsureEditorPreviewCanvasHierarchy()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (HasManagedCanvasHierarchyInEditor() && !NeedsHubResourcePanelEditorRefresh())
            {
                return;
            }

            EnsureCanvasGroups();
        }

        private bool NeedsHubResourcePanelEditorRefresh()
        {
            if (!IsHubScene() || transform == null)
            {
                return false;
            }

            Transform resourcePanel = FindNamedUiTransform(HubResourcePanelObjectName);
            Transform resourceAmountText = FindNamedUiTransform(HubResourceAmountTextObjectName);
            bool hasResourcePanel = resourcePanel != null;
            bool hasResourceAmountText = resourceAmountText != null;
            bool hasLegacyObjects = FindNamedUiTransform(LegacyHubResourcePanelObjectName) != null
                                    || FindNamedUiTransform(LegacyHubResourceAmountTextObjectName) != null
                                    || FindNamedUiTransform(LegacyHubResourcePanelInnerObjectName) != null
                                    || FindNamedUiTransform(LegacyHubResourcePanelCoinIconObjectName) != null;
            bool panelIsInactive = resourcePanel != null && !resourcePanel.gameObject.activeSelf;
            bool amountTextIsInactive = resourceAmountText != null && !resourceAmountText.gameObject.activeSelf;
            bool hasBadgeChildren = resourcePanel != null
                                    && resourcePanel.Find(HubResourcePanelInnerObjectName) != null
                                    && resourcePanel.Find(HubResourcePanelCoinIconObjectName) != null;
            return !hasResourcePanel
                   || !hasResourceAmountText
                   || hasLegacyObjects
                   || panelIsInactive
                   || amountTextIsInactive
                   || !hasBadgeChildren;
        }

        private bool HasManagedCanvasHierarchyInEditor()
        {
            if (transform == null)
            {
                return false;
            }

            bool hasBaseHierarchy = transform.Find(HudRootName) != null
                                    && transform.Find(PopupRootName) != null
                                    && transform.Find($"{HudRootName}/{HudStatusGroupName}") != null
                                    && transform.Find($"{PopupRootName}/{PopupShellGroupName}") != null;
            if (!hasBaseHierarchy)
            {
                return false;
            }

            return !IsHubScene()
                   || (transform.Find($"{HudRootName}/{HudPanelButtonGroupObjectName}") != null
                       && transform.Find($"{PopupRootName}/{PopupFrameGroupName}") != null);
        }

        private void ApplyEditorPreviewState(bool showPopupPreview, PrototypeUIPreviewPanel previewPanel)
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (goldText != null && string.IsNullOrWhiteSpace(goldText.text))
            {
                goldText.text = IsHubScene() ? "120" : "골드 120 · 평판 4";
            }

            if (interactionPromptText != null && string.IsNullOrWhiteSpace(interactionPromptText.text))
            {
                interactionPromptText.text = defaultPromptText;
            }

            activeHubPanel = IsHubScene() && showPopupPreview ? ConvertPreviewPanel(previewPanel) : HubPopupPanel.None;
            ApplyMenuPanelState();

            if (guideText != null)
            {
                guideText.text = activeHubPanel == HubPopupPanel.None
                    ? IsHubScene()
                        ? "하단 버튼과 기본 HUD 배치를 편집 모드에서 확인하는 프리뷰입니다."
                        : "탐험 HUD 카드 배치를 편집 모드에서 확인하는 프리뷰입니다."
                    : "현재 팝업 스킨과 캡션 배치를 편집 모드에서 확인하는 프리뷰입니다.";
                guideText.gameObject.SetActive(true);
                SetNamedObjectActive("GuideBackdrop", true);
            }

            if (resultText != null)
            {
                bool showResultPreview = !IsHubScene() && activeHubPanel == HubPopupPanel.None;
                resultText.text = showResultPreview ? "탐험 및 영업 결과 문구가 이 위치에 표시됩니다." : string.Empty;
                resultText.gameObject.SetActive(showResultPreview);
                SetNamedObjectActive("ResultBackdrop", showResultPreview);
            }

            if (activeHubPanel != HubPopupPanel.None)
            {
                ApplyEditorPreviewPopupText(previewPanel);
            }
            else if (!IsHubScene() && inventoryText != null)
            {
                inventoryText.text = PrototypeUIPopupCatalog.GetExplorationInventoryPreviewText();
            }
        }

        private void ApplyEditorPreviewPopupText(PrototypeUIPreviewPanel previewPanel)
        {
            PrototypeUIPreviewContent previewContent = PrototypeUIPopupCatalog.GetPreviewContent(previewPanel);
            PopupPanelContent popupContent = BuildPreviewPopupContent(previewContent);

            if (inventoryText != null)
            {
                inventoryText.text = string.Empty;
            }

            if (selectedRecipeText != null)
            {
                selectedRecipeText.text = popupContent.DetailText;
            }

            RefreshPopupBodyItemBoxes(popupContent.Entries);
            RefreshRefrigeratorPopupSlots(activeHubPanel == HubPopupPanel.Refrigerator ? popupContent.Entries : null);
        }

        private static PopupPanelContent BuildPreviewPopupContent(PrototypeUIPreviewContent previewContent)
        {
            List<PopupListEntry> entries = new();
            if (!string.IsNullOrWhiteSpace(previewContent.LeftText))
            {
                string normalized = previewContent.LeftText.Replace("\r\n", "\n").Replace('\r', '\n');
                string[] lines = normalized.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string trimmed = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(trimmed))
                    {
                        continue;
                    }

                    string title = trimmed.StartsWith("- ")
                        ? trimmed.Substring(2).Trim()
                        : trimmed;
                    title = title.Replace("[선택] ", string.Empty);
                    entries.Add(new PopupListEntry(
                        $"preview:{i}",
                        title,
                        "편집 모드 프리뷰 항목",
                        previewContent.RightText,
                        null,
                        entries.Count == 0,
                        null));
                }
            }

            return new PopupPanelContent(entries, previewContent.RightText);
        }

        private static HubPopupPanel ConvertPreviewPanel(PrototypeUIPreviewPanel previewPanel)
        {
            return previewPanel switch
            {
                PrototypeUIPreviewPanel.None => HubPopupPanel.None,
                PrototypeUIPreviewPanel.Storage => HubPopupPanel.Storage,
                PrototypeUIPreviewPanel.Refrigerator => HubPopupPanel.Refrigerator,
                PrototypeUIPreviewPanel.Recipe => HubPopupPanel.Recipe,
                PrototypeUIPreviewPanel.Upgrade => HubPopupPanel.Upgrade,
                PrototypeUIPreviewPanel.Materials => HubPopupPanel.Materials,
                _ => HubPopupPanel.None
            };
        }

    }
}
#endif
