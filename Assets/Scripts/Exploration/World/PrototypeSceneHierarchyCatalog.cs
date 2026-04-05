using System;
using System.Collections.Generic;

namespace Exploration.World
{
    /// <summary>
    /// 지원 씬이 공통으로 따르는 Hierarchy 그룹 이름과 부모 관계를 정의합니다.
    /// 빌더, 에디터 정리 도구, 자동 감사가 같은 기준을 공유하도록 한 곳에 모아 둡니다.
    /// </summary>
    public readonly struct PrototypeSceneHierarchyEntry
    {
        public PrototypeSceneHierarchyEntry(string objectName, string parentName, int siblingIndex)
        {
            ObjectName = objectName;
            ParentName = parentName;
            SiblingIndex = siblingIndex;
        }

        public string ObjectName { get; }
        public string ParentName { get; }
        public int SiblingIndex { get; }
    }

    /// <summary>
    /// 지원 씬의 월드 Hierarchy 규칙을 정의합니다.
    /// Canvas 내부 구조는 별도 UI 레이아웃 카탈로그가 관리하고, 여기서는 월드/시스템 루트만 다룹니다.
    /// </summary>
    public static class PrototypeSceneHierarchyCatalog
    {
        public const string SceneWorldRootName = "SceneWorldRoot";
        public const string SceneGameplayRootName = "SceneGameplayRoot";
        public const string SceneSystemRootName = "SceneSystemRoot";

        public const string WorldVisualRootName = "WorldVisualRoot";
        public const string WorldBoundsRootName = "WorldBoundsRoot";

        public const string PlayerRootName = "PlayerRoot";
        public const string SpawnRootName = "SpawnRoot";
        public const string PortalRootName = "PortalRoot";
        public const string InteractionRootName = "InteractionRoot";
        public const string ResourceRootName = "ResourceRoot";
        public const string ZoneRootName = "ZoneRoot";

        private static readonly string[] SupportedSceneNameList =
        {
            "Hub",
            "Beach",
            "DeepForest",
            "AbandonedMine",
            "WindHill"
        };

        private static readonly PrototypeSceneHierarchyEntry[] SharedGroupEntries =
        {
            new(SceneWorldRootName, string.Empty, 0),
            new(SceneGameplayRootName, string.Empty, 1),
            new(SceneSystemRootName, string.Empty, 2),
            new(WorldVisualRootName, SceneWorldRootName, 0),
            new(WorldBoundsRootName, SceneWorldRootName, 1),
            new(PlayerRootName, SceneGameplayRootName, 0),
            new(SpawnRootName, SceneGameplayRootName, 1),
            new(PortalRootName, SceneGameplayRootName, 2)
        };

        private static readonly PrototypeSceneHierarchyEntry[] SharedLeafEntries =
        {
            new("Canvas", string.Empty, 3),
            new("GameManager", SceneSystemRootName, 0),
            new("Main Camera", SceneSystemRootName, 2),
            new("EventSystem", SceneSystemRootName, 3),
            new("CameraBounds", WorldBoundsRootName, 0),
            new("Jonggu", PlayerRootName, 0)
        };

        private static readonly PrototypeSceneHierarchyEntry[] HubGroupEntries =
        {
            new(InteractionRootName, SceneGameplayRootName, 3)
        };

        private static readonly PrototypeSceneHierarchyEntry[] HubLeafEntries =
        {
            new("RestaurantManager", SceneSystemRootName, 1),
            new("HubArtRoot", WorldVisualRootName, 0),
            new("HubMovementBounds", WorldBoundsRootName, 1),
            new("HubEntry", SpawnRootName, 0),
            new("GoToBeach", PortalRootName, 0),
            new("BeachPortalPad", "GoToBeach", 0),
            new("GoToDeepForest", PortalRootName, 1),
            new("ForestPortalPad", "GoToDeepForest", 0),
            new("GoToAbandonedMine", PortalRootName, 2),
            new("MinePortalPad", "GoToAbandonedMine", 0),
            new("GoToWindHill", PortalRootName, 3),
            new("WindPortalPad", "GoToWindHill", 0),
            new("RecipeSelector", InteractionRootName, 0),
            new("ServiceCounter", InteractionRootName, 1),
            new("StorageStation", InteractionRootName, 2),
            new("UpgradeStation", InteractionRootName, 3)
        };

