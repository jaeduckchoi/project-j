# GeneratedSources UI 자산 정리

이 폴더는 `Assets/Resources/Generated`에 반영하거나 참고하는 UI 디자인 소스를 모아 둔 위치다.
현재는 프레임, 버튼, 메시지 박스 원본을 먼저 정리해 두었다.

## 분류 기준

- `Buttons`
  UI 버튼 아이콘이나 보조 버튼 원본을 둔다.
- `MessageBoxes`
  시스템 텍스트, NPC 상호작용 메시지 같은 텍스트 박스 원본을 둔다.
- `PanelVariants`
  패널 외곽과 배경 스타일 후보를 둔다.

## 현재 매핑

- `Buttons/close-button.png`
  원본 `X_button.png`
  `PopupCloseButton` 후보
- `Buttons/help-button.png`
  원본 `how_button.png`
  `GuideText` 옆 도움말 버튼 또는 허브 팝업 헤더 보조 도움말 버튼 후보
- `MessageBoxes/system-text-box.png`
  원본 `text box.png`
  `PopupLeftBody`, `PopupRightBody`, `GuideBackdrop`, `ResultBackdrop` 후보
- `MessageBoxes/interaction-text-box.png`
  원본 `text box2.png`
  `InteractionPromptText` 배경 또는 향후 NPC 대화 말풍선 후보

## 패널 바리에이션

- `PanelVariants/dark-outline-panel.png`
  원본 `frame1.png`
- `PanelVariants/dark-thin-outline-panel.png`
  원본 `frame2.png`
- `PanelVariants/dark-solid-panel.png`
  원본 `frame3.png`
- `PanelVariants/light-outline-panel.png`
  원본 `frame4.png`
- `PanelVariants/dark-outline-panel-alt.png`
  원본 `frame5.png`
- `PanelVariants/light-solid-panel.png`
  원본 `frame6.png`

## 주의사항

- 이 폴더는 디자인 소스 보관 위치이고, 런타임이 직접 읽는 경로는 아니다.
- 실제 반영이 필요하면 `Assets/Resources/Generated`와 빌더 경로를 함께 맞춘다.
- 실제 후보 매핑은 `ui-candidate-map.json` 파일에도 함께 정리한다.
