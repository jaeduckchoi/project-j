# 식당 기능 제작 안내

[projectJ.uproject](../../projectJ.uproject)를 기준으로 작성합니다. 게임 런타임은 저장된 Blueprint이며 [jonggu 패키지](../../Content/Python/jonggu/)는 에디터 제작·검증 소스입니다. 디렉터리·이름·저장 호환 규칙은 [STRUCTURE](STRUCTURE.md), 게임과 UI의 현재 범위는 [GAMEPLAY](../Design/GAMEPLAY.md)·[UI](../Design/UI.md)에 있습니다.

## 실행

아래 PowerShell 명령은 **프로젝트 루트**에서 실행합니다. 새 프로세스를 여는 런처를 실행하기 전에 현재 프로젝트 에디터의 작업을 저장하고 정상 종료합니다. 런처가 기존 에디터를 대신 닫거나 미저장 작업을 보존해 주지는 않습니다.

```powershell
python Tools/Unreal/check_sources.py
./Tools/Unreal/run.ps1 -Task Build
./Tools/Unreal/run.ps1 -Task Verify
./Tools/Unreal/run.ps1 -Task Capture -CaptureMode Adaptive
./Tools/Unreal/run.ps1 -Task Structure
```

| 작업 | 목적 |
|---|---|
| `check_sources.py` | 엔진을 열지 않고 Python 구문·경로·내부 import·순수 UI 계약 검사 |
| `Build` | 식당 전용 Blueprint·UI·맵 구성 재생성 및 저장 계약 검사 |
| `Verify` | 실제 PIE에서 영업·입력·저장·맵 순환 검사 |
| `Capture` | 실제 렌더링 에디터에서 지정 화면 캡처와 관련 UI 검사 |
| `Structure` | 애셋 경로·참조·맵 배치·기존 저장 fixture 복원 검사 |
| `Baseline` | 기존 충돌 미통과 사례 재현. [기준 결과](../Validation/BASELINE.md)와 개별 항목 비교 |

`CaptureMode`는 `Popups`, `HUD`, `Adaptive`, `Coverage` 중에서 선택합니다. 팝업, 최초 일반 HUD, 요약·투명화, 가림 장면을 각각 검사합니다. `-Sizes`는 Capture에서만 사용하며 기본값은 아래 세 해상도입니다.

```powershell
./Tools/Unreal/run.ps1 -Task Capture -CaptureMode Popups -Sizes '1280x720,1920x1080,1189x862'
./Tools/Unreal/run.ps1 -Task Baseline
```

런처는 Epic Launcher 설치 목록 또는 `UNREAL_ENGINE_ROOT`에서 UE 5.8을 찾습니다. `UNREAL_ENGINE_ROOT`는 엔진 설치 루트이고 필요한 경우 `-EngineExecutable` 인수에 `UnrealEditor.exe` 위치를 전달합니다. 엔진 위치는 각 PC의 설정값으로 관리합니다.

열린 **일반 편집용 에디터**에서 재생성하려면 Play를 종료하고 Output Log의 콘솔에서 다음 한 줄을 실행합니다. 프로젝트 위치를 실행 중인 패키지에서 찾으므로 PC의 절대 경로를 입력하지 않습니다.

```text
py import runpy, jonggu.paths; runpy.run_path(str(jonggu.paths.ROOT / "Tools/Unreal/entry.py"))
```

[entry.py](../../Tools/Unreal/entry.py)는 매번 `jonggu.*`를 다시 읽고 기본 Build를 실행합니다. `-JongguTask`, `-Jonggu*QA`, `-ExecutePythonScript` 인수로 열린 검증 전용 에디터는 작업 선택이 고정되고 검사가 끝나면 종료될 수 있으므로 일반 편집·빌드에 재사용하지 않습니다.

`Refactor`는 이미 끝낸 경로 이관을 위한 유지보수 작업입니다. 일반 재생성 명령이 아니며 전용 이전 상태 백업을 요구합니다. 일상적인 수정에는 Build와 필요한 검증만 사용합니다.

## 변경할 소스

아래 축약 경로는 **`Content/Python/jonggu/` 기준**이며 다른 루트의 입력은 별도로 표시합니다.