        private static readonly PrototypeSceneHierarchyEntry[] BeachGroupEntries =
        {
            new(ResourceRootName, SceneGameplayRootName, 3)
        };

        private static readonly PrototypeSceneHierarchyEntry[] BeachLeafEntries =
        {
            new("BeachMovementBounds", WorldBoundsRootName, 1),
            new("TopWall", WorldBoundsRootName, 2),
            new("BottomWall", WorldBoundsRootName, 3),
            new("LeftWall", WorldBoundsRootName, 4),
            new("RightWall", WorldBoundsRootName, 5),
            new("SandBase", WorldVisualRootName, 0),
            new("OceanBand", WorldVisualRootName, 1),
            new("ShoreLine", WorldVisualRootName, 2),
            new("Dock", WorldVisualRootName, 3),
            new("BoatMark", WorldVisualRootName, 4),
            new("RockClusterA", WorldVisualRootName, 5),
            new("RockClusterB", WorldVisualRootName, 6),
            new("GrassPatch", WorldVisualRootName, 7),
            new("BeachTitle", WorldVisualRootName, 8),
            new("BeachEntry", SpawnRootName, 0),
            new("ReturnToHub", PortalRootName, 0),
            new("FishSpot01", ResourceRootName, 0),
            new("FishSpot01_Pad", "FishSpot01", 0),
            new("FishSpot02", ResourceRootName, 1),
            new("FishSpot02_Pad", "FishSpot02", 0),
            new("ShellSpot01", ResourceRootName, 2),
            new("ShellSpot01_Pad", "ShellSpot01", 0),
            new("ShellSpot02", ResourceRootName, 3),
            new("ShellSpot02_Pad", "ShellSpot02", 0),
            new("SeaweedSpot01", ResourceRootName, 4),
            new("SeaweedSpot01_Pad", "SeaweedSpot01", 0)
        };

        private static readonly PrototypeSceneHierarchyEntry[] DeepForestGroupEntries =
        {
            new(ResourceRootName, SceneGameplayRootName, 3),
            new(ZoneRootName, SceneGameplayRootName, 4)
        };

        private static readonly PrototypeSceneHierarchyEntry[] DeepForestLeafEntries =
        {
            new("ForestMovementBounds", WorldBoundsRootName, 1),
            new("TopWall", WorldBoundsRootName, 2),
            new("BottomWall", WorldBoundsRootName, 3),
            new("LeftWall", WorldBoundsRootName, 4),
            new("RightWall", WorldBoundsRootName, 5),
            new("ForestBase", WorldVisualRootName, 0),
            new("ForestPath", WorldVisualRootName, 1),
            new("ForestCanopy", WorldVisualRootName, 2),
            new("SwampPoolA", WorldVisualRootName, 3),
            new("SwampPoolB", WorldVisualRootName, 4),
            new("NarrowPathRock", WorldVisualRootName, 5),
            new("ForestTitle", WorldVisualRootName, 6),
            new("ForestEntry", SpawnRootName, 0),
            new("ReturnFromForest", PortalRootName, 0),
            new("HerbPatch01", ResourceRootName, 0),
            new("HerbPatch01_Pad", "HerbPatch01", 0),
            new("HerbPatch02", ResourceRootName, 1),
            new("HerbPatch02_Pad", "HerbPatch02", 0),
            new("MushroomPatch01", ResourceRootName, 2),
            new("MushroomPatch01_Pad", "MushroomPatch01", 0),
            new("MushroomPatch02", ResourceRootName, 3),
            new("MushroomPatch02_Pad", "MushroomPatch02", 0),
            new("ForestGuide", ZoneRootName, 0),
            new("ForestSwampZone", ZoneRootName, 1)
        };

