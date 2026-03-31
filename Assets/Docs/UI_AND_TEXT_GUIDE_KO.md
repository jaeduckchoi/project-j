# 종구의 식당 UI 및 텍스트 문서

## 1. UI 디자인 방향

UI는 탐험 화면을 가리지 않으면서도 `상태 확인 -> 의사결정 -> 실행` 흐름이 한눈에 보이도록 카드형 구조로 정리한다.
전체 인상은 `brown/grey 벡터 패널 + 짙은 액션 독 + 기능별 포인트 컬러` 조합을 기준으로 잡는다.
기본 스킨 원본은 `Assets/Design/UIDesign/Vector` 에 정리하고, 화면 시안은 `Assets/Design/UIDesign/Mockups` 에 따로 둔다.
즉 `Assets/Design` 은 디자인 원본과 검토 자료 보관용이고, 실제 게임에서 직접 쓰는 리소스는 생성 경로 또는 런타임 리소스 경로로 별도 관리한다.
벡터 원본을 수정했더라도 곧바로 런타임 반영으로 이어지지 않으므로, 실제 적용 시에는 빌더와 리소스 경로를 함께 확인한다.

## 2. 현재 HUD 배치

### 좌상단 상태 카드

- 현재 지역 이름
- 골드 / 평판
- 플레이어가 지금 어디에 있고 어떤 진행 상태인지 바로 읽는 영역

### 상단 중앙 흐름 리본

- 현재 날짜 / 단계
- 오늘 해야 할 일 안내 문구
- 넓은 한 줄 리본 형태로 두어 탐험 중에도 중앙 시야를 크게 막지 않음

### 좌하단 2단 카드

- `가방` 카드
- `창고` 카드
- 같은 계열 정보는 좌측에 세로로 묶어 자원 정리 흐름을 분명히 보이게 함
- 창고 카드는 근접 자동 노출이 아니라 `StorageStation` 에서 `E`를 눌렀을 때 팝업으로 연다.

### 우측 3단 카드

- `오늘의 메뉴`
- `영업 결과`
- `업그레이드`
- 장사와 성장 정보는 우측에 모아 허브 복귀 후 의사결정 구간을 빠르게 읽게 함

### 하단 중앙 프롬프트 필

- 현재 상호작용 키 안내
- 어두운 배경 위 흰 글자로 고정해 어떤 지역에서도 바로 읽히도록 함

### 우하단 액션 독

- 탐험 스킵
- 장사 스킵
- 다음 날
- 일반 정보 카드와 분리해서 `행동 버튼 묶음` 이라는 성격이 바로 보이게 함

## 3. 컬러와 시각 규칙

- 상태 / 흐름 카드 상단 바는 `앰버`
- 가방 카드는 `오션`
- 창고 카드는 `포레스트`
- 영업 결과 카드는 `코럴`
- 업그레이드 카드는 `골드`
- 버튼 독은 어두운 남청색 바탕을 써서 정보 카드와 층위를 분리한다

## 4. 텍스트 가독성 기준

- 주요 UI 텍스트는 TextMesh Pro 기반으로 통일한다.
- 카드 텍스트는 여백과 줄간격을 따로 줘서 다중 줄 정보가 뭉치지 않게 한다.
- 캡션은 작은 볼드체와 넓은 자간으로 카드 구역을 빠르게 구분하게 한다.
- 월드 라벨은 굵은 글자와 외곽선을 사용해 바닥색에 덜 묻히도록 한다.
- 다중 줄 본문 텍스트는 자동 축소 + 마스킹 기준으로 카드 Rect 밖으로 넘치지 않게 유지한다.
- 한 줄 텍스트와 버튼 라벨도 자동 축소 + 말줄임표 기준을 써서 작은 해상도에서 튀어나오지 않게 한다.

## 5. 한글 처리 기준

- TMP 기본 폰트는 `MalgunGothic SDF` 기준으로 통일한다.
- TMP 설정에서 한글 줄바꿈 규칙을 켠다.
- generated 폰트가 다시 생성돼도 같은 경로를 기본 폰트로 참조하게 유지한다.

## 6. 런타임과 빌더 역할 분리

- `Assets/Scripts/UI/UIManager.cs`
  플레이 중 실제 UI 동작과 상태 갱신을 담당하는 메인 진입점이다.
