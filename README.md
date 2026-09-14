# 종구의 식당

Unreal Engine 5.8 / Paper2D로 만드는 PC 싱글플레이 식당 운영 게임입니다. 게임은 저장된 Blueprint로 실행하며 Python은 에디터 제작·검증에 사용합니다.

## 시작하기

[projectJ.uproject](projectJ.uproject)를 열고 Content Browser에서 `/Game/Jonggu/Maps/L_Hub`를 연 뒤 Play를 실행합니다. 준비 → 선택적 해변 채집 → 메뉴 결정 → 조리·서빙 → 결산 → 다음 날 준비로 이어집니다. 조개 없이 기본 메뉴로 영업할 수 있고, 조개 1개가 있으면 특선 찌개 두 그릇을 만들 수 있습니다.

| 조작 | 동작 |
|---|---|
| WASD / 방향키 | 이동 |
| E | 가까운 대상과 상호작용·완성품 수령·서빙 |
| 마우스 | 메뉴·조리·영업 버튼, HUD 접기·펼치기 |
| Esc / F10 | 전체 일시정지·이전 화면 복귀 |
| Shift+Esc | 에디터 Play 종료 |

저장은 프로젝트 루트 기준 `Saved/SaveGames/JongguPrototypeCheckpoint.sav` 한 슬롯입니다. 영업 재시도는 개점 직전 메뉴·재고·매출을 함께 복원합니다. 일반 Play와 식당 Build에는 외부 Unity 프로젝트가 필요하지 않습니다.

## 작업과 문서

- [프로젝트 작업 지침](AGENTS.md): 변경 범위, 원본 보존, 검증·기록 규칙
- [문서 안내](Docs/README.md): 현재 명세·제작 안내·검증·과거 기록의 목차
- [게임 규칙](Docs/Design/GAMEPLAY.md) · [팝업과 HUD 명세](Docs/Design/UI.md)
- [소스·애셋 구조와 이름](Docs/Development/STRUCTURE.md) · [제작·검증 실행](Docs/Development/AUTHORING.md)
- [현재 검증 상태](Docs/Validation/VALIDATION.md) · [수동 플레이 관찰](Docs/Validation/PLAYTEST.md)

지속적인 기능 변경은 제작 소스에 반영하고 Blueprint를 재생성합니다. 기본 식당 Build와 기존 월드·플레이어 작성은 분리되어 있습니다. `Saved/`에는 로컬 저장·로그·검증 결과가 쌓이며 버전 관리에 포함하지 않습니다.