        private static readonly PrototypeSceneHierarchyEntry[] AbandonedMineGroupEntries =
        {
            new(ResourceRootName, SceneGameplayRootName, 3),
            new(ZoneRootName, SceneGameplayRootName, 4)
        };

        private static readonly PrototypeSceneHierarchyEntry[] AbandonedMineLeafEntries =
        {
            new("MineBounds", WorldBoundsRootName, 1),
            new("TopWall", WorldBoundsRootName, 2),
            new("BottomWall", WorldBoundsRootName, 3),
            new("LeftWall", WorldBoundsRootName, 4),
            new("RightWall", WorldBoundsRootName, 5),
            new("MineBase", WorldVisualRootName, 0),
            new("MineTunnel", WorldVisualRootName, 1),
            new("MineChamber", WorldVisualRootName, 2),
            new("MineRockA", WorldVisualRootName, 3),
            new("MineRockB", WorldVisualRootName, 4),
            new("MineRockC", WorldVisualRootName, 5),
            new("MineTitle", WorldVisualRootName, 6),
            new("MineEntry", SpawnRootName, 0),
            new("ReturnFromMine", PortalRootName, 0),
            new("GlowMoss01", ResourceRootName, 0),
            new("GlowMoss01_Pad", "GlowMoss01", 0),
            new("GlowMoss02", ResourceRootName, 1),
            new("GlowMoss02_Pad", "GlowMoss02", 0),
            new("GlowMoss03", ResourceRootName, 2),
            new("GlowMoss03_Pad", "GlowMoss03", 0),
            new("MineGuide", ZoneRootName, 0),
            new("MineDarkness", ZoneRootName, 1)
        };

        private static readonly PrototypeSceneHierarchyEntry[] WindHillGroupEntries =
        {
            new(ResourceRootName, SceneGameplayRootName, 3),
            new(ZoneRootName, SceneGameplayRootName, 4)
        };

        private static readonly PrototypeSceneHierarchyEntry[] WindHillLeafEntries =
        {
            new("WindHillBounds", WorldBoundsRootName, 1),
            new("TopWall", WorldBoundsRootName, 2),
            new("BottomWall", WorldBoundsRootName, 3),
            new("LeftWall", WorldBoundsRootName, 4),
            new("RightWall", WorldBoundsRootName, 5),
            new("HillBase", WorldVisualRootName, 0),
            new("CliffBand", WorldVisualRootName, 1),
            new("WindLane", WorldVisualRootName, 2),
            new("CliffRockA", WorldVisualRootName, 3),
            new("CliffRockB", WorldVisualRootName, 4),
            new("WindHillTitle", WorldVisualRootName, 5),
            new("WindHillEntry", SpawnRootName, 0),
            new("WindHillShortcutEntry", SpawnRootName, 1),
            new("ReturnFromWindHill", PortalRootName, 0),
            new("WindHillShortcut", PortalRootName, 1),
            new("WindHerb01", ResourceRootName, 0),
            new("WindHerb01_Pad", "WindHerb01", 0),
            new("WindHerb02", ResourceRootName, 1),
            new("WindHerb02_Pad", "WindHerb02", 0),
            new("WindHerb03", ResourceRootName, 2),
            new("WindHerb03_Pad", "WindHerb03", 0),
            new("WindGuide", ZoneRootName, 0),
            new("WindLaneZone", ZoneRootName, 1)
        };

        private static readonly Dictionary<string, PrototypeSceneHierarchyEntry[]> SceneGroupEntriesMap = new(StringComparer.Ordinal)
        {
            ["Hub"] = HubGroupEntries,
            ["Beach"] = BeachGroupEntries,
            ["DeepForest"] = DeepForestGroupEntries,
            ["AbandonedMine"] = AbandonedMineGroupEntries,
            ["WindHill"] = WindHillGroupEntries
        };

