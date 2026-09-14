# 메인 HUD 디자인과 실제 Play 검증

> **보관 상태:** 2026-09-14 당시 구현·검증 기록 · 2026-09-15 문서 통합 때 보관
> 현재 UI 기준은 [UI.md](../../Design/UI.md), 최신 검증 상태는 [검증 색인](../../Validation/VALIDATION.md)을 따릅니다.
> 본문의 검사 수와 “현재·최종·완료”는 당시 시점을 뜻하며, 이번 문서 정리에서 재검증한 결과가 아닙니다. `Saved`의 최신 보고서 파일은 후속 실행으로 바뀔 수 있습니다. 링크만 현재 문서 위치에 맞추었으며 작성 소스의 옛 이름은 이력으로 남깁니다.
> 이 기록의 “대상이 없을 때 E 기본 안내”는 후속 요약 HUD에서 취소되었습니다. 현재는 안내 내용이 없으면 숨깁니다.

이 문서는 최초 메인 HUD 이관 기록입니다. 이후 적용한 요약·접기·자동 투명화와 기구 위 조리 말풍선은 [ADAPTIVE_HUD.md](ADAPTIVE_HUD.md)를 참고하세요.

Unity 참고 프로젝트의 Hub HUD에서 갈색 픽셀 패널, 코인 표시, 화면 가장자리의 상태 카드와 하단 조작 구성을 가져왔습니다. 현재 Unreal에 이관되어 있던 `dark_thin_outline_panel`, `dark_solid_panel`, `system_text_box`, `coin` 텍스처를 `/Game/Jonggu/UI/Textures`에 UI 전용 자산으로 복제했습니다. 정확한 원본·대상 경로는 [build_popup_assets.py](../../../Content/Python/jonggu/ui/assets.py)의 `TEXTURE_SOURCES`에 있습니다. 패널은 모서리를 유지하는 9분할로 표시하고, 음식 그림과 Galmuri11 글꼴은 기존 팝업 자산을 공유합니다. Unity 원본은 읽기 전용 참고 자료입니다.

참고 기준은 외부 Unity 원본 위치를 나타내는 `JONGGU_UNITY_SOURCE` 설정 아래의 `Assets/Level/Scenes/Hub.unity`와 `Assets/Resources/Generated/ui-layout-bindings.asset`의 실제 저장 상태입니다. 우측 코인 패널·하단 버튼 묶음·종이 안내 상자를 현재 게임의 메뉴 선택·영업 조절에 맞췄습니다. 기존 Unity의 성장·재료 관리 기능은 이번 게임 범위에 추가하지 않았습니다.

[build_ui.py](../../../Content/Python/jonggu/ui/hud.py)의 `HUD_RECTS`와 `BUTTONS`가 배치와 클릭 영역의 기준입니다. 날짜·영업 상태·매출, 손님 두 자리의 음식과 대기시간, 팬·냄비 진행률과 남은 그릇, 조개와 든 접시를 표시합니다. 아래에는 상호작용 안내와 메뉴 선택·유입 조절·마감·일시정지 조작을 둡니다. 화면 0의 버튼은 팝업이 열리면 입력 대상에서 빠지고, 기존 팝업이 HUD 위에 그려집니다.

표시 데이터는 `BP_RestaurantManager.RefreshHUD`가 세션과 실제 주문·접시 객체에서 전달합니다. `day`, `elapsed`, `served_count`, `guest_count`, `interaction_hint`와 음식 ID·특선 여부를 사용하며, 빈손·빈 기구·비활성 주문의 음식 ID는 `-1`, 비활성 주문의 대기시간은 `0`입니다. 매출은 영업 중에만 `total_revenue + base_revenue + bonus_revenue`를 표시하고, 결산에서는 이미 합산된 `total_revenue`만 표시합니다. 따라서 오늘 수입을 결산 뒤에 다시 더하지 않습니다. 게임 규칙과 조개 재고를 HUD에서 변경하지 않습니다.

[verify_popup_pie.py](../../../Content/Python/jonggu/validation/verify_popup_pie.py)를 새 QA 에디터에서 아래 인수로 실행합니다. 아래 스크립트 경로는 프로젝트 루트 기준입니다. 기존 `-JongguPopupQA` 모드는 별도로 유지됩니다.

