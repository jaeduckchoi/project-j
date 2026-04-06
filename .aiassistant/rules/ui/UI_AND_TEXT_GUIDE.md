---
적용: 항상
---

# 종구의 식당 UI 및 텍스트 가이드

## 1. UI 방향성

UI는 탐험 화면을 과하게 가리지 않으면서도 `상태 확인 -> 판단 -> 행동` 흐름이 한눈에 읽히도록 유지해야 한다.
의도하는 시각 방향은 `갈색/회색 벡터 패널 + 더 어두운 액션 도크 + 역할별 포인트 색상`이다.

- 기본 디자인 원본: `Assets/Design/UIDesign/Vector`
- 목업과 검토 보드: `Assets/Design/UIDesign/Mockups`
- 생성 UI 원본 아트: `Assets/Design/GeneratedSources/UI`

`Assets/Design`는 디자인 원본 저장소 전용이다. 실제 게임은 생성 경로와 런타임 리소스 경로를 참조해야 한다.

## 2. 현재 HUD 구성

### 좌측 상단 상태 카드

- 현재 지역 이름
- 골드와 평판
- 플레이어가 루프 어디쯤 와 있는지 빠르게 읽을 수 있는 정보

### 상단 중앙 흐름 리본

- 현재 날짜와 단계
- 다음에 해야 할 일을 안내하는 가이드 텍스트
- 플레이 화면을 과하게 가리지 않는 넓은 단일 행 형태

### 우측 하단 액션 도크

- `Skip Exploration`
- `Skip Service`
- `Next Day`

이 구역은 정보 카드와 분리된 행동 묶음으로 명확하게 읽혀야 한다.

### 허브 팝업

- `Cooking Menu`
- `Upgrade`
- `Materials`
- `Storage`

모든 허브 팝업은 같은 프레임 구조를 공유하며, `PopupTitle`, `PopupLeftCaption`, 좌우 바디 패널 같은 공용 앵커를 사용한다.

## 3. 색상과 시각 규칙

- 상태 카드와 흐름 카드는 `amber` 포인트를 사용한다.
- 가방 또는 재료 관련 표면은 `ocean` 포인트를 사용한다.
- 창고 관련 표면은 `forest` 포인트를 사용한다.
- 영업 결과 관련 표면은 `coral` 포인트를 사용한다.
- 업그레이드 관련 표면은 `gold` 포인트를 사용한다.
- 액션 도크는 어두운 네이비 계열 바탕을 사용한다.

## 4. 텍스트 가독성 기준

- 주요 UI 텍스트는 TextMesh Pro로 통일한다.
- 카드 텍스트에는 전용 패딩과 줄 간격을 적용해 여러 줄 정보가 뭉치지 않게 한다.
- 카드 구역 구분에는 작은 볼드 캡션과 넓은 자간을 사용한다.
- 월드 라벨은 외곽선을 포함한 굵은 스타일로 유지해 복잡한 배경 위에서도 읽히게 한다.
- 여러 줄 본문 텍스트는 auto-sizing과 masking을 사용해 카드 Rect 안에 머무르게 한다.

## 5. 한글 텍스트와 폰트 기준

- 기본 TMP 본문 폰트: `Assets/Generated/Fonts/maplestoryLightSdf.asset`
- 기본 TMP 제목 폰트: `Assets/Generated/Fonts/maplestoryBoldSdf.asset`
- TMP Settings는 한글 줄바꿈이 유지되도록 설정한다.
- `malgunGothicSdf.asset`는 폴백 또는 레거시 자산으로 남아 있을 수 있지만, 현재 빌더 기본값은 메이플스토리 계열이다.

## 6. 런타임과 빌더의 역할

- `Assets/Scripts/UI/UIManager.cs`
  UI 동작과 상태 갱신의 주 런타임 진입점이다.
- `Assets/Scripts/UI/PopupPauseStateUtility.cs`
  허브 팝업의 시간 정지와 복구 값을 계산한다.
- `Assets/Scripts/UI/Controllers/PrototypeUIDesignController.cs`
  에디터 프리뷰 상태와 프리뷰 보조 기능을 관리한다.
- `Assets/Scripts/UI/Content/PrototypeUIPopupCatalog.cs`
  허브 팝업 제목, 좌우 캡션, 프리뷰 샘플 텍스트를 저장한다.
- `Assets/Scripts/UI/Layout/PrototypeUILayout.UI.cs`
  일반 HUD 레이아웃을 관리한다.
- `Assets/Scripts/UI/Layout/PrototypeUILayout.Popup.cs`
  팝업 프레임 레이아웃과 반복되는 바디 박스 레이아웃을 관리한다.
