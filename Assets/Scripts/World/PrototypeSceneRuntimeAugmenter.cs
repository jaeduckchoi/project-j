using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// 씬 재생성 전에도 필요한 상호작용을 현재 장면에 즉시 보강한다.
public static class PrototypeSceneRuntimeAugmenter
{
    /*
     * 현재 씬 이름에 맞는 프로토타입 보강 작업을 실행합니다.
     */
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

            case "AbandonedMine":
                EnsureAbandonedMineReady();
                break;

            case "WindHill":
                EnsureWindHillReady();
                break;
        }
    }

    /*
     * 허브 씬의 창고 선택 패드와 폐광산 포탈을 보강합니다.
     */
    private static void EnsureHubReady()
    {
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

    /*
     * 바람 언덕 숏컷 포탈과 숏컷용 스폰 포인트를 보강합니다.
     */
    private static void EnsureWindHillReady()
    {
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
            true,
            ToolType.None,
            6,
            "평판 6을 모으면 바람 언덕의 지름길을 이용할 수 있습니다.",
            new Color(0.55f, 0.77f, 0.95f));
    }

    /*
     * 폐광산의 채집, 어둠, 잔해, 안내 문구를 보강합니다.
     */
    private static void EnsureAbandonedMineReady()
    {
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

    /*
     * 허브에 폐광산 이동 포탈이 없으면 기존 포탈을 복제해 추가합니다.
     */
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
            true,
            ToolType.Lantern,
            0,
            "작업대에서 랜턴을 준비해야 폐광산 안쪽을 안전하게 탐험할 수 있습니다.",
            new Color(0.72f, 0.74f, 0.78f));
    }

    /*
     * 폐광산 내부 어둠 지대를 찾거나 새로 만들어 설정합니다.
     */
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

    /*
     * 폐광산 잔해 구간의 감속 지대 이름과 수치를 정리합니다.
     */
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

    /*
     * placeholder 채집 오브젝트를 발광 이끼 전용 채집 지점으로 바꿉니다.
     */
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

    /*
     * 창고 패드 복제본을 찾거나 새로 만든 뒤 원하는 동작으로 설정합니다.
     */
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

    /*
     * 창고 패드의 액션, 색상, 월드 라벨을 함께 설정합니다.
     */
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

    /*
     * 포탈 목적지, 잠금 조건, 색상, 월드 라벨을 함께 설정합니다.
     */
    private static void ConfigurePortal(
        ScenePortal portal,
        string targetSceneName,
        string spawnPointId,
        string label,
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
            label,
            requireMorningExplore,
            requiredToolType,
            requiredReputation,
            lockedGuideText);

        UpdatePrimaryRendererColor(portal.gameObject, color);
        UpdateWorldLabel(portal.gameObject, label);
    }

    /*
     * 필요한 스폰 포인트를 찾거나 새로 만든 뒤 위치와 id 를 맞춥니다.
     */
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

    /*
     * 오브젝트의 기본 SpriteRenderer 색상을 바꿉니다.
     */
    private static void UpdatePrimaryRendererColor(GameObject root, Color color)
    {
        if (root == null)
        {
            return;
        }

        SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = color;
        }
    }

    /*
     * 오브젝트 자식의 TextMeshPro 라벨 문자열을 갱신합니다.
     */
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
    }

    /*
     * 특정 이름의 TextMeshPro 오브젝트에 직접 문자열을 씁니다.
     */
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

    /*
     * 이름으로 GameObject 를 찾은 뒤 원하는 컴포넌트를 반환합니다.
     */
    private static T FindComponentByName<T>(string objectName) where T : Component
    {
        GameObject go = GameObject.Find(objectName);
        return go != null ? go.GetComponent<T>() : null;
    }
}
