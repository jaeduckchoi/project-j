using TMPro;
using Core;
using Interaction;
using Inventory;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting.APIUpdating;

// 허브 창고 팝업을 여는 단일 상호작용 지점이다.
namespace Storage
{
    [MovedFrom(false, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "StorageStation")]
    public class StorageStation : MonoBehaviour, IInteractable
    {
    [SerializeField] private StorageManager storageManager;
    [SerializeField] private StorageStationAction stationAction = StorageStationAction.StoreAll;
    [SerializeField] private string promptLabel = "창고 열기";

    public string InteractionPrompt
    {
        get
        {
            StorageManager currentStorageManager = ResolveStorageManager();
            InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
            if (currentStorageManager == null || inventory == null)
            {
                return string.Empty;
            }

            return $"[E] {promptLabel}";
        }
    }

    public Transform InteractionTransform => transform;

    /*
     * 허브에 배치된 StorageManager 참조를 자동으로 찾습니다.
     */
    private void Awake()
    {
        if (storageManager == null)
        {
            storageManager = FindFirstObjectByType<StorageManager>();
        }

        ApplyUnifiedHubStoragePresentation();
    }

    /*
     * 런타임 보강이나 씬 생성 단계에서 패드 역할을 다시 설정합니다.
     */
    public void Configure(StorageManager manager, StorageStationAction action, string label)
    {
        storageManager = manager;
        stationAction = action;

        if (!string.IsNullOrWhiteSpace(label))
        {
            promptLabel = label;
        }
    }

    /*
     * 창고와 인벤토리만 있으면 막힌 상태 설명까지 포함해 상호작용을 허용합니다.
     */
    public bool CanInteract(GameObject interactor)
    {
        StorageManager currentStorageManager = ResolveStorageManager();
        InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
        return currentStorageManager != null && inventory != null;
    }

    /*
     * 패드 종류에 따라 맡기기, 꺼내기, 선택 순환 동작을 실행합니다.
     */
    public void Interact(GameObject interactor)
    {
        StorageManager currentStorageManager = ResolveStorageManager();
        InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
        if (currentStorageManager == null || inventory == null)
        {
            return;
        }

        FindFirstObjectByType<UIManager>()?.ShowStoragePanel();
        GameManager.Instance?.DayCycle?.ShowHintOnce(
            "first_storage_popup_open",
            "창고 팝업에서 Q/W로 맡기기, A/S로 꺼내기를 진행할 수 있습니다.");
    }

    private void ApplyUnifiedHubStoragePresentation()
    {
        if (SceneManager.GetActiveScene().name != "Hub")
        {
            return;
        }

        StorageStation[] stations = FindObjectsByType<StorageStation>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
        StorageStation primaryStation = null;

        foreach (StorageStation station in stations)
        {
            if (station == null)
            {
                continue;
            }

            if (station.gameObject.name == "StorageStation")
            {
                primaryStation = station;
                break;
            }

            if (primaryStation == null && station.stationAction == StorageStationAction.CycleInventorySelection)
            {
                primaryStation = station;
            }
        }

        if (primaryStation == null && stations.Length > 0)
        {
            primaryStation = stations[0];
        }

        if (primaryStation != this)
        {
            gameObject.SetActive(false);
            return;
        }

        promptLabel = "창고 열기";

        TextMeshPro worldLabel = GetComponentInChildren<TextMeshPro>(true);
        if (worldLabel != null)
        {
            worldLabel.text = "창고";
        }
    }

    /*
     * 우선 GameManager 에서, 없으면 씬 검색으로 StorageManager 를 찾습니다.
     */
    private StorageManager ResolveStorageManager()
    {
        if (storageManager == null && GameManager.Instance != null)
        {
            storageManager = GameManager.Instance.Storage;
        }

        if (storageManager == null)
        {
            storageManager = FindFirstObjectByType<StorageManager>();
        }

        return storageManager;
    }
}

    public enum StorageStationAction
{
    StoreAll,
    WithdrawAll,
    StoreSelected,
    WithdrawSelected,
    CycleInventorySelection,
    CycleStorageSelection
}
}