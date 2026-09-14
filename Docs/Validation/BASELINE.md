# 기존 배치·이동·충돌 기준

최초 배치 기록은 **2026-09-13 23:56:12 KST**이며 식당 기능으로 Unreal 애셋을 변경하기 전 상태입니다. 이 문서는 기존 구성과 미통과 항목을 구분하기 위한 기준입니다. 현재 기능 검증 상태는 [VALIDATION.md](VALIDATION.md)에 있습니다.

기존 `Saved/CollisionWork` 자료는 로컬 `Saved/Archives/CollisionWorkBeforeRefactor.zip`에 보관했습니다. 현재 작성 소스는 [Tools/World](../../Tools/World/README.md), 식당 배치 데이터는 [layout.json](../../SourceData/Restaurant/layout.json)입니다. `Saved/` 증거와 백업은 버전 관리 대상이 아니므로 새 클론에는 없을 수 있습니다.

## 자동 배치 계산과 당시 화면 검토

- 맵 패키지: `/Game/Jonggu/Maps/L_Hub`, `/Game/Jonggu/Maps/L_Beach`.
- 당시 충돌 프리미티브는 Hub 16개, Beach 74개였습니다. 다시 계산한 규칙·manifest의 ID, 중심, 크기는 이전 실제 PIE 읽기 결과와 0.02cm 이내로 일치했습니다.
- 모든 상호작용 지점·도착점·손님 위치에 대해 최대 5cm 간격의 경로 표본 **2496개**를 검사했습니다. 플레이어 반경 24cm에 필수 여유 2cm를 더했습니다. 실제 PIE 프리미티브의 부호 있는 거리 계산과 재생성한 명세의 26cm 원·OBB 검사가 일치했습니다. 지점·경로별 여유 거리와 원본 SHA256은 `layout.json`에 있습니다.
- 기존 GPU 이미지 `Saved/CollisionWork/QA/StaticDepth/Hub_current_RGB.png`, `Beach_current_RGB.png`를 육안으로 확인했습니다. 모든 식당 기능 위치는 기존 3200×1800cm 카메라 프레임 안에 있습니다. 해변 도착점과 조개 3곳은 오른쪽 모래 위이며, 원본 플레이어 위치 `(1750,0,1200)`은 런타임 진입 위치 적용 전까지 화면 밖에 남아 있습니다.
- 허브는 원본 팬·냄비 그림, 메뉴판, 왼쪽·중앙 테이블을 사용합니다. 부엌 진입 경로는 X=0을 지난 뒤 Z=470입니다. 상호작용 좌표는 바닥 위치이며 원본 그림이나 충돌을 그 좌표로 옮기지 않았습니다. 손님 위치는 테이블 뒤 통행 가능한 바닥이고 손님은 비충돌입니다. 기존 의자 스프라이트는 유지했습니다.
- 허브 귀환점 `(-715,0,-520)`은 포털 `(-715,0,-700)`에서 180cm 떨어져 130cm 상호작용 반경 밖입니다. 해변 도착점 `(550,0,400)`은 귀환 포털 `(350,0,400)`에서 200cm 떨어져 있습니다. 포털은 겹침으로 자동 발동하지 않고 직접 상호작용합니다.

이 결과는 배치 기하 계산과 당시 이미지 검토의 근거입니다. 물리 키 입력, UI 포커스, 손님 애니메이션, 모든 가림 경계의 수동 순회를 검증한 기록은 아닙니다. 이후 맵 재열기와 식당 기능 PIE 검사는 현재 검증 인덱스에서 별도로 확인합니다.

## 기존 실패와 변경 전 재현

이전 `Saved/CollisionWork/QA/collision_pie_report.json`은 이미 `success=false`였으며 실패는 다음 한 항목이었습니다.

- 검사: `up_only_corner_case_exercises_sideways_collision_slide`
- 맵·시나리오: `Beach` / `left_then_up_only_corner`
- 시작: `[-345.0, 0.0, 536.0]`
- 입력: 왼쪽 이동 후 0.18초에 위쪽으로 전환, 1.0초에 해제, 1.45초에 종료
- 대상 충돌 ID: `collision:Beach:water:280:234:312:354`

이 항목은 해당 시나리오가 실제 옆 방향 충돌 미끄러짐을 일으켰는지를 검사합니다. 실패만으로 입력 방향 처리 결함이 입증되는 것은 아닙니다.

식당 변경 전에 같은 시나리오를 실제 PIE에서 재실행한 [baseline_collision.json](../../Saved/PrototypeQA/baseline_collision.json)은 **594개 검사 중 3개 미통과**를 기록했습니다.

1. `horizontal_intent_sets_expected_initial_facing`
2. `up_only_intent_faces_back_despite_collision_velocity`
3. `up_only_corner_case_exercises_sideways_collision_slide`

새 재현의 첫 이동 표본은 0.4초로 설정된 0.18초 방향 전환보다 늦었습니다. 이 표본 시각의 한계와 이전 실패를 식당 기능의 새 회귀와 구분합니다. 이 기준 작성에서는 이동 코드와 시나리오 좌표를 바꾸지 않았으며, 이후 해결됐다는 검증 기록도 없습니다. 원본 이동 전체가 통과한 것으로 해석하지 않습니다.

## 과거 자료의 범위와 보존 조건

- 기준 시점 플레이어 애셋·빌드 보고서 버전은 `blueprint-player-walk-9-input-facing`입니다. 더 오래된 outside-Migration v5와 stage 보고서 본문의 v8 성공 기록은 현재 전체 통과 근거가 아닙니다.
- 당시 stage cleanup 보고서는 `maps_reopen_verified=false`였습니다. 독립 `unreal_readback.json`, `walk_pie_report.json`, `cleanup_reference_readback.json`도 초기 기준에는 없었습니다. 별도 `import_report`의 가져오기 2회 성공·맵 재열기 기록과 이 범위를 혼동하지 않습니다.
- 원본 `SourceCamera`, 플레이어 그림, 충돌, Unity 마커 위치를 보존하며 게임용 Actor는 별도 소유권으로 구분합니다. 신규 Actor에 `JongguMigration:` 태그를 사용하지 않습니다.
- 초기에는 이전 마이그레이션 경로 정리가 필요했으나 현재 소스 경로는 [구조 정리 기록](REFACTOR.md)에 따라 승격됐습니다. 전체 마이그레이션과 원본 플레이어 재생성은 식당 빌드에 포함하지 않습니다.
