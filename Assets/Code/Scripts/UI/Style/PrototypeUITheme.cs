using UnityEngine;

namespace UI.Style
{
    /// <summary>
    /// 씬 이름만으로 HUD 기본색과 강조색을 고를 수 있게 공용 테마 팔레트를 둡니다.
    /// </summary>
    public readonly struct PrototypeUITheme
    {
        /// <summary>
        /// HUD에서 반복해서 쓰는 배경색과 강조색 세트를 한 번에 전달한다.
        /// </summary>
        public PrototypeUITheme(
            Color parchment,
            Color paper,
            Color glass,
            Color text,
            Color oceanAccent,
            Color forestAccent,
            Color amberAccent,
            Color coralAccent,
            Color goldAccent,
            Color dock,
            Color actionText)
        {
            Parchment = parchment;
            Paper = paper;
            Glass = glass;
            Text = text;
            OceanAccent = oceanAccent;
            ForestAccent = forestAccent;
            AmberAccent = amberAccent;
            CoralAccent = coralAccent;
            GoldAccent = goldAccent;
            Dock = dock;
            ActionText = actionText;
        }

        public Color Parchment { get; }
        public Color Paper { get; }
        public Color Glass { get; }
        public Color Text { get; }
        public Color OceanAccent { get; }
        public Color ForestAccent { get; }
        public Color AmberAccent { get; }
        public Color CoralAccent { get; }
        public Color GoldAccent { get; }
        public Color Dock { get; }
        public Color ActionText { get; }
    }

    /// <summary>
    /// 탐험 지역마다 다른 색 분위기를 주되 HUD 구조는 그대로 유지한다.
    /// </summary>
    public static class PrototypeUIThemePalette
    {
        /// <summary>
        /// 씬 이름에 맞는 기본 테마를 반환하고, 모르는 씬은 공통 기본값으로 떨어진다.
        /// </summary>
        public static PrototypeUITheme GetForScene(string sceneName)
        {
            return sceneName switch
            {
                "Beach" => new PrototypeUITheme(
                    new Color(0.99f, 0.97f, 0.90f, 1f),
                    new Color(1.00f, 0.98f, 0.94f, 1f),
                    new Color(0.89f, 0.95f, 1.00f, 1f),
                    new Color(0.18f, 0.26f, 0.35f, 1f),
                    new Color(0.15f, 0.63f, 0.92f, 1f),
                    new Color(0.23f, 0.67f, 0.46f, 1f),
                    new Color(0.98f, 0.73f, 0.27f, 1f),
                    new Color(0.95f, 0.42f, 0.31f, 1f),
                    new Color(0.80f, 0.73f, 0.25f, 1f),
                    new Color(0.18f, 0.59f, 0.86f, 1f),
                    Color.white),
                "DeepForest" => new PrototypeUITheme(
                    new Color(0.93f, 0.97f, 0.90f, 1f),
                    new Color(0.95f, 0.98f, 0.93f, 1f),
                    new Color(0.88f, 0.95f, 0.87f, 1f),
                    new Color(0.18f, 0.26f, 0.20f, 1f),
                    new Color(0.18f, 0.58f, 0.49f, 1f),
                    new Color(0.25f, 0.64f, 0.31f, 1f),
                    new Color(0.80f, 0.65f, 0.20f, 1f),
                    new Color(0.67f, 0.28f, 0.34f, 1f),
                    new Color(0.52f, 0.73f, 0.24f, 1f),
                    new Color(0.21f, 0.48f, 0.31f, 1f),
                    Color.white),
                "AbandonedMine" => new PrototypeUITheme(
                    new Color(0.90f, 0.92f, 0.95f, 1f),
                    new Color(0.93f, 0.95f, 0.97f, 1f),
                    new Color(0.84f, 0.89f, 0.94f, 1f),
                    new Color(0.20f, 0.23f, 0.29f, 1f),
                    new Color(0.31f, 0.71f, 0.78f, 1f),
                    new Color(0.47f, 0.63f, 0.44f, 1f),
                    new Color(0.85f, 0.64f, 0.22f, 1f),
                    new Color(0.74f, 0.35f, 0.28f, 1f),
                    new Color(0.73f, 0.74f, 0.35f, 1f),
                    new Color(0.28f, 0.35f, 0.45f, 1f),
                    Color.white),
                "WindHill" => new PrototypeUITheme(
                    new Color(0.94f, 0.98f, 1.00f, 1f),
                    new Color(0.97f, 0.99f, 1.00f, 1f),
                    new Color(0.90f, 0.96f, 1.00f, 1f),
                    new Color(0.19f, 0.26f, 0.35f, 1f),
                    new Color(0.26f, 0.68f, 0.95f, 1f),
                    new Color(0.44f, 0.72f, 0.38f, 1f),
                    new Color(0.95f, 0.78f, 0.25f, 1f),
                    new Color(0.86f, 0.38f, 0.44f, 1f),
                    new Color(0.63f, 0.82f, 0.44f, 1f),
                    new Color(0.34f, 0.70f, 0.92f, 1f),
                    Color.white),
                _ => new PrototypeUITheme(
                    new Color(0.96f, 0.97f, 0.99f, 1f),
                    new Color(0.98f, 0.98f, 0.99f, 1f),
                    new Color(0.93f, 0.95f, 0.98f, 1f),
                    new Color(0.23f, 0.27f, 0.34f, 1f),
                    new Color(0.18f, 0.66f, 0.90f, 1f),
                    new Color(0.19f, 0.74f, 0.46f, 1f),
                    new Color(0.94f, 0.74f, 0.10f, 1f),
                    new Color(0.93f, 0.24f, 0.39f, 1f),
                    new Color(0.54f, 0.80f, 0.25f, 1f),
                    new Color(0.22f, 0.60f, 0.87f, 1f),
                    Color.white)
            };
        }
    }
}
