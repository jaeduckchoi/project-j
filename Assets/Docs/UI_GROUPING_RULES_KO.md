# 종구네식당 UI 그룹 규칙

## 1. 목적

이 문서는 현재 런타임, 에디터 프리뷰, 기본 UI 빌더가 공통으로 따르는 `Canvas` 하위 그룹 구조를 정리한다.
허브 팝업과 HUD를 손으로 정리하거나 새 UI를 추가할 때도 아래 규칙을 기준으로 맞춘다.

## 2. 최상위 규칙

- `Canvas` 바로 아래에는 `HUDRoot`, `PopupRoot`만 둔다.
- HUD 계열 오브젝트는 `HUDRoot` 아래에 둔다.
- 허브 팝업 계열 오브젝트는 `PopupRoot` 아래에 둔다.
- `InventoryText`는 예외적으로 허브 씬에서는 `PopupRoot`, 탐험 씬에서는 `HUDRoot` 기준으로 배치된다.

권장 구조:

```text
Canvas
├─ HUDRoot
└─ PopupRoot
```

## 3. HUDRoot 하위 그룹

- `HUDRoot` 아래 오브젝트는 역할별 하위 그룹으로 나눈다.
- 그룹 이름은 고정한다.

권장 구조:

```text
Canvas
└─ HUDRoot
   ├─ HUDStatusGroup
   │  ├─ TopLeftPanel
   │  ├─ TopLeftAccent
   │  ├─ PhaseBadge
   │  ├─ GoldText
   │  └─ DayPhaseText
   ├─ HUDInventoryGroup
   │  ├─ InventoryCard
   │  ├─ InventoryAccent
   │  ├─ InventoryCaption
   │  └─ InventoryText
   ├─ HUDActionGroup
   │  ├─ CenterBottomPanel
   │  ├─ ActionDock
   │  ├─ ActionAccent
   │  └─ ActionCaption
   ├─ HUDButtonGroup
   │  ├─ SkipExplorationButton
   │  ├─ SkipServiceButton
   │  ├─ NextDayButton
   │  ├─ RecipePanelButton
   │  ├─ UpgradePanelButton
   │  └─ MaterialPanelButton
   ├─ HUDPromptGroup
   │  ├─ PromptBackdrop
   │  └─ InteractionPromptText
   └─ HUDOverlayGroup
      ├─ GuideBackdrop
      ├─ GuideText
      ├─ ResultBackdrop
      └─ RestaurantResultText
```

## 4. PopupRoot 하위 그룹

- `PopupOverlay`는 `PopupShellGroup` 아래에 둔다.
- 실제 팝업 본체는 `PopupFrame` 하나를 기준으로 관리한다.
- `PopupFrame` 안에서 제목, 닫기 버튼, 좌우 반프레임을 다시 묶는다.
- `PopupFrameLeft`, `PopupFrameRight`는 단순 그룹 이름이 아니라 실제 반쪽 프레임 오브젝트다.
- `PopupFrameHeader`는 기존 씬 호환용 빈 그룹으로 남을 수 있지만, 새 배치 기준은 `PopupFrame` 내부다.

권장 구조:

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

## 5. 이름 규칙

- 최상위 루트는 `HUDRoot`, `PopupRoot`를 유지한다.
- HUD 하위 그룹 이름은 `HUDStatusGroup`, `HUDInventoryGroup`, `HUDActionGroup`, `HUDButtonGroup`, `HUDPromptGroup`, `HUDOverlayGroup`를 유지한다.
- 팝업 그룹 이름은 `PopupShellGroup`, `PopupFrame`, `PopupFrameLeft`, `PopupFrameRight`, `PopupFrameHeader`를 유지한다.
- 좌우 바디는 `PopupLeftBody`, `PopupRightBody`를 사용한다.
- 반복 박스는 `PopupLeftItemBox01`, `PopupRightItemBox01`처럼 2자리 번호를 사용한다.
- 박스 안 텍스트는 같은 번호의 `ItemText` 1개만 둔다.

## 6. 편집 규칙

- `현재 설정 프리뷰 적용`은 스타일과 레이아웃 프리뷰만 갱신한다.
- 실제 계층 재배치는 `Canvas 그룹 정리`에서만 수행한다.
- 허브 팝업을 수정하면 `Assets/Scripts/UI/UIManager.cs`와 `Assets/Editor/JongguMinimalPrototypeBuilder.cs`를 함께 맞춘다.
- 팝업 프레임 구조를 바꾸면 `Assets/Scripts/UI/Layout/Popup/PrototypeUILayout.Popup.cs`와 `Assets/Scripts/UI/Style/Popup/PrototypeUISkinCatalog.Popup.cs`도 같이 갱신한다.

## 7. 체크리스트

- `Canvas` 아래가 `HUDRoot`, `PopupRoot`로 정리되어 있는가
- HUD 오브젝트가 올바른 HUD 하위 그룹 아래에 들어가는가
- `PopupOverlay`가 `PopupShellGroup` 아래에 있는가
- `PopupTitle`, `PopupCloseButton`, `PopupFrameLeft`, `PopupFrameRight`가 `PopupFrame` 아래에 있는가
- `PopupLeftBody`, `PopupRightBody`가 각 반프레임 안에 들어가는가
- `ItemBox -> ItemText` 반복 구조가 유지되는가
- `Canvas 그룹 정리` 후에도 직접 만든 수동 그룹이 의도치 않게 풀리지 않는가
