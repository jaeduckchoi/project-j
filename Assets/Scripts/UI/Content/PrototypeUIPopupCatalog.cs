using UI.Controllers;

namespace UI.Content
{
    /*
     * 팝업 제목과 좌우 캡션 묶음을 에디터 프리뷰용으로 전달한다.
     */
    public readonly struct PrototypeUIPopupDefinition
    {
        /*
         * 각 패널에서 재사용할 헤더 문구를 구조체로 고정한다.
         */
        public PrototypeUIPopupDefinition(string title, string leftCaption, string rightCaption)
        {
            Title = title;
            LeftCaption = leftCaption;
            RightCaption = rightCaption;
        }

        public string Title { get; }
        public string LeftCaption { get; }
        public string RightCaption { get; }
    }

    /*
     * 좌우 본문 샘플 텍스트를 프리뷰 패널 단위로 전달한다.
     */
    public readonly struct PrototypeUIPreviewContent
    {
        /*
         * 실제 게임 데이터가 없어도 확인할 수 있는 예시 문구를 보관한다.
         */
        public PrototypeUIPreviewContent(string leftText, string rightText)
        {
            LeftText = leftText;
            RightText = rightText;
        }

        public string LeftText { get; }
        public string RightText { get; }
    }

    /*
     * 허브 팝업 제목, 캡션, 프리뷰 샘플 문구를 한 곳에서 관리한다.
     */
    public static class PrototypeUIPopupCatalog
    {
        /*
         * 패널 종류에 따라 팝업 제목과 좌우 캡션을 반환한다.
         */
        public static PrototypeUIPopupDefinition GetDefinition(PrototypeUIPreviewPanel panel)
        {
            return panel switch
            {
                PrototypeUIPreviewPanel.Storage => new PrototypeUIPopupDefinition("창고", "보관 목록", "보관 상세"),
                PrototypeUIPreviewPanel.Recipe => new PrototypeUIPopupDefinition("요리 메뉴", "메뉴 목록", "메뉴 상세"),
                PrototypeUIPreviewPanel.Upgrade => new PrototypeUIPopupDefinition("업그레이드", "업그레이드 목록", "업그레이드 상세"),
                _ => new PrototypeUIPopupDefinition("재료", "재료 목록", "재료 상세")
            };
        }

        /*
         * 에디터 프리뷰에서 보이는 좌우 예시 본문을 구성한다.
         */
        public static PrototypeUIPreviewContent GetPreviewContent(PrototypeUIPreviewPanel panel)
        {
            return panel switch
            {
                PrototypeUIPreviewPanel.Storage => new PrototypeUIPreviewContent(
                    "말린 허브 x6\n- 조개 x9\n- 버섯 x3",
                    "현재 선택: 말린 허브 x6\n후반부에 안정적으로 보관 중\n\n맡기기 후보: 허브 x2\nW 맡기기\n\n꺼내기 후보: 조개 x1\nS 꺼내기"),
                PrototypeUIPreviewPanel.Recipe => new PrototypeUIPreviewContent(
                    "[선택] 허브 조개찜\n- 버섯 수프\n- 바다 샐러드",
                    "허브 조개찜\n해변 허브를 넣어 만드는 대표 메뉴입니다.\n\n판매가 24골드 / 평판 +2\n가방 소모량 2\n\n필요 재료\n조개 4/2\n허브 2/1"),
                PrototypeUIPreviewPanel.Upgrade => new PrototypeUIPreviewContent(
                    "가방 확장\n- 손님 끌기\n- 작업대 보강",
                    "다음 행동: 가방 확장\n지금 바로 진행 가능\n\n필요 재료\n조개 6/4\n버섯 3/2\n\n효과\n가방을 12칸으로 확장"),
                _ => new PrototypeUIPreviewContent(
                    "조개 x4\n- 허브 x2\n- 버섯 x1\n- 베리 x3",
                    "가방 4/8칸\n보유 재료 정리 중\n\n선택 메뉴: 허브 조개찜\n가방 소모량 2\n\n필요 재료\n조개 4/2\n허브 2/1")
            };
        }

        /*
         * 탐험 HUD 재료 카드 프리뷰에 쓸 짧은 예시 문구다.
         */
        public static string GetExplorationInventoryPreviewText()
        {
            return "조개 x4\n- 허브 x2\n- 버섯 x1";
        }
    }
}
