using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// 기존 씬 직렬화에 남아 있는 보강 오브젝트를 런타임에서 정리한다.
public static class PrototypeSceneRuntimeAugmenter
{
    public static void EnsureSceneReady(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        switch (scene.name)
        {
            case "Hub":
                EnsureHubReady();
                break;

            case "Beach":
                EnsureBeachReady();
                break;

            case "DeepForest":
                EnsureDeepForestReady();
                break;

            case "AbandonedMine":
                EnsureAbandonedMineReady();
                break;

            case "WindHill":
                EnsureWindHillReady();
                break;
        }
    }

    // 허브는 예전 독립 표지판을 제거하고 현재 포털 자식 라벨만 남긴다.
    private static void EnsureHubReady()
    {
        CleanupLegacyObjects("ExitSign", "ForestSign", "MineSign", "WindHillSign");
        UpdatePortalDisplayLabel("GoToBeach", "바닷가로");
        UpdatePortalDisplayLabel("GoToDeepForest", "깊은 숲");
        UpdatePortalDisplayLabel("GoToAbandonedMine", "폐광산");
        UpdatePortalDisplayLabel("GoToWindHill", "바람 언덕");

        StorageManager storage = GameManager.Instance != null
            ? GameManager.Instance.Storage
            : Object.FindFirstObjectByType<StorageManager>();
        StorageStation depositStation = FindComponentByName<StorageStation>("StorageDeposit");
        StorageStation withdrawStation = FindComponentByName<StorageStation>("StorageWithdraw");

        if (storage != null)
        {
            if (depositStation != null)
            {
                ConfigureStorageStation(
                    depositStation,
                    storage,
                    StorageStationAction.StoreSelected,
                    "맡기기",
                    new Color(0.83f, 0.66f, 0.33f));
            }

            if (withdrawStation != null)
            {
                ConfigureStorageStation(
                    withdrawStation,
                    storage,
                    StorageStationAction.WithdrawSelected,
                    "꺼내기",
                    new Color(0.68f, 0.54f, 0.29f));
            }

            if (depositStation != null)
            {
                EnsureStorageStationClone(
                    "StorageSelectDeposit",
                    depositStation.gameObject,
                    new Vector3(4.9f, 1.8f, 0f),
                    storage,
                    StorageStationAction.CycleInventorySelection,
                    "맡길 품목",
                    new Color(0.93f, 0.77f, 0.43f));
            }

            if (withdrawStation != null)
            {
                EnsureStorageStationClone(
                    "StorageSelectWithdraw",
                    withdrawStation.gameObject,
                    new Vector3(4.9f, 0.5f, 0f),
                    storage,
                    StorageStationAction.CycleStorageSelection,
                    "꺼낼 품목",
                    new Color(0.79f, 0.67f, 0.43f));
            }
        }

        EnsureMinePortal();
    }

    private static void EnsureBeachReady()
    {
        CleanupLegacyObjects("BoatLabel");
        UpdatePortalDisplayLabel("ReturnToHub", "식당 복귀");
    }

    private static void EnsureDeepForestReady()
    {
        CleanupLegacyObjects("ForestBoatLabel");
        UpdatePortalDisplayLabel("ReturnFromForest", "식당 복귀");
    }

    // 바람 언덕은 복귀 포털 중복 라벨을 제거하고 지름길 포털을 유지한다.
    private static void EnsureWindHillReady()
    {
        CleanupLegacyObjects("WindReturnLabel", "WindShortcutLabel");
        UpdatePortalDisplayLabel("ReturnFromWindHill", "식당 복귀");

        EnsureSpawnPoint("WindHillShortcutEntry", "WindHillShortcutEntry", new Vector3(7.8f, 4.4f, 0f));

        ScenePortal templatePortal = FindComponentByName<ScenePortal>("ReturnFromWindHill");
        if (templatePortal == null)
        {
            return;
        }

        ScenePortal shortcutPortal = FindComponentByName<ScenePortal>("WindHillShortcut");
        if (shortcutPortal == null)
        {
            GameObject clone = Object.Instantiate(templatePortal.gameObject);
            clone.name = "WindHillShortcut";
            clone.transform.position = new Vector3(-6.8f, -2.8f, 0f);
            shortcutPortal = clone.GetComponent<ScenePortal>();
        }

        ConfigurePortal(
            shortcutPortal,
            "WindHill",
            "WindHillShortcutEntry",
            "정상 지름길",
            "정상 지름길",
            true,
            ToolType.None,
            6,
            "평판 6을 모으면 바람 언덕의 지름길을 이용할 수 있습니다.",
            new Color(0.55f, 0.77f, 0.95f));
    }

