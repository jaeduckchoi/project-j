using System;
using CoreLoop.Core;
using Management.Inventory;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace UI
{
    public partial class UIManager
    {
        public void ShowStoragePanel()
        {
            if (!IsHubScene() || !IsPlayerNearStorageStation())
            {
                return;
            }

            activeHubPanel = HubPopupPanel.Storage;
            RefreshStorageText();
            ApplyMenuPanelState();
            RefreshStoragePanelVisibility();
        }

        private void ToggleHubPanel(HubPopupPanel targetPanel)
        {
            if (!IsHubScene())
            {
                return;
            }

            activeHubPanel = activeHubPanel == targetPanel ? HubPopupPanel.None : targetPanel;
            ApplyMenuPanelState();
        }

        private void HandlePopupCloseInput()
        {
            if (activeHubPanel == HubPopupPanel.None || !ReadPopupClosePressed())
            {
                return;
            }

            CloseActiveHubPanel();
        }

        private void CloseActiveHubPanel()
        {
            if (activeHubPanel == HubPopupPanel.None)
            {
                return;
            }

            activeHubPanel = HubPopupPanel.None;
            ApplyMenuPanelState();
            RefreshStoragePanelVisibility();
        }

        private static bool ReadPopupClosePressed()
        {
            bool pressed = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                pressed = true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= Input.GetKeyDown(KeyCode.Escape);
#endif

            return pressed;
        }

        private void HandleStoragePopupInput()
        {
            if (activeHubPanel != HubPopupPanel.Storage || cachedStorage == null || GameManager.Instance == null || GameManager.Instance.Inventory == null)
            {
                return;
            }

            InventoryManager inventory = GameManager.Instance.Inventory;
            bool changed = false;

            if (ReadPopupActionPressed(KeyCode.Q, keyboard => keyboard.qKey))
            {
                changed |= cachedStorage.CycleInventorySelection(inventory);
                GameManager.Instance?.DayCycle?.ShowHintOnce(
                    "first_storage_select_deposit",
                    "왼쪽 목록에서 맡길 재료를 고르고 맡기기 동작으로 창고에 보관할 수 있습니다.");
            }

            if (ReadPopupActionPressed(KeyCode.W, keyboard => keyboard.wKey))
            {
                if (GameManager.Instance != null
                    && GameManager.Instance.RemoteSession != null
                    && GameManager.Instance.RemoteSession.TryStoreSelected(cachedStorage, inventory))
                {
                    changed = true;
                }
                else
                {
                    changed |= cachedStorage.StoreSelectedFromInventory(inventory) > 0;
                }
            }

            if (ReadPopupActionPressed(KeyCode.A, keyboard => keyboard.aKey))
            {
                changed |= cachedStorage.CycleStoredSelection();
                GameManager.Instance?.DayCycle?.ShowHintOnce(
                    "first_storage_select_withdraw",
                    "보관 목록에서 꺼낼 재료를 고른 뒤 꺼내기 동작으로 가방으로 되돌릴 수 있습니다.");
            }

            if (ReadPopupActionPressed(KeyCode.S, keyboard => keyboard.sKey))
            {
                if (GameManager.Instance != null
                    && GameManager.Instance.RemoteSession != null
                    && GameManager.Instance.RemoteSession.TryWithdrawSelected(cachedStorage, inventory))
                {
                    changed = true;
                }
                else
                {
                    changed |= cachedStorage.WithdrawSelectedToInventory(inventory) > 0;
                }
            }

            if (changed)
            {
                RefreshAll();
            }
            else
            {
                RefreshHubPopupContent();
            }
        }

        private static bool ReadPopupActionPressed(KeyCode legacyKey, Func<Keyboard, KeyControl> keySelector)
        {
            bool pressed = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                KeyControl key = keySelector(keyboard);
                if (key != null && key.wasPressedThisFrame)
                {
                    pressed = true;
                }
            }
#endif

#if !ENABLE_LEGACY_INPUT_MANAGER
            _ = legacyKey;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= Input.GetKeyDown(legacyKey);
#endif

            return pressed;
        }
    }
}
