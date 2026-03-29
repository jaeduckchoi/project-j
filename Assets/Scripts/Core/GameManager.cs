using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting.APIUpdating;
using Economy;
using Flow;
using Inventory;
using Player;
using Storage;
using Tools;
using Upgrade;
using World;

// 인벤토리, 창고, 업그레이드, 도구, 하루 흐름, 경제, 씬 이동 상태를 유지하는 전역 게임 진입점이다.
namespace Core
{
    [MovedFrom(false, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "GameManager")]
    public class GameManager : MonoBehaviour
    {
    [Header("Scene Names")]
    [SerializeField] private string hubSceneName = "Hub";
    [SerializeField] private string firstExplorationSceneName = "Beach";

    [Header("Managers")]
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private StorageManager storageManager;
    [SerializeField] private EconomyManager economyManager;
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private DayCycleManager dayCycleManager;
    [SerializeField] private UpgradeManager upgradeManager;

    private string _pendingSpawnPointId;

    public static GameManager Instance { get; private set; }

    public InventoryManager Inventory => inventoryManager;
    public StorageManager Storage => storageManager;
    public EconomyManager Economy => economyManager;
    public ToolManager Tools => toolManager;
    public DayCycleManager DayCycle => dayCycleManager;
    public UpgradeManager Upgrades => upgradeManager;
    public string HubSceneName => hubSceneName;
    public string FirstExplorationSceneName => firstExplorationSceneName;

    /*
     * 전역 GameManager 싱글턴을 구성하고 필요한 매니저를 즉시 초기화합니다.
     */
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 씬마다 따로 놓치더라도 여기서 필수 매니저를 보장합니다.
        inventoryManager = EnsureManager(inventoryManager);
        storageManager = EnsureManager(storageManager);
        economyManager = EnsureManager(economyManager);
        toolManager = EnsureManager(toolManager);
        dayCycleManager = EnsureManager(dayCycleManager);
        upgradeManager = EnsureManager(upgradeManager);

        // 프로토타입은 저장 로드가 없으므로 시작 시점에 바로 런타임 상태를 세웁니다.
        inventoryManager?.InitializeIfNeeded();
        storageManager?.InitializeIfNeeded();
        economyManager?.InitializeIfNeeded();
        toolManager?.InitializeIfNeeded();
        dayCycleManager?.InitializeIfNeeded();
        upgradeManager?.InitializeIfNeeded();
    }

    /*
     * 씬 로드 콜백을 등록해 씬 전환 후 후처리를 받을 수 있게 합니다.
     */
    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    /*
     * 비활성화 시 씬 로드 콜백을 정리합니다.
     */
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    /*
     * 허브로 이동할 때 사용하는 단축 진입점입니다.
     */
    public void LoadHub(string spawnPointId = "")
    {
        LoadScene(hubSceneName, spawnPointId);
    }

    /*
     * 첫 탐험 지역으로 이동할 때 사용하는 단축 진입점입니다.
     */
    public void LoadFirstExploration(string spawnPointId = "")
    {
        LoadScene(firstExplorationSceneName, spawnPointId);
    }

    /*
     * 씬 이름과 스폰 포인트를 받아 실제 씬 전환을 수행합니다.
     */
    public void LoadScene(string sceneName, string spawnPointId = "")
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("LoadScene failed: sceneName is empty.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"LoadScene failed: scene '{sceneName}' is not available.");
            dayCycleManager?.ShowTemporaryGuide(
                $"{sceneName} 씬이 아직 준비되지 않았습니다. Unity에서 Tools > Jonggu Restaurant > Build Minimal Prototype를 다시 실행해 주세요.");
            return;
        }

        // 하루 단계는 이동 직전에 바뀌어야 허브 복귀 / 출발 상태가 꼬이지 않습니다.
        dayCycleManager?.HandleSceneTravel(SceneManager.GetActiveScene().name, sceneName, hubSceneName);

        _pendingSpawnPointId = spawnPointId;
        SceneManager.LoadScene(sceneName);
    }

    /*
     * 첫 진입 씬에도 런타임 보강과 지역 진입 안내를 적용합니다.
     */
    private void Start()
    {
        PrototypeSceneRuntimeAugmenter.EnsureSceneReady(SceneManager.GetActiveScene());
        dayCycleManager?.HandleSceneEntered(SceneManager.GetActiveScene().name);
    }

    /*
     * 씬이 바뀐 직후 누락 오브젝트 보강과 스폰 위치 이동을 처리합니다.
     */
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PrototypeSceneRuntimeAugmenter.EnsureSceneReady(scene);
        TryMovePlayerToPendingSpawn(scene);
        dayCycleManager?.HandleSceneEntered(scene.name);
    }

    /*
     * 직전에 요청된 스폰 포인트가 있으면 플레이어를 해당 위치로 옮깁니다.
     */
    private void TryMovePlayerToPendingSpawn(Scene scene)
    {
        if (string.IsNullOrWhiteSpace(_pendingSpawnPointId))
        {
            return;
        }

        // 플레이어가 아직 생성되지 않은 경우에는 조용히 종료합니다.
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            return;
        }

        // 같은 씬 안에서 spawnId 가 맞는 포인트를 찾으면 즉시 이동합니다.
        SceneSpawnPoint[] spawnPoints = FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None);
        foreach (SceneSpawnPoint spawnPoint in spawnPoints)
        {
            if (!spawnPoint.Matches(_pendingSpawnPointId))
            {
                continue;
            }

            player.SetWorldPosition(spawnPoint.transform.position);
            _pendingSpawnPointId = string.Empty;
            return;
        }

        Debug.LogWarning($"Spawn point '{_pendingSpawnPointId}' was not found in scene '{scene.name}'.");
        _pendingSpawnPointId = string.Empty;
    }

    /*
     * 비어 있는 매니저 참조를 현재 GameObject 에서 확보하거나 새로 추가합니다.
     */
    private T EnsureManager<T>(T currentManager) where T : Component
    {
        if (currentManager != null)
        {
            return currentManager;
        }

        T manager = GetComponent<T>();
        if (manager == null)
        {
            manager = gameObject.AddComponent<T>();
        }

        return manager;
    }
    }
}