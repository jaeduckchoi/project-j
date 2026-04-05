using System.Collections.Generic;
using UnityEngine;

namespace World
{
    /// <summary>
    /// 허브 16:9 고정 화면 아트와 상호작용 지점 좌표를 한 곳에서 관리한다.
    /// 1920x1080 원본 아트를 월드 19.2 x 10.8 크기에 대응시켜 같은 구도를 유지한다.
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
            UpgradeSlotLeft,
            UpgradeSlotCenter,
            UpgradeSlotRight,
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

        public readonly struct HubUpgradeSlotPlacement
        {
            public HubUpgradeSlotPlacement(
                string slotObjectName,
                string priceObjectName,
                HubArtSpriteId spriteId,
                Vector3 position,
                int goldCost)
            {
                SlotObjectName = slotObjectName;
                PriceObjectName = priceObjectName;
                SpriteId = spriteId;
                Position = position;
                GoldCost = goldCost;
            }

            public string SlotObjectName { get; }
            public string PriceObjectName { get; }
            public HubArtSpriteId SpriteId { get; }
            public Vector3 Position { get; }
            public int GoldCost { get; }
            public string GoldCostLabel => GoldCost.ToString("#,0");
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

        public const float TargetAspectWidth = 16f;
        public const float TargetAspectHeight = 9f;
        public const float TargetAspectRatio = TargetAspectWidth / TargetAspectHeight;

        public const float ScreenWidth = 19.2f;
        public const float ScreenHeight = 10.8f;

        public static readonly float ScreenOrthographicSize = ScreenHeight * 0.5f;
        public static readonly Vector3 CameraPosition = Vector3.zero;
        public static readonly Vector2 CameraSize = new(ScreenWidth, ScreenHeight);

        public static readonly Vector3 BackgroundPosition = Vector3.zero;
        public static readonly Vector3 BackgroundScale = Vector3.one;
        public const string TableRootObjectName = "HubTableGroup";

        public static readonly Vector3 FloorBackgroundPosition = Vector3.zero;
        public static readonly Vector3 WallBackgroundPosition = new(-1.32f, 4.39f, 0f);
        public static readonly Vector3 BarPosition = new(-1.58f, 2.16f, 0f);
        // 테이블 묶음은 그룹 기준 좌표와 자식 오프셋을 나눠 두어 나중에 한 번에 이동시키기 쉽게 유지한다.
        public static readonly Vector3 TableGroupPosition = new(-6.48f, -1.57f, 0f);
        public static readonly Vector3 TableTopLocalPosition = new(0f, 1.53f, 0f);
        public static readonly Vector3 TableMiddleLocalPosition = Vector3.zero;
        public static readonly Vector3 TableBottomLocalPosition = new(0f, -1.53f, 0f);
        public static readonly Vector3 TableTopPosition = TableGroupPosition + TableTopLocalPosition;
        public static readonly Vector3 TableMiddlePosition = TableGroupPosition + TableMiddleLocalPosition;
        public static readonly Vector3 TableBottomPosition = TableGroupPosition + TableBottomLocalPosition;
        public static readonly Vector3 UpgradeSlotLeftPosition = new(-2.20f, -1.61f, 0f);
        public static readonly Vector3 UpgradeSlotCenterPosition = new(1.85f, -1.61f, 0f);
        public static readonly Vector3 UpgradeSlotRightPosition = new(5.89f, -1.61f, 0f);
        public static readonly Vector3 UpgradePrice5000Position = new(-2.12f, -1.62f, 0f);
        public static readonly Vector3 UpgradePrice20000Position = new(1.86f, -1.62f, 0f);
        public static readonly Vector3 UpgradePrice100000Position = new(5.89f, -1.62f, 0f);
        public static readonly Vector3 UpgradePriceTextLocalOffset = new(0f, -0.02f, 0f);
        public static readonly Vector3 ExploreSignPosition = new(-7.90f, -4.89f, 0f);
        public static readonly Vector3 ExploreSignBackdropScale = new(2.88f, 0.82f, 1f);
        public static readonly Vector3 WarehouseSignPosition = new(7.55f, 4.98f, 0f);
        public static readonly Vector3 WarehouseSignBackdropScale = new(2.78f, 0.82f, 1f);
        public static readonly Vector3 SignTextLocalOffset = new(0f, 0.03f, 0f);
        public static readonly Vector3 FrontOutlinePosition = Vector3.zero;
        public static readonly Vector3 TodayMenuBoardPosition = new(-0.82f, 4.06f, 0f);
        public static readonly Vector3 TodayMenuHeaderLabelLocalPosition = new(-1.96f, 0.04f, 0f);
        public static readonly Vector3 TodayMenuHeaderShadowLocalOffset = new(0.06f, -0.06f, 0f);
        public static readonly Vector3 TodayMenuEntryLeftLocalPosition = new(0.86f, 0.02f, 0f);
        public static readonly Vector3 TodayMenuEntryCenterLocalPosition = new(1.96f, 0.02f, 0f);
        public static readonly Vector3 TodayMenuEntryRightLocalPosition = new(3.06f, 0.02f, 0f);
        public static readonly Vector3 TodayMenuEntryBackdropScale = Vector3.one;
        public static readonly Vector3 TodayMenuEntryIconLocalOffset = new(0f, 0f, 0f);
        public static readonly Vector3 TodayMenuEntryIconScale = Vector3.one;

        public const int FloorSortingOrder = -30;
        public const int WallSortingOrder = -25;
        public const int ObjectSortingOrder = 4;
        public const int SignSortingOrder = 6;
        public const int SignTextSortingOrder = 7;
        public const int ForegroundSortingOrder = 18;
        public const int TodayMenuBackdropSortingOrder = 7;
        public const int TodayMenuItemSortingOrder = 8;
        public const int TodayMenuTextSortingOrder = 9;
        public const float UpgradePriceFontSize = 3.2f;
        public const float UpgradePriceTextScale = 0.28f;
        public const float SignFontSize = 4.0f;
        public const float SignTextScale = 0.40f;
        public const float SignCharacterSpacing = 0.04f;
        public const float TodayMenuHeaderFontSize = 3.8f;
        public const float TodayMenuHeaderTextScale = 0.31f;
        public const float WorldTextStrongOutlineWidth = 0.22f;
        public const float WorldTextNormalOutlineWidth = 0.16f;

        public static readonly Color SignBackdropColor = new(0.94f, 0.86f, 0.68f, 0.96f);
        public static readonly Color SignTextColor = new(0.19f, 0.12f, 0.08f, 1f);
        public static readonly Color UpgradePriceTextColor = new(0.23f, 0.15f, 0.09f, 1f);
        public static readonly Color WorldTextOutlineColor = new(0.18f, 0.10f, 0.05f, 0.94f);
        public static readonly Color TodayMenuHeaderTextColor = new(0.99f, 0.93f, 0.82f, 1f);
        public static readonly Color TodayMenuHeaderShadowColor = new(0.39f, 0.23f, 0.15f, 1f);
        public static readonly Color TodayMenuBackdropColor = Color.white;
        public static readonly Color TodayMenuSelectedBackdropColor = new(1f, 0.93f, 0.74f, 1f);
        public static readonly Color TodayMenuEmptyBackdropColor = new(0.84f, 0.84f, 0.84f, 0.72f);
        public static readonly Color TodayMenuIconColor = Color.white;
        public static readonly Color TodayMenuEmptyIconColor = new(1f, 1f, 1f, 0.24f);

        public static readonly Vector3 MovementBoundsPosition = new(0f, -0.05f, 0f);
        public static readonly Vector2 MovementBoundsSize = new(18.7f, 10.1f);

        public static readonly Vector3 PlayerStartPosition = new(-7.15f, -4.15f, 0f);
        public static readonly Vector3 HubEntryPosition = PlayerStartPosition;

        public static readonly Vector3 RecipeSelectorPosition = new(-1.10f, 2.45f, 0f);
        public static readonly Vector3 ServiceCounterPosition = new(-3.10f, 2.20f, 0f);

        public static readonly Vector3 StorageStationPosition = new(8.00f, 4.10f, 0f);
        public static readonly Vector3 StorageStationScale = new(1.80f, 1.35f, 1f);

        public static readonly Vector3 UpgradeStationPosition = new(1.86f, -1.55f, 0f);
        public static readonly Vector3 UpgradeStationScale = new(2.70f, 3.65f, 1f);

        public static readonly Vector3 GoToBeachPosition = new(-7.80f, -4.18f, 0f);
        public static readonly Vector3 GoToDeepForestPosition = new(-6.85f, -4.18f, 0f);
        public static readonly Vector3 GoToAbandonedMinePosition = new(-5.90f, -4.18f, 0f);
        public static readonly Vector3 GoToWindHillPosition = new(-4.95f, -4.18f, 0f);

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

        // 전경 외곽선 하단의 실제 막힘 구간(좌측 출입구를 제외한 밴드)에 맞춰 충돌 범위를 유지한다.
        public static readonly Vector3 BottomWallColliderPosition = new(1.50f, -4.95f, 0f);
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

        private static readonly HubUpgradeSlotPlacement[] UpgradeSlotPlacementList =
        {
            new("HubUpgradeSlotLeft", "HubUpgradePrice5000", HubArtSpriteId.UpgradeSlotLeft, UpgradeSlotLeftPosition, 5000),
            new("HubUpgradeSlotCenter", "HubUpgradePrice20000", HubArtSpriteId.UpgradeSlotCenter, UpgradeSlotCenterPosition, 20000),
            new("HubUpgradeSlotRight", "HubUpgradePrice100000", HubArtSpriteId.UpgradeSlotRight, UpgradeSlotRightPosition, 100000)
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

        public static IReadOnlyList<HubArtPlacement> ArtPlacements => ArtPlacementList;
        public static IReadOnlyList<HubColliderPlacement> ColliderPlacements => ColliderPlacementList;
        public static IReadOnlyList<HubTablePlacement> TablePlacements => TablePlacementList;
        public static IReadOnlyList<HubUpgradeSlotPlacement> UpgradeSlotPlacements => UpgradeSlotPlacementList;
        public static IReadOnlyList<HubFloorSignPlacement> FloorSignPlacements => FloorSignPlacementList;

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
    }
}
