using System.Collections.Generic;
using UnityEngine;

namespace Code.Scripts.Exploration.World
{
    /// <summary>
    /// 허브 씬의 논리 타일 좌표와 직렬화된 월드 좌표를 한 곳에서 관리한다.
    /// 허브는 32x18 타일 월드를 기준으로 하고, 1타일 = 1유닛 계약을 그대로 사용한다.
    /// </summary>
    public static class HubRoomLayout
    {
        public enum HubArtAnchor
        {
            BackgroundLayer,
            ObjectLayer,
            ForegroundLayer
        }

        public enum HubArtSpriteId
        {
            FloorBackground,
            WallBackground,
            Bar,
            TableUnlocked,
            FrontOutline
        }

        public readonly struct HubArtPlacement
        {
            public HubArtPlacement(string objectName, HubArtSpriteId spriteId, HubArtAnchor anchor, Vector3 localPosition, int sortingOrder)
            {
                ObjectName = objectName;
                SpriteId = spriteId;
                Anchor = anchor;
                LocalPosition = localPosition;
                SortingOrder = sortingOrder;
            }

            public string ObjectName { get; }
            public HubArtSpriteId SpriteId { get; }
            public HubArtAnchor Anchor { get; }
            public Vector3 LocalPosition { get; }
            public int SortingOrder { get; }
        }

        public readonly struct HubColliderPlacement
        {
            public HubColliderPlacement(string objectName, string parentObjectName, Vector3 localPosition, Vector3 scale)
            {
                ObjectName = objectName;
                ParentObjectName = parentObjectName;
                LocalPosition = localPosition;
                Scale = scale;
            }

            public string ObjectName { get; }
            public string ParentObjectName { get; }
            public Vector3 LocalPosition { get; }
            public Vector3 Scale { get; }
        }

        public readonly struct HubTablePlacement
        {
            public HubTablePlacement(
                string groupObjectName,
                string tableObjectName,
                string colliderObjectName,
                Vector3 localPosition,
                Vector3 colliderLocalPosition)
            {
                GroupObjectName = groupObjectName;
                TableObjectName = tableObjectName;
                ColliderObjectName = colliderObjectName;
                LocalPosition = localPosition;
                ColliderLocalPosition = colliderLocalPosition;
            }

            public string GroupObjectName { get; }
            public string TableObjectName { get; }
            public string ColliderObjectName { get; }
            public Vector3 LocalPosition { get; }
            public Vector3 ColliderLocalPosition { get; }
        }

        public readonly struct HubFloorSignPlacement
        {
            public HubFloorSignPlacement(
                string objectName,
                string content,
                Vector3 position,
                Vector3 backdropScale,
                Vector3 textLocalPosition,
                float fontSize,
                float textScale,
                float characterSpacing)
            {
                ObjectName = objectName;
                Content = content;
                Position = position;
                BackdropScale = backdropScale;
                TextLocalPosition = textLocalPosition;
                FontSize = fontSize;
                TextScale = textScale;
                CharacterSpacing = characterSpacing;
            }

            public string ObjectName { get; }
            public string Content { get; }
            public Vector3 Position { get; }
            public Vector3 BackdropScale { get; }
            public Vector3 TextLocalPosition { get; }
            public float FontSize { get; }
            public float TextScale { get; }
            public float CharacterSpacing { get; }
        }

        public readonly struct HubDecorBlockPlacement
        {
            public HubDecorBlockPlacement(
                string rootObjectName,
                Vector3 rootPosition,
                string objectName,
                Vector3 localPosition,
                Vector3 scale,
                Color color,
                int sortingOrder)
            {
                RootObjectName = rootObjectName;
                RootPosition = rootPosition;
                ObjectName = objectName;
                LocalPosition = localPosition;
                Scale = scale;
                Color = color;
                SortingOrder = sortingOrder;
            }

            public string RootObjectName { get; }
            public Vector3 RootPosition { get; }
            public string ObjectName { get; }
            public Vector3 LocalPosition { get; }
            public Vector3 Scale { get; }
            public Color Color { get; }
            public int SortingOrder { get; }
        }

        public const float TargetAspectWidth = 16f;
        public const float TargetAspectHeight = 9f;
        public const float TargetAspectRatio = TargetAspectWidth / TargetAspectHeight;

