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
│  └─ Content/
│     ├─ Jonggu/
│     │  ├─ Maps/                 # L_Hub, L_Beach
│     │  ├─ Blueprints/
│     │  │  ├─ Game/              # BP_JongguGameMode
│     │  │  ├─ Player/            # BP_JongguPlayer
│     │  │  └─ Collision/         # BP_CollisionBox, BP_CollisionSphere
│     │  ├─ Textures/             # 원본 이미지의 카테고리별 보관
│     │  ├─ Sprites/              # PaperSprite, Player/Runtime 프레임
│     │  └─ Materials/
│     └─ Python/                 # 에디터 카메라 초기화
└─ Migration/Jonggu/
   ├─ migration_config.json
   ├─ Tools/                     # 추출·임포트·Blueprint 제작 도구
   ├─ Data/                      # 원본·정규화 manifest, collision_rules.json
   ├─ QA/                        # 최신 검증 보고서·캡처
   └─ Archive/                   # 백업·이전 로그·Unity 검증 복제본
```

사용하지 않는 TopDown 템플릿과 관련 외부 패키지 244개, 빈 디렉터리 200개를 참조 감사와 백업 후 정리했습니다. 제거한 파일은 총 135,667,122바이트이며 백업은 `D:/laeti-dev/Migration/Jonggu/Archive/CollisionSetup/unused_topdown_template.zip`에 보관합니다. 삭제 결과와 후속 맵 재열기 상태는 변환 도구의 `QA/cleanup_report.json`에서 확인합니다. 원본 이미지·스프라이트 라이브러리는 재생성에 필요하므로 보관합니다.

## 화면과 플레이어

`Content/Jonggu/Maps`의 **L_Hub**, **L_Beach**를 엽니다. 카메라는 16:9 직교 투영, 폭 3,200cm입니다. 뷰포트는 **라이팅포함(Lit)**으로 사용하고, 원본 구도를 복원하려면 `SourceCamera`를 Pilot합니다. 전체 배치는 Pilot을 해제해 살펴봅니다. Beach의 저장된 시작 구도에서는 플레이어가 화면 밖에 있습니다.

기존 Unity `PlayerRoot`는 **BP_JongguPlayer** Pawn으로 연결합니다. Play 후 게임 뷰포트를 클릭하면 WASD·방향키로 XZ 평면을 8방향 이동합니다. 예를 들어 W+D는 오른쪽 위, S+A는 왼쪽 아래입니다. 대각선과 직선의 최고 속도는 모두 400cm/s이고, 짧은 가속·감속을 적용합니다. 바라보는 방향은 키 입력을 기준으로 하므로 위 입력만 누를 때 장애물 가장자리에서 옆으로 미끄러져도 위를 봅니다. 대각선에서는 마지막으로 바라보던 방향이 입력과 맞으면 유지합니다. 걷기·대기 전환은 실제 이동을 기준으로 합니다.

걷기는 원본 이미지의 머리·몸통·양발을 나눈 PaperSprite 4개로 8개 포즈를 재생합니다. 짧은 발의 교차, 몸통의 작은 들썩임, 조금 늦게 따라오는 머리 움직임을 사용하며 원본 픽셀과 외형을 유지합니다. 실제 이동 거리 160cm마다 한 주기가 진행됩니다. 정지하면 마지막 방향의 기존 두 대기 프레임을 0.3초 간격으로 표시합니다. 원본 PPU 80과 발밑 피벗 `(0.5, 0.08)`은 동일합니다. 시각 효과는 캐릭터 그림에 적용하고 충돌 루트는 이동 경로를 유지합니다.

`BP_JongguPlayer`의 Player 설정에서 `move_speed`, `walk_cycle_distance`(작을수록 잦은 발걸음), `walk_bob_cm`, `walk_lean_degrees`를 조절할 수 있습니다. 이동은 8방향이며 그림은 기존 정면·후면·측면과 좌우 반전을 사용합니다.

`BP_JongguGameMode`는 레벨에 배치된 플레이어가 Player 0의 조작을 받도록 사용합니다. 기존 소스 카메라의 고정 구도를 유지합니다. 플레이어는 Blueprint로 실행되며 C++ 빌드 도구나 런타임 Python이 필요하지 않습니다.

## 애셋별 충돌

Hub 가구와 벽, Beach 나무·등대·선체는 발밑이나 바닥을 차지하는 면적으로 이동을 막습니다. 테이블·의자, 메뉴판 받침, 나무 밑동에 각각 같은 애셋 규칙을 적용하므로 그림의 윗부분이나 나뭇잎이 별도 장애물이 되지 않습니다. Beach의 잔디·모래·부두는 통행 가능하고 물과 맵 외곽은 차단합니다.

충돌 정본은 [collision_rules.json](D:/laeti-dev/Migration/Jonggu/Data/collision_rules.json)입니다. 재사용 Blueprint는 `/Game/Jonggu/Blueprints/Collision/BP_CollisionBox`와 `BP_CollisionSphere`이며, 스프라이트와 분리된 루트 도형을 사용합니다. `JongguObstacle` 프로필은 Pawn 차단·QueryOnly·물리 시뮬레이션 없음으로 설정합니다. 플레이어의 반지름 24cm와 최고 속도 400cm/s는 유지합니다.

에디터에서 충돌 표시를 켜거나 충돌 Actor를 선택하면 녹색 도형을 확인할 수 있습니다. 게임 화면에는 도형을 표시하지 않습니다. 현재 Hub는 원본 99개와 충돌 16개, Beach는 원본 56개와 충돌 74개 Actor를 저장합니다. 애셋별 크기·오프셋·해안 타일 분류와 수정 방법은 [충돌 설정 안내](D:/laeti-dev/Migration/Jonggu/COLLISION_GUIDE.md)에 정리되어 있습니다.

앞뒤 가림도 발밑 위치를 기준으로 합니다. 플레이어가 메뉴판·카운터 앞에 있으면 앞에, 뒤로 이동하면 물체 뒤에 그려집니다. 메뉴판의 글·그림과 분할 카운터의 장식은 같은 물체의 기준점을 공유합니다. 플레이어 머리·몸통·발은 한 순서로 움직이고, 잔디·모래·부두 같은 바닥은 아래에 남습니다. 이 규칙은 `collision_rules.json`의 `depth_sorting`에서 관리합니다.

## 애셋과 재생성

원본 이미지 249개, 변환 스프라이트 정의 262개, 렌더러 41개, Beach 타일 2,504칸을 다룹니다. 플레이어의 대기 프레임 6개는 `Sprites/Player/Runtime`, 원본 텍스처 영역을 사용하는 걷기 파츠 12개는 `Sprites/Player/WalkParts`에 둡니다. 기존 플레이어 렌더러는 원본 ID와 배치를 가진 숨김 참조로 남고 Pawn의 스프라이트 컴포넌트가 화면을 표시합니다. 걷기 파츠의 재생성은 변환 도구의 `configure_player_walk.py`, 포즈와 이동 그래프는 `build_player_blueprint.py`가 관리합니다.

원본 Unity 프로젝트 `D:\laeti-dev\project-j-temp\project-j`는 읽기만 합니다. UI 화면, 조리·서빙·탐험·저장 시스템과 수영·승선·물체 밀기·상호작용·맵 전환은 후속 범위입니다. UI 폴더에는 이미지 애셋만 있습니다. Beach의 진입·복귀 마커는 기존 물 위 좌표 (0, 0)를 유지하므로 맵 전환을 구현할 때 별도로 수정해야 합니다.

재실행 방법은 [변환 도구 안내](D:/laeti-dev/Migration/Jonggu/README.md), 검증 결과는 [QA 보고서](D:/laeti-dev/Migration/Jonggu/QA/VALIDATION_REPORT.md)를 참고합니다. 관리 대상 Actor는 원본 ID로 갱신되므로 해당 Actor의 수동 편집값은 재실행 시 원본 값으로 돌아갑니다.

충돌은 JSON 설정과 변환 도구에서 재생성합니다. `run_migration.ps1`은 작성된 `collision_rules.json`을 덮어쓰지 않으며, 설정을 바꾼 뒤 재임포트 2회·맵 재열기·충돌 PIE 검증을 수행합니다. 실행별 성공 여부는 최신 QA 보고서에서 확인합니다.

실제 Unity 엔진 캡처는 라이선스 오류 198로 수행하지 못했습니다. 원본 데이터의 소프트웨어 참조와 Unreal 실제 렌더 캡처를 구분해 검증합니다.
