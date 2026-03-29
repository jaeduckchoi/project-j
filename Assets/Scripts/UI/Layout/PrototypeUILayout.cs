using UnityEngine;

/*
 * UI와 Popup 레이아웃이 공통으로 쓰는 RectTransform 좌표 타입입니다.
 */
namespace UI.Layout
{
public readonly struct PrototypeUIRect
{
    // 앵커, 피벗, 위치, 크기를 한 번에 넘겨 레이아웃 상수를 단순화한다.
    public PrototypeUIRect(Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        AnchorMin = anchorMin;
        AnchorMax = anchorMax;
        Pivot = pivot;
        AnchoredPosition = anchoredPosition;
        SizeDelta = sizeDelta;
    }

    public Vector2 AnchorMin { get; }
    public Vector2 AnchorMax { get; }
    public Vector2 Pivot { get; }
    public Vector2 AnchoredPosition { get; }
    public Vector2 SizeDelta { get; }
}

/*
 * 실제 배치 값은 UI/HUD 파일과 Popup 파일로 나눠 관리합니다.
 */
public static partial class PrototypeUILayout
{
}
}