        public const int TilePixelsPerUnit = 64;
        public const int HubTileWidth = 32;
        public const int HubTileHeight = 18;
        // Hub.unity 는 32x18 타일 월드를 1타일 = 1유닛 기준으로 직렬화한다.
        public const float WorldUnitsPerTile = 1f;
        public const float ScreenWidth = HubTileWidth * WorldUnitsPerTile;
        public const float ScreenHeight = HubTileHeight * WorldUnitsPerTile;
        public const float CameraVisibleTileWidth = ScreenWidth;
        public const float CameraVisibleTileHeight = ScreenHeight;

        // 허브는 32x18 월드 전체가 1920x1080 한 화면에 들어오도록 잡는다.
        public static readonly Vector2 CameraSafePadding = Vector2.zero;
        public static readonly Vector3 CameraPosition = Vector3.zero;
        public static readonly Vector2 CameraSize = new(CameraVisibleTileWidth + (CameraSafePadding.x * 2f), CameraVisibleTileHeight + (CameraSafePadding.y * 2f));
        public static readonly float ScreenOrthographicSize = CameraSize.y * 0.5f;

        public static readonly Vector3 BackgroundPosition = Vector3.zero;
        public static readonly Vector3 BackgroundScale = Vector3.one;
        public const float FloorTilePixelsPerUnit = TilePixelsPerUnit;
        public static readonly Vector3 FloorTileScale = new(WorldUnitsPerTile, WorldUnitsPerTile, 1f);
        public static readonly Vector2 FloorTileTiledSize = new(HubTileWidth, HubTileHeight);
        public const string TableRootObjectName = "HubTableGroup";

        public static readonly Vector3 FloorBackgroundPosition = Vector3.zero;
        public static readonly Vector3 WallBackgroundPosition = new(-1.32f, 4.39f, 0f);
        public static readonly Vector3 BarPosition = new(-1.58f, 2.16f, 0f);
        public const string BarLeftVisualObjectName = "HubBarLeftVisual";
        public const string BarRightVisualObjectName = "HubBarRightVisual";
        public const float BarVisualHeight = 1.17f;
        public const float BarVisualGapWidth = 1.29f;
        public static readonly Vector2 BarLeftVisualSize = new(9.81f, BarVisualHeight);
        public static readonly Vector2 BarRightVisualSize = new(4.06f, BarVisualHeight);
        public static readonly Vector3 BarLeftVisualLocalPosition = new(-(BarVisualGapWidth + BarRightVisualSize.x) * 0.5f, 0f, 0f);
        public static readonly Vector3 BarRightVisualLocalPosition = new((BarVisualGapWidth + BarLeftVisualSize.x) * 0.5f, 0f, 0f);
        // 상호작용 테이블 묶음은 논리 타일 좌표로 표현하되, 현재 씬 배치와 같은 월드 결과를 유지한다.
        public static readonly Vector3 TableGroupPosition = TileToWorld(-10.8f, -2.616667f);
        public static readonly Vector3 TableTopLocalPosition = TileOffset(0f, 2.55f);
        public static readonly Vector3 TableMiddleLocalPosition = Vector3.zero;
        public static readonly Vector3 TableBottomLocalPosition = TileOffset(0f, -2.55f);
        public static readonly Vector3 TableTopPosition = TableGroupPosition + TableTopLocalPosition;
        public static readonly Vector3 TableMiddlePosition = TableGroupPosition + TableMiddleLocalPosition;
        public static readonly Vector3 TableBottomPosition = TableGroupPosition + TableBottomLocalPosition;
        public static readonly Vector3 ExploreSignPosition = TileToWorld(-13.166667f, -8.15f);
        public static readonly Vector3 ExploreSignBackdropScale = new(2.88f, 0.82f, 1f);
        public static readonly Vector3 WarehouseSignPosition = TileToWorld(12.583333f, 8.3f);
        public static readonly Vector3 WarehouseSignBackdropScale = new(2.78f, 0.82f, 1f);
        public static readonly Vector3 SignTextLocalOffset = new(0f, 0.03f, 0f);
        public const string StorageVisualRootObjectName = "HubStorageStationVisual";
        public const string UpgradeWorkbenchVisualRootObjectName = "HubUpgradeWorkbenchVisual";
        public const string PortalZoneVisualRootObjectName = "HubPortalZoneVisual";
        public static readonly Vector3 StorageVisualPosition = TileToWorld(12.566667f, 6.266667f);
        public static readonly Vector3 UpgradeWorkbenchVisualPosition = TileToWorld(3.1f, -5.366667f);
        public static readonly Vector3 PortalZoneVisualPosition = TileToWorld(-10.633333f, -7.366667f);
        public static readonly Vector3 FrontOutlinePosition = Vector3.zero;
        public static readonly Vector3 TodayMenuBoardPosition = TileToWorld(-1.366667f, 6.766667f);
        public static readonly Vector3 TodayMenuHeaderLabelLocalPosition = TileOffset(-3.266667f, 0.066667f);
        public static readonly Vector3 TodayMenuHeaderShadowLocalOffset = new(0.06f, -0.06f, 0f);
        public static readonly Vector3 TodayMenuEntryLeftLocalPosition = TileOffset(1.433333f, 0.033333f);
        public static readonly Vector3 TodayMenuEntryCenterLocalPosition = TileOffset(3.266667f, 0.033333f);
        public static readonly Vector3 TodayMenuEntryRightLocalPosition = TileOffset(5.1f, 0.033333f);
        public static readonly Vector3 TodayMenuEntryBackdropScale = Vector3.one;
        public static readonly Vector3 TodayMenuEntryIconLocalOffset = new(0f, 0f, 0f);
        public static readonly Vector3 TodayMenuEntryIconScale = Vector3.one;