    // 폐광산은 복귀 라벨 정리와 탐험 보강을 함께 처리한다.
    private static void EnsureAbandonedMineReady()
    {
        CleanupLegacyObjects("MineReturnLabel");
        UpdatePortalDisplayLabel("ReturnFromMine", "식당 복귀");

        EnsureSpawnPoint("MineEntry", "MineEntry", new Vector3(-12f, -6.2f, 0f));
        EnsureMineDarknessZone();
        ConfigureMineSlowZone();
        ConfigureMineGatherable("MushroomPatch01", new Vector3(4.4f, 3.2f, 0f));
        ConfigureMineGatherable("HerbPatch01", new Vector3(8.2f, 1.0f, 0f));
        ConfigureMineGatherable("HerbPatch02", new Vector3(11.6f, 4.4f, 0f));

        GatherableResource extraGatherable = FindComponentByName<GatherableResource>("MushroomPatch02");
        if (extraGatherable != null)
        {
            extraGatherable.gameObject.SetActive(false);
        }

        GuideTriggerZone guideTrigger = FindComponentByName<GuideTriggerZone>("MineGuide");
        if (guideTrigger != null)
        {
            guideTrigger.Configure(
                "mine_intro",
                "폐광산은 어둡고 동선이 좁습니다. 안쪽으로 들어가기 전 귀환 길을 먼저 확인하세요.",
                5f,
                true);
        }

        UpdateTextByObjectName("MineTitle", "폐광산");
    }

    // 허브에 폐광산 포털이 빠져 있으면 기존 포털을 복제해 채운다.
    private static void EnsureMinePortal()
    {
        ScenePortal templatePortal = FindComponentByName<ScenePortal>("GoToWindHill");
        if (templatePortal == null)
        {
            templatePortal = FindComponentByName<ScenePortal>("GoToDeepForest");
        }

        if (templatePortal == null)
        {
            return;
        }

        ScenePortal minePortal = FindComponentByName<ScenePortal>("GoToAbandonedMine");
        if (minePortal == null)
        {
            GameObject clone = Object.Instantiate(templatePortal.gameObject);
            clone.name = "GoToAbandonedMine";
            clone.transform.position = new Vector3(8.5f, -0.2f, 0f);
            minePortal = clone.GetComponent<ScenePortal>();
        }

        ConfigurePortal(
            minePortal,
            "AbandonedMine",
            "MineEntry",
            "폐광산으로 이동",
            "폐광산",
            true,
            ToolType.Lantern,
            0,
            "작업대에서 랜턴을 준비해야 폐광산 안쪽을 안전하게 탐험할 수 있습니다.",
            new Color(0.72f, 0.74f, 0.78f));
    }

    private static void EnsureMineDarknessZone()
    {
        GameObject zone = GameObject.Find("MineDarkness");
        if (zone == null)
        {
            zone = new GameObject("MineDarkness");
        }

        zone.transform.position = new Vector3(4.8f, 0.6f, 0f);

        BoxCollider2D collider = zone.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = zone.AddComponent<BoxCollider2D>();
        }

        collider.isTrigger = true;
        collider.size = new Vector2(18f, 10.8f);

        DarknessZone darknessZone = zone.GetComponent<DarknessZone>();
        if (darknessZone == null)
        {
            darknessZone = zone.AddComponent<DarknessZone>();
        }

