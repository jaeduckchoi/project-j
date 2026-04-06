---
적용: 항상
---

# 종구의 식당 UI 그룹 규칙

## 1. 목적

이 문서는 런타임 UI, 에디터 프리뷰, 기본 UI 빌더가 함께 사용하는 `Canvas` 자식 그룹 구조를 정의한다.
허브 팝업을 수동으로 정리하거나 새 UI 오브젝트를 추가할 때도 같은 기준을 따른다.

## 2. 최상위 규칙

- `Canvas` 바로 아래에는 `HUDRoot`와 `PopupRoot`만 둔다.
- HUD 관련 오브젝트는 `HUDRoot` 아래에 둔다.
- 허브 팝업 관련 오브젝트는 `PopupRoot` 아래에 둔다.
- `InventoryText`는 허브 팝업 본문 요소로만 유지하며 `PopupRoot`의 측면 구조 아래에 둔다.

권장 구조는 다음과 같다.

```text
Canvas
├─ HUDRoot
└─ PopupRoot
```

## 3. `HUDRoot` 자식 그룹

- `HUDRoot` 아래 오브젝트는 역할 기준으로 나눈다.
- 그룹 이름은 고정한다.
- 허브 진행 버튼은 `ActionDock` 아래에 두고, `HUDBottomGroup`은 공용 Canvas 그룹 슬롯으로 유지한다.
- `HUDInventoryGroup`, `HUDButtonGroup` 같은 레거시 그룹은 남기지 않는다.

권장 구조는 다음과 같다.

```text
Canvas
└─ HUDRoot
   ├─ HUDStatusGroup
   │  ├─ TopLeftPanel
   │  ├─ TopLeftAccent
   │  ├─ PhaseBadge
   │  ├─ GoldText
   │  └─ DayPhaseText
   ├─ HUDActionGroup
   │  ├─ ActionDock
   │  │  ├─ ActionCaption
   │  │  ├─ SkipExplorationButton
   │  │  ├─ SkipServiceButton
   │  │  └─ NextDayButton
   │  └─ ActionAccent
   ├─ HUDBottomGroup
   ├─ HUDPanelButtonGroup
   │  ├─ RecipePanelButton
   │  ├─ UpgradePanelButton
   │  └─ MaterialPanelButton
   ├─ InteractionPromptBackdrop
   ├─ InteractionPromptText
   └─ HUDOverlayGroup
      ├─ GuideBackdrop
      ├─ GuideText
      ├─ GuideHelpButton
      ├─ ResultBackdrop
      └─ RestaurantResultText
```

## 4. `PopupRoot` 자식 그룹

- `PopupOverlay`는 `PopupShellGroup` 아래에 둔다.
- 팝업 본문은 하나의 `PopupFrame`을 중심으로 관리한다.
- `PopupFrame`은 제목, 닫기 버튼, 좌우 반쪽 프레임을 묶는다.
- `PopupFrameHeader`는 레거시 호환 그룹으로 남을 수 있으나, 현재 활성 기준은 `PopupFrame` 내부 구조다.

권장 구조는 다음과 같다.

```text
Canvas
└─ PopupRoot
   ├─ PopupShellGroup
   │  └─ PopupOverlay
   ├─ PopupFrame
   │  ├─ PopupTitle
   │  ├─ PopupCloseButton
   │  ├─ PopupFrameLeft
   │  │  ├─ PopupLeftCaption
   │  │  ├─ PopupLeftBody
   │  │  │  ├─ PopupLeftItemBox01
   │  │  │  │  └─ PopupLeftItemText01
   │  │  │  ├─ PopupLeftItemBox02
   │  │  │  │  └─ PopupLeftItemText02
   │  │  │  ├─ PopupLeftItemBox03
   │  │  │  │  └─ PopupLeftItemText03
   │  │  │  └─ PopupLeftItemBox04
   │  │  │     └─ PopupLeftItemText04
   │  │  └─ InventoryText
   │  └─ PopupFrameRight
   │     ├─ PopupRightCaption
   │     ├─ PopupRightBody
   │     │  ├─ PopupRightItemBox01
   │     │  │  └─ PopupRightItemText01
   │     │  ├─ PopupRightItemBox02
   │     │  │  └─ PopupRightItemText02
   │     │  ├─ PopupRightItemBox03
   │     │  │  └─ PopupRightItemText03
   │     │  └─ PopupRightItemBox04
   │     │     └─ PopupRightItemText04
   │     ├─ StorageText
   │     ├─ SelectedRecipeText
   │     └─ UpgradeText
   └─ PopupFrameHeader
```

## 5. 네이밍 규칙

- 최상위 루트는 `HUDRoot`, `PopupRoot`를 유지한다.
- HUD 그룹 이름은 `HUDStatusGroup`, `HUDActionGroup`, `HUDBottomGroup`, `HUDPanelButtonGroup`, `HUDOverlayGroup`를 유지한다.
- 팝업 그룹 이름은 `PopupShellGroup`, `PopupFrame`, `PopupFrameLeft`, `PopupFrameRight`, `PopupFrameHeader`를 유지한다.
- 좌우 본문은 `PopupLeftBody`, `PopupRightBody`를 사용한다.
- 반복 아이템 박스는 `PopupLeftItemBox01`, `PopupRightItemBox01`처럼 두 자리 번호를 사용한다.
- 각 아이템 박스에는 대응되는 `ItemText`가 정확히 하나씩 있어야 한다.

## 6. 편집 규칙

- `Apply Preview`는 스타일과 레이아웃 프리뷰만 갱신해야 한다.
- 실제 계층 재그룹화는 `Canvas Grouping`을 통해 수행한다.
- 허브 팝업 구조가 바뀌면 `Assets/Scripts/UI/UIManager.cs`와 `Assets/Editor/JongguMinimalPrototypeBuilder.cs`를 함께 갱신한다.
- 팝업 프레임 구조가 바뀌면 `Assets/Scripts/UI/Layout/PrototypeUILayout.Popup.cs`와 `Assets/Scripts/UI/Style/PrototypeUISkinCatalog.Popup.cs`도 함께 갱신한다.
- 지원 씬 저장 시 관리 대상 값도 자동 동기화되므로, 수동 그룹 재정리 후 저장 결과를 다시 확인한다.

## 7. 체크리스트

- `Canvas`가 `HUDRoot`와 `PopupRoot`로 직접 정리되어 있는가?
- HUD 오브젝트가 올바른 HUD 자식 그룹 아래에 배치되어 있는가?
- `PopupOverlay`가 `PopupShellGroup` 아래에 있는가?
- `PopupTitle`, `PopupCloseButton`, `PopupFrameLeft`, `PopupFrameRight`가 모두 `PopupFrame` 아래에 있는가?
- `PopupLeftBody`, `PopupRightBody`가 올바른 반쪽 프레임 안에 있는가?
- `ItemBox -> ItemText` 반복 구조가 유지되는가?
- `Canvas Grouping` 이후 수동 관리 그룹이 안정적으로 유지되는가?