        public const int FloorSortingOrder = -30;
        public const int WallSortingOrder = -25;
        public const int ObjectSortingOrder = 4;
        public const int SignSortingOrder = 6;
        public const int SignTextSortingOrder = 7;
        public const int ForegroundSortingOrder = 18;
        public const int StationShadowSortingOrder = 2;
        public const int StationBackSortingOrder = 3;
        public const int StationFrontSortingOrder = 5;
        public const int StationAccentSortingOrder = 6;
        public const int TodayMenuBackdropSortingOrder = 7;
        public const int TodayMenuItemSortingOrder = 8;
        public const int TodayMenuTextSortingOrder = 9;
        public const float SignFontSize = 4.0f;
        public const float SignTextScale = 0.40f;
        public const float SignCharacterSpacing = 0.04f;
        public const float TodayMenuHeaderFontSize = 3.8f;
        public const float TodayMenuHeaderTextScale = 0.31f;
        public const float WorldTextStrongOutlineWidth = 0.22f;
        public const float WorldTextNormalOutlineWidth = 0.16f;

        public static readonly Color SignBackdropColor = new(0.94f, 0.86f, 0.68f, 0.96f);
        public static readonly Color SignTextColor = new(0.19f, 0.12f, 0.08f, 1f);
        public static readonly Color WorldTextOutlineColor = new(0.18f, 0.10f, 0.05f, 0.94f);
        public static readonly Color TodayMenuHeaderTextColor = new(0.99f, 0.93f, 0.82f, 1f);
        public static readonly Color TodayMenuHeaderShadowColor = new(0.39f, 0.23f, 0.15f, 1f);
        public static readonly Color TodayMenuBackdropColor = Color.white;
        public static readonly Color TodayMenuSelectedBackdropColor = new(1f, 0.93f, 0.74f, 1f);
        public static readonly Color TodayMenuEmptyBackdropColor = new(0.84f, 0.84f, 0.84f, 0.72f);
        public static readonly Color TodayMenuIconColor = Color.white;
        public static readonly Color TodayMenuEmptyIconColor = new(1f, 1f, 1f, 0.24f);
        public static readonly Color HubDecorOutlineColor = new(0.14f, 0.09f, 0.07f, 1f);
        public static readonly Color HubDecorShadowColor = new(0f, 0f, 0f, 0.18f);
        public static readonly Color StorageBodyColor = new(0.62f, 0.66f, 0.64f, 1f);
        public static readonly Color StorageDoorColor = new(0.78f, 0.82f, 0.80f, 1f);
        public static readonly Color StorageShelfColor = new(0.45f, 0.33f, 0.25f, 1f);
        public static readonly Color StorageCrateColor = new(0.62f, 0.38f, 0.22f, 1f);
        public static readonly Color StorageSignPanelColor = new(0.94f, 0.86f, 0.68f, 1f);
        public static readonly Color UpgradeWorkbenchTopColor = new(0.56f, 0.36f, 0.24f, 1f);
        public static readonly Color UpgradeWorkbenchFrontColor = new(0.38f, 0.24f, 0.17f, 1f);
        public static readonly Color UpgradeToolAccentColor = new(0.74f, 0.78f, 0.80f, 1f);
        public static readonly Color UpgradeToolGoldColor = new(0.94f, 0.66f, 0.25f, 1f);
        public static readonly Color PortalZoneBaseColor = new(0.25f, 0.22f, 0.20f, 0.72f);