- `Assets/Scripts/UI/Controllers/PrototypeUIDesignController.cs`
  편집 모드 프리뷰와 디자인 확인용 상태를 따로 들고, `UIManager`에 현재 프리뷰 패널을 전달한다.
  Canvas 내부 오브젝트를 `HUDRoot`, `PopupRoot`로 정리하는 기준도 같이 맞춘다.
- `Assets/Scripts/UI/Content/PrototypeUIPopupCatalog.cs`
  허브 팝업 제목, 좌우 캡션, 편집 모드 샘플 문구를 한곳에서 관리한다.
- `Assets/Scripts/UI/Layout/PrototypeUILayout.cs`
  `PrototypeUIRect` 공용 타입과 레이아웃 진입점을 유지한다.
- `Assets/Scripts/UI/Layout/PrototypeUILayout.UI.cs`
  일반 HUD와 허브 기본 UI 배치를 모아 관리한다.
- `Assets/Scripts/UI/Layout/PrototypeUILayout.Popup.cs`
  허브 팝업 프레임, 본문, 닫기 버튼 배치와 본문 내부 반복 아이템 박스 레이아웃을 모아 관리한다.
- `Assets/Scripts/UI/Style/PrototypeUISkinCatalog.cs`
  UI/HUD와 Popup 스킨 매핑의 공용 진입점을 유지한다.
- `Assets/Scripts/UI/Style/PrototypeUISkinCatalog.UI.cs`
  일반 HUD 패널과 버튼의 SVG 매핑을 관리한다.
- `Assets/Scripts/UI/Style/PrototypeUISkinCatalog.Popup.cs`
  팝업 외곽, 내부 본문, 닫기 버튼의 SVG 매핑을 관리한다.
- `Assets/Scripts/UI/Style/PrototypeUISkin.cs`
  `PrototypeUISkinCatalog`가 정한 UI 리소스 경로를 기준으로 스프라이트를 읽어 9-slice 스프라이트로 다시 만들어 런타임/빌더 공용으로 적용한다. 실제 렌더링과 캐시만 담당한다.
- `Assets/Scripts/UI/Style/PrototypeUITheme.cs`
  씬별 색상 테마와 공통 UI 색 기준을 관리한다.
- `Assets/Editor/UI/PrototypeUIDesignControllerEditor.cs`
  인스펙터에서 현재 SVG 매핑 경로를 바로 확인하고, `Canvas Grouping`, `Apply Preview`, `Refresh SVG Cache`를 실행한다.
- `Assets/Editor/UI/UIManagerEditor.cs`
  런타임 UI 매니저에서 `Canvas 그룹 정리`와 디자인 컨트롤러 연결/선택 진입점을 제공한다.
- `Assets/Editor/JongguMinimalPrototypeBuilder.cs`
  새로 씬을 생성할 때도 같은 카드 구조, SVG 버튼/패널 스킨, 텍스트 마스킹 기준과 디자인 컨트롤러 구성이 기본으로 나오게 한다.
- `Assets/TextMesh Pro/Resources/TMP Settings.asset`
  한글 폰트와 줄바꿈 규칙을 전역 기본값으로 유지한다.

## 7. 편집 모드 프리뷰

