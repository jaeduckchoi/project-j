# 종구의 식당 프로토타입 제작 도구

현재 Unreal 프로젝트 `D:/laeti-dev/ProjectJ/projectJ.uproject`에서 식당·선택적 조개 채집 순환을 만드는 소스입니다. Python은 에디터에서 Blueprint와 배치를 작성할 때만 실행합니다. 게임 실행은 저장된 Blueprint와 엔진 기본 기능으로 이루어지며, 프로젝트 C++ 모듈이나 런타임 Python을 사용하지 않습니다.

## 실행과 조작

`Content/Jonggu/Maps/L_Hub`를 열고 Play를 실행합니다. WASD·방향키로 이동하고, 가까운 대상의 화면 안내가 나타나면 E로 상호작용합니다. 메뉴·손님 수·조리 방식·영업 조절은 마우스로 선택합니다. Esc는 전체 일시정지·재개이며 F10도 같은 기능을 수행합니다. 이 프로젝트에서 에디터 Play 종료는 Shift+Esc입니다.

준비 중 김치볶음밥·김치찌개·김치전에서 1~2종과 손님 4명 또는 6명을 선택합니다. 기본 재료는 무한이며 탐험을 생략해도 영업할 수 있습니다. 해변에는 하루 1회씩 사용할 수 있는 조개 채집 지점 3곳이 있습니다. 같은 날 맵을 왕복해도 재생되지 않고, 남은 조개는 다음 날에 보관됩니다. 준비·탐험에 시간 제한은 없습니다.

| 조리 | 기구 | 기본 시간 | 완성량 | 조개 비용 |
|---|---|---:|---:|---:|
| 김치볶음밥 | 팬 | 8초 | 1그릇 | 0 |
| 김치찌개 | 냄비 | 16초 | 1그릇 | 0 |
| 김치전 | 팬 | 12초 | 1그릇 | 0 |
| 조개 특선 찌개 | 냄비 | 16초 | 2그릇 | 1 |

팬·냄비는 하나씩입니다. 영업 중 선택한 메뉴를 미리 조리할 수 있으며 조리 선택창을 열어도 시간은 흐릅니다. 조개는 빈 냄비에서 특선 조리를 성공적으로 시작할 때만 차감합니다. 기본 찌개와 특선은 같은 김치찌개 주문에 제공할 수 있습니다.

E로 완성품을 한 그릇씩 수령하고 주문한 손님에게 서빙합니다. 들 수 있는 접시는 하나입니다. 잘못된 서빙은 접시를 없애지 않습니다. 반납대에서 든 접시를 폐기할 수 있고, 접시를 든 채 완성된 기구에 E를 누르면 선택창에서 남은 완성품을 비울 수 있습니다. 완성품은 상하거나 타지 않으며, 폐기해도 조개는 환불되지 않습니다.

손님은 비충돌 캐릭터로 두 좌석에 등장합니다. 이동 AI와 시간 초과 퇴장은 없습니다. 첫 손님은 개점 즉시, 이후에는 기본 20초 간격으로 빈 좌석에 등장하며 선택 메뉴를 번갈아 주문합니다. ‘손님 받기 중단’은 조리·기존 주문을 계속 진행하면서 신규 도착만 막고 재개할 수 있습니다. ‘마감하기’는 새 손님을 더 받지 않고 기존 주문을 처리한 뒤 결산합니다. 예정 손님을 모두 대접해도 결산합니다. 기본 매출은 10, 주문 후 30초 이내 추가 2, 60초 이내 추가 1입니다.

## 저장과 재시도

단일 슬롯 `JongguPrototypeCheckpoint`에 날짜·조개 재고·당일 채집 기록·메뉴·예정 손님 수·누적 매출·최근 결산을 저장합니다. 기본 로컬 파일은 `Saved/SaveGames/JongguPrototypeCheckpoint.sav`입니다.

준비 진입, 해변 출발·귀환, 개점 직전, 결산에서 구간 저장합니다. 진행 중 주문·조리·접시는 중간 저장하지 않습니다. 일시정지 화면의 ‘이번 영업 다시 시작’은 개점 직전 메뉴·재고·매출을 함께 복원하고 준비 상태로 돌아갑니다. 다시 영업을 시작하면 같은 선택으로 비교할 수 있습니다. 맵을 이동하거나 다음 날로 넘어가는 과정에서 기존 구간 저장을 이어서 사용합니다.

