// 탐험에서 사용하는 상시 해금형 도구 종류다.
public enum ToolType
{
    None,
    Rake,
    FishingRod,
    Sickle,
    Lantern
}

// 도구 열거형을 UI 친화적인 문자열로 바꿔준다.
public static class ToolTypeExtensions
{
    /*
     * 도구 종류를 화면 표시용 이름으로 변환한다.
     */
    public static string GetDisplayName(this ToolType toolType)
    {
        return toolType switch
        {
            ToolType.Rake => "갈퀴",
            ToolType.FishingRod => "낚시대",
            ToolType.Sickle => "낫",
            ToolType.Lantern => "랜턴",
            _ => "도구 없음"
        };
    }
}
