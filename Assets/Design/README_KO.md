# Assets/Design 관리 가이드

`Assets/Design`은 게임이 직접 읽는 런타임 자산이 아니라, 사람이 관리하는 디자인 원본과 참고 자료를 두는 폴더다.
실제 게임에서 바로 읽는 파일은 `Assets/Generated` 또는 `Assets/Resources/Generated` 기준으로 관리하고, 이 폴더는 그 결과물을 만들기 위한 원본과 참고본을 분리해 둔다.

## 현재 관리 구조

- `GeneratedSources`
  generated 결과물과 연결되는 디자인 소스를 둔다.
  현재는 `UI`, `Sprites`, `Fonts`, `Data` 기준으로 나눠 두고, 실제 파일이 있는 것은 `UI` 하위 프레임 원본이다.
- `UIDesign`
  벡터 원본, 목업, 검토용 export처럼 UI 작업용 원본 폴더를 둔다.
- `References`
  외부 참고 이미지, 인터페이스 캡처, 메모를 둔다.
- `Archive`
  현재 기준에서 직접 쓰지 않는 구안이나 보관본을 둔다.

## 관리 원칙

- 게임이 직접 읽는 파일은 `Design`에 두지 않는다.
- generated 결과물과 연결되는 디자인 소스는 `GeneratedSources`에 모은다.
- 순수 제작용 원본은 `UIDesign` 아래에서 관리한다.
- 디자인 소스 자산 파일명은 기본적으로 kebab-case로 맞춘다.
- 최종 반영은 `Generated` 또는 `Resources/Generated` 경로와 빌더 기준을 먼저 맞춘 뒤 진행한다.
