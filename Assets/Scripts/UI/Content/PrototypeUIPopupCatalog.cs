public readonly struct PrototypeUIPopupDefinition
{
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

public readonly struct PrototypeUIPreviewContent
{
    public PrototypeUIPreviewContent(string leftText, string rightText)
    {
        LeftText = leftText;
        RightText = rightText;
    }

    public string LeftText { get; }
    public string RightText { get; }
}

/*
 * 허브 팝업 제목, 캡션, 편집 모드 샘플 문구를 한곳에서 관리합니다.
 */
public static class PrototypeUIPopupCatalog
{
    public static PrototypeUIPopupDefinition GetDefinition(PrototypeUIPreviewPanel panel)
    {
        return panel switch
        {
            PrototypeUIPreviewPanel.Storage => new PrototypeUIPopupDefinition("창고", "- 보관 목록", "- 보관 정보"),
            PrototypeUIPreviewPanel.Recipe => new PrototypeUIPopupDefinition("요리 메뉴", "- 메뉴 목록", "- 금일 메뉴"),
            PrototypeUIPreviewPanel.Upgrade => new PrototypeUIPopupDefinition("업그레이드", "- 업그레이드 목록", "- 업그레이드 정보"),
            _ => new PrototypeUIPopupDefinition("재료", "- 재료 목록", "- 재료 정보")
        };
    }

    public static PrototypeUIPreviewContent GetPreviewContent(PrototypeUIPreviewPanel panel)
    {
        return panel switch
        {
            PrototypeUIPreviewPanel.Storage => new PrototypeUIPreviewContent(
                "- 말린 허브 x6\n- 조개 x9\n- 버섯 x3",
                "맡길 재료: 허브 x2\n꺼낼 재료: 조개 x1\n\nQ 품목 변경\nW 맡기기\nA 꺼낼 재료 변경\nS 꺼내기"),
            PrototypeUIPreviewPanel.Recipe => new PrototypeUIPreviewContent(
                "- [선택] 허브 조개찜\n- 버섯 수프\n- 바다 샐러드",
                "허브 조개찜\n해풍 허브를 얹은 대표 메뉴입니다.\n\n- 판매가: 24\n- 평판: +2\n- 가능 수량: 2\n- 필요 재료\n  조개 4/2\n  허브 2/1"),
            PrototypeUIPreviewPanel.Upgrade => new PrototypeUIPreviewContent(
                "- 가방 확장\n- 랜턴 해금\n- 작업대 보강",
                "다음 행동: 가방 확장\n지금 바로 진행 가능\n\n조개 상자를 묶어 12칸까지 넓힙니다."),
            _ => new PrototypeUIPreviewContent(
                "- 조개 x4\n- 허브 x2\n- 버섯 x1\n- 해초 x3",
                "가방 4/8칸\n선택 메뉴: 허브 조개찜\n- 필요 재료\n  조개 4/2\n  허브 2/1")
        };
    }

    public static string GetExplorationInventoryPreviewText()
    {
        return "- 조개 x4\n- 허브 x2\n- 버섯 x1";
    }
}