채집·메뉴 선택·주문·조리·수령·서빙·폐기·유입 조절·마감 이벤트는 GameInstance의 세션 기록과 `Saved/Logs/projectJ.log`의 `[JongguPrototype]` 행에 남깁니다. 이벤트 기록은 플레이 관찰을 보조하며 음식의 재미를 자동 판정하지 않습니다.

## 제작 소스와 재생성

Unreal에서 이 프로젝트를 연 상태로 Play를 종료한 뒤, Output Log의 콘솔에서 다음 명령으로 전용 프로토타입을 재생성합니다.

```text
py "D:/laeti-dev/ProjectJ/Tools/Prototype/build_prototype.py"
```

팝업은 Unity Hub에 저장된 베이지 픽셀 프레임과 갈무리11 디자인을 사용합니다. `build_popup_assets.py`는 이미 이관한 UI·음식 이미지를 전용 자산으로 복제하고, 원본과 동일한 Galmuri11 Regular·Bold를 가져옵니다. 최초 글꼴 가져오기는 Slate가 있는 일반 에디터에서 실행해야 합니다. 글꼴 자산이 저장된 이후에는 commandlet 재생성도 가능합니다. Unity 원본은 수정하지 않습니다.

진입점은 열린 프로젝트의 실제 경로를 확인합니다. 최초 실행의 기존 맵·플레이어·GameMode·설정 기준 자료는 `Saved/PrototypeQA/Baseline`에 보관합니다. 생성 결과는 `/Game/Jonggu/Prototype`과 두 맵의 전용 Actor·설정에 저장합니다. 이 폴더의 도구는 기존 전체 마이그레이션이나 플레이어 생성기를 실행하지 않습니다.

| 작성할 대상 | 정본 소스 |
|---|---|
| 요리 이름·기구·조리 시간·완성량·조개 비용 | `data_types.py`의 `RECIPES` |
| 손님 간격·기본 가격·추가 보상·대기시간 구간·영업 규칙 | `build_manager.py` |
| 날짜·재고·저장 구간·단일 저장 슬롯·행동 기록 | `build_session.py` |
| UI 문구·화면 배치·버튼 ID와 클릭 영역 | `build_ui.py`의 `HUD_FIELDS`, `hud_layout.py`의 `BUTTONS`·공유 영역 |
| HUD·팝업 전용 픽셀 이미지·갈무리 글꼴 | `build_popup_assets.py`, `Resources/Fonts` |
| 상호작용 지점·맵 도착 위치·손님 표시 위치 | `layout.json` |
| 전용 GameMode·Controller·상호작용 Actor·맵 설정 | `build_world.py` |

조리 버튼의 이름·시간·완성량·조개 비용은 `RECIPES`에서 생성합니다. 손님의 주문은 요리 ID로 비교하며 특선은 김치찌개의 추가 조리 방식입니다. `BP_RecipeDefinition`·`BP_PrototypeOrder`·`BP_PrototypeDish`는 명시적인 Blueprint 데이터 객체입니다. 상호작용 계약은 별도 Unreal BPI 대신 공통 `BP_PrototypeInteractable`의 `interaction_id`와 `GetInteractionId`로 제공합니다. GameInstance는 맵을 넘는 상태를, 맵별 `BP_PrototypeManager`는 주문·조리·접시·UI를 관리합니다.

`BP_PrototypeManager`의 `base_price`, `arrival_interval`, `bonus_fast_seconds`, `bonus_slow_seconds`, `bonus_fast`, `bonus_slow` 기본값으로 현재 수치를 확인할 수 있습니다. 에디터에서 한 실험용 변경을 계속 유지하려면 `build_manager.py`에도 반영합니다. 전용 Blueprint의 수동 그래프·기본값 변경은 생성기를 다시 실행하면 소스로 돌아갑니다.

두 맵은 전용 GameMode·Controller를 연결하고 Pawn 자동 생성을 끕니다. 배치된 기존 `BP_JongguPlayer`와 `SourceCamera`를 사용합니다. 새 Actor는 `JongguPrototype`·`JongguPrototypeId:` 태그로 관리합니다. 기존 그림·충돌·플레이어·카메라와 Unity 마커는 별도 소유이며, `layout.json`의 상호작용 위치를 맞추기 위해 원본 그림이나 충돌을 이동하지 않습니다.

