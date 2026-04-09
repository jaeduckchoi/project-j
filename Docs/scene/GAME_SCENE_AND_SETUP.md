# 씬 및 설정 가이드

## 기본 원칙

- `Assets/Scenes`의 실제 씬 직렬화 값이 정본입니다.
- 런타임 코드는 누락된 오브젝트, 컴포넌트, 참조만 보강해야 합니다.

## 주요 기준

- 씬 구조 정리: `Assets/Editor/PrototypeSceneHierarchyOrganizer.cs`
- 월드 레이아웃 상수: 관련 `Exploration/World/*` 코드
- UI 구조: `Assets/Scripts/UI/UIManager.cs`, `Assets/Resources/Generated/ui-layout-overrides.asset`

## 검증

- 구조 감사와 경량 게임플레이 감사 메뉴는 제거되었습니다.
- 현재는 씬 직렬화, 관련 런타임 코드, 정적 검색 결과를 기준으로 검증합니다.
- Unity 실행이나 컴파일을 직접 확인하지 못했다면 결과 보고에 그 사실을 함께 적습니다.