```text
-JongguHUDQA -RenderOffscreen -PopupSizes=1280x720,1920x1080,1189x862 -ExecutePythonScript="Content/Python/jonggu/validation/verify_popup_pie.py"
```

[verify_main_hud.py](../../../Content/Python/jonggu/validation/verify_main_hud.py)는 준비, 주문 2건, 두 기구 조리 중, 특선 완성품·운반 접시, 유입 중단, 마감 중, 메뉴 팝업, 해변의 8개 상태를 세 해상도에서 캡처합니다. 1회 전체 실행의 예상 PNG는 24개입니다. 실제 Blueprint의 `Click`, `Advance`, `Pickup`, `Serve`, `Finish`를 호출하며, 해변은 실제 `Travel`과 채집으로 진입합니다. 각 캡처에 현재 HUD 필드, 원본 주문·접시 자료, 버튼 영역과 뷰포트·PNG 크기를 함께 기록합니다.

별도 7개 검사는 일시정지 열기·복귀, 팝업 뒤 일반 버튼 숨김, 영업 중 수입 반영, 서빙 후 남는 잘못된 음식 아이콘, 결산 수입의 중복 합산, 결산 뒤 음식 정리를 확인합니다. 모든 캡처에서도 표시 필드를 실제 객체와 대조합니다. 촬영 동안 입력·영업 Tick을 멈춰 상태를 고정하지만, HUD는 실제 PIE 뷰포트에 계속 렌더링합니다. 창 크기는 최대 3회 측정·보정하며 요청한 뷰포트와 PNG 크기가 정확히 일치해야 통과합니다. `-RenderOffscreen`은 물리 모니터보다 넓은 Unreal 가상 화면을 제공하고, 이미지를 사후 확대·축소하지 않습니다.

출력은 `Saved/MainHUDQA/After/<UTC>/capture_report.json`과 상태별 PNG이며, 최신 보고서는 `Saved/MainHUDQA/latest_capture_report.json`입니다. 검증용 저장 슬롯은 실행마다 분리하고 종료 때 삭제합니다. 에디터 설정과 전역 해상도는 메모리에서 복원하며, QA 에디터 종료 후 보고서에 기록된 `EditorPerProjectUserSettings.before.ini`를 원래 설정 경로에 복원합니다.

2026-09-14 최종 구현은 조리 중 남은 시간과 완성 수량을 구분합니다. 접시·조개 카드는 오른쪽에 배치하여 시작 위치의 캐릭터를 가리지 않으며, 가까운 대상이 없으면 E 상호작용 기본 안내를 표시합니다. 물리 키보드·마우스로 하는 수동 플레이는 수행하지 않았으며, 조작감과 재미에 대한 사람의 평가는 이 자동 검증에 포함하지 않습니다.

최종 검증은 구간 저장 19개, 기존 실제 Play 120개(영업 46·팝업 43·맵/이동 31), 메인 HUD 실제 Blueprint 7개를 통과했습니다. 세 해상도에서 총 24개 실제 PNG의 크기와 표시 상태를 검증했습니다. 한글·음식 이름·픽셀 모서리, 조리 중/완성, 특선·빈 좌석, 해변, 유입 중단·마감, 팝업 겹침을 시각 확인했습니다. 기존 플레이어·GameMode 자산은 기준 SHA-256과 동일합니다.

[최종 결과](../../../Saved/MainHUDQA/final_report.json) · [현재 캡처 보고서](../../../Saved/MainHUDQA/latest_capture_report.json) · [변경 전 화면](../../../Saved/MainHUDQA/Before/preparation_1280.png)

![영업 중 메인 HUD](../../../Saved/MainHUDQA/After/20260914T125413_319713Z/1280x720/02_cooking_busy.png)

[1080p 준비 화면](../../../Saved/MainHUDQA/After/20260914T125413_319713Z/1920x1080/00_preparation.png) · [세로 여백과 특선](../../../Saved/MainHUDQA/After/20260914T125413_319713Z/1189x862/03_ready_special_and_held_plate.png) · [해변](../../../Saved/MainHUDQA/After/20260914T125413_319713Z/1280x720/07_beach.png)
