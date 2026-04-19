using System;
using System.Collections.Generic;
using Management.Storage;
using Restaurant.Kitchen;
using Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace UI
{
    public partial class UIManager
    {
        private readonly struct PopupStationBindingDef
        {
            public PopupStationBindingDef(string popupKey, Type componentType)
            {
                PopupKey = popupKey;
                ComponentType = componentType;
            }

            public string PopupKey { get; }
            public Type ComponentType { get; }
        }

        private static readonly PopupStationBindingDef[] PopupStationBindings =
        {
            new("Storage", typeof(StorageStation)),
            new("Refrigerator", typeof(RefrigeratorStation)),
            new("FrontCounter", typeof(FrontCounterStation))
        };

        private void Awake()
        {
            EnsureEventSystemExists();
            ApplyPopupInteractionBindings(SceneManager.GetActiveScene());
        }

        /// <summary>
        /// 씬 전환 뒤에도 UI 바인딩을 다시 연결할 수 있도록 sceneLoaded 콜백을 등록합니다.
        /// </summary>
        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            StorageStation.StoragePanelRequested += HandleStoragePanelRequested;
            RefrigeratorStation.PanelRequested += HandleRefrigeratorPanelRequested;
            FrontCounterStation.PanelRequested += HandleFrontCounterPanelRequested;
            RefrigeratorStation.RuntimePanelOpener = ShowRefrigeratorPanel;
        }

        /// <summary>
        /// 시작 시점에 현재 씬 기준 참조를 다시 찾고 전체 UI를 한 번 갱신합니다.
        /// </summary>
        private void Start()
        {
            ApplyPopupInteractionBindings(SceneManager.GetActiveScene());
            BindSceneReferences();
            ApplyTextPresentation();
            BindButtons();
            RefreshAll();
        }

        /// <summary>
        /// 매 프레임 상호작용 문구와 버튼 표시 상태를 최신 상태로 유지합니다.
        /// </summary>
        private void Update()
        {
            HandlePopupCloseInput();
            HandleStoragePopupInput();
            RefreshInteractionPrompt();
            RefreshButtonStates();
        }

        /// <summary>
        /// 등록했던 이벤트와 버튼 리스너를 정리해 중복 호출을 막습니다.
        /// </summary>
        private void OnDisable()
        {
            RestorePopupPauseIfNeeded();
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            StorageStation.StoragePanelRequested -= HandleStoragePanelRequested;
            RefrigeratorStation.PanelRequested -= HandleRefrigeratorPanelRequested;
            FrontCounterStation.PanelRequested -= HandleFrontCounterPanelRequested;
            if (ReferenceEquals(RefrigeratorStation.RuntimePanelOpener, (Action)ShowRefrigeratorPanel))
            {
                RefrigeratorStation.RuntimePanelOpener = null;
            }
            UnbindKitchenFlow();
            UnbindInventory();
            UnbindStorage();
            UnbindEconomy();
            UnbindTools();
            UnbindRestaurant();
            UnbindCustomerService();
            UnbindDayCycle();
            UnbindUpgradeManager();
            UnbindButtons();
        }

        /// <summary>
        /// 창고 상호작용이 UI 계층으로 전달되면 현재 씬 조건을 다시 확인한 뒤 창고 팝업을 엽니다.
        /// </summary>
        private void HandleStoragePanelRequested()
        {
            ShowStoragePanel();
        }

        /// <summary>
        /// 씬 전환 직후 새 씬의 플레이어와 매니저 참조를 다시 묶습니다.
        /// </summary>
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureEventSystemExists();
            ApplyPopupInteractionBindings(scene);
            activeHubPanel = HubPopupPanel.None;
            BindSceneReferences();
            ApplyTextPresentation();
            BindButtons();
            RefreshAll();
        }

        private static void ApplyPopupInteractionBindings(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            PopupInteractionBindingSettings settings = PopupInteractionBindingSettings.GetCurrent();
            if (settings == null)
            {
                return;
            }

            for (int i = 0; i < PopupStationBindings.Length; i++)
            {
                ApplyPopupInteractionBinding(scene, settings, PopupStationBindings[i]);
            }
        }

        private static void ApplyPopupInteractionBinding(Scene scene, PopupInteractionBindingSettings settings, PopupStationBindingDef def)
        {
            if (def.ComponentType == null
                || !settings.TryGetEntry(def.PopupKey, out PopupInteractionBindingEntry entry)
                || string.IsNullOrWhiteSpace(entry.SceneObjectPath))
            {
                return;
            }

            GameObject target = ResolvePopupBindingTarget(scene, entry.SceneObjectPath);
            if (target == null)
            {
                return;
            }

            if (target.GetComponent(def.ComponentType) == null)
            {
                target.AddComponent(def.ComponentType);
            }

            List<Component> components = FindPopupBindingComponents(scene, def.ComponentType);
            for (int i = 0; i < components.Count; i++)
            {
                Component component = components[i];
                if (component == null || component.gameObject == target)
                {
                    continue;
                }

                DestroyPopupBindingComponent(component);
            }
        }

        private static List<Component> FindPopupBindingComponents(Scene scene, Type componentType)
        {
            List<Component> result = new();
            if (componentType == null)
            {
                return result;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root != null)
                {
                    result.AddRange(root.GetComponentsInChildren(componentType, true));
                }
            }

            return result;
        }

        private static GameObject ResolvePopupBindingTarget(Scene scene, string sceneObjectPath)
        {
            if (string.IsNullOrWhiteSpace(sceneObjectPath))
            {
                return null;
            }

            string[] parts = sceneObjectPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null || parts.Length == 0 || !string.Equals(root.name, parts[0], StringComparison.Ordinal))
                {
                    continue;
                }

                Transform current = root.transform;
                for (int i = 1; i < parts.Length && current != null; i++)
                {
                    current = current.Find(parts[i]);
                }

                if (current != null)
                {
                    return current.gameObject;
                }
            }

            return null;
        }

        private static void DestroyPopupBindingComponent(Component component)
        {
            if (component == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(component);
                return;
            }

            UnityEngine.Object.DestroyImmediate(component);
        }

        private static void EnsureEventSystemExists()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            ConfigureInputSystemUiModule(inputModule);
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// Input System 기본 액션 생성 경로 대신 런타임 UI 액션 에셋을 직접 연결합니다.
        /// 이렇게 해야 InputActionReference 생성 예외 없이 버튼 입력을 안정적으로 받을 수 있습니다.
        /// </summary>
        private static void ConfigureInputSystemUiModule(InputSystemUIInputModule inputModule)
        {
            InputActionAsset asset = EnsureRuntimeUiInputActionsAsset();

            inputModule.actionsAsset = asset;
            inputModule.point = InputActionReference.Create(asset.FindAction("UI/Point", true));
            inputModule.leftClick = InputActionReference.Create(asset.FindAction("UI/LeftClick", true));
            inputModule.rightClick = InputActionReference.Create(asset.FindAction("UI/RightClick", true));
            inputModule.middleClick = InputActionReference.Create(asset.FindAction("UI/MiddleClick", true));
            inputModule.scrollWheel = InputActionReference.Create(asset.FindAction("UI/ScrollWheel", true));
            inputModule.move = InputActionReference.Create(asset.FindAction("UI/Move", true));
            inputModule.submit = InputActionReference.Create(asset.FindAction("UI/Submit", true));
            inputModule.cancel = InputActionReference.Create(asset.FindAction("UI/Cancel", true));
            inputModule.trackedDevicePosition = InputActionReference.Create(asset.FindAction("UI/TrackedDevicePosition", true));
            inputModule.trackedDeviceOrientation = InputActionReference.Create(asset.FindAction("UI/TrackedDeviceOrientation", true));
        }

        private static InputActionAsset EnsureRuntimeUiInputActionsAsset()
        {
            if (_runtimeUiActionsAsset != null)
            {
                return _runtimeUiActionsAsset;
            }

            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "RuntimeUiInputActions";
            asset.hideFlags = HideFlags.HideAndDontSave;

            InputActionMap uiMap = new("UI");
            asset.AddActionMap(uiMap);

            InputAction pointAction = uiMap.AddAction("Point", InputActionType.PassThrough);
            pointAction.expectedControlType = "Vector2";
            pointAction.AddBinding("<Mouse>/position");
            pointAction.AddBinding("<Pen>/position");
            pointAction.AddBinding("<Touchscreen>/primaryTouch/position");

            InputAction leftClickAction = uiMap.AddAction("LeftClick", InputActionType.PassThrough);
            leftClickAction.expectedControlType = "Button";
            leftClickAction.AddBinding("<Mouse>/leftButton");
            leftClickAction.AddBinding("<Pen>/tip");
            leftClickAction.AddBinding("<Touchscreen>/primaryTouch/press");

            InputAction rightClickAction = uiMap.AddAction("RightClick", InputActionType.PassThrough);
            rightClickAction.expectedControlType = "Button";
            rightClickAction.AddBinding("<Mouse>/rightButton");

            InputAction middleClickAction = uiMap.AddAction("MiddleClick", InputActionType.PassThrough);
            middleClickAction.expectedControlType = "Button";
            middleClickAction.AddBinding("<Mouse>/middleButton");

            InputAction scrollWheelAction = uiMap.AddAction("ScrollWheel", InputActionType.PassThrough);
            scrollWheelAction.expectedControlType = "Vector2";
            scrollWheelAction.AddBinding("<Mouse>/scroll");

            InputAction moveAction = uiMap.AddAction("Move", InputActionType.PassThrough);
            moveAction.expectedControlType = "Vector2";
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick");
            moveAction.AddBinding("<Gamepad>/dpad");

            InputAction submitAction = uiMap.AddAction("Submit", InputActionType.Button);
            submitAction.expectedControlType = "Button";
            submitAction.AddBinding("<Keyboard>/enter");
            submitAction.AddBinding("<Keyboard>/numpadEnter");
            submitAction.AddBinding("<Keyboard>/space");
            submitAction.AddBinding("<Gamepad>/buttonSouth");

            InputAction cancelAction = uiMap.AddAction("Cancel", InputActionType.Button);
            cancelAction.expectedControlType = "Button";
            cancelAction.AddBinding("<Keyboard>/escape");
            cancelAction.AddBinding("<Gamepad>/buttonEast");

            InputAction trackedPositionAction = uiMap.AddAction("TrackedDevicePosition", InputActionType.PassThrough);
            trackedPositionAction.expectedControlType = "Vector3";
            trackedPositionAction.AddBinding("<XRController>/devicePosition");

            InputAction trackedOrientationAction = uiMap.AddAction("TrackedDeviceOrientation", InputActionType.PassThrough);
            trackedOrientationAction.expectedControlType = "Quaternion";
            trackedOrientationAction.AddBinding("<XRController>/deviceRotation");

            _runtimeUiActionsAsset = asset;
            return _runtimeUiActionsAsset;
        }
#endif

        private void HandleInventoryChanged()
        {
            RefreshInventoryText();
            RefreshStorageText();
            RefreshSelectedRecipeText(cachedRestaurant != null ? cachedRestaurant.SelectedRecipe : null);
            RefreshUpgradeText();
        }

        private void HandleEconomyChanged(int _)
        {
            RefreshEconomyText();
            RefreshUpgradeText();
        }

        private void HandleToolsChanged()
        {
            RefreshUpgradeText();
        }

        /// <summary>
        /// 해상도 차이로 HUD가 무너지지 않도록 캔버스 스케일 기준을 고정합니다.
        /// </summary>
        private void ApplyCanvasScaleSettings()
        {
            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                return;
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
        }
    }
}