- `PrototypeUIDesignController` 인스펙터의 `Editor Preview` 항목에서 허브 팝업 프리뷰 여부와 대상 패널을 고를 수 있다.
- 현재 씬의 `Canvas` 아래 오브젝트는 `HUDRoot`, `PopupRoot`로 정리하고, 같은 기준으로 런타임 탐색과 새 씬 생성도 맞춘다.
- `현재 설정 프리뷰 적용` 버튼을 누르면 Play 모드 없이도 Scene 뷰에서 카드와 팝업 스킨 배치를 바로 확인할 수 있다.
- `씬 빌더 미리보기` 버튼을 누르면 현재 열린 지원 씬에 허브 월드 장식과 지역 보강 오브젝트까지 다시 적용해 UI 프리뷰와 함께 볼 수 있다.
- 허브 팝업 본문은 좌우 각각 여러 개의 아이템 박스로 나뉘며, 편집 모드 프리뷰도 같은 박스 구조를 기준으로 보인다.
- `Canvas Grouping` 버튼을 누르면 기존 평면 구조 Canvas도 관리용 그룹 구조로 재배치된다.
- `Refresh SVG Cache` 버튼은 `PrototypeUISkin`이 만든 임시 스프라이트 캐시를 비우고 다시 렌더링한다.
- 지원하는 Canvas 씬을 저장하면 현재 씬 Canvas 아래 UI `RectTransform` 값과 `Image.sprite/type/color/preserveAspect`, `TextMeshProUGUI`, `Button` 표시 값이 `Assets/Resources/Generated/ui-layout-overrides.asset` 자산에 자동 저장된다.
- `Hub` 저장 시 공용 UI 오버라이드와 탐험 씬 HUD 기준이 함께 갱신되고, 탐험 씬 저장 시에는 현재 씬 값만 공용 오버라이드 위에 자동으로 덮어쓴다.
- 이 자산은 빌더, 런타임 `UIManager`, 자동 감사 코드가 함께 읽으므로, 씬에서 맞춘 Canvas UI 배치와 Image 표시값을 다음 빌드와 런타임에도 유지할 때 사용한다.
- 기본 HUD와 허브 팝업 일부는 `Assets/Design/GeneratedSources/UI` 원본을 `Assets/Generated/Sprites/UI`, `Assets/Resources/Generated/Sprites/UI` 로 미러링한 generated 스프라이트를 우선 사용한다.
- `PopupCloseButton`, `GuideHelpButton`, `InteractionPromptBackdrop`, `GuideBackdrop`, `ResultBackdrop`, `PopupFrame`, `PopupFrameLeft`, `PopupFrameRight`, `PopupLeftBody`, `PopupRightBody`, `PopupLeftItemBox*`, `PopupRightItemBox*` 는 위 generated UI 스프라이트 기준으로 맞춘다.
- 위 오브젝트도 씬 저장으로 자동 동기화된 Image 오버라이드가 있으면 generated 기본값 위에 마지막으로 덮어쓴다.

## 10. 디자인 소스 후보 매핑

- `Assets/Design/GeneratedSources/UI/Buttons/close-button.png`
  현재 허브 팝업의 `PopupCloseButton` 후보 원본이다.
- `Assets/Design/GeneratedSources/UI/Buttons/help-button.png`
  `GuideText` 옆 도움말 버튼이나 허브 팝업 헤더 보조 도움말 버튼 후보 원본이다.
- `Assets/Design/GeneratedSources/UI/MessageBoxes/system-text-box.png`
  허브 팝업 본문 `PopupLeftBody`, `PopupRightBody` 또는 시스템 안내 카드 `GuideBackdrop`, `ResultBackdrop` 후보 원본이다.
- `Assets/Design/GeneratedSources/UI/MessageBoxes/interaction-text-box.png`
  탐험 씬 `InteractionPromptText` 배경이나 향후 NPC 대화 말풍선 후보 원본이다.
- 빌더는 위 원본을 `Assets/Generated/Sprites/UI/Buttons|MessageBoxes|Panels` 와 `Assets/Resources/Generated/Sprites/UI/Buttons|MessageBoxes|Panels` 로 복사하고, 씬 생성과 런타임 `UIManager` 는 해당 generated 경로를 직접 읽는다.

## 8. 해상도 대응

- 캔버스는 `1920 x 1080` 기준 해상도에 맞춰 스케일한다.
- 허브 월드 카메라도 `16:9` 고정 화면 구도를 기준으로 계산하고, 배경/오브젝트/전경 레이어가 같은 월드 좌표계를 공유한다.
- 폭과 높이 비중을 반반으로 맞춰 16:9 기준 배치가 과하게 무너지지 않게 한다.
- 기존 씬도 런타임에서 스케일러 설정을 다시 적용한다.

## 9. 확인할 포인트

- 에디터 Play 기준 한글이 깨지지 않는지
- 탐험 지역마다 프롬프트와 상단 안내가 충분히 읽히는지
- 가방 / 창고 / 메뉴 / 업그레이드 다중 줄 텍스트가 카드 밖으로 과하게 넘치지 않는지
- 창고 패널이 허브에서 `E` 상호작용으로 열리고 `Esc` 로 닫히며, 열려 있는 동안 시간이 멈추는지
- 버튼 독이 우하단 카드와 겹치지 않는지
- 월드 라벨이 밝은 바닥과 어두운 바닥 모두에서 읽히는지

