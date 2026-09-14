# 팝업 디자인 구현과 검증

Unity 참고 프로젝트의 실제 Hub 씬에 저장된 베이지 픽셀 디자인을 현재 Unreal 프로토타입의 메뉴·팬·냄비·일시정지·결산에 적용했습니다. 이 팝업 작업에서는 정상 상태 HUD와 영업 규칙, 저장 자료형을 유지했습니다. 이후 요청으로 추가한 메인 HUD 디자인은 [MAIN_HUD_DESIGN.md](D:/laeti-dev/ProjectJ/Tools/Prototype/MAIN_HUD_DESIGN.md)에 별도로 기록합니다.

## 디자인과 작성 소스

- `build_ui.py`: 공통 9분할 프레임·종이 카드·버튼, Galmuri11 Regular/Bold, 공유 `BUTTONS`와 `POPUP_PANELS`
- `build_popup_assets.py`: 기존 이관 이미지의 UI 전용 복제본과 갈무리 글꼴 가져오기
- `build_manager.py`: 기구 진행률·완성품·조개·선택 가능 여부·비활성 사유·결산 수치의 표시용 전달, 공유 영역으로 클릭 검사
- `build_world.py`: 실제 뷰포트와 검은 여백을 고려한 포인터 좌표 변환, 호버·누름 상태

1280×720 기준 메뉴·조리 창은 1120×536, 일시정지는 680×392, 결산은 840×500입니다. 제목 32px·본문 22px·보조 18px, 내부 여백 32px, 주요 실행 버튼 높이 48px 이상을 사용합니다. 프레임 원본 32×32의 모서리 8px을 따로 그려 늘어남을 막고, X 이미지는 투명 여백을 제외한 원본 영역을 표시합니다.

기본 색은 베이지 `#C4AF86`, 종이 `#F3ECDB`, 글자 `#1F1F1F`, 금색 `#E6CB87`이며 주요 버튼은 녹색 `#37583E`입니다. 팝업 뒤 월드와 HUD에 검정 52%를 적용합니다. 선택은 체크와 색으로, 호버·누름은 밝기와 테두리로 구별합니다. 비활성 조리에는 메뉴 미선택·기구 사용 중·조개 부족 등의 이유를 표시하고 입력을 막습니다.

메뉴 오른쪽에는 선택 메뉴·팬과 냄비 사용 방식·손님 수·조개를 표시합니다. 조리는 한 번의 클릭으로 시작하며 두 기구 모두 조개 수를 표시합니다. X는 메뉴·조리를 닫고 일시정지에서는 이전 화면으로 돌아갑니다. 결산에는 X가 없고 다음 날 준비만 주요 실행으로 제공합니다. 안내와 오류는 팝업 내부에 표시합니다.

Unity 원본은 읽기 전용으로 참고했습니다. 복사한 Regular/Bold TTF의 SHA-256은 원본과 동일하며, 기존 플레이어와 기존 GameMode 자산도 기준 해시와 같습니다. 원본 글꼴의 라이선스 안내를 `Resources/Fonts`에 함께 보관했습니다. 전체 마이그레이션은 실행하지 않았습니다.

## 검증 기록

2026-09-14 검증 완료: Blueprint 컴파일·저장 19개, 실제 Play 검사 120개(영업 46·팝업 43·맵/이동 31), 세 가지 해상도의 실제 PNG 33개가 통과했습니다. 각 팝업의 한글·음식 이름·모서리·버튼 상태·결산 여백을 시각 확인했습니다. 실행 결과는 `Saved/PopupDesignQA`와 `Saved/PrototypeQA`에 보관합니다. 최종 종합 기록은 `Saved/PopupDesignQA/final_report.json`입니다.

- `Saved/PrototypeQA/build_report.json`: Blueprint 컴파일, 맵 재열기, 저장 검사
- `Saved/PrototypeQA/pie_report.json`: 실제 Play에서 기존 영업 46개, 팝업 43개, 이동·맵 순환 31개 검사
- `Saved/PopupDesignQA/latest_capture_report.json`: 해상도와 상태별 실제 Play PNG 및 뷰포트 크기
- `Saved/PopupDesignQA/Before`: 변경 전 소스와 실제 Play 화면
- `Saved/PopupDesignQA/source_integrity.json`: 글꼴·기존 Blueprint 보존 확인

`verify_popup_pie.py`는 1280×720, 1920×1080, 1189×862 창에서 정상 HUD와 다섯 팝업, 호버·누름·조개 부족·조리 중·남은 완성품을 캡처합니다. 운영체제와 Slate의 창 테두리 차이를 실제 뷰포트 측정값으로 보정하며, PNG 크기가 요청값과 정확히 일치해야 통과합니다. 물리 모니터 작업 영역보다 큰 1080p 창은 `-RenderOffscreen`으로 Unreal의 가상 화면을 사용합니다. 엔진 RHI·Canvas가 실제 요청 크기의 PIE 뷰포트에 그린 PNG를 검사하며, 캡처 이미지를 사후 확대·축소하지 않습니다. 검증용 저장 슬롯은 분리하고 종료 시 지우며, 실행 후 기록된 에디터 설정 백업을 복원합니다.

물리 키보드·마우스의 수동 플레이와 재미에 대한 사람의 평가는 자동 검증 결과와 구분합니다. 기존 이동·충돌 검사에서 남아 있던 항목은 `BASELINE.md`를 따릅니다. 이번 팝업 변경으로 기존 플레이어 그래프를 수정하지 않았습니다.

## 최종 화면

![메뉴 선택](D:/laeti-dev/ProjectJ/Saved/PopupDesignQA/After/20260914T122845_991324Z/1280x720/01_menu_selected.png)

[변경 전 메뉴](D:/laeti-dev/ProjectJ/Saved/PopupDesignQA/Before/02_menu.png) · [1080p 냄비](D:/laeti-dev/ProjectJ/Saved/PopupDesignQA/After/20260914T122845_991324Z/1920x1080/03_pot.png) · [세로 여백 결산](D:/laeti-dev/ProjectJ/Saved/PopupDesignQA/After/20260914T122845_991324Z/1189x862/05_summary.png)
