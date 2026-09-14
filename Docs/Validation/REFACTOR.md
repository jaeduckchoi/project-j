# 구조·이름 정리 검증

최신 종합 기록: **2026-09-15 00:05:52 KST** (`2026-09-14T15:05:52Z`). 게임 플레이 자체를 유지하면서 제작 코드, 애셋 경로, Outliner 표시 이름, 원본 자료와 검증 산출물의 위치를 정리한 결과입니다. 게임 런타임은 Blueprint입니다. 이번 문서 정리에서는 아래 검사를 재실행하지 않았습니다.

## 변경 범위

- 제작 코드를 [Content/Python/jonggu](../../Content/Python/jonggu) 패키지로 옮겨 UI·게임플레이·월드·검증을 분리했습니다. HUD 진입 모듈은 810→72줄, 관리자 조립부는 554→74줄이며 실제 로직은 역할별 모듈에 있습니다.
- 게임용 애셋 29개를 `Core`, `Gameplay`, `Data`, `UI`로 이동하고 역할·음식·자료형을 드러내는 이름으로 정리했습니다. Actor 표시 이름과 폴더도 역할별로 정리했습니다.
- `BP_PrototypeSave`는 기존 `.sav`의 클래스 이름 호환 때문에 이름을 유지하고 `Core/Save`로 이동했습니다. 이전 패키지 경로는 [CoreRedirects](../../Config/DefaultEngine.ini)로 처리합니다.
- `Saved/CollisionWork`의 권위 있는 소스를 [Tools/World](../../Tools/World/README.md)로 승격했습니다. 기존 179개 파일은 로컬 `Saved/Archives/CollisionWorkBeforeRefactor.zip`에 보관했습니다. 옛 `Tools/Prototype`, 미사용 폰트, 빈 콘텐츠 폴더를 정리했습니다.
- 프로젝트·애셋 경로를 중앙화하고 엔진 자동 검색, 새 보고서 확인, 에디터 설정 복원을 수행하는 [run.ps1](../../Tools/Unreal/run.ps1)을 추가했습니다. 터미널 시작 경로도 현재 프로젝트에서 구합니다.

## 자동 검증 결과

| 검사 | 기록 시각 (KST) | 결과 |
|---|---|---|
| 소스·경로 | 2026-09-14 23:59:04 | Python 70개 문법, 내부 import 123개, 애셋 경로 29개, 다른 작업 폴더에서 21개 모듈 import 통과. 기존 UI 배치·계약 AST 동일 |
| 구조·이전 저장 | 2026-09-15 00:03:40 | **377개 통과**. 정리 전 SaveGame 바이트를 native 로드와 새 GameInstance의 `LoadCheckpoint`로 복원. 원본 플레이어·GameMode 해시, 원본 Actor 소유권·위치 보존. 남은 이전 경로 리다이렉터와 이전 패키지 의존성 0 |
| 실제 PIE 순환 | 2026-09-14 23:56:49 | 영업 46, 팝업 43, PIE 순환 31, 접기 상태의 맵 이동·재시도·다음 날·새 Play 6개 통과 |
| 저장·HUD 포함 기능 합계 | 종합 기록 기준 | 저장 19, 위 PIE 126, HUD 7, 요약·투명화 12를 합해 **164개 통과** |
| 실제 PIE 캡처 | 2026-09-14 23:58:04 시작 | 1280×720, 1920×1080, 1189×862에서 18개 상태씩 **54장**. 실제 뷰포트와 PNG 크기 확인 |

실제 게임 Blueprint를 호출해 검사했습니다. 캡처는 각 상태에서 게임 Tick을 고정하며, 시간 진행·전체 일시정지는 별도 런타임 검사로 확인합니다. 사용자 저장 파일은 전후 바이트가 동일하고 검증용 슬롯은 정리됐습니다. 캡처 종료 후 에디터 창 설정도 복원했습니다.

로컬 상세 증거: [종합](../../Saved/RefactorQA/final_report.json), [구조](../../Saved/RefactorQA/structure_report.json), [소스](../../Saved/RefactorQA/source_report.json), [PIE](../../Saved/RefactorQA/pie_report.json), [캡처](../../Saved/RefactorQA/capture_report.json). 최종 PNG 위치는 `Saved/AdaptiveHUDQA/After/20260914T145804_252627Z`입니다.

## 화면 검토와 미검증 범위

54장에는 요약·상세·혼합, 조리 진행·완료 수량, 플레이어 가림, 호버, 일시정지, 준비 상태가 포함됩니다. 캡처 이미지의 한글·배치·말풍선을 육안으로 확인했습니다. [20개 패널 영역 비교](../../Saved/RefactorQA/pixel_comparison.json)의 평균 RGB 차이는 최대 0.259/255이며 완전한 픽셀 일치를 주장하지 않습니다.

운영체제의 물리 키보드·마우스를 쓰는 수동 조작감 평가는 하지 않았습니다. 기존 충돌 기준의 594개 중 3개 미통과와 플레이어의 선택 이유·재미에 대한 미검증 상태는 각각 [BASELINE.md](BASELINE.md), [PLAYTEST.md](PLAYTEST.md)에 유지합니다.

## 재현 자료와 로컬 보관물

실행 명령과 엔진 경로 설정은 [제작 안내](../Development/AUTHORING.md#실행)를 따릅니다. 구조 검증 소스는 [verify_refactor.py](../../Content/Python/jonggu/validation/verify_refactor.py), 호스트 소스 검사는 [check_sources.py](../../Tools/Unreal/check_sources.py)입니다.

이전 저장과 원본 상태 fixture는 [Tools/Unreal/Fixtures](../../Tools/Unreal/Fixtures)에서 버전 관리합니다. Structure 검사는 지정된 테스트 슬롯이 없을 때만 정확한 fixture 바이트를 임시로 복사하고, 검사 후 자신이 만든 파일만 제거합니다. 사용자 슬롯은 대상으로 삼지 않습니다.

`Saved/`는 선택적 로컬 증거이며 새 클론에 없을 수 있습니다. `Saved/RefactorQA/BeforeRefactor.zip`은 정리 전 소스·설정·생성 애셋 백업이고, 같은 폴더에는 이동 이력과 검사 결과가 있습니다. `--compare-before`의 과거 AST 비교는 이 백업이 있을 때만 가능하며, 기본 검사의 필수 입력은 현재 소스와 버전 관리 fixture에 있습니다. 전체 마이그레이션과 기존 플레이어 생성기는 이번 정리에서 실행하지 않았습니다.