현재 충돌·플레이어 에셋에 대응하는 최신 로컬 작성 자료는 `Saved/CollisionWork/Data`·`Tools`에 있습니다. 이 자료의 `migration_config.json`에도 이전 `D:/laeti-dev/jproject` 대상 경로가 남아 있으므로 별도 재생성 전에 현재 프로젝트 경로와 에셋 버전을 대조해야 합니다. 외부 `D:/laeti-dev/Migration/Jonggu`는 읽기 전용 역사 자료입니다. 외부 생성기나 `run_migration.ps1`을 현재 프로젝트에 그대로 실행하지 않습니다.

## 팝업 디자인

메뉴·팬·냄비·일시정지·결산은 공통 베이지 픽셀 프레임과 Galmuri11을 사용합니다. 선택 체크, 버튼 호버·누름, 비활성 이유, 메뉴·기구 사용 요약을 표시합니다. 일반 HUD도 Unity Hub의 갈색 픽셀 패널·코인·하단 조작 구성을 이어받아 날짜·매출·주문·기구·접시·조개를 표시합니다. 디자인 기준과 해상도별 실제 Play 결과는 [POPUP_DESIGN.md](D:/laeti-dev/ProjectJ/Tools/Prototype/POPUP_DESIGN.md)에 있습니다.

메인 HUD의 배치와 실제 Play 화면은 [MAIN_HUD_DESIGN.md](D:/laeti-dev/ProjectJ/Tools/Prototype/MAIN_HUD_DESIGN.md)에 있습니다. 화면 오른쪽 아래 일시정지 버튼도 Esc와 같은 전체 일시정지를 실행합니다. UI 위치와 클릭 가능한 영역은 `hud_layout.py`의 공유 정의를 기준으로 변경합니다. 일반 HUD는 주문·기구·소지품을 각각 클릭해 접고 펼칠 수 있으며, 플레이어와 겹친 패널은 자동으로 흐려집니다. 팬·냄비 위에는 남은 조리 시간과 완료 수량을 표시합니다. 상세 동작은 [ADAPTIVE_HUD.md](D:/laeti-dev/ProjectJ/Tools/Prototype/ADAPTIVE_HUD.md)에 있습니다.

## 검증 기록과 범위

최종 결과와 검증 한계는 [VALIDATION.md](D:/laeti-dev/ProjectJ/Tools/Prototype/VALIDATION.md), 실제 운영 비교와 사람의 관찰 양식은 [PLAYTEST.md](D:/laeti-dev/ProjectJ/Tools/Prototype/PLAYTEST.md)에 있습니다.

[build_report.json](D:/laeti-dev/ProjectJ/Saved/PrototypeQA/build_report.json)은 생성 단계·Blueprint 컴파일·구간 저장 검사·맵 배치 결과를 기록합니다. [pie_report.json](D:/laeti-dev/ProjectJ/Saved/PrototypeQA/pie_report.json)은 Play 중 순환·상호작용·채집·UI·영업 검사와 캡처를 기록합니다. 보고서는 로컬 실행 결과이며, 파일 존재만으로 모든 검증이 완료되었다고 판단하지 않습니다. 실행 시각·실패 항목·물리 키보드 검증 여부를 함께 확인합니다.

기존 이동·충돌 검증의 미통과 항목과 배치 근거는 [BASELINE.md](D:/laeti-dev/ProjectJ/Tools/Prototype/BASELINE.md)에 구분되어 있습니다. 맵 재열기·기존 카메라·이동·가림 유지, 일시정지 후 조작 복원, 저장 복제 방지는 프로토타입 기능과 함께 검증합니다. 팬을 공유하는 메뉴와 팬·냄비를 나눠 쓰는 메뉴, 기본 찌개와 특선이 실제 작업 순서를 바꾸는지는 별도 플레이 관찰로 기록합니다.

이번 목표는 Unreal 에디터 Play입니다. 성장 업그레이드·물때·두 번째 채집 재료·수동 조리 미니게임·관계 성장·수영·승선·물체 밀기·독립 실행 파일은 포함하지 않습니다.
