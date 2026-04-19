namespace Restaurant.Kitchen
{
    /// <summary>
    /// 주방 상호작용 지점 또는 조리기구의 역할을 구분합니다.
    /// </summary>
    public enum KitchenToolType
    {
        Refrigerator,
        FrontCounter,
        CuttingBoard,
        Pot,
        FryingPan,
        Fryer
    }

    /// <summary>
    /// 허브 조리 흐름에서 다루는 주방 항목의 조리 상태를 나타냅니다.
    /// </summary>
    public enum KitchenItemState
    {
        Raw,
        Prepped,
        Cooked,
        FinalDish
    }

    /// <summary>
    /// 조리 진행 방식과 완료 판정 방식을 구분합니다.
    /// </summary>
    public enum KitchenProgressMode
    {
        ManualHold,
        AutoProgress,
        FinalizeOnly
    }
}
