# 종구의 식당 · Unreal 2D

Unreal Engine 5.8.2 / Paper2D 프로젝트입니다. 루트의 `projectJ.uproject`를 열면 Hub와 원본 직교 카메라 구도로 시작합니다.

## 저장소와 브랜치

[project-j 저장소](https://github.com/jaeduckchoi/project-j)에서 `main`은 Unity 프로젝트를 유지하고, `migration/unreal`은 현재 Unreal 프로젝트를 관리합니다. 이 브랜치는 Unity `main`의 이력에서 분기했습니다.

`Config/`, `Content/`, `projectJ.uproject`를 버전 관리하며, Unreal 캐시·임시 파일과 개인 IDE·Codex 설정은 제외합니다. 아래의 `Migration/Jonggu` 변환 도구와 QA 자료는 저장소 외부의 로컬 자료입니다.

## Codex 연결

Epic의 내장 **Unreal MCP**와 **EditorToolset** 플러그인이 에디터 대상으로 활성화되어 있습니다. 이 프로젝트를 열면 MCP 서버가 `http://127.0.0.1:8000/mcp`에서 자동으로 시작합니다. 현재 PC의 Codex 연결은 프로젝트의 `.codex/config.toml`에 등록되어 있습니다. 이 파일은 개인 로컬 설정으로 저장소에서 제외하므로, 새 환경에서는 별도로 연결을 설정합니다.

언리얼 프로젝트를 연 뒤 Codex에서 이 프로젝트를 사용합니다. 설정 직후에는 Codex의 MCP 서버를 재시작하거나 Codex 앱을 다시 열어 새 도구를 불러옵니다. 액터 조회·배치·속성 변경, 에셋 검색, 블루프린트 편집 등에 사용할 수 있습니다. 예: “현재 선택한 액터를 알려줘”, “BP_JongguPlayer 에셋을 찾아줘”. 일부 편집 도구는 Play 모드를 종료한 뒤 사용해야 합니다.

도구는 `list_toolsets` → `describe_toolset` → `call_tool` 순서로 찾고 호출합니다. 반환된 전체 toolset 이름을 사용하고 호출은 순차 실행합니다. 여러 언리얼 프로젝트를 동시에 열 때는 각 프로젝트의 서버 포트와 Codex URL을 다르게 설정합니다. 연결은 이 PC 내부에서만 사용합니다.

설정 위치는 `Config/DefaultEditorPerProjectUserSettings.ini`의 `ModelContextProtocolSettings`이며, `bAutoStartServer=False`로 자동 시작을 끌 수 있습니다. 이 기능은 Unreal 5.8의 실험적 기능입니다. [Epic 공식 안내](https://dev.epicgames.com/documentation/unreal-engine/unreal-mcp-in-unreal-editor?lang=ko) · [Codex MCP 안내](https://learn.chatgpt.com/docs/extend/mcp?surface=cli)

**에디터 내부 터미널:** 내장 `Terminal` 플러그인도 활성화되어 있습니다. 플러그인 활성화 후 에디터를 재시작하고 **툴(Tools) → Terminal**을 열면 프로젝트 폴더의 명령 프롬프트가 표시됩니다. 여기서 `codex.exe`를 실행하면 에디터 안에서 Codex와 대화할 수 있습니다. 현재 PC의 `codex` 명령은 이전 npm CLI를 먼저 찾으므로 `codex.exe`를 사용합니다. 새 Terminal 탭마다 프로젝트 폴더와 `TERM=xterm-256color`를 설정하며, Codex는 직접 실행합니다.

## 프로젝트 구조

```text
D:/laeti-dev/
├─ ProjectJ/
│  ├─ projectJ.uproject
│  ├─ Config/
│  ├─ Content/
│  │  ├─ Jonggu/
│  │  │  ├─ Maps/                 # L_Hub, L_Beach
│  │  │  ├─ Blueprints/
│  │  │  │  ├─ Game/              # BP_JongguGameMode
│  │  │  │  ├─ Player/            # BP_JongguPlayer
│  │  │  │  └─ Collision/         # BP_CollisionBox, BP_CollisionSphere
│  │  │  ├─ Prototype/            # 식당·채집·UI·저장 Blueprint
│  │  │  ├─ Textures/             # 원본 이미지의 카테고리별 보관
│  │  │  ├─ Sprites/              # PaperSprite, Player/Runtime 프레임
│  │  │  └─ Materials/
│  │  └─ Python/                 # 에디터 카메라 초기화
│  ├─ Tools/Prototype/           # 현재 프로토타입 제작·검증 소스
│  └─ Saved/
│     ├─ PrototypeQA/            # 로컬 빌드·PIE 검증 자료
│     └─ CollisionWork/          # 최신 로컬 충돌·플레이어 작업 자료
└─ Migration/Jonggu/             # 읽기 전용 역사 자료; 현재 재생성에 직접 사용하지 않음
   ├─ migration_config.json
   ├─ Tools/                     # 추출·임포트·Blueprint 제작 도구
   ├─ Data/                      # 원본·정규화 manifest, collision_rules.json
   ├─ QA/                        # 이전 변환 검증 보고서·캡처
   └─ Archive/                   # 백업·이전 로그·Unity 검증 복제본
```

사용하지 않는 TopDown 템플릿과 관련 외부 패키지 244개, 빈 디렉터리 200개를 참조 감사와 백업 후 정리했습니다. 제거한 파일은 총 135,667,122바이트이며 백업은 `D:/laeti-dev/Migration/Jonggu/Archive/CollisionSetup/unused_topdown_template.zip`에 보관합니다. 삭제 결과와 후속 맵 재열기 상태는 변환 도구의 `QA/cleanup_report.json`에서 확인합니다. 원본 이미지·스프라이트 라이브러리는 재생성에 필요하므로 보관합니다.

## 식당 프로토타입 플레이

**L_Hub**에서 Play를 실행하면 **준비 → 선택적 해변 채집 → 메뉴 결정 → 조리·서빙 → 결산 → 다음 날 준비**를 진행합니다. 기존 맵과 플레이어 위에 독립된 Blueprint 기능을 추가했으며, 실행 중 Python이나 프로젝트 C++ 모듈을 사용하지 않습니다. 완료 목표는 Unreal 에디터의 Play입니다.

| 조작 | 동작 |
|---|---|
| WASD / 방향키 | 이동 |
| E | 가까운 메뉴판·기구·손님·조개·맵 이동 지점과 상호작용 |
| 마우스 클릭 | 메뉴·손님 수·조리 선택과 영업 버튼 사용 |
| Esc / F10 | 전체 일시정지와 재개 |
| Shift+Esc | 에디터 Play 종료 |

준비 중 김치볶음밥·김치찌개·김치전에서 **1~2종**, 손님 **4명 또는 6명**을 선택합니다. 기본 재료는 무한 제공됩니다. 해변의 조개 3곳은 각각 하루 1회 채집할 수 있으며, 시간·가방 제한은 없습니다. 같은 날 왕복해도 조개는 다시 생기지 않고, 남은 조개는 다음 날로 이어집니다. 영업 중에는 해변으로 이동하지 못합니다.

팬 1개는 김치볶음밥 **8초** 또는 김치전 **12초**, 냄비 1개는 김치찌개 **16초**를 조리합니다. 기본 조리는 한 그릇, **조개 특선 찌개는 조개 1개로 16초에 두 그릇**입니다. 특선도 김치찌개 주문에 제공하므로 조개가 없으면 기본 찌개를 만들면 됩니다. 주문 전에 미리 조리할 수 있고, 조리 선택창을 열어도 영업 시간은 흐릅니다.

완성품은 기구 앞에서 E로 한 그릇씩 수령하고 해당 손님에게 E로 서빙합니다. 손에는 접시 1개만 들 수 있습니다. 잘못된 주문에 서빙해도 접시는 유지됩니다. 반납대에서 든 접시를 폐기할 수 있고, **접시를 든 채 완성된 기구에서 E를 누르면 선택창의 ‘완성품 비우기’**로 남은 음식을 정리할 수 있습니다. 완성품은 타거나 상하지 않으며, 폐기한 조개는 돌려받지 못합니다.

손님은 좌석 2개에 등장하며 기다려도 떠나지 않습니다. 첫 손님은 개점 직후, 이후에는 기본 20초 간격으로 빈 좌석에 도착합니다. **‘손님 받기 중단/재개’**는 신규 도착만 조절합니다. **‘마감하기’**는 신규 도착을 끝내고 받은 주문을 모두 마친 뒤 결산합니다. 예정 손님을 모두 대접해도 결산합니다. 음식당 기본 매출은 10, 주문 후 30초 이내 추가 2, 60초 이내 추가 1입니다. 결산에서 서빙 수·매출·조개 사용·특선 제공을 확인하고 다음 날로 넘어갑니다.

저장은 단일 슬롯 `JongguPrototypeCheckpoint`를 사용합니다. 준비·해변 이동·개점 직전·결산 구간을 저장하며, 진행 중 주문과 조리는 중간 저장하지 않습니다. 일시정지 화면의 **‘이번 영업 다시 시작’**은 개점 직전 메뉴·재고·매출을 복원하고 준비 상태로 돌아갑니다. 채집·조리·서빙 등의 행동은 로컬 로그에 기록합니다.

성장 업그레이드, 물때, 추가 채집 재료, 수동 조리 미니게임, 관계 성장, 수영·승선·물체 밀기, 독립 실행 파일은 이번 범위에 포함하지 않습니다. 수치 조절과 제작 소스는 [프로토타입 제작 안내](D:/laeti-dev/ProjectJ/Tools/Prototype/README.md)를 참고하세요. 빌드 결과는 [build_report.json](D:/laeti-dev/ProjectJ/Saved/PrototypeQA/build_report.json), Play 검증 결과는 [pie_report.json](D:/laeti-dev/ProjectJ/Saved/PrototypeQA/pie_report.json)에 기록합니다. 보고서의 실행 시각과 개별 검증 항목을 기준으로 확인하며, 기능 검증과 실제 플레이의 재미 평가는 구분합니다.

## 화면과 플레이어

`Content/Jonggu/Maps`의 **L_Hub**, **L_Beach**를 엽니다. 카메라는 16:9 직교 투영, 폭 3,200cm입니다. 뷰포트는 **라이팅포함(Lit)**으로 사용하고, 원본 구도를 복원하려면 `SourceCamera`를 Pilot합니다. 전체 배치는 Pilot을 해제해 살펴봅니다. Beach의 원본 저장 배치에서는 플레이어가 화면 밖에 있지만, 프로토타입 Play 시작·맵 이동에서는 별도로 지정한 통행 가능한 도착 위치로 이동합니다.

기존 Unity `PlayerRoot`는 **BP_JongguPlayer** Pawn으로 연결합니다. Play 후 게임 뷰포트를 클릭하면 WASD·방향키로 XZ 평면을 8방향 이동합니다. 예를 들어 W+D는 오른쪽 위, S+A는 왼쪽 아래입니다. 대각선과 직선의 최고 속도는 모두 400cm/s이고, 짧은 가속·감속을 적용합니다. 바라보는 방향은 키 입력을 기준으로 하므로 위 입력만 누를 때 장애물 가장자리에서 옆으로 미끄러져도 위를 봅니다. 대각선에서는 마지막으로 바라보던 방향이 입력과 맞으면 유지합니다. 걷기·대기 전환은 실제 이동을 기준으로 합니다.

걷기는 원본 이미지의 머리·몸통·양발을 나눈 PaperSprite 4개로 8개 포즈를 재생합니다. 짧은 발의 교차, 몸통의 작은 들썩임, 조금 늦게 따라오는 머리 움직임을 사용하며 원본 픽셀과 외형을 유지합니다. 실제 이동 거리 160cm마다 한 주기가 진행됩니다. 정지하면 마지막 방향의 기존 두 대기 프레임을 0.3초 간격으로 표시합니다. 원본 PPU 80과 발밑 피벗 `(0.5, 0.08)`은 동일합니다. 시각 효과는 캐릭터 그림에 적용하고 충돌 루트는 이동 경로를 유지합니다.

`BP_JongguPlayer`의 Player 설정에서 `move_speed`, `walk_cycle_distance`(작을수록 잦은 발걸음), `walk_bob_cm`, `walk_lean_degrees`를 조절할 수 있습니다. 이동은 8방향이며 그림은 기존 정면·후면·측면과 좌우 반전을 사용합니다.

기존 `BP_JongguGameMode` 에셋은 보존합니다. 프로토타입의 두 맵은 전용 `BP_PrototypeGameMode`·`BP_PrototypeController`를 사용하며, Pawn 자동 생성을 끄고 레벨에 배치된 `BP_JongguPlayer`와 기존 `SourceCamera`를 연결합니다. 플레이어는 Blueprint로 실행되며 C++ 빌드 도구나 런타임 Python이 필요하지 않습니다.

## 애셋별 충돌

Hub 가구와 벽, Beach 나무·등대·선체는 발밑이나 바닥을 차지하는 면적으로 이동을 막습니다. 테이블·의자, 메뉴판 받침, 나무 밑동에 각각 같은 애셋 규칙을 적용하므로 그림의 윗부분이나 나뭇잎이 별도 장애물이 되지 않습니다. Beach의 잔디·모래·부두는 통행 가능하고 물과 맵 외곽은 차단합니다.

현재 저장된 맵·충돌 에셋에 대응하는 최신 로컬 작성 자료는 [Saved/CollisionWork](D:/laeti-dev/ProjectJ/Saved/CollisionWork)의 `Data/collision_rules.json`과 `Tools`에 있습니다. 외부 `Migration/Jonggu` 자료는 이전 버전의 읽기 전용 참고 자료입니다. 재사용 Blueprint는 `/Game/Jonggu/Blueprints/Collision/BP_CollisionBox`와 `BP_CollisionSphere`이며, 스프라이트와 분리된 루트 도형을 사용합니다. `JongguObstacle` 프로필은 Pawn 차단·QueryOnly·물리 시뮬레이션 없음으로 설정합니다. 플레이어의 반지름 24cm와 최고 속도 400cm/s는 유지합니다.

에디터에서 충돌 표시를 켜거나 충돌 Actor를 선택하면 녹색 도형을 확인할 수 있습니다. 게임 화면에는 도형을 표시하지 않습니다. 프로토타입 추가 Actor를 제외한 기존 구성은 Hub 원본 99개와 충돌 16개, Beach 원본 56개와 충돌 74개입니다. 애셋별 크기·오프셋·해안 타일 분류와 수정 방법은 최신 로컬 [충돌 설정 안내](D:/laeti-dev/ProjectJ/Saved/CollisionWork/COLLISION_GUIDE.md)를 참고합니다.

앞뒤 가림도 발밑 위치를 기준으로 합니다. 플레이어가 메뉴판·카운터 앞에 있으면 앞에, 뒤로 이동하면 물체 뒤에 그려집니다. 메뉴판의 글·그림과 분할 카운터의 장식은 같은 물체의 기준점을 공유합니다. 플레이어 머리·몸통·발은 한 순서로 움직이고, 잔디·모래·부두 같은 바닥은 아래에 남습니다. 이 규칙은 `collision_rules.json`의 `depth_sorting`에서 관리합니다.

## 애셋과 재생성

원본 이미지 249개, 변환 스프라이트 정의 262개, 렌더러 41개, Beach 타일 2,504칸을 다룹니다. 플레이어의 대기 프레임 6개는 `Sprites/Player/Runtime`, 원본 텍스처 영역을 사용하는 걷기 파츠 12개는 `Sprites/Player/WalkParts`에 둡니다. 기존 플레이어 렌더러는 원본 ID와 배치를 가진 숨김 참조로 남고 Pawn의 스프라이트 컴포넌트가 화면을 표시합니다. 걷기 파츠의 재생성은 최신 로컬 `Saved/CollisionWork/Tools/configure_player_walk.py`, 포즈와 이동 그래프는 같은 폴더의 `build_player_blueprint.py`가 관리합니다. 프로토타입 생성기는 이 플레이어 생성기를 실행하지 않습니다.

원본 Unity 프로젝트 `D:\laeti-dev\project-j-temp\project-j`는 읽기만 합니다. 이번 프로토타입의 UI·조리·서빙·조개 채집·저장·상호작용·맵 전환은 `/Game/Jonggu/Prototype`에서 독립적으로 관리합니다. 기존 이미지 UI 폴더와 Unity 진입·복귀 마커는 보존하며, 실제 이동에는 `Tools/Prototype/layout.json`의 별도 도착 위치와 상호작용 지점을 사용합니다.

현재 프로토타입 재생성 진입점은 [Tools/Prototype/build_prototype.py](D:/laeti-dev/ProjectJ/Tools/Prototype/build_prototype.py)입니다. 이 도구는 전용 Blueprint와 `JongguPrototype` 소유 Actor·맵 설정만 관리하며, 기존 맵의 그림·충돌·플레이어 생성과 전체 마이그레이션을 실행하지 않습니다. 생성 결과의 영구적인 변경은 에셋을 직접 편집하기보다 해당 제작 소스에서 수정합니다.

충돌·플레이어 재생성은 별도 작업입니다. `Saved/CollisionWork`와 외부 변환 자료에는 이전 프로젝트 경로가 남아 있을 수 있으므로, 실제 대상 `D:/laeti-dev/ProjectJ/projectJ.uproject`와 현재 에셋 버전을 먼저 대조합니다. 외부 `Migration/Jonggu`의 이전 생성기나 `run_migration.ps1`을 현재 프로젝트에 그대로 실행하지 않습니다. 기존 미통과 검증과 이번 변경의 검증 범위는 [프로토타입 기준 기록](D:/laeti-dev/ProjectJ/Tools/Prototype/BASELINE.md)에 구분합니다.

실제 Unity 엔진 캡처는 라이선스 오류 198로 수행하지 못했습니다. 원본 데이터의 소프트웨어 참조와 Unreal 실제 렌더 캡처를 구분해 검증합니다.
