# 빌드 및 생성 기준

## 기본 메뉴

현재 기본 유지보수 흐름은 `Tools > Jonggu Restaurant > Prototype Build and Audit` 하나입니다.
이 메뉴는 생성 자산 동기화, Build Settings 정리, Canvas 오버라이드 동기화, 누락된 지원 씬 복구, 구조 감사를 한 번에 수행하는 기본 경로입니다.

## 빌더의 책임

빌더는 아래 항목만 책임집니다.

- generated 자산과 리소스 경로 동기화
- `Assets/Resources/Generated/ui-layout-overrides.asset` 동기화
- Build Settings 씬 목록 정리
- 누락된 지원 씬 복구
- `PrototypeSceneAudit` 실행

빌더는 아래 항목의 정본이 아닙니다.

- 기존 지원 씬에 이미 저장된 월드 직렬화 값
- 기존 지원 씬의 정적 배치를 다시 밀어넣는 작업
- generated 결과물만 직접 고쳐서 해결하는 방식

## generated 경로 기준

- generated 자산 루트의 실제 기준은 `Assets/Resources/Generated/prototype-generated-asset-settings.asset`입니다.
- 코드 기준은 `Assets/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`입니다.
- UI 입력 원본은 `Assets/Design/GeneratedSources/UI/{Buttons,MessageBoxes,PanelVariants}`입니다.
- UI 출력 경로는 `Assets/Resources/Generated/Sprites/UI/{Buttons,MessageBoxes,Panels}`입니다.
- 글꼴과 데이터 출력도 같은 설정 자산 기준으로 정렬합니다.

## 빌드 흐름에서 지켜야 할 점

- 지원 씬에 직접 저장한 값은 먼저 씬 저장으로 확정한 뒤 빌더를 실행합니다.
- `Hub` Canvas 기준을 먼저 공용 오버라이드 자산으로 동기화합니다.
- 현재 열려 있는 다른 지원 씬이 있으면 해당 씬 관리 값만 마지막에 다시 반영합니다.
- 기존 지원 씬은 재생성하지 않고, 누락된 씬만 안전한 기본 구조로 복구합니다.
- runtime augmenter는 누락 보강만 하며, 이미 저장된 씬 값을 기본적으로 덮어쓰지 않습니다.
- 지원 씬 Canvas가 비어 있으면 build/auto-sync는 이를 전체 managed UI 삭제로 취급하지 않고, `UIManager` editor preview 기준 baseline을 캡처합니다.
- `HubUpgradeSlotLeft/Center/Right`와 가격 텍스트는 빌드 경로에서 더 이상 재생성하지 않습니다.

## 다시 실행해야 할 때

- generated 스프라이트, 폰트, 데이터 경로가 어긋났을 때
- Build Settings 씬 목록을 다시 맞춰야 할 때
- 지원 Canvas 씬 저장 결과를 다른 씬에도 반영해야 할 때
- 누락된 지원 씬을 복구해야 할 때
- `PrototypeSceneAudit` 기준 구조가 깨졌을 때

## 고위험 변경

- 허브 아트 교체 시 `Assets/Design` 원본, `Assets/Resources/Generated/Sprites/Hub`, `HubRoomLayout`, `PrototypeSceneRuntimeAugmenter`, `JongguMinimalPrototypeBuilder`, 지원 씬 직렬화를 함께 확인합니다.
- `HubWallBackground`, `HubFrontOutline`를 조정할 때는 생성 PNG와 리소스 복사 경로를 같이 확인합니다.
- 허브 바닥 타일은 기본적으로 `1 월드 유닛 = 32 px` 기준을 유지합니다.
- 허브 카운터 비주얼은 `HubBarLeftVisual`, `HubBarRightVisual` 분리 구조와 각 파츠 `spriteBorder`를 함께 맞춥니다.

## 실패 처리

- generated 결과물이 이상해도 결과물만 직접 수정하지 말고 생성 경로와 빌더 코드를 먼저 고칩니다.
- `PrototypeSceneAudit`가 실패하면 빌드 성공으로 간주하지 않습니다.
- Unity 실행이나 컴파일을 직접 확인하지 못했다면 최종 보고에 그 사실을 남깁니다.