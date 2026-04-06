---
적용: 항상
---

# 종구의 식당 빌드 및 생성 가이드

## 1. 에디터에서 다시 빌드하는 방법

1. Unity에서 프로젝트를 연다.
2. `Tools > Jonggu Restaurant > Prototype Build and Audit`를 실행한다.
3. 빌드 단계가 끝나면 생성 씬 감사가 자동으로 이어서 실행되어야 한다.
4. 필요하면 `Light Automation Audit`를 다시 실행해 핵심 게임플레이 규칙을 재확인한다.
5. `Assets/Scenes/Hub.unity`를 열고 Play 모드에서 흐름을 검증한다.

## 2. 생성되거나 갱신되는 대상

- `Hub.unity`
- `Beach.unity`
- `DeepForest.unity`
- `AbandonedMine.unity`
- `WindHill.unity`
- 생성 자원 및 임시 자산
- 생성 스프라이트
- 생성 TMP 폰트
- Build Settings 씬 목록
- 공용 Canvas UI 오버라이드 자산

## 3. 빌드 흐름에서 수행하는 작업

- 먼저 `Hub` Canvas 값을 공용 오버라이드 자산에 동기화한다.
- 그 공용 기준을 바탕으로 `Beach`, `DeepForest`, `AbandonedMine`, `WindHill` UI를 재생성한다.
- 지원 씬 월드 계층을 `SceneWorldRoot`, `SceneGameplayRoot`, `SceneSystemRoot`, `Canvas` 기준으로 다시 정렬한다.
- `Assets/Generated/GameData`의 생성 데이터 자산을 kebab-case 파일 규칙에 맞춰 정리한다.
- `maplestoryLightSdf`, `maplestoryBoldSdf` 기준으로 본문/제목 TMP 폰트를 재생성한다.
- 런타임 데이터 매니페스트를 `Assets/Resources/Generated/generated-game-data-manifest.asset` 기준에 맞춘다.
- `Assets/Generated/Sprites`와 `Assets/Resources/Generated/Sprites` 양쪽에서 생성 스프라이트 폴더 역할을 유지한다.
- `PrototypeSceneAudit`를 실행해 생성 씬 구조와 레이아웃을 검증한다.

## 4. 메뉴 역할

- `Prototype Build and Audit`
  생성 자산 준비, 기본 씬 재빌드, 생성 씬 감사를 기본 흐름 한 번에 수행한다.
- `Rebuild Generated Assets and Scenes`
  감사 없이 생성 단계만 수행한다.
- `Run Generated Scene Audit Only`
  `Hub`, `Beach`, `DeepForest`, `AbandonedMine`, `WindHill`의 저장 구조를 다시 점검한다.
- `Organize Active Scene Hierarchy`
  현재 열려 있는 지원 씬을 공용 월드 그룹 루트 기준으로 다시 묶고 저장한다.
- `Light Automation Audit`
  day-loop, 허브 팝업 일시정지, 포탈 잠금, 누락 씬 안내 같은 핵심 규칙을 구조 감사 위에서 빠르게 확인한다.

## 5. 다시 실행해야 하는 경우

- 씬이 손상되었거나 생성 자산 참조가 누락되었을 때
- 생성 폰트나 스프라이트를 다시 만들어야 할 때
- 빌더 기반 레이아웃 또는 UI 구조를 복원해야 할 때
- 지원 Canvas 씬의 공용 관리 UI 값이 바뀌었고 그 변경을 다른 지원 씬에도 전파해야 할 때
- day-loop 규칙, 포탈 잠금, 팝업 일시정지 동작이 바뀌어 회귀 검증이 필요할 때

## 6. 주의 사항

- 빌더 출력물만 직접 수정하지 않는다. 먼저 원본 빌더 코드와 레이아웃 상수를 수정한다.
- 지원 Canvas 씬을 저장하면 Canvas 자식의 `RectTransform`, 부모 그룹과 형제 순서, 삭제 상태, `Image`, `TextMeshProUGUI`, `Button` 표시 값이 공용 자산에 자동 저장된다.
- 공용 UI 오버라이드 자산 경로는 `Assets/Resources/Generated/ui-layout-overrides.asset`다.
- 지원 씬에서 빌더 관리 오브젝트 값을 직접 조정하면 빌더는 같은 오브젝트 이름 기준으로 `Transform`, 활성 상태, 월드 `SpriteRenderer`·`TextMeshPro`·`Collider2D`, `Camera`, 포털·지대·채집·스테이션·매니저의 안전한 직렬화 값만 다시 적용한다.
- 씬 오브젝트 참조는 그대로 복사하지 않고 빌더가 새 씬에 맞게 다시 연결하므로, 값 동기화가 필요하면 빌더 관리 오브젝트 이름을 유지해야 한다.
- 지원 씬 월드 계층은 `scene/SCENE_HIERARCHY_GROUPING_RULES.md`를 따른다.
- `Prototype Build and Audit`는 먼저 `Hub` 기준을 읽고, 마지막에 현재 열려 있는 씬의 Canvas 값을 다시 적용한다.
- 빌더가 직접 생성하지 않는 런타임 전용 팝업 리소스는 `Assets/Resources/Generated/Sprites/UI` 아래에 유지해야 한다.
- 생성 씬 감사가 실패하면 빌드 흐름 전체를 실패로 간주하고 원인을 먼저 해결한다.

### Canvas UI 복구 원칙

- 과거 커밋 기준 UI 복구가 필요할 때는 전체 씬 롤백보다 `ui-layout-overrides.asset`, 필요한 TMP 폰트 자산, 지원 씬의 `Canvas` 하위와 `UIManager` 직렬화만 기준 커밋에 맞추는 방식을 우선한다.
- `GuideHelpButton`, `ActionAccent`처럼 시점에 따라 제거된 관리 대상 UI는 `ui-layout-overrides.asset`의 `removedObjectNames`, 씬 직렬화, `UIManager` 직렬화가 함께 맞아야 하며, 빌더와 `UIManager`가 같은 제거 기준을 쓰는지 확인한다.
- Unity 씬 YAML을 직접 편집할 때는 `%YAML 1.1`, `%TAG !u! tag:unity3d.com,2011:` 헤더를 보존하고, 복구 후에는 `Canvas` 바깥 직렬화가 바뀌지 않았는지 검토한다.

## 7. 텍스트와 폰트 메모

- 생성 TMP 폰트는 프로젝트 기본 폰트 기준으로 다시 만들어진다.
- 빌드 후 생성되는 UI와 월드 텍스트는 현재 TMP Settings와 빌더 기준 값을 따른다.
- 기본 본문 폰트는 `maplestoryLightSdf`, 제목 폰트는 `maplestoryBoldSdf`다.

## 8. 검증 메모

- 이 문서는 현재 메뉴 구조와 코드 기준에 맞춰 갱신되었다.
- Unity 실행과 실제 배치 컴파일은 이 작업에서 직접 검증하지 못했으므로, 이후 에디터 메뉴 검증이 추가로 필요하다.