- `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutSettings.cs`
  Canvas 레이아웃과 표시값 오버라이드를 저장하고 적용한다.
- `Assets/Scripts/UI/Style/PrototypeUISkinCatalog.UI.cs`
  일반 HUD 패널과 버튼을 생성 리소스에 매핑한다.
- `Assets/Scripts/UI/Style/PrototypeUISkinCatalog.Popup.cs`
  팝업 프레임, 바디, 닫기 버튼 스킨을 매핑한다.
- `Assets/Scripts/UI/Style/PrototypeUISkin.cs`
  리소스 경로에서 스프라이트를 로드하고 9-slice 스프라이트로 다시 구성한다.
- `Assets/Editor/JongguMinimalPrototypeBuilder.cs`
  씬 생성 시 동일한 HUD/팝업 구조와 기본 폰트를 설정한다.

## 7. 에디터 프리뷰와 자동 동기화

- `PrototypeUIDesignControllerEditor`
  `Canvas Grouping`, `Apply Preview`, `Open Scene Builder Preview`, `Refresh SVG Cache`, `Reset Canvas UI Layouts`를 제공한다.
- `PrototypeUICanvasAutoSync`
  지원 Canvas 씬 저장 시 공용 UI 오버라이드 자산을 다시 동기화하고, 같은 관리 대상 변경을 다른 지원 씬에도 전파한다.
- 공용 자산 `Assets/Resources/Generated/ui-layout-overrides.asset`는 첫 동기화 시 자동 생성된다.
- `Hub` 저장 시 공용 HUD 기준이 갱신되며, 탐험 씬 저장 시에는 해당 씬에서 관리하는 값만 공용 기준 위에 덮어쓴다.
- 빌더 관리 월드 오브젝트는 지원 씬에서 같은 이름의 오브젝트 값을 저장하면 빌더가 `Transform`, 활성 상태, 월드 스프라이트·텍스트·콜라이더와 포털·지대·채집·스테이션 수치를 다시 반영한다.
- 다만 `GameManager`, `UIManager`, `blockingCollider`처럼 씬 오브젝트를 가리키는 참조는 그대로 복사하지 않고 빌더가 새 씬에서 다시 연결해야 하며, 이름이 바뀌면 동기화되지 않는다.

### Canvas UI 복구 원칙

- 과거 커밋 기준으로 Canvas UI를 복구할 때는 전체 씬 롤백보다 `ui-layout-overrides.asset`, 필요한 TMP 폰트 자산, 씬 `Canvas` 하위, `UIManager` 직렬화를 함께 맞추는 방식을 우선한다.
- `GuideHelpButton`, `ActionAccent`처럼 관리 대상 UI의 존재 여부가 바뀐 경우에는 `removedObjectNames`와 실제 씬 `Canvas` 구조, `UIManager` 직렬화가 서로 일치해야 하며, 빌더와 `UIManager`도 같은 제거 기준을 사용해야 한다.
- Unity 씬 YAML을 직접 편집할 때는 파일 맨 앞 `%YAML 1.1`, `%TAG !u! tag:unity3d.com,2011:` 헤더를 보존해야 한다.

## 8. 생성 UI 리소스 기준

- 생성 UI 원본 아트: `Assets/Design/GeneratedSources/UI`
- 생성 출력 경로: `Assets/Generated/Sprites/UI`
- 런타임 참조 경로: `Assets/Resources/Generated/Sprites/UI`

현재 이 시스템을 사용하는 공용 오브젝트 예시는 다음과 같다.

- `PopupCloseButton`
- `GuideHelpButton`
- `InteractionPromptBackdrop`
- `GuideBackdrop`
- `ResultBackdrop`
- `PopupFrame`
- `PopupFrameLeft`
- `PopupFrameRight`
- `PopupLeftBody`
- `PopupRightBody`
- `PopupLeftItemBox*`
- `PopupRightItemBox*`

## 9. 해상도 지원

- Canvas 레이아웃 기준은 `1920 x 1080`이다.
- 허브 월드 카메라도 `16:9` 고정 구도 기준을 사용한다.
- 기존 씬은 런타임에 scaler 설정을 다시 적용해야 한다.

## 10. 확인할 것

- 한글 텍스트 렌더링이 안정적인가?
- 모든 탐험 씬에서 프롬프트와 상단 가이드가 읽기 쉬운가?
- 여러 줄 텍스트가 카드 영역 밖으로 넘치지 않는가?
- 창고와 허브 팝업이 열려 있을 때 시간이 멈추는가?
- 팝업을 닫은 뒤 원래 시간 흐름이 복구되는가?
- 생성 스프라이트 참조가 `Assets/Resources/Generated`와 일치하는가?