        darknessZone.Configure(
            0.45f,
            "랜턴이 없으면 폐광산 안쪽을 안전하게 이동할 수 없습니다.",
            "mine_darkness");
    }

    private static void ConfigureMineSlowZone()
    {
        MovementModifierZone slowZone = FindComponentByName<MovementModifierZone>("MineLooseRubble");
        if (slowZone == null)
        {
            slowZone = FindComponentByName<MovementModifierZone>("ForestSwampZone");
        }

        if (slowZone == null)
        {
            return;
        }

        slowZone.gameObject.name = "MineLooseRubble";
        slowZone.Configure(
            0.68f,
            ToolType.None,
            "무너진 잔해가 발을 붙잡습니다. 좁은 길에서는 욕심내지 말고 천천히 움직이세요.",
            "mine_loose_rubble");
    }

    private static void ConfigureMineGatherable(string objectName, Vector3 position)
    {
        GatherableResource gatherable = FindComponentByName<GatherableResource>(objectName);
        ResourceData glowMoss = GeneratedGameDataLocator.FindGeneratedResource("GlowMoss", "발광 이끼");
        if (gatherable == null || glowMoss == null)
        {
            return;
        }

        gatherable.transform.position = position;
        gatherable.Configure(glowMoss, ToolType.Lantern, 1, 2);
        UpdatePrimaryRendererColor(gatherable.gameObject, new Color(0.45f, 0.95f, 0.78f));
        UpdateWorldLabel(gatherable.gameObject, "발광 이끼");
    }

    private static void EnsureStorageStationClone(
        string objectName,
        GameObject template,
        Vector3 position,
        StorageManager storage,
        StorageStationAction action,
        string label,
        Color color)
    {
        StorageStation station = FindComponentByName<StorageStation>(objectName);
        if (station == null)
        {
            GameObject clone = Object.Instantiate(template);
            clone.name = objectName;
            clone.transform.position = position;
            station = clone.GetComponent<StorageStation>();
        }

        ConfigureStorageStation(station, storage, action, label, color);
    }

    private static void ConfigureStorageStation(
        StorageStation station,
        StorageManager storage,
        StorageStationAction action,
        string label,
        Color color)
    {
        if (station == null)
        {
            return;
        }

        station.Configure(storage, action, label);
        UpdatePrimaryRendererColor(station.gameObject, color);
        UpdateWorldLabel(station.gameObject, label);
    }

    private static void ConfigurePortal(
        ScenePortal portal,
        string targetSceneName,
        string spawnPointId,
        string promptLabel,
        string worldLabel,
        bool requireMorningExplore,
        ToolType requiredToolType,
        int requiredReputation,
        string lockedGuideText,
        Color color)
    {
        if (portal == null)
        {
            return;
        }

        portal.Configure(
            targetSceneName,
            spawnPointId,
            promptLabel,
            requireMorningExplore,
            requiredToolType,
            requiredReputation,
            lockedGuideText);

        UpdatePrimaryRendererColor(portal.gameObject, color);
        UpdateWorldLabel(portal.gameObject, string.IsNullOrWhiteSpace(worldLabel) ? promptLabel : worldLabel);
    }

    private static void EnsureSpawnPoint(string objectName, string spawnId, Vector3 position)
    {
        SceneSpawnPoint spawnPoint = FindComponentByName<SceneSpawnPoint>(objectName);
        if (spawnPoint == null)
        {
            GameObject go = new(objectName);
            go.transform.position = position;
            spawnPoint = go.AddComponent<SceneSpawnPoint>();
        }

        spawnPoint.transform.position = position;
        spawnPoint.Configure(spawnId);
    }

    private static void CleanupLegacyObjects(params string[] objectNames)
    {
        if (objectNames == null)
        {
            return;
        }

        foreach (string objectName in objectNames)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                continue;
            }

            GameObject go = GameObject.Find(objectName);
            if (go != null)
            {
                Object.Destroy(go);
            }
        }
    }

    private static void UpdatePortalDisplayLabel(string objectName, string worldLabel)
    {
        ScenePortal portal = FindComponentByName<ScenePortal>(objectName);
        if (portal != null)
        {
            UpdateWorldLabel(portal.gameObject, worldLabel);
        }
    }

    private static void UpdatePrimaryRendererColor(GameObject root, Color color)
    {
        if (root == null)
        {
            return;
        }

        SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = root.GetComponentInChildren<SpriteRenderer>();
        }

        if (renderer != null)
        {
            renderer.color = color;
        }
    }

    private static void UpdateWorldLabel(GameObject root, string labelText)
    {
        if (root == null || string.IsNullOrWhiteSpace(labelText))
        {
            return;
        }

        TextMeshPro label = root.GetComponentInChildren<TextMeshPro>(true);
        if (label == null)
        {
            return;
        }

        label.text = labelText;
        label.gameObject.name = root.name + "_Label";
        ApplyCompactLabelOffset(root, label);
    }

    private static void UpdateTextByObjectName(string objectName, string text)
    {
        if (string.IsNullOrWhiteSpace(objectName) || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        GameObject go = GameObject.Find(objectName);
        if (go == null)
        {
            return;
        }

        TextMeshPro textMesh = go.GetComponent<TextMeshPro>();
        if (textMesh != null)
        {
            textMesh.text = text;
        }
    }

    private static T FindComponentByName<T>(string objectName) where T : Component
    {
        GameObject go = GameObject.Find(objectName);
        return go != null ? go.GetComponent<T>() : null;
    }

    private static void ApplyCompactLabelOffset(GameObject root, TextMeshPro label)
    {
        if (root == null || label == null)
        {
            return;
        }

        float? compactY = null;

        if (root.GetComponent<ScenePortal>() != null)
        {
            compactY = 0.90f;
        }
        else if (root.GetComponent<RecipeSelectorStation>() != null || root.GetComponent<ServiceCounterStation>() != null)
        {
            compactY = 0.86f;
        }
        else if (root.GetComponent<StorageStation>() != null)
        {
            compactY = 0.78f;
        }
        else if (root.GetComponent<UpgradeStation>() != null)
        {
            compactY = 0.82f;
        }
        else if (root.GetComponent<GatherableResource>() != null)
        {
            compactY = 0.64f;
        }
        else if (root.GetComponent<PlayerController>() != null)
        {
            compactY = 0.46f;
        }

        if (!compactY.HasValue)
        {
            return;
        }

        Transform labelTransform = label.transform;
        Vector3 localPosition = labelTransform.localPosition;
        labelTransform.localPosition = new Vector3(localPosition.x, compactY.Value, localPosition.z);
    }
}
