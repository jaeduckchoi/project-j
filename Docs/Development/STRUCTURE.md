# Unreal 소스·애셋 구조

## 원칙

게임 런타임은 Blueprint, Python은 에디터 제작·검증으로 유지합니다. Unreal이 검색하는 `Content/Python`에 `jonggu` 패키지를 두고, 호스트 런처와 기존 월드 작성 도구는 `Tools`에 둡니다. JSON 배치 정본은 `SourceData`, 글꼴 원본·라이선스는 `SourceAssets`, 실행 결과는 무시되는 `Saved`에 보관합니다. 캐시 폴더에 작성 원본을 두지 않습니다.

이 구조는 Epic의 [디렉터리 구조](https://dev.epicgames.com/documentation/unreal-engine/unreal-engine-directory-structure)와 [에디터 Python 경로](https://dev.epicgames.com/documentation/unreal-engine/scripting-the-unreal-editor-using-python)를 따릅니다. 제작 라이브러리의 import 자체가 빌드·임포트·맵 변경을 실행하지 않도록 실행 진입점과 구분합니다. `validation/verify_*`에는 실행 드라이버도 있으므로 일반 라이브러리처럼 임의로 import하지 않습니다.

## 디렉터리

아래 파일 경로는 **프로젝트 루트 기준 상대 경로**입니다. 애셋 표의 `Jonggu/` 경로는 `Content/` 기준이며 `/Game/`은 Unreal의 패키지 루트입니다.

| 경로 | 내용 |
|---|---|
| [Config](../../Config/) | 프로젝트·입력·렌더·저장 경로 호환 설정 |
| [Content/Jonggu](../../Content/Jonggu/) | 저장된 게임 Blueprint·맵·UI·아트 |
| [Content/Python/jonggu](../../Content/Python/jonggu/) | 에디터 제작 패키지 |
| [SourceData/Restaurant](../../SourceData/Restaurant/) | 상호작용·기본 도착·손님 배치 입력 |
| [SourceAssets/Fonts](../../SourceAssets/Fonts/) | 글꼴 원본과 라이선스 |
| [Tools/Unreal](../../Tools/Unreal/) | 공통 실행·호스트 소스 검사·고정 fixture |
| [Tools/World](../../Tools/World/README.md) | 기존 플레이어·충돌·정렬 작성과 검증 입력 |
| [Docs](../README.md) | 현재 설계·제작·검증과 과거 기록 |
| `Saved/` | 로컬 저장·로그·검증 결과·복구용 보관물. 버전 관리 제외 |

## 애셋 이름과 경로

| 경로 | 역할·예시 |
|---|---|
| `Jonggu/Core` | `BP_RestaurantGameInstance`, `BP_RestaurantGameMode`, `BP_RestaurantPlayerController` |
| `Jonggu/Core/Save` | `BP_PrototypeSave` |
| `Jonggu/Gameplay/Restaurant` | `BP_RestaurantManager` |
| `Jonggu/Gameplay/Interaction` | `BP_RestaurantInteractable`, `Materials/M_InteractionText` |
| `Jonggu/Data/Recipes` | `BP_RecipeDefinition`, `BP_Recipe_KimchiFriedRice`, `BP_Recipe_ClamStew` 등 |
| `Jonggu/Data/Runtime` | `BP_FoodOrder`, `BP_PreparedDish` |
| `Jonggu/UI/Blueprints` | `BP_RestaurantHUD` |
| `Jonggu/UI/Textures` | `T_UI_PopupFrame`, `T_UI_KimchiStew` 등 |
| `Jonggu/UI/Fonts` | `FF_Galmuri11_Regular` / `F_Galmuri11_Regular`, Bold 쌍 |

[에셋 명명 권장사항](https://dev.epicgames.com/documentation/en-us/unreal-engine/recommended-asset-naming-conventions-in-unreal-engine-projects)에 따라 `BP_`, `T_`, `M_` 등 실제 에셋 종류를 나타내는 접두사를 사용합니다. 요리·주문 자료는 현재 UObject Blueprint이므로 `DA_`나 `ST_`로 오인시키지 않습니다. 음식 숫자 인덱스 대신 요리 이름을 파일명에 사용합니다.

[Content/Python/jonggu/assets.py](../../Content/Python/jonggu/assets.py)의 `ASSETS`가 작성 경로 정본입니다. `asset_path`, `object_path`, `generated_class_path`로 패키지·오브젝트·생성 클래스 경로를 구분합니다. 기존 경로는 `LEGACY_ASSET_PATHS`와 Config의 CoreRedirects에 호환 기록으로만 둡니다. 새 파일의 목적지는 항상 ASSETS입니다.

2026-09-15 구조 검증 당시 이관된 텍스처·스프라이트 511개의 해시 이름은 Unity 원본 ID와 manifest 대응에 쓰이므로 유지합니다. 기존 플레이어 프레임 18개, 맵·카메라·충돌도 현재 계약을 유지합니다. 이 라이브러리에서 게임 전용으로 사용하는 UI 사본에 읽기 쉬운 이름을 부여했습니다.

## 코드 분리

아래 패키지 경로는 `Content/Python/jonggu/` 기준입니다. 실제 수정·실행 방법과 Hub 귀환 위치의 고정 좌표 예외는 [제작 안내](AUTHORING.md)에 있습니다.

- `blueprints/graph.py`: 변수·노드·분기·캐스트·컴파일·저장 도구. 캐스트의 기반 클래스는 이름 문자열이 아닌 실제 타입으로 판별합니다.
- `gameplay/manager.py`: Blueprint 변수와 함수를 등록하는 조립 코드. `service`, `cooking`, `input`, `presentation`이 각각 동작을 작성하고 `constants`·`graph_helpers`가 공유 계약을 제공합니다.
- `gameplay/session.py`: 구간 저장·복원·날짜·채집. `data.py`는 순수 요리 정의와 명시적으로 호출하는 자료형 생성기를 제공합니다.
- `ui/contracts.py`, `layout.py`: 상태·입력·표시·가림 공통 정의. `primitives`, `renderer`, `panels`, `theme`, `visibility`는 Canvas 도구·일반 HUD·팝업·색·투명화를 분리합니다.
- `world/build.py`: 기존 맵에 식당 기능을 조립합니다. Controller·상호작용·Actor 식별·INI 처리는 별도 모듈입니다.
- `validation`: 저장·영업·UI·이동·캡처·구조 검증. 사용자 저장 슬롯을 테스트에 사용하지 않습니다.
- `Tools/World`: 기존 충돌·플레이어 작성 원본. 식당 Build는 이 생성기를 자동 실행하지 않습니다.

## Actor 표시 이름

`Gameplay/Managers`, `Gameplay/Restaurant/Interactions`, `Gameplay/Restaurant/Customers`, `Gameplay/Beach/Interactions`로 Outliner를 정리합니다. `CookingPan`, `CookingPot`, `MenuBoard`, `ServiceSeat_01`, `ClamGather_01`처럼 역할을 표시합니다. `JongguPrototype`·`JongguPrototypeId:` 태그와 기존 Unity ID는 안정적인 소유권·식별자로 유지하며, 표시 이름에 게임 로직을 결합하지 않습니다. `SourceCamera`는 기존 도구의 명시적 계약이라 유지합니다.

## 저장 호환과 변경 방법

애셋 이동은 파일 탐색기에서 하지 않고 Unreal AssetTools를 사용합니다. 참조를 저장한 뒤 엔진의 [리다이렉터 정리](https://dev.epicgames.com/documentation/en-us/unreal-engine/asset-redirectors-in-unreal-engine)를 실행합니다. 기존 `.sav`는 클래스 오브젝트 이름을 포함합니다. **`BP_PrototypeSave`는 저장 호환을 위한 이름 예외**로 유지하고 패키지만 `/Game/Jonggu/Core/Save/BP_PrototypeSave`로 이동했습니다. 로더가 이전 패키지를 새 패키지로 찾은 뒤 같은 클래스 이름을 읽도록 [DefaultEngine.ini](../../Config/DefaultEngine.ini)의 `PackageRedirects`와 `ClassRedirects`를 유지합니다. 클래스 리다이렉트만으로 저장 클래스 이름 변경을 해결했다고 가정하지 않습니다.

슬롯 `JongguPrototypeCheckpoint`, 스키마 버전 1, 저장 필드는 유지합니다. 관련 변경은 [이전 저장 fixture](../../Tools/Unreal/Fixtures/LegacyCheckpoint.sav)의 바이트를 실제 엔진·GameInstance에서 읽어 검증합니다. 새로 만든 저장만 읽는 검사로 호환 검증을 대체하지 않습니다.

실행 명령·QA 에디터 사용·보고서 확인은 [AUTHORING](AUTHORING.md)에서 관리합니다. 현재 승인된 게임 범위는 [GAMEPLAY](../Design/GAMEPLAY.md), UI 계약은 [UI](../Design/UI.md)를 따릅니다.

이번 변경 기록과 자동/수동 검증 범위는 [리팩토링 검증](../Validation/REFACTOR.md)에 정리합니다.