        // 하단 전경선과 벽 두께 때문에 이동 경계는 전체 32x18 타일보다 살짝 안쪽으로 줄여 둔다.
        public static readonly Vector3 MovementBoundsPosition = TileToWorld(0f, -0.25f);
        public static readonly Vector2 MovementBoundsSize = TileSize(31.166667f, 17.166667f);

        public static readonly Vector3 PlayerStartPosition = TileToWorld(-11.916667f, -6.916667f);
        public static readonly Vector3 HubEntryPosition = PlayerStartPosition;

        public static readonly Vector3 RecipeSelectorPosition = TileToWorld(-0.833333f, 4.083333f);
        public static readonly Vector3 RecipeSelectorScale = TileScale(2.583333f, 2.583333f);
        public static readonly Vector3 ServiceCounterPosition = TileToWorld(-4.166667f, 3.666667f);
        public static readonly Vector3 ServiceCounterScale = TileScale(3.25f, 2.583333f);

        public static readonly Vector3 StorageStationPosition = TileToWorld(13.333333f, 6.833333f);
        public static readonly Vector3 StorageStationScale = TileScale(3f, 2.25f);

        public static readonly Vector3 UpgradeStationPosition = TileToWorld(3.1f, -2.583333f);
        public static readonly Vector3 UpgradeStationScale = TileScale(4.5f, 6.083333f);

        public static readonly Vector3 GoToBeachPosition = TileToWorld(-13f, -6.966667f);
        public static readonly Vector3 GoToDeepForestPosition = TileToWorld(-11.416667f, -6.966667f);
        public static readonly Vector3 GoToAbandonedMinePosition = TileToWorld(-9.833333f, -6.966667f);
        public static readonly Vector3 GoToWindHillPosition = TileToWorld(-8.25f, -6.966667f);

        public static readonly Vector3 PortalScale = new(0.72f, 0.82f, 1f);
        public static readonly Vector3 PortalPadScale = new(0.78f, 0.22f, 1f);

        public static readonly Vector3 BeachPortalPadPosition = GoToBeachPosition + new Vector3(0f, -0.23f, 0f);
        public static readonly Vector3 ForestPortalPadPosition = GoToDeepForestPosition + new Vector3(0f, -0.23f, 0f);
        public static readonly Vector3 MinePortalPadPosition = GoToAbandonedMinePosition + new Vector3(0f, -0.23f, 0f);
        public static readonly Vector3 WindPortalPadPosition = GoToWindHillPosition + new Vector3(0f, -0.23f, 0f);

        public static readonly Vector3 TopWallColliderPosition = new(-1.12f, 4.46f, 0f);
        public static readonly Vector3 TopWallColliderScale = new(15.20f, 1.00f, 1f);
        public static readonly Vector3 TopWallColliderLocalPosition = TopWallColliderPosition - WallBackgroundPosition;

        public static readonly Vector3 LeftWallColliderPosition = new(-8.96f, 0.55f, 0f);
        public static readonly Vector3 LeftWallColliderScale = new(0.72f, 7.35f, 1f);
        public static readonly Vector3 LeftWallColliderLocalPosition = LeftWallColliderPosition - WallBackgroundPosition;

        public static readonly Vector3 RightWallColliderPosition = new(8.96f, -1.12f, 0f);
        public static readonly Vector3 RightWallColliderScale = new(0.72f, 6.85f, 1f);
        public static readonly Vector3 RightWallColliderLocalPosition = RightWallColliderPosition - WallBackgroundPosition;