        private static readonly Dictionary<string, PrototypeSceneHierarchyEntry[]> SceneLeafEntriesMap = new(StringComparer.Ordinal)
        {
            ["Hub"] = HubLeafEntries,
            ["Beach"] = BeachLeafEntries,
            ["DeepForest"] = DeepForestLeafEntries,
            ["AbandonedMine"] = AbandonedMineLeafEntries,
            ["WindHill"] = WindHillLeafEntries
        };

        public static IReadOnlyList<string> SupportedSceneNames => SupportedSceneNameList;

        public static bool IsSupportedScene(string sceneName)
        {
            return !string.IsNullOrWhiteSpace(sceneName)
                   && SceneLeafEntriesMap.ContainsKey(sceneName)
                   && SceneGroupEntriesMap.ContainsKey(sceneName);
        }

        public static IEnumerable<PrototypeSceneHierarchyEntry> EnumerateGroupEntries(string sceneName)
        {
            if (!TryGetSceneEntries(sceneName, out PrototypeSceneHierarchyEntry[] sceneGroupEntries, out _))
            {
                yield break;
            }

            for (int index = 0; index < SharedGroupEntries.Length; index++)
            {
                yield return SharedGroupEntries[index];
            }

            for (int index = 0; index < sceneGroupEntries.Length; index++)
            {
                yield return sceneGroupEntries[index];
            }
        }

        public static IEnumerable<PrototypeSceneHierarchyEntry> EnumerateLeafEntries(string sceneName)
        {
            if (!TryGetSceneEntries(sceneName, out _, out PrototypeSceneHierarchyEntry[] sceneLeafEntries))
            {
                yield break;
            }

            for (int index = 0; index < SharedLeafEntries.Length; index++)
            {
                yield return SharedLeafEntries[index];
            }

            for (int index = 0; index < sceneLeafEntries.Length; index++)
            {
                yield return sceneLeafEntries[index];
            }
        }

        public static IEnumerable<PrototypeSceneHierarchyEntry> EnumerateManagedEntries(string sceneName)
        {
            foreach (PrototypeSceneHierarchyEntry entry in EnumerateGroupEntries(sceneName))
            {
                yield return entry;
            }

            foreach (PrototypeSceneHierarchyEntry entry in EnumerateLeafEntries(sceneName))
            {
                yield return entry;
            }
        }

        public static HashSet<string> GetManagedObjectNames(string sceneName)
        {
            HashSet<string> names = new(StringComparer.Ordinal);
            foreach (PrototypeSceneHierarchyEntry entry in EnumerateManagedEntries(sceneName))
            {
                if (!string.IsNullOrWhiteSpace(entry.ObjectName))
                {
                    names.Add(entry.ObjectName);
                }
            }

            return names;
        }

        public static bool TryGetEntry(string sceneName, string objectName, out PrototypeSceneHierarchyEntry entry)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                entry = default;
                return false;
            }

            foreach (PrototypeSceneHierarchyEntry current in EnumerateManagedEntries(sceneName))
            {
                if (string.Equals(current.ObjectName, objectName, StringComparison.Ordinal))
                {
                    entry = current;
                    return true;
                }
            }

            entry = default;
            return false;
        }

        public static bool IsGroupObject(string sceneName, string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return false;
            }

            foreach (PrototypeSceneHierarchyEntry entry in EnumerateGroupEntries(sceneName))
            {
                if (string.Equals(entry.ObjectName, objectName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetSceneEntries(
            string sceneName,
            out PrototypeSceneHierarchyEntry[] groupEntries,
            out PrototypeSceneHierarchyEntry[] leafEntries)
        {
            if (string.IsNullOrWhiteSpace(sceneName)
                || !SceneGroupEntriesMap.TryGetValue(sceneName, out groupEntries)
                || !SceneLeafEntriesMap.TryGetValue(sceneName, out leafEntries))
            {
                groupEntries = Array.Empty<PrototypeSceneHierarchyEntry>();
                leafEntries = Array.Empty<PrototypeSceneHierarchyEntry>();
                return false;
            }

            return true;
        }
    }
}