| 대상 | 작성 원본 |
|---|---|
| 요리 이름·기구·시간·수량·조개 비용 | [gameplay/data.py](../../Content/Python/jonggu/gameplay/data.py)의 `RECIPES` |
| 영업 기본 수치·Blueprint 변수·함수 등록 | [gameplay/manager.py](../../Content/Python/jonggu/gameplay/manager.py) |
| 손님·도착·서빙·매출·마감 | [gameplay/service.py](../../Content/Python/jonggu/gameplay/service.py) |
| 조리·수령·비우기 | [gameplay/cooking.py](../../Content/Python/jonggu/gameplay/cooking.py) |
| 저장·날짜·채집·로그 | [gameplay/session.py](../../Content/Python/jonggu/gameplay/session.py) |
| 입력·화면·이동 차단 | [gameplay/input.py](../../Content/Python/jonggu/gameplay/input.py), [world/controller.py](../../Content/Python/jonggu/world/controller.py) |
| 관리자에서 HUD로 전달할 상태 | [gameplay/presentation.py](../../Content/Python/jonggu/gameplay/presentation.py), [ui/contracts.py](../../Content/Python/jonggu/ui/contracts.py) |
| 표시·클릭·가림 영역 | [ui/layout.py](../../Content/Python/jonggu/ui/layout.py) |
| Canvas 공통 도구·일반 HUD·팝업 | [ui/primitives.py](../../Content/Python/jonggu/ui/primitives.py), [ui/renderer.py](../../Content/Python/jonggu/ui/renderer.py), [ui/panels.py](../../Content/Python/jonggu/ui/panels.py) |
| 투명화·투영 | [ui/visibility.py](../../Content/Python/jonggu/ui/visibility.py) |
| UI 이미지·글꼴 가져오기 | [ui/assets.py](../../Content/Python/jonggu/ui/assets.py), 프로젝트 루트의 [SourceAssets/Fonts](../../SourceAssets/Fonts/) |
| 상호작용·기본 도착·손님 위치 | 프로젝트 루트의 [SourceData/Restaurant/layout.json](../../SourceData/Restaurant/layout.json) |
| 게임용 Actor·맵 설정 | [world/build.py](../../Content/Python/jonggu/world/build.py), [world/actors.py](../../Content/Python/jonggu/world/actors.py), [world/interaction.py](../../Content/Python/jonggu/world/interaction.py) |
| 애셋 패키지·클래스 경로 | [assets.py](../../Content/Python/jonggu/assets.py) |

Hub 귀환 위치에는 현재 예외가 있습니다. `gameplay/service.py`의 `setup`은 `entry_marker == "hub_return"`이면 고정 좌표 `(-715, 0, -520)`를 사용합니다. 해당 위치를 바꿀 때는 layout뿐 아니라 이 분기도 함께 확인합니다.

생성 Blueprint의 함수·EventGraph·기본값을 직접 수정하면 다음 Build에서 재작성됩니다. 지속할 변경은 Python·SourceData의 해당 원본에 먼저 반영한 뒤 생성 `.uasset`·`.umap`도 함께 저장합니다. 제작 라이브러리와 실행 드라이버를 구분하며 `validation/verify_*`는 일반 라이브러리처럼 임의로 import하지 않습니다.

## 보존할 계약

저장은 `JongguPrototypeCheckpoint` 한 슬롯, 스키마 버전 1입니다. 기존 `.sav` 호환을 위해 `BP_PrototypeSave`의 클래스 이름과 이전 경로 리다이렉트를 유지합니다. 접기 상태 `hud_expanded_mask`는 GameInstance의 세션 전용 값이므로 저장하지 않습니다. 표시 데이터는 HUD가 받고 조리·재고·서빙 판정은 관리자에서 수행합니다.

Build는 식당 전용 애셋과 두 맵의 게임 소유 Actor·설정을 관리합니다. 기존 Pawn·SourceCamera·아트·충돌은 별도의 [월드·플레이어 도구](../../Tools/World/README.md)에서 작성합니다. `JongguPrototype`·`JongguPrototypeId:` 태그가 소유권·식별자이므로 Outliner 표시 이름을 판정에 사용하지 않습니다.

최초 글꼴 임포트는 일반 에디터에서 수행합니다. 현재 저장된 `FF_Galmuri11_*`와 `F_Galmuri11_*`는 커맨드릿 Build에서 재사용합니다. UI 이미지 원본은 기존 Textures 라이브러리이며 일반 Build에는 Unity 폴더가 필요하지 않습니다.

## 결과 확인

보고서는 프로젝트 루트 기준 `Saved/` 아래에 생성됩니다. 버전 관리하지 않는 로컬 결과이므로 새 클론에는 없을 수 있습니다.

| 결과 | 프로젝트 루트 기준 경로 |
|---|---|
| Build·저장 계약 | `Saved/PrototypeQA/build_report.json` |
| PIE 영업 순환 | `Saved/PrototypeQA/pie_report.json` |
| 팝업 캡처 | `Saved/PopupDesignQA/` |
| 일반 HUD 캡처 | `Saved/MainHUDQA/` |
| 요약·투명화 캡처 | `Saved/AdaptiveHUDQA/` |
| 구조·이전 저장 호환 | `Saved/RefactorQA/structure_report.json` |
| 소스 검사 | `Saved/RefactorQA/source_report.json` |

런처는 이번 실행에서 새 보고서가 생성됐는지와 성공 여부를 확인합니다. Baseline은 기존 실패 재현이 목적이라 새 보고서 생성까지만 확인하므로 개별 결과를 읽어야 합니다. Verify·Capture·Baseline이 별도 에디터에서 변경한 창 설정은 종료 후 실행 전 상태로 복원합니다.

자동 검사는 검증용 저장 슬롯을 사용합니다. 통과 결과를 운영체제 키보드·마우스의 직접 조작이나 재미 관찰로 확대하지 않습니다. 최신 상태는 [검증 색인](../Validation/VALIDATION.md), 수동 절차는 [플레이 관찰](../Validation/PLAYTEST.md)에서 관리합니다.