        // 플레이어 물리 루트가 발 기준이므로 캡슐 반높이만큼 내려 전경 외곽선과 체감 충돌선을 맞춘다.
        public static readonly Vector3 BottomWallColliderPosition = new(1.50f, -5.475f, 0f);
        public static readonly Vector3 BottomWallColliderScale = new(16.20f, 0.90f, 1f);
        public static readonly Vector3 BottomWallColliderLocalPosition = BottomWallColliderPosition - WallBackgroundPosition;

        public static readonly Vector3 BarColliderPosition = new(-1.58f, 1.96f, 0f);
        public static readonly Vector3 BarColliderScale = new(13.29f, 0.58f, 1f);
        public static readonly Vector3 BarLeftColliderPosition = new(-4.26f, 1.96f, 0f);
        public static readonly Vector3 BarLeftColliderScale = new(9.45f, 0.58f, 1f);
        public static readonly Vector3 BarLeftColliderLocalPosition = BarLeftColliderPosition - BarPosition;
        public static readonly Vector3 BarRightColliderPosition = new(3.97f, 1.96f, 0f);
        public static readonly Vector3 BarRightColliderScale = new(3.84f, 0.58f, 1f);
        public static readonly Vector3 BarRightColliderLocalPosition = BarRightColliderPosition - BarPosition;

        public static readonly Vector3 TableTopColliderPosition = TableTopPosition;
        public static readonly Vector3 TableMiddleColliderPosition = TableMiddlePosition;
        public static readonly Vector3 TableBottomColliderPosition = TableBottomPosition;
        public static readonly Vector3 TableColliderScale = new(1.52f, 0.40f, 1f);

        public static readonly Vector3[] TodayMenuEntryLocalPositions =
        {
            TodayMenuEntryLeftLocalPosition,
            TodayMenuEntryCenterLocalPosition,
            TodayMenuEntryRightLocalPosition
        };

        public static readonly string[] HiddenInteractionObjectNames =
        {
            "RecipeSelector",
            "ServiceCounter",
            "StorageStation",
            "UpgradeStation",
            "GoToBeach",
            "GoToDeepForest",
            "GoToAbandonedMine",
            "GoToWindHill"
        };

        private static readonly HubArtPlacement[] ArtPlacementList =
        {
            new("HubFloorBackground", HubArtSpriteId.FloorBackground, HubArtAnchor.BackgroundLayer, FloorBackgroundPosition, FloorSortingOrder),
            new("HubWallBackground", HubArtSpriteId.WallBackground, HubArtAnchor.BackgroundLayer, WallBackgroundPosition, WallSortingOrder),
            new("HubBar", HubArtSpriteId.Bar, HubArtAnchor.ObjectLayer, BarPosition, ObjectSortingOrder),
            new("HubFrontOutline", HubArtSpriteId.FrontOutline, HubArtAnchor.ForegroundLayer, FrontOutlinePosition, ForegroundSortingOrder)
        };

        private static readonly HubColliderPlacement[] ColliderPlacementList =
        {
            new("HubTopWallCollider", "HubWallBackground", TopWallColliderLocalPosition, TopWallColliderScale),
            new("HubLeftWallCollider", "HubWallBackground", LeftWallColliderLocalPosition, LeftWallColliderScale),
            new("HubRightWallCollider", "HubWallBackground", RightWallColliderLocalPosition, RightWallColliderScale),
            new("HubBottomWallCollider", "HubWallBackground", BottomWallColliderLocalPosition, BottomWallColliderScale),
            new("HubBarLeftCollider", "HubBar", BarLeftColliderLocalPosition, BarLeftColliderScale),
            new("HubBarRightCollider", "HubBar", BarRightColliderLocalPosition, BarRightColliderScale)
        };

        private static readonly HubTablePlacement[] TablePlacementList =
        {
            new("HubTableTopGroup", "HubTableTop", "HubTableTopCollider", TableTopLocalPosition, Vector3.zero),
            new("HubTableMiddleGroup", "HubTableMiddle", "HubTableMiddleCollider", TableMiddleLocalPosition, Vector3.zero),
            new("HubTableBottomGroup", "HubTableBottom", "HubTableBottomCollider", TableBottomLocalPosition, Vector3.zero)
        };

        private static readonly HubFloorSignPlacement[] FloorSignPlacementList =
        {
            new(
                "HubExploreSign",
                "탐험",
                ExploreSignPosition,
                ExploreSignBackdropScale,
                SignTextLocalOffset,
                SignFontSize,
                SignTextScale,
                SignCharacterSpacing),
            new(
                "HubWarehouseSign",
                "창고",
                WarehouseSignPosition,
                WarehouseSignBackdropScale,
                SignTextLocalOffset,
                SignFontSize,
                SignTextScale,
                SignCharacterSpacing)
        };

