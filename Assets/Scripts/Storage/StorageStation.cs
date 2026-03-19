using UnityEngine;

// 허브 창고에서 품목 선택, 맡기기, 꺼내기를 수행하는 상호작용 지점이다.
public class StorageStation : MonoBehaviour, IInteractable
{
    [SerializeField] private StorageManager storageManager;
    [SerializeField] private StorageStationAction stationAction = StorageStationAction.StoreAll;
    [SerializeField] private string promptLabel = "창고 사용";

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

            InventoryEntry selectedInventoryEntry = currentStorageManager.GetSelectedInventoryEntry(inventory);
            InventoryEntry selectedStorageEntry = currentStorageManager.GetSelectedStoredEntry();

            return stationAction switch
            {
                StorageStationAction.StoreAll when inventory.RuntimeItems.Count == 0 => "맡길 재료 없음",
                StorageStationAction.WithdrawAll when !currentStorageManager.HasAnyStoredItems() => "꺼낼 재료 없음",
                StorageStationAction.StoreSelected when selectedInventoryEntry == null => "맡길 품목 없음",
                StorageStationAction.WithdrawSelected when selectedStorageEntry == null => "꺼낼 품목 없음",
                StorageStationAction.CycleInventorySelection when selectedInventoryEntry == null => "맡길 품목 없음",
                StorageStationAction.CycleStorageSelection when selectedStorageEntry == null => "꺼낼 품목 없음",
                StorageStationAction.StoreSelected => $"[E] {promptLabel}: {selectedInventoryEntry.Resource.DisplayName}",
                StorageStationAction.WithdrawSelected => $"[E] {promptLabel}: {selectedStorageEntry.Resource.DisplayName}",
                StorageStationAction.CycleInventorySelection => $"[E] {promptLabel}: {selectedInventoryEntry.Resource.DisplayName}",
                StorageStationAction.CycleStorageSelection => $"[E] {promptLabel}: {selectedStorageEntry.Resource.DisplayName}",
                _ => $"[E] {promptLabel}"
            };
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

        switch (stationAction)
        {
            case StorageStationAction.StoreAll:
                currentStorageManager.StoreAllFromInventory(inventory);
                GameManager.Instance?.DayCycle?.ShowHintOnce(
                    "first_storage_store",
                    "창고에 남는 재료를 맡겨 두면 다음 장사나 업그레이드 때 다시 꺼내 쓸 수 있습니다.");
                break;

            case StorageStationAction.WithdrawAll:
                currentStorageManager.WithdrawAllToInventory(inventory);
                GameManager.Instance?.DayCycle?.ShowHintOnce(
                    "first_storage_withdraw",
                    "장사에 필요한 재료를 창고에서 꺼내 메뉴를 준비해 보세요.");
                break;

            case StorageStationAction.StoreSelected:
                currentStorageManager.StoreSelectedFromInventory(inventory);
                break;

            case StorageStationAction.WithdrawSelected:
                currentStorageManager.WithdrawSelectedToInventory(inventory);
                break;

            case StorageStationAction.CycleInventorySelection:
                currentStorageManager.CycleInventorySelection(inventory);
                GameManager.Instance?.DayCycle?.ShowHintOnce(
                    "first_storage_select_deposit",
                    "품목 변경 칸에서 맡길 재료를 고른 뒤 맡기기 칸에서 옮길 수 있습니다.");
                break;

            case StorageStationAction.CycleStorageSelection:
                currentStorageManager.CycleStoredSelection();
                GameManager.Instance?.DayCycle?.ShowHintOnce(
                    "first_storage_select_withdraw",
                    "품목 변경 칸에서 꺼낼 재료를 고른 뒤 꺼내기 칸에서 인벤토리로 돌릴 수 있습니다.");
                break;
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
