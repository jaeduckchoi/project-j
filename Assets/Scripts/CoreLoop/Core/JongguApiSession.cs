using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using CoreLoop.Flow;
using Exploration.Gathering;
using Exploration.World;
using Management.Inventory;
using Management.Storage;
using Management.Tools;
using Management.Upgrade;
using Restaurant;
using Shared.Data;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace CoreLoop.Core
{
    /// <summary>
    /// 인접 Spring Boot API와 Unity 런타임 상태를 연결하는 원격 세션 관리자다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JongguApiSession : MonoBehaviour
    {
        private const string DefaultBaseUrl = "http://localhost:8080";
        private const string SavedPlayerIdKey = "jonggu.api.player-id";

        [Header("Remote API")]
        [SerializeField] private bool enableApiSync = true;
        [SerializeField] private string apiBaseUrl = DefaultBaseUrl;
        [SerializeField] private string playerDisplayName = "종구";
        [SerializeField] private bool resumeSavedPlayer = true;
        [SerializeField] private bool loadRemoteRegionOnStart = true;
        [SerializeField] private bool verboseLogging;

        private JongguApiBootstrap bootstrap;
        private JongguApiPlayerSnapshot currentSnapshot;
        private string currentPlayerId = string.Empty;
        private bool initializationStarted;
        private bool sessionReady;
        private bool requestInFlight;
        private bool remoteDisabled;

        public bool IsRemoteConfigured => enableApiSync && !remoteDisabled && !string.IsNullOrWhiteSpace(apiBaseUrl);
        public bool IsReady => IsRemoteConfigured && sessionReady;
        public bool IsBusy => requestInFlight;
        public string CurrentPlayerId => currentPlayerId;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        public void BeginSession()
        {
            if (!IsRemoteConfigured || initializationStarted)
            {
                return;
            }

            initializationStarted = true;
            RunExclusive(BeginSessionRoutine());
        }

        public bool TryTravel(ScenePortal portal)
        {
            if (!TryBeginRemoteAction("이동 정보를 API와 동기화하는 중입니다."))
            {
                return false;
            }

            if (portal == null)
            {
                ShowGuide("이동 포탈 정보를 찾지 못했습니다.");
                return true;
            }

            string fromRegionCode = ResolveCurrentRegionCode();
            string toRegionCode = portal.TargetSceneName;
            if (!TryResolvePortalCode(fromRegionCode, toRegionCode, out string portalCode))
            {
                ShowGuide("현재 포탈과 연결된 API 규칙을 찾지 못했습니다.");
                return true;
            }

            bool shouldSkipExplorationAfterReturn =
                string.Equals(toRegionCode, GameManager.Instance != null ? GameManager.Instance.HubSceneName : "Hub", StringComparison.Ordinal)
                && currentSnapshot != null
                && string.Equals(currentSnapshot.currentPhase, "morning_explore", StringComparison.Ordinal)
                && !string.Equals(fromRegionCode, toRegionCode, StringComparison.Ordinal);

            RunExclusive(TravelRoutine(portalCode, portal.TargetSceneName, portal.TargetSpawnPointId, shouldSkipExplorationAfterReturn));
            return true;
        }

        public bool TryGather(GatherableResource gatherable)
        {
            if (!TryBeginRemoteAction("채집 결과를 API와 동기화하는 중입니다."))
            {
                return false;
            }

            if (gatherable == null || !gatherable.TryCreateGatherAttempt(out ResourceData resource, out int amount))
            {
                ShowGuide("채집 정보를 준비하지 못했습니다.");
                return true;
            }

            string resourceCode = ResolveResourceCode(resource);
            if (string.IsNullOrWhiteSpace(resourceCode))
            {
                ShowGuide("채집 자원 코드가 비어 있습니다.");
                return true;
            }

            RunExclusive(GatherRoutine(gatherable, ResolveCurrentRegionCode(), resource, resourceCode, amount));
            return true;
        }

        public bool TrySkipExploration()
        {
            if (!TryBeginRemoteAction("오전 탐험 종료를 API와 동기화하는 중입니다."))
            {
                return false;
            }

            RunExclusive(SkipExplorationRoutine());
            return true;
        }

        public bool TrySelectRecipe(RestaurantManager restaurantManager, int recipeIndex)
        {
            if (!TryBeginRemoteAction("메뉴 선택을 API와 동기화하는 중입니다."))
            {
                return false;
            }

            if (restaurantManager == null)
            {
                ShowGuide("메뉴 정보를 찾지 못했습니다.");
                return true;
            }

            IReadOnlyList<RecipeData> recipes = restaurantManager.AvailableRecipes;
            if (recipes == null || recipeIndex < 0 || recipeIndex >= recipes.Count || recipes[recipeIndex] == null)
            {
                ShowGuide("선택할 메뉴를 찾지 못했습니다.");
                return true;
            }

            RecipeData recipe = recipes[recipeIndex];
            string recipeCode = ResolveRecipeCode(recipe);
            if (string.IsNullOrWhiteSpace(recipeCode))
            {
                ShowGuide("메뉴 코드가 비어 있습니다.");
                return true;
            }

            RunExclusive(SelectRecipeRoutine(recipe, recipeCode));
            return true;
        }

        public bool TryRunService(RestaurantManager restaurantManager)
        {
            if (!TryBeginRemoteAction("영업 결과를 API와 동기화하는 중입니다."))
            {
                return false;
            }

            if (restaurantManager == null)
            {
                ShowGuide("영업 정보를 찾지 못했습니다.");
                return true;
            }

            RunExclusive(RunServiceRoutine());
            return true;
        }

        public bool TrySkipService(RestaurantManager restaurantManager)
        {
            if (!TryBeginRemoteAction("영업 건너뛰기를 API와 동기화하는 중입니다."))
            {
                return false;
            }

            RunExclusive(SkipServiceRoutine());
            return true;
        }

        public bool TryAdvanceToNextDay()
        {
            if (!TryBeginRemoteAction("다음 날 전환을 API와 동기화하는 중입니다."))
            {
                return false;
            }

            RunExclusive(AdvanceToNextDayRoutine());
            return true;
        }

        public bool TryStoreSelected(StorageManager storageManager, InventoryManager inventoryManager)
        {
            if (!TryBeginRemoteAction("창고 맡기기를 API와 동기화하는 중입니다."))
            {
                return false;
            }

            if (storageManager == null || inventoryManager == null)
            {
                ShowGuide("창고나 인벤토리 정보를 찾지 못했습니다.");
                return true;
            }

            InventoryEntry selectedEntry = storageManager.GetSelectedInventoryEntry(inventoryManager);
            if (selectedEntry == null || selectedEntry.resource == null || selectedEntry.amount <= 0)
            {
                ShowGuide("맡길 재료가 없습니다.");
                return true;
            }

            RunExclusive(StorageTransferRoutine(
                "deposit",
                selectedEntry.resource,
                ResolveResourceCode(selectedEntry.resource),
                selectedEntry.amount,
                $"{selectedEntry.resource.DisplayName} x{selectedEntry.amount}을(를) 창고에 맡겼습니다."));
            return true;
        }

        public bool TryWithdrawSelected(StorageManager storageManager, InventoryManager inventoryManager)
        {
            if (!TryBeginRemoteAction("창고 꺼내기를 API와 동기화하는 중입니다."))
            {
                return false;
            }

            if (storageManager == null || inventoryManager == null)
            {
                ShowGuide("창고나 인벤토리 정보를 찾지 못했습니다.");
                return true;
            }

            InventoryEntry selectedEntry = storageManager.GetSelectedStoredEntry();
            if (selectedEntry == null || selectedEntry.resource == null || selectedEntry.amount <= 0)
            {
                ShowGuide("꺼낼 재료가 없습니다.");
                return true;
            }

            RunExclusive(StorageTransferRoutine(
                "withdraw",
                selectedEntry.resource,
                ResolveResourceCode(selectedEntry.resource),
                selectedEntry.amount,
                $"{selectedEntry.resource.DisplayName} x{selectedEntry.amount}을(를) 창고에서 꺼냈습니다."));
            return true;
        }

        public bool TryPerformPreferredUpgrade(UpgradeManager upgradeManager)
        {
            if (!TryBeginRemoteAction("업그레이드를 API와 동기화하는 중입니다."))
            {
                return false;
            }

            if (upgradeManager == null)
            {
                ShowGuide("업그레이드 정보를 찾지 못했습니다.");
                return true;
            }

            if (!TryResolvePreferredUpgradeCode(upgradeManager, out string upgradeCode))
            {
                ShowGuide("현재 우선 업그레이드에 맞는 API 코드를 찾지 못했습니다.");
                return true;
            }

            RunExclusive(PurchaseUpgradeRoutine(upgradeCode));
            return true;
        }

        private IEnumerator BeginSessionRoutine()
        {
            string savedPlayerId = resumeSavedPlayer ? LoadSavedPlayerId() : string.Empty;

            bool bootstrapResolved = false;
            yield return SendRequest(
                UnityWebRequest.kHttpVerbGET,
                "/api/v1/bootstrap",
                null,
                responseText =>
                {
                    JongguApiBootstrapEnvelope envelope = JsonUtility.FromJson<JongguApiBootstrapEnvelope>(responseText);
                    bootstrap = envelope != null ? envelope.data : null;
                    bootstrapResolved = bootstrap != null;
                },
                (_, message) => ShowGuide(message));

            if (!bootstrapResolved)
            {
                DisableRemote("API bootstrap을 불러오지 못해 로컬 상태로 계속합니다.");
                yield break;
            }

            bool snapshotLoaded = false;
            if (!string.IsNullOrWhiteSpace(savedPlayerId))
            {
                yield return SendRequest(
                    UnityWebRequest.kHttpVerbGET,
                    $"/api/v1/players/{savedPlayerId}/snapshot",
                    null,
                    responseText =>
                    {
                        JongguApiPlayerSnapshotEnvelope envelope = JsonUtility.FromJson<JongguApiPlayerSnapshotEnvelope>(responseText);
                        if (envelope == null || envelope.data == null)
                        {
                            return;
                        }

                        currentPlayerId = envelope.data.playerId;
                        ApplySnapshot(envelope.data, syncScene: loadRemoteRegionOnStart);
                        snapshotLoaded = true;
                    },
                    (code, message) =>
                    {
                        if (string.Equals(code, "PLAYER_NOT_FOUND", StringComparison.Ordinal))
                        {
                            ClearSavedPlayerId();
                            return;
                        }

                        ShowGuide(message);
                    });
            }

            if (!snapshotLoaded)
            {
                string createBody = JsonUtility.ToJson(new JongguApiCreatePlayerRequest { displayName = playerDisplayName });
                bool playerCreated = false;
                yield return SendRequest(
                    UnityWebRequest.kHttpVerbPOST,
                    "/api/v1/players",
                    createBody,
                    responseText =>
                    {
                        JongguApiCreatePlayerEnvelope envelope = JsonUtility.FromJson<JongguApiCreatePlayerEnvelope>(responseText);
                        if (envelope == null || envelope.data == null || envelope.data.snapshot == null)
                        {
                            return;
                        }

                        currentPlayerId = envelope.data.playerId;
                        SavePlayerId(currentPlayerId);
                        ApplySnapshot(envelope.data.snapshot, syncScene: loadRemoteRegionOnStart);
                        playerCreated = true;
                    },
                    (_, message) => ShowGuide(message));

                if (!playerCreated)
                {
                    DisableRemote("API 세션을 만들지 못해 로컬 상태로 계속합니다.");
                    yield break;
                }
            }
            else
            {
                SavePlayerId(currentPlayerId);
            }

            sessionReady = true;
        }

        private IEnumerator TravelRoutine(string portalCode, string targetSceneName, string spawnPointId, bool shouldSkipExplorationAfterReturn)
        {
            string travelBody = JsonUtility.ToJson(new JongguApiTravelRequest { portalCode = portalCode });
            bool travelSucceeded = false;
            yield return SendRequest(
                UnityWebRequest.kHttpVerbPOST,
                $"/api/v1/players/{currentPlayerId}/travel",
                travelBody,
                responseText =>
                {
                    JongguApiPlayerSnapshotEnvelope envelope = JsonUtility.FromJson<JongguApiPlayerSnapshotEnvelope>(responseText);
                    if (envelope == null || envelope.data == null)
                    {
                        return;
                    }

                    ApplySnapshot(envelope.data, syncScene: false);
                    travelSucceeded = true;
                },
                (_, message) => ShowGuide(message));

            if (!travelSucceeded)
            {
                yield break;
            }

            if (shouldSkipExplorationAfterReturn)
            {
                yield return SendRequest(
                    UnityWebRequest.kHttpVerbPOST,
                    $"/api/v1/players/{currentPlayerId}/exploration/skip",
                    null,
                    responseText =>
                    {
                        JongguApiPlayerSnapshotEnvelope envelope = JsonUtility.FromJson<JongguApiPlayerSnapshotEnvelope>(responseText);
                        if (envelope == null || envelope.data == null)
                        {
                            return;
                        }

                        ApplySnapshot(envelope.data, syncScene: false);
                    },
                    (_, message) => ShowGuide(message));
            }

            GameManager.Instance?.LoadSceneFromRemoteState(targetSceneName, spawnPointId);
        }

        private IEnumerator GatherRoutine(GatherableResource gatherable, string regionCode, ResourceData resource, string resourceCode, int amount)
        {
            string requestBody = JsonUtility.ToJson(new JongguApiGatherRequest
            {
                regionCode = regionCode,
                resourceCode = resourceCode,
                quantity = amount
            });

            yield return SendRequest(
                UnityWebRequest.kHttpVerbPOST,
                $"/api/v1/players/{currentPlayerId}/gathers",
                requestBody,
                responseText =>
                {
                    JongguApiGatherEnvelope envelope = JsonUtility.FromJson<JongguApiGatherEnvelope>(responseText);
                    if (envelope == null || envelope.data == null || envelope.data.snapshot == null)
                    {
                        return;
                    }

                    ApplySnapshot(envelope.data.snapshot, syncScene: false);

                    if (envelope.data.success && envelope.data.quantityGranted > 0)
                    {
                        gatherable.ApplyRemoteGatherSuccess();
                        ShowGuide($"{resource.DisplayName} x{envelope.data.quantityGranted}을(를) 챙겼습니다.");
                        GameManager.Instance?.DayCycle?.ShowHintOnce(
                            "first_gather_hint",
                            "모은 재료는 식당 메뉴와 인벤토리 업그레이드에 바로 사용할 수 있습니다.",
                            5f);
                    }
                    else
                    {
                        ShowGuide(TranslateApiMessage(null, envelope.data.message, "채집에 실패했습니다."));
                    }
                },
                (_, message) => ShowGuide(message));
        }

        private IEnumerator SkipExplorationRoutine()
        {
            yield return SendRequest(
                UnityWebRequest.kHttpVerbPOST,
                $"/api/v1/players/{currentPlayerId}/exploration/skip",
                null,
                responseText =>
                {
                    JongguApiPlayerSnapshotEnvelope envelope = JsonUtility.FromJson<JongguApiPlayerSnapshotEnvelope>(responseText);
                    if (envelope == null || envelope.data == null)
                    {
                        return;
                    }

                    ApplySnapshot(envelope.data, syncScene: true);
                },
                (_, message) => ShowGuide(message));
        }

        private IEnumerator SelectRecipeRoutine(RecipeData recipe, string recipeCode)
        {
            string requestBody = JsonUtility.ToJson(new JongguApiSelectRecipeRequest { recipeCode = recipeCode });
            yield return SendRequest(
                UnityWebRequest.kHttpVerbPOST,
                $"/api/v1/players/{currentPlayerId}/recipes/select",
                requestBody,
                responseText =>
                {
                    JongguApiPlayerSnapshotEnvelope envelope = JsonUtility.FromJson<JongguApiPlayerSnapshotEnvelope>(responseText);
                    if (envelope == null || envelope.data == null)
                    {
                        return;
                    }

                    ApplySnapshot(envelope.data, $"메뉴를 {recipe.DisplayName}(으)로 변경했습니다.", syncScene: false);
                },
                (_, message) => ShowGuide(message));
        }

        private IEnumerator RunServiceRoutine()
        {
            yield return SendRequest(
                UnityWebRequest.kHttpVerbPOST,
                $"/api/v1/players/{currentPlayerId}/service/run",
                null,
                responseText =>
                {
                    JongguApiServiceRunEnvelope envelope = JsonUtility.FromJson<JongguApiServiceRunEnvelope>(responseText);
                    if (envelope == null || envelope.data == null || envelope.data.snapshot == null)
                    {
                        return;
                    }

                    ApplySnapshot(envelope.data.snapshot, syncScene: false, serviceResult: BuildServiceResultText(envelope.data));
                },
                (_, message) => ShowGuide(message));
        }

        private IEnumerator SkipServiceRoutine()
        {
            yield return SendRequest(
                UnityWebRequest.kHttpVerbPOST,
                $"/api/v1/players/{currentPlayerId}/service/skip",
                null,
                responseText =>
                {
                    JongguApiServiceRunEnvelope envelope = JsonUtility.FromJson<JongguApiServiceRunEnvelope>(responseText);
                    if (envelope == null || envelope.data == null || envelope.data.snapshot == null)
                    {
                        return;
                    }

                    ApplySnapshot(envelope.data.snapshot, syncScene: false, serviceResult: BuildServiceResultText(envelope.data));
                },
                (_, message) => ShowGuide(message));
        }

        private IEnumerator AdvanceToNextDayRoutine()
        {
            yield return SendRequest(
                UnityWebRequest.kHttpVerbPOST,
                $"/api/v1/players/{currentPlayerId}/day/next",
                null,
                responseText =>
                {
                    JongguApiPlayerSnapshotEnvelope envelope = JsonUtility.FromJson<JongguApiPlayerSnapshotEnvelope>(responseText);
                    if (envelope == null || envelope.data == null)
                    {
                        return;
                    }

                    ApplySnapshot(envelope.data, syncScene: true);
                },
                (_, message) => ShowGuide(message));
        }

        private IEnumerator StorageTransferRoutine(string actionPath, ResourceData resource, string resourceCode, int quantity, string successMessage)
        {
            string requestBody = JsonUtility.ToJson(new JongguApiStorageTransferRequest
            {
                resourceCode = resourceCode,
                quantity = quantity
            });

            yield return SendRequest(
                UnityWebRequest.kHttpVerbPOST,
                $"/api/v1/players/{currentPlayerId}/storage/{actionPath}",
                requestBody,
                responseText =>
                {
                    JongguApiPlayerSnapshotEnvelope envelope = JsonUtility.FromJson<JongguApiPlayerSnapshotEnvelope>(responseText);
                    if (envelope == null || envelope.data == null)
                    {
                        return;
                    }

                    ApplySnapshot(envelope.data, storageMessage: successMessage, syncScene: false);
                },
                (_, message) => ShowGuide(message));
        }

        private IEnumerator PurchaseUpgradeRoutine(string upgradeCode)
        {
            bool previouslyHadLantern = HasUnlockedTool(currentSnapshot, "Lantern");
            int previousInventoryLimit = currentSnapshot != null ? currentSnapshot.inventorySlotLimit : 0;

            yield return SendRequest(
                UnityWebRequest.kHttpVerbPOST,
                $"/api/v1/players/{currentPlayerId}/upgrades/{upgradeCode}/purchase",
                null,
                responseText =>
                {
                    JongguApiUpgradePurchaseEnvelope envelope = JsonUtility.FromJson<JongguApiUpgradePurchaseEnvelope>(responseText);
                    if (envelope == null || envelope.data == null || envelope.data.snapshot == null)
                    {
                        return;
                    }

                    ApplySnapshot(envelope.data.snapshot, syncScene: false);

                    if (!previouslyHadLantern && HasUnlockedTool(envelope.data.snapshot, "Lantern"))
                    {
                        GameManager.Instance?.DayCycle?.ShowHintOnce(
                            "first_unlock_lantern",
                            "랜턴을 준비했습니다. 이제 폐광산처럼 어두운 지역에도 들어갈 수 있습니다.");
                    }

                    if (envelope.data.snapshot.inventorySlotLimit > previousInventoryLimit)
                    {
                        GameManager.Instance?.DayCycle?.ShowHintOnce(
                            "first_upgrade_inventory",
                            "인벤토리가 넓어지면 한 번 탐험에서 더 많은 재료를 챙겨 돌아올 수 있습니다.");
                    }
                },
                (_, message) => ShowGuide(message));
        }

        private void ApplySnapshot(
            JongguApiPlayerSnapshot snapshot,
            string storageMessage = null,
            bool syncScene = false,
            string serviceResult = null)
        {
            if (snapshot == null)
            {
                return;
            }

            currentSnapshot = snapshot;
            currentPlayerId = snapshot.playerId;

            GameManager gameManager = GameManager.Instance != null ? GameManager.Instance : GetComponent<GameManager>();
            if (gameManager == null)
            {
                return;
            }

            gameManager.Inventory?.ApplyRemoteState(snapshot.inventorySlotLimit, ResolveInventoryEntries(snapshot.inventoryResources));
            gameManager.Storage?.ApplyRemoteState(ResolveInventoryEntries(snapshot.storageResources), storageMessage);
            gameManager.Economy?.ApplyRemoteState(snapshot.gold, snapshot.reputation);
            gameManager.Tools?.ApplyRemoteState(ResolveUnlockedTools(snapshot.unlockedTools));

            string settlementSummary = BuildSettlementSummary(snapshot.lastSettlementSummary);
            if (string.IsNullOrWhiteSpace(settlementSummary)
                && string.Equals(snapshot.currentPhase, "settlement", StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(serviceResult))
            {
                settlementSummary = serviceResult;
            }

            gameManager.DayCycle?.ApplyRemoteState(snapshot.currentDay, snapshot.currentPhase, settlementSummary);

            RestaurantManager restaurantManager = FindFirstObjectByType<RestaurantManager>();
            if (restaurantManager != null)
            {
                restaurantManager.ApplyRemoteState(snapshot.selectedRecipe, snapshot.serviceCapacity, serviceResult);
            }

            if (syncScene)
            {
                SyncSceneToRegion(snapshot.currentRegion);
            }
        }

        private void SyncSceneToRegion(string regionCode)
        {
            if (string.IsNullOrWhiteSpace(regionCode))
            {
                return;
            }

            if (string.Equals(SceneManager.GetActiveScene().name, regionCode, StringComparison.Ordinal))
            {
                return;
            }

            GameManager.Instance?.LoadSceneFromRemoteState(regionCode, GetDefaultSpawnPointId(regionCode));
        }

        private IEnumerable<InventoryEntry> ResolveInventoryEntries(List<JongguApiResourceAmount> amounts)
        {
            List<InventoryEntry> entries = new();
            if (amounts == null)
            {
                return entries;
            }

            foreach (JongguApiResourceAmount amount in amounts)
            {
                if (amount == null || amount.quantity <= 0 || string.IsNullOrWhiteSpace(amount.resourceCode))
                {
                    continue;
                }

                ResourceData resource = GeneratedGameDataLocator.FindGeneratedResource(amount.resourceCode);
                if (resource == null)
                {
                    if (verboseLogging)
                    {
                        Debug.LogWarning($"[JongguApiSession] Unknown resource code '{amount.resourceCode}'.");
                    }

                    continue;
                }

                entries.Add(new InventoryEntry(resource, amount.quantity));
            }

            return entries;
        }

        private IEnumerable<ToolType> ResolveUnlockedTools(List<string> toolCodes)
        {
            List<ToolType> resolvedTools = new();
            if (toolCodes == null)
            {
                return resolvedTools;
            }

            foreach (string toolCode in toolCodes)
            {
                ToolType toolType = ResolveToolType(toolCode);
                if (toolType != ToolType.None)
                {
                    resolvedTools.Add(toolType);
                }
            }

            return resolvedTools;
        }

        private bool TryResolvePortalCode(string fromRegionCode, string toRegionCode, out string portalCode)
        {
            portalCode = string.Empty;

            if (bootstrap?.portalRules == null)
            {
                return false;
            }

            foreach (JongguApiPortalRule portalRule in bootstrap.portalRules)
            {
                if (portalRule == null)
                {
                    continue;
                }

                if (string.Equals(portalRule.fromRegionCode, fromRegionCode, StringComparison.Ordinal)
                    && string.Equals(portalRule.toRegionCode, toRegionCode, StringComparison.Ordinal))
                {
                    portalCode = portalRule.code;
                    return !string.IsNullOrWhiteSpace(portalCode);
                }
            }

            return false;
        }

        private bool TryResolvePreferredUpgradeCode(UpgradeManager upgradeManager, out string upgradeCode)
        {
            upgradeCode = string.Empty;

            if (bootstrap?.upgrades == null || currentSnapshot == null)
            {
                return false;
            }

            switch (upgradeManager.GetPreferredAction())
            {
                case UpgradeWorkbenchAction.UnlockTool:
                {
                    string toolCode = upgradeManager.GetPreferredToolType().ToString();
                    foreach (JongguApiUpgradeDefinition upgrade in bootstrap.upgrades)
                    {
                        if (upgrade == null)
                        {
                            continue;
                        }

                        if (string.Equals(upgrade.upgradeType, "TOOL_UNLOCK", StringComparison.Ordinal)
                            && string.Equals(upgrade.toolCode, toolCode, StringComparison.Ordinal))
                        {
                            upgradeCode = upgrade.code;
                            return !string.IsNullOrWhiteSpace(upgradeCode);
                        }
                    }

                    break;
                }

                case UpgradeWorkbenchAction.UpgradeInventory:
                {
                    JongguApiUpgradeDefinition bestCandidate = null;
                    foreach (JongguApiUpgradeDefinition upgrade in bootstrap.upgrades)
                    {
                        if (upgrade == null
                            || !string.Equals(upgrade.upgradeType, "INVENTORY_SLOT", StringComparison.Ordinal)
                            || upgrade.targetValue <= currentSnapshot.inventorySlotLimit)
                        {
                            continue;
                        }

                        if (bestCandidate == null || upgrade.targetValue < bestCandidate.targetValue)
                        {
                            bestCandidate = upgrade;
                        }
                    }

                    if (bestCandidate != null)
                    {
                        upgradeCode = bestCandidate.code;
                        return !string.IsNullOrWhiteSpace(upgradeCode);
                    }

                    break;
                }
            }

            return false;
        }

        private bool TryBeginRemoteAction(string pendingMessage)
        {
            if (!enableApiSync || remoteDisabled || string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                return false;
            }

            if (!sessionReady)
            {
                ShowGuide(pendingMessage);
                return true;
            }

            if (requestInFlight)
            {
                ShowGuide("이전 API 요청이 아직 끝나지 않았습니다.");
                return true;
            }

            return true;
        }

        private void RunExclusive(IEnumerator routine)
        {
            requestInFlight = true;
            StartCoroutine(RunExclusiveRoutine(routine));
        }

        private IEnumerator RunExclusiveRoutine(IEnumerator routine)
        {
            yield return routine;
            requestInFlight = false;
        }

        private IEnumerator SendRequest(
            string method,
            string path,
            string bodyJson,
            Action<string> onSuccess,
            Action<string, string> onFailure)
        {
            using UnityWebRequest request = new(BuildUrl(path), method);
            request.downloadHandler = new DownloadHandlerBuffer();

            if (!string.IsNullOrWhiteSpace(bodyJson))
            {
                byte[] bodyBytes = Encoding.UTF8.GetBytes(bodyJson);
                request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                request.SetRequestHeader("Content-Type", "application/json");
            }

            request.SetRequestHeader("Accept", "application/json");

            if (verboseLogging)
            {
                Debug.Log($"[JongguApiSession] {method} {request.url}");
            }

            yield return request.SendWebRequest();

            string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            bool httpSucceeded = request.responseCode >= 200 && request.responseCode < 300;

            if (verboseLogging && !string.IsNullOrWhiteSpace(responseText))
            {
                Debug.Log($"[JongguApiSession] Response {request.responseCode}: {responseText}");
            }

            if (httpSucceeded)
            {
                onSuccess?.Invoke(responseText);
                yield break;
            }

            string errorCode = ExtractErrorCode(responseText);
            string errorMessage = TranslateApiMessage(errorCode, ExtractErrorMessage(responseText, request.error), "API 요청을 처리하지 못했습니다.");
            onFailure?.Invoke(errorCode, errorMessage);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!sessionReady || currentSnapshot == null)
            {
                return;
            }

            ApplySnapshot(currentSnapshot, syncScene: false);
        }

        private void DisableRemote(string message)
        {
            remoteDisabled = true;
            sessionReady = false;
            requestInFlight = false;
            ShowGuide(message);
        }

        private void ShowGuide(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            GameManager.Instance?.DayCycle?.ShowTemporaryGuide(message, 5f);
        }

        private string ResolveCurrentRegionCode()
        {
            if (!string.IsNullOrWhiteSpace(currentSnapshot?.currentRegion))
            {
                return currentSnapshot.currentRegion;
            }

            return SceneManager.GetActiveScene().name;
        }

        private static string ResolveResourceCode(ResourceData resource)
        {
            if (resource == null)
            {
                return string.Empty;
            }

            return !string.IsNullOrWhiteSpace(resource.ResourceId) ? resource.ResourceId : resource.name;
        }

        private static string ResolveRecipeCode(RecipeData recipe)
        {
            if (recipe == null)
            {
                return string.Empty;
            }

            return !string.IsNullOrWhiteSpace(recipe.RecipeId) ? recipe.RecipeId : recipe.name;
        }

        private static ToolType ResolveToolType(string toolCode)
        {
            return toolCode switch
            {
                "Rake" => ToolType.Rake,
                "FishingRod" => ToolType.FishingRod,
                "Sickle" => ToolType.Sickle,
                "Lantern" => ToolType.Lantern,
                _ => ToolType.None
            };
        }

        private static bool HasUnlockedTool(JongguApiPlayerSnapshot snapshot, string toolCode)
        {
            if (snapshot?.unlockedTools == null || string.IsNullOrWhiteSpace(toolCode))
            {
                return false;
            }

            foreach (string unlockedTool in snapshot.unlockedTools)
            {
                if (string.Equals(unlockedTool, toolCode, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetDefaultSpawnPointId(string regionCode)
        {
            return regionCode switch
            {
                "Hub" => "HubEntry",
                "Beach" => "BeachEntry",
                "DeepForest" => "ForestEntry",
                "AbandonedMine" => "MineEntry",
                "WindHill" => "WindHillEntry",
                _ => string.Empty
            };
        }

        private string BuildUrl(string path)
        {
            return $"{apiBaseUrl.TrimEnd('/')}{path}";
        }

        private static string BuildSettlementSummary(JongguApiDayRunSummary summary)
        {
            if (summary == null)
            {
                return string.Empty;
            }

            if (summary.serviceSkipped)
            {
                return "오늘 영업 결과\n- 장사를 건너뛰었습니다.";
            }

            StringBuilder builder = new();
            builder.AppendLine("오늘 영업 결과");
            if (!string.IsNullOrWhiteSpace(summary.selectedRecipeCode))
            {
                RecipeData recipe = GeneratedGameDataLocator.FindGeneratedRecipe(summary.selectedRecipeCode);
                string recipeLabel = recipe != null ? recipe.DisplayName : summary.selectedRecipeCode;
                builder.AppendLine($"- 선택 메뉴: {recipeLabel}");
            }

            builder.AppendLine($"- 판매 수량: {summary.soldCount}");
            builder.AppendLine($"- 획득 골드: +{summary.earnedGold}");
            builder.Append($"- 평판 변화: +{summary.earnedReputation}");
            return builder.ToString();
        }

        private string BuildServiceResultText(JongguApiServiceRunResponse response)
        {
            if (response == null)
            {
                return "오늘 영업 결과\n- 결과를 불러오지 못했습니다.";
            }

            if (response.skipped)
            {
                return "오늘 영업 결과\n- 장사를 건너뛰었습니다.";
            }

            string recipeLabel = response.recipeCode;
            RecipeData recipe = GeneratedGameDataLocator.FindGeneratedRecipe(response.recipeCode);
            if (recipe != null)
            {
                recipeLabel = recipe.DisplayName;
            }

            StringBuilder builder = new();
            builder.AppendLine("오늘 영업 결과");
            builder.AppendLine($"- 판매 메뉴: {recipeLabel} x{response.soldCount}");
            builder.AppendLine($"- 준비 가능 인분: {response.cookableCount}/{response.requestedCapacity}");
            builder.AppendLine($"- 획득 골드: +{response.earnedGold}");
            builder.Append($"- 평판 변화: +{response.earnedReputation}");
            return builder.ToString();
        }

        private static string ExtractErrorCode(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return string.Empty;
            }

            JongguApiErrorEnvelope envelope = JsonUtility.FromJson<JongguApiErrorEnvelope>(responseText);
            return envelope?.error != null ? envelope.error.code : string.Empty;
        }

        private static string ExtractErrorMessage(string responseText, string fallback)
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return string.IsNullOrWhiteSpace(fallback) ? "API 서버에 연결하지 못했습니다." : fallback;
            }

            JongguApiErrorEnvelope envelope = JsonUtility.FromJson<JongguApiErrorEnvelope>(responseText);
            if (envelope?.error != null && !string.IsNullOrWhiteSpace(envelope.error.message))
            {
                return envelope.error.message;
            }

            return string.IsNullOrWhiteSpace(fallback) ? "API 요청을 처리하지 못했습니다." : fallback;
        }

        private static string TranslateApiMessage(string errorCode, string rawMessage, string fallback)
        {
            return errorCode switch
            {
                "PLAYER_NOT_FOUND" => "저장된 플레이어 세션을 찾지 못했습니다.",
                "INVALID_PHASE" => "현재 단계에서는 이 행동을 수행할 수 없습니다.",
                "INVALID_REGION" => "현재 지역에서는 이 행동을 수행할 수 없습니다.",
                "PORTAL_NOT_ACCESSIBLE" => "현재 위치에서는 그 포탈을 이용할 수 없습니다.",
                "TOOL_REQUIRED" => "필요한 도구가 아직 준비되지 않았습니다.",
                "REPUTATION_REQUIRED" => "필요 평판이 부족합니다.",
                "INVENTORY_CAPACITY_EXCEEDED" => "인벤토리 칸이 부족합니다.",
                "INSUFFICIENT_RESOURCE" => "필요 재료가 부족합니다.",
                "STORAGE_ONLY_IN_HUB" => "창고는 Hub에서만 사용할 수 있습니다.",
                "RECIPE_NOT_SELECTED" => "영업 전에 메뉴를 먼저 골라 주세요.",
                "UPGRADE_NOT_AVAILABLE" => "아직 구매할 수 없는 업그레이드입니다.",
                "UPGRADE_ALREADY_PURCHASED" => "이미 구매한 업그레이드입니다.",
                "VALIDATION_ERROR" => "API 요청 형식이 올바르지 않습니다.",
                _ => string.IsNullOrWhiteSpace(rawMessage) ? fallback : rawMessage
            };
        }

        private static string LoadSavedPlayerId()
        {
            return PlayerPrefs.GetString(SavedPlayerIdKey, string.Empty);
        }

        private static void SavePlayerId(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return;
            }

            PlayerPrefs.SetString(SavedPlayerIdKey, playerId);
            PlayerPrefs.Save();
        }

        private static void ClearSavedPlayerId()
        {
            PlayerPrefs.DeleteKey(SavedPlayerIdKey);
            PlayerPrefs.Save();
        }
    }

    [Serializable]
    public sealed class JongguApiCreatePlayerRequest
    {
        public string displayName;
    }

    [Serializable]
    public sealed class JongguApiTravelRequest
    {
        public string portalCode;
    }

    [Serializable]
    public sealed class JongguApiGatherRequest
    {
        public string regionCode;
        public string resourceCode;
        public int quantity;
    }

    [Serializable]
    public sealed class JongguApiSelectRecipeRequest
    {
        public string recipeCode;
    }

    [Serializable]
    public sealed class JongguApiStorageTransferRequest
    {
        public string resourceCode;
        public int quantity;
    }

    [Serializable]
    public sealed class JongguApiErrorEnvelope
    {
        public bool success;
        public JongguApiError error;
    }

    [Serializable]
    public sealed class JongguApiPlayerSnapshotEnvelope
    {
        public bool success;
        public JongguApiPlayerSnapshot data;
        public JongguApiError error;
    }

    [Serializable]
    public sealed class JongguApiCreatePlayerEnvelope
    {
        public bool success;
        public JongguApiCreatePlayerResponse data;
        public JongguApiError error;
    }

    [Serializable]
    public sealed class JongguApiBootstrapEnvelope
    {
        public bool success;
        public JongguApiBootstrap data;
        public JongguApiError error;
    }

    [Serializable]
    public sealed class JongguApiGatherEnvelope
    {
        public bool success;
        public JongguApiGatherResponse data;
        public JongguApiError error;
    }

    [Serializable]
    public sealed class JongguApiServiceRunEnvelope
    {
        public bool success;
        public JongguApiServiceRunResponse data;
        public JongguApiError error;
    }

    [Serializable]
    public sealed class JongguApiUpgradePurchaseEnvelope
    {
        public bool success;
        public JongguApiUpgradePurchaseResponse data;
        public JongguApiError error;
    }

    [Serializable]
    public sealed class JongguApiError
    {
        public string code;
        public string message;
    }

    [Serializable]
    public sealed class JongguApiCreatePlayerResponse
    {
        public string playerId;
        public JongguApiPlayerSnapshot snapshot;
    }

    [Serializable]
    public sealed class JongguApiBootstrap
    {
        public List<JongguApiPortalRule> portalRules;
        public List<JongguApiUpgradeDefinition> upgrades;
    }

    [Serializable]
    public sealed class JongguApiPortalRule
    {
        public string code;
        public string fromRegionCode;
        public string toRegionCode;
    }

    [Serializable]
    public sealed class JongguApiUpgradeDefinition
    {
        public string code;
        public string upgradeType;
        public string toolCode;
        public int targetValue;
    }

    [Serializable]
    public sealed class JongguApiPlayerSnapshot
    {
        public string playerId;
        public int currentDay;
        public string currentPhase;
        public string currentRegion;
        public int gold;
        public int reputation;
        public int serviceCapacity;
        public int inventorySlotLimit;
        public string selectedRecipe;
        public List<JongguApiResourceAmount> inventoryResources;
        public List<JongguApiResourceAmount> storageResources;
        public List<string> unlockedTools;
        public JongguApiDayRunSummary currentDayRun;
        public JongguApiDayRunSummary lastSettlementSummary;
    }

    [Serializable]
    public sealed class JongguApiResourceAmount
    {
        public string resourceCode;
        public int quantity;
    }

    [Serializable]
    public sealed class JongguApiDayRunSummary
    {
        public int dayNumber;
        public string selectedRecipeCode;
        public bool serviceSkipped;
        public int soldCount;
        public int earnedGold;
        public int earnedReputation;
    }

    [Serializable]
    public sealed class JongguApiGatherResponse
    {
        public bool success;
        public string message;
        public string regionCode;
        public string resourceCode;
        public int quantityRequested;
        public int quantityGranted;
        public JongguApiPlayerSnapshot snapshot;
    }

    [Serializable]
    public sealed class JongguApiServiceRunResponse
    {
        public string recipeCode;
        public int requestedCapacity;
        public int cookableCount;
        public int soldCount;
        public int earnedGold;
        public int earnedReputation;
        public bool skipped;
        public JongguApiPlayerSnapshot snapshot;
    }

    [Serializable]
    public sealed class JongguApiUpgradePurchaseResponse
    {
        public string upgradeCode;
        public JongguApiPlayerSnapshot snapshot;
    }
}