        private static readonly HubDecorBlockPlacement[] DecorBlockPlacementList =
        {
            new(StorageVisualRootObjectName, StorageVisualPosition, "HubStorageStationShadow", new Vector3(0.08f, -0.10f, 0f), new Vector3(2.56f, 1.48f, 1f), HubDecorShadowColor, StationBackSortingOrder),
            new(StorageVisualRootObjectName, StorageVisualPosition, "HubStorageStationOutline", Vector3.zero, new Vector3(2.48f, 1.48f, 1f), HubDecorOutlineColor, StationFrontSortingOrder),
            new(StorageVisualRootObjectName, StorageVisualPosition, "HubStorageStationBody", Vector3.zero, new Vector3(2.30f, 1.30f, 1f), StorageBodyColor, StationAccentSortingOrder),
            new(StorageVisualRootObjectName, StorageVisualPosition, "HubStorageFridgeDoor", new Vector3(-0.72f, -0.02f, 0f), new Vector3(0.72f, 1.14f, 1f), StorageDoorColor, StationAccentSortingOrder + 1),
            new(StorageVisualRootObjectName, StorageVisualPosition, "HubStorageShelfTop", new Vector3(0.38f, 0.30f, 0f), new Vector3(1.08f, 0.26f, 1f), StorageShelfColor, StationAccentSortingOrder + 1),
            new(StorageVisualRootObjectName, StorageVisualPosition, "HubStorageShelfBottom", new Vector3(0.38f, -0.30f, 0f), new Vector3(1.08f, 0.26f, 1f), StorageShelfColor, StationAccentSortingOrder + 1),
            new(StorageVisualRootObjectName, StorageVisualPosition, "HubStorageCrate", new Vector3(0.17f, -0.31f, 0f), new Vector3(0.42f, 0.40f, 1f), StorageCrateColor, StationAccentSortingOrder + 2),
            new(StorageVisualRootObjectName, StorageVisualPosition, "HubStorageJar", new Vector3(0.76f, -0.31f, 0f), new Vector3(0.30f, 0.40f, 1f), UpgradeToolGoldColor, StationAccentSortingOrder + 2),
            new(StorageVisualRootObjectName, StorageVisualPosition, "HubStorageBlankSign", new Vector3(0f, 0.90f, 0f), new Vector3(1.36f, 0.34f, 1f), StorageSignPanelColor, StationAccentSortingOrder + 2),

            new(UpgradeWorkbenchVisualRootObjectName, UpgradeWorkbenchVisualPosition, "HubUpgradeWorkbenchShadow", new Vector3(0.10f, -0.10f, 0f), new Vector3(5.30f, 1.34f, 1f), HubDecorShadowColor, StationShadowSortingOrder),
            new(UpgradeWorkbenchVisualRootObjectName, UpgradeWorkbenchVisualPosition, "HubUpgradeWorkbenchOutline", Vector3.zero, new Vector3(5.14f, 1.22f, 1f), HubDecorOutlineColor, StationBackSortingOrder),
            new(UpgradeWorkbenchVisualRootObjectName, UpgradeWorkbenchVisualPosition, "HubUpgradeWorkbenchTop", new Vector3(0f, 0.16f, 0f), new Vector3(4.88f, 0.58f, 1f), UpgradeWorkbenchTopColor, StationBackSortingOrder),
            new(UpgradeWorkbenchVisualRootObjectName, UpgradeWorkbenchVisualPosition, "HubUpgradeWorkbenchFront", new Vector3(0f, -0.28f, 0f), new Vector3(4.88f, 0.38f, 1f), UpgradeWorkbenchFrontColor, StationBackSortingOrder),
            new(UpgradeWorkbenchVisualRootObjectName, UpgradeWorkbenchVisualPosition, "HubUpgradeBlankSignLeft", new Vector3(-1.55f, 0.24f, 0f), new Vector3(0.86f, 0.30f, 1f), StorageSignPanelColor, StationBackSortingOrder),
            new(UpgradeWorkbenchVisualRootObjectName, UpgradeWorkbenchVisualPosition, "HubUpgradeBlankSignCenter", new Vector3(0f, 0.24f, 0f), new Vector3(0.86f, 0.30f, 1f), StorageSignPanelColor, StationBackSortingOrder),
            new(UpgradeWorkbenchVisualRootObjectName, UpgradeWorkbenchVisualPosition, "HubUpgradeBlankSignRight", new Vector3(1.55f, 0.24f, 0f), new Vector3(0.86f, 0.30f, 1f), StorageSignPanelColor, StationBackSortingOrder),
            new(UpgradeWorkbenchVisualRootObjectName, UpgradeWorkbenchVisualPosition, "HubUpgradeToolHandle", new Vector3(-0.62f, -0.18f, 0f), new Vector3(0.18f, 0.52f, 1f), StorageShelfColor, StationBackSortingOrder),
            new(UpgradeWorkbenchVisualRootObjectName, UpgradeWorkbenchVisualPosition, "HubUpgradeToolHead", new Vector3(-0.42f, 0.02f, 0f), new Vector3(0.44f, 0.18f, 1f), UpgradeToolAccentColor, StationBackSortingOrder),
            new(UpgradeWorkbenchVisualRootObjectName, UpgradeWorkbenchVisualPosition, "HubUpgradeGearCore", new Vector3(0.70f, -0.08f, 0f), new Vector3(0.34f, 0.34f, 1f), UpgradeToolGoldColor, StationBackSortingOrder),

            new(PortalZoneVisualRootObjectName, PortalZoneVisualPosition, "HubPortalZoneShadow", new Vector3(0.08f, -0.08f, 0f), new Vector3(3.92f, 0.64f, 1f), HubDecorShadowColor, StationShadowSortingOrder),
            new(PortalZoneVisualRootObjectName, PortalZoneVisualPosition, "HubPortalZoneBase", Vector3.zero, new Vector3(3.78f, 0.52f, 1f), PortalZoneBaseColor, StationShadowSortingOrder),
            new(PortalZoneVisualRootObjectName, PortalZoneVisualPosition, "HubPortalZoneLeftCap", new Vector3(-1.70f, 0f, 0f), new Vector3(0.32f, 0.40f, 1f), new Color(0.98f, 0.83f, 0.51f, 1f), StationBackSortingOrder),
            new(PortalZoneVisualRootObjectName, PortalZoneVisualPosition, "HubPortalZoneForestCap", new Vector3(-0.57f, 0f, 0f), new Vector3(0.32f, 0.40f, 1f), new Color(0.70f, 0.86f, 0.44f, 1f), StationBackSortingOrder),
            new(PortalZoneVisualRootObjectName, PortalZoneVisualPosition, "HubPortalZoneMineCap", new Vector3(0.57f, 0f, 0f), new Vector3(0.32f, 0.40f, 1f), new Color(0.74f, 0.74f, 0.78f, 1f), StationBackSortingOrder),
            new(PortalZoneVisualRootObjectName, PortalZoneVisualPosition, "HubPortalZoneWindCap", new Vector3(1.70f, 0f, 0f), new Vector3(0.32f, 0.40f, 1f), new Color(0.82f, 0.92f, 0.98f, 1f), StationBackSortingOrder)
        };

