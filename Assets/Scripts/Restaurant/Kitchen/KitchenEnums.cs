namespace Restaurant.Kitchen
{
    public enum KitchenToolType
    {
        Refrigerator,
        FrontCounter,
        CuttingBoard,
        Pot,
        FryingPan,
        Fryer
    }

    public enum KitchenItemState
    {
        Raw,
        Prepped,
        Cooked,
        FinalDish
    }

    public enum KitchenProgressMode
    {
        ManualHold,
        AutoProgress,
        FinalizeOnly
    }
}
