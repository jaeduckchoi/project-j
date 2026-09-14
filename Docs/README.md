# 문서 안내

정리 기준일: 2026-09-15. 현재 Unreal 프로젝트에서 사용할 규칙과 과거 기록을 구분합니다. 작업을 시작할 때는 [루트 작업 지침](../AGENTS.md)을 읽고 아래에서 필요한 문서만 확인합니다.

## 현재 문서

| 필요한 내용 | 문서 | 담당 범위 |
|---|---|---|
| 프로젝트 열기·기본 조작 | [프로젝트 소개](../README.md) | 가장 짧은 실행 안내 |
| 게임 규칙·이번 범위 | [GAMEPLAY](Design/GAMEPLAY.md) | 준비·채집·영업·저장과 제외한 기능 |
| 화면·상호작용 표시 | [UI](Design/UI.md) | 팝업·요약 HUD·투명화·조리 말풍선 |
| 코드·애셋 이름과 책임 | [STRUCTURE](Development/STRUCTURE.md) | 작성 원본, 생성 결과, 경로·식별자·저장 호환 |
| 빌드·검증 실행 | [AUTHORING](Development/AUTHORING.md) | 수정할 소스와 실제 실행 명령 |
| 기존 맵·플레이어 작성 | [월드 도구](../Tools/World/README.md) | 충돌·정렬·이동 작성과 검증 입력 |
| 최신 확인 결과 | [VALIDATION](Validation/VALIDATION.md) | 날짜·검사 범위·통과·미통과·미실시 상태 |
| 구조 변경의 상세 증거 | [REFACTOR](Validation/REFACTOR.md) | 이전 저장 호환, 참조, 원본 보존, 캡처 |
| 기존 충돌 미통과 | [BASELINE](Validation/BASELINE.md) | 식당 변경 이전의 재현 기준 |
| 사람의 조작감·재미 관찰 | [PLAYTEST](Validation/PLAYTEST.md) | 진행 절차, 미실시 관찰, 후속 가설 |

현재 명세는 승인된 범위와 구현을 설명하며, 기능이 검증되었다는 증거는 검증 문서에서 확인합니다. 소스와 문서가 다르면 불일치를 기록하고 관련 문서를 함께 고칩니다. 검증 보고서의 과거 통과를 새 환경의 실행 결과로 취급하지 않습니다.

## 원본과 결과

문서의 로컬 파일 링크는 **해당 문서 위치를 기준으로 한 상대 경로**입니다. 명령 예제는 별도 표시가 없으면 프로젝트 루트에서 실행합니다. 파일 경로 표의 기준도 각 문서에 명시합니다. `/Game/Jonggu/...`는 Unreal 패키지 경로이며 PC의 절대 파일 경로가 아닙니다. 엔진·외부 Unity 위치는 환경 변수나 실행 인수로 지정하고 개인 드라이브 경로를 문서에 고정하지 않습니다.

[Content/Python/jonggu](../Content/Python/jonggu/)는 에디터 제작 소스, [SourceData](../SourceData/)는 작성 데이터, [SourceAssets](../SourceAssets/)는 원본·라이선스입니다. [Tools/Unreal/Fixtures](../Tools/Unreal/Fixtures/)와 [Tools/World/Fixtures](../Tools/World/Fixtures/)는 재현에 필요한 버전 관리 입력입니다.

`Saved/`의 보고서·PNG·백업은 **선택적 로컬 증거**여서 새 클론에는 없을 수 있습니다. 이름이 고정된 보고서는 재실행하면 교체됩니다. 이 경로를 작성 원본이나 필수 문서 저장 장소로 쓰지 않습니다. 개인 `.codex`·IDE 설정은 이 문서 체계와 별도로 관리합니다.

## 과거 자료

- [팝업 이관 기록](Archive/Design/POPUP_DESIGN.md)
- [최초 메인 HUD 기록](Archive/Design/MAIN_HUD_DESIGN.md)
- [요약 HUD·투명화 구현 기록](Archive/Design/ADAPTIVE_HUD.md)
- [첫 프로토타입 단계별 검증](Archive/Validation/2026-09-14-PROTOTYPE-HISTORY.md)
- [2026-09-13 기획 리서치](Archive/Research/2026-09-13-DESIGN-RESEARCH.md)

과거 문서는 당시 선택과 검증을 보존합니다. 리서치는 사용자 제공 참고 자료이며 이번 정리에서 외부 사실을 다시 검증하지 않았습니다. 채택하지 않은 제안을 구현 요구로 간주하지 않습니다. 현재 게임 규칙과 UI 명세가 이후 결정의 기준입니다.

## 갱신 규칙

규칙·UI·구조·실행 명령은 위의 담당 문서 한 곳에서 관리하고 다른 문서는 링크로 연결합니다. 새 기능이나 구조 변경과 함께 해당 문서를 갱신합니다. 검증 결과에는 실행 시각, 대상, 실제 수행 범위, 미통과·미실시를 남기고 자동 실행·이미지 확인·사람의 조작·재미 관찰을 구분합니다.

교체한 설계나 누적 작업 기록은 `Archive/` 아래로 옮기고 당시 날짜·보관 날짜·현재 기준 문서 링크를 붙입니다. 보관할 때에도 로컬 경로는 상대 경로로 고칩니다. 문서만 고친 작업에는 링크·경로·내용 검사를 적용하고 엔진 재실행 여부를 명시합니다.