        public static IReadOnlyList<HubArtPlacement> ArtPlacements => ArtPlacementList;
        public static IReadOnlyList<HubColliderPlacement> ColliderPlacements => ColliderPlacementList;
        public static IReadOnlyList<HubTablePlacement> TablePlacements => TablePlacementList;
        public static IReadOnlyList<HubFloorSignPlacement> FloorSignPlacements => FloorSignPlacementList;
        public static IReadOnlyList<HubDecorBlockPlacement> DecorBlockPlacements => DecorBlockPlacementList;

        // 기존 방 전환 허브 보강 코드와의 컴파일 호환용 상수들이다.
        // 현재 허브는 고정 화면 레이어 구조를 사용하므로 실제 배치는 위 좌표를 기준으로 맞춘다.
        public static readonly float MainHallOrthographicSize = ScreenOrthographicSize;
        public static readonly float SideRoomOrthographicSize = ScreenOrthographicSize;

        public static readonly Vector3 MainHallCameraPosition = CameraPosition;
        public static readonly Vector2 MainHallCameraSize = CameraSize;
        public static readonly Vector3 WorkshopCameraPosition = CameraPosition;
        public static readonly Vector2 WorkshopCameraSize = CameraSize;
        public static readonly Vector3 StorageCameraPosition = CameraPosition;
        public static readonly Vector2 StorageCameraSize = CameraSize;

