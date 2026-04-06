---
적용: 항상
---

# 종구의 식당 빌드 및 생성 가이드

## 1. 에디터에서 다시 빌드하는 방법

1. Unity에서 프로젝트를 연다.
2. `Tools > Jonggu Restaurant > Prototype Build and Audit`를 실행한다.
3. 빌드 단계가 끝나면 생성 씬 감사가 자동으로 이어서 실행되어야 한다.
4. `Assets/Scenes/Hub.unity`를 열고 Play 모드에서 흐름을 검증한다.

## 2. 생성되거나 갱신되는 대상

- 누락된 지원 씬 파일만 최소한으로 다시 생성
- 생성 자원 및 임시 자산
- 생성 스프라이트
- 생성 TMP 폰트
- Build Settings 씬 목록
- 공용 Canvas UI 오버라이드 자산
- 기존 지원 씬에 저장된 월드 직렬화 값 유지

## 3. 빌드 흐름에서 수행하는 작업

- 먼저 열려 있는 지원 씬의 dirty 상태를 저장해 에디터에서 직접 조정한 정적 값을 씬 직렬화에 확정한다.
- 먼저 `Hub` Canvas 값을 공용 오버라이드 자산에 동기화한다.
- 현재 열려 있는 지원 씬이 있으면 그 Canvas 마지막 조정값을 공용 오버라이드 위에 다시 덮어쓴다.
- `Assets/Resources/Generated/GameData`의 생성 데이터 자산을 kebab-case 파일 규칙에 맞춰 정리한다.
- `maplestoryLightSdf`, `maplestoryBoldSdf` 기준으로 본문/제목 TMP 폰트를 재생성한다.
- 런타임 데이터 매니페스트를 `Assets/Resources/Generated/generated-game-data-manifest.asset` 기준에 맞춘다.
- 생성 스프라이트 폴더는 `Assets/Resources/Generated/Sprites` 기준으로 유지한다.
- 기존 지원 씬은 다시 쓰지 않고, 누락된 씬 파일만 기본 빌더 구조로 복구한다.
- 런타임은 위 지원 씬에 저장된 값을 정본으로 사용하고, 빌더는 동기화와 누락 복구만 담당한다.
- Build Settings 씬 목록을 현재 지원 씬 경로에 맞춰 다시 동기화한다.
- `PrototypeSceneAudit`를 실행해 생성 씬 구조와 레이아웃을 검증한다.

## 4. 메뉴 역할

- `Prototype Build and Audit`
  생성 자산, Canvas 오버라이드, Build Settings, 누락된 지원 씬 복구, 생성 씬 감사를 한 번에 동기화한다.
- 보조 유지보수 메뉴는 기본 UI에서 노출하지 않고 코드 내부 경로로 유지한다.

## 5. 다시 실행해야 하는 경우

- 씬이 손상되었거나 생성 자산 참조가 누락되었을 때
- 생성 폰트나 스프라이트를 다시 만들어야 할 때
- 누락된 지원 씬 파일을 기본 구조로 복구해야 할 때
- 지원 Canvas 씬의 공용 관리 UI 값이 바뀌었고 그 변경을 다른 지원 씬에도 전파해야 할 때
- Build Settings 씬 목록과 generated 데이터 자산 경로를 다시 맞춰야 할 때

## 6. 주의 사항

- 빌더 출력물만 직접 수정하지 않는다. 먼저 원본 빌더 코드와 레이아웃 상수를 수정한다.
- 지원 Canvas 씬을 저장하면 Canvas 자식의 `RectTransform`, 부모 그룹과 형제 순서, 삭제 상태, `Image`, `TextMeshProUGUI`, `Button` 표시 값이 공용 자산에 자동 저장된다.
- 공용 UI 오버라이드 자산 경로는 `Assets/Resources/Generated/ui-layout-overrides.asset`다.
- 메인 빌드는 기존 지원 씬을 재생성하지 않으므로, 에디터에서 지원 씬에 직접 저장한 정적 값이 정본이다.
- 런타임 보강은 누락된 오브젝트, 누락된 컴포넌트, 끊어진 참조만 보충하고 기존 씬 값은 덮어쓰지 않는다.
- 누락된 지원 씬을 복구할 때만 빌더가 같은 오브젝트 이름 기준의 안전한 직렬화 값과 참조 재연결 규칙을 사용한다.
- 지원 씬 월드 계층은 `scene/SCENE_HIERARCHY_GROUPING_RULES.md`를 따른다.
- `Prototype Build and Audit`는 먼저 `Hub` 기준을 읽고, 마지막에 현재 열려 있는 씬의 Canvas 값을 다시 적용한다.
- 빌더가 직접 생성하지 않는 런타임 전용 팝업 리소스는 `Assets/Resources/Generated/Sprites/UI` 아래에 유지해야 한다.
- 허브 월드 아트를 교체할 때는 `Assets/Resources/Generated/Sprites/Hub`를 갱신하고, `HubRoomLayout`, `PrototypeSceneRuntimeAugmenter`, `JongguMinimalPrototypeBuilder`의 배치 기준과 자산 경로를 같은 구조로 맞춘다.
- `HubWallBackground`와 `HubFrontOutline`는 빌드 시 생성 PNG와 리소스 복사 경로가 함께 갱신되는지 검토한다.
- 허브 벽 아트 정렬이 어긋나면 생성 PNG만 고치지 말고 `JongguMinimalPrototypeBuilder`의 타일 배치 값, 배경/전경 위치, 리소스 경로를 먼저 수정한 뒤 다시 빌드한다.
- 허브 바닥 타일은 `1 월드 유닛 = 32 px` 기준을 유지하는지, 허브 카운터는 `HubBarLeftVisual`/`HubBarRightVisual` 분리 구조와 각 파츠의 `spriteBorder`가 맞는지 함께 확인한다.
- 생성 씬 감사가 실패하면 빌드 흐름 전체를 실패로 간주하고 원인을 먼저 해결한다.

### Canvas UI 복구 원칙

- 과거 커밋 기준 UI 복구가 필요할 때는 전체 씬 롤백보다 `ui-layout-overrides.asset`, 필요한 TMP 폰트 자산, 지원 씬의 `Canvas` 하위와 `UIManager` 직렬화만 기준 커밋에 맞추는 방식을 우선한다.
- `GuideHelpButton`, `ActionAccent`처럼 시점에 따라 제거된 관리 대상 UI는 `ui-layout-overrides.asset`의 `removedObjectNames`, 씬 직렬화, `UIManager` 직렬화가 함께 맞아야 하며, 빌더와 `UIManager`가 같은 제거 기준을 쓰는지 확인한다.
- Unity 씬 YAML을 직접 편집할 때는 `%YAML 1.1`, `%TAG !u! tag:unity3d.com,2011:` 헤더를 보존하고, 복구 후에는 `Canvas` 바깥 직렬화가 바뀌지 않았는지 검토한다.

## 7. 텍스트와 폰트 메모

- 생성 TMP 폰트는 프로젝트 기본 폰트 기준으로 다시 만들어진다.
- 빌드 후 새로 생성된 UI와 월드 텍스트는 현재 TMP Settings와 빌더 기준 값을 따른다.
- 기본 본문 폰트는 `maplestoryLightSdf`, 제목 폰트는 `maplestoryBoldSdf`다.

## 8. 검증 메모

- 이 문서는 현재 메뉴 구조와 코드 기준에 맞춰 갱신되었다.
- Unity 실행과 실제 배치 컴파일은 이 작업에서 직접 검증하지 못했으므로, 이후 에디터 메뉴 검증이 추가로 필요하다.