        public static readonly Vector3 WorkshopZonePosition = CameraPosition;
        public static readonly Vector2 WorkshopZoneSize = Vector2.one;
        public static readonly Vector3 StorageZonePosition = CameraPosition;
        public static readonly Vector2 StorageZoneSize = Vector2.one;

        public static readonly Vector3 WorkshopFloorPosition = BackgroundPosition;
        public static readonly Vector3 WorkshopFloorScale = BackgroundScale;
        public static readonly Vector3 StorageFloorPosition = BackgroundPosition;
        public static readonly Vector3 StorageFloorScale = BackgroundScale;
        public static readonly Vector3 UpperDividerPosition = BackgroundPosition;
        public static readonly Vector3 UpperDividerScale = Vector3.one;

        public static readonly Vector3 WorkshopFrontLeftPosition = LeftWallColliderPosition;
        public static readonly Vector3 WorkshopFrontRightPosition = TableTopColliderPosition;
        public static readonly Vector3 StorageFrontLeftPosition = TableMiddleColliderPosition;
        public static readonly Vector3 StorageFrontRightPosition = RightWallColliderPosition;
        public static readonly Vector3 FrontOccluderScale = Vector3.one;
        public static readonly Vector3 FrontCollisionScale = Vector3.one;

        public static readonly Vector3 RestaurantTitlePosition = new(0f, 4.5f, 0f);
        public static readonly Vector3 KitchenCounterPosition = BarColliderPosition;
        public static readonly Vector3 KitchenCounterScale = BarColliderScale;
        public static readonly Vector3 MenuBoardBackPosition = new(-1.2f, 3.4f, 0f);
        public static readonly Vector3 MenuBoardBackScale = new(3.2f, 1.2f, 1f);
        public static readonly Vector3 TableLeftPosition = TableTopColliderPosition;
        public static readonly Vector3 TableRightPosition = TableMiddleColliderPosition;
        public static readonly Vector3 TableScale = TableColliderScale;

        public static readonly Vector3 WorkshopWallPosition = new(-1.2f, 3.7f, 0f);
        public static readonly Vector3 WorkshopWallScale = new(3.0f, 1.0f, 1f);
        public static readonly Vector3 WorkbenchSignPosition = new(-1.2f, 4.3f, 0f);

        public static readonly Vector3 StorageWallPosition = StorageStationPosition;
        public static readonly Vector3 StorageWallScale = StorageStationScale;
        public static readonly Vector3 StorageSignPosition = new(7.6f, 4.55f, 0f);

        public static readonly Vector3 DoorPadPosition = GoToBeachPosition;
        public static readonly Vector3 DoorPadScale = PortalPadScale;
        public static readonly Vector3 DoorFramePosition = GoToDeepForestPosition;
        public static readonly Vector3 DoorFrameScale = PortalScale;
        public static readonly Vector3 PortalPadPosition = BeachPortalPadPosition;

        public static readonly Color RoomFloorColor = new(0.86f, 0.72f, 0.49f, 1f);
        public static readonly Color RoomWallColor = new(0.46f, 0.30f, 0.18f, 1f);
        public static readonly Color FrontOccluderColor = new(0.34f, 0.21f, 0.13f, 1f);

        private static Vector3 TileToWorld(float tileX, float tileY, float z = 0f)
        {
            return new Vector3(tileX * WorldUnitsPerTile, tileY * WorldUnitsPerTile, z);
        }

        private static Vector3 TileOffset(float tileX, float tileY, float z = 0f)
        {
            return TileToWorld(tileX, tileY, z);
        }

        private static Vector2 TileSize(float tileWidth, float tileHeight)
        {
            return new Vector2(tileWidth * WorldUnitsPerTile, tileHeight * WorldUnitsPerTile);
        }

        private static Vector3 TileScale(float tileWidth, float tileHeight, float z = 1f)
        {
            return new Vector3(tileWidth * WorldUnitsPerTile, tileHeight * WorldUnitsPerTile, z);
        }
    }
}
