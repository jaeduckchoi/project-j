using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using System.Collections.Generic;

// 탑다운 플레이어 이동과 상호작용 입력을 처리한다.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float moveSpeed = 4f;
    [SerializeField] private InteractionDetector interactionDetector;

    private Rigidbody2D body;
    private Vector2 moveInput;
    private readonly Dictionary<object, float> movementMultiplierSources = new();
    private readonly Dictionary<object, Vector2> externalVelocitySources = new();

#if ENABLE_INPUT_SYSTEM
    private InputAction moveAction;
    private InputAction interactAction;
#endif

    public Vector2 CurrentVelocity { get; private set; }
    public Vector2 LastMoveDirection { get; private set; } = Vector2.down;
    public InteractionDetector InteractionDetector => interactionDetector;
    public float EffectiveMoveSpeed => moveSpeed * GetMovementMultiplier();

    /*
     * 이동과 상호작용에 필요한 컴포넌트와 입력 액션을 준비합니다.
     */
    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();

        if (interactionDetector == null)
        {
            interactionDetector = GetComponentInChildren<InteractionDetector>();
        }

#if ENABLE_INPUT_SYSTEM
        SetupInputActions();
#endif
    }

    /*
     * 입력 액션을 활성화합니다.
     */
    private void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        moveAction?.Enable();
        interactAction?.Enable();
#endif
    }

    /*
     * 비활성화 시 외부 이동 보정 상태와 입력 액션을 정리합니다.
     */
    private void OnDisable()
    {
        movementMultiplierSources.Clear();
        externalVelocitySources.Clear();

#if ENABLE_INPUT_SYSTEM
        moveAction?.Disable();
        interactAction?.Disable();
#endif
    }

    /*
     * 동적으로 만든 입력 액션 리소스를 해제합니다.
     */
    private void OnDestroy()
    {
#if ENABLE_INPUT_SYSTEM
        moveAction?.Dispose();
        interactAction?.Dispose();
#endif
    }

    /*
     * 프레임마다 이동 입력을 읽고 상호작용 키 입력을 처리합니다.
     */
    private void Update()
    {
        moveInput = ReadMoveInput();

        if (ReadInteractPressed() && interactionDetector != null)
        {
            interactionDetector.TryInteract(gameObject);
        }
    }

    /*
     * 입력 속도와 외부 힘을 합쳐 Rigidbody2D 속도를 적용합니다.
     */
    private void FixedUpdate()
    {
        Vector2 desiredVelocity = moveInput * EffectiveMoveSpeed + GetExternalVelocity();
        body.linearVelocity = desiredVelocity;
        CurrentVelocity = body.linearVelocity;

        if (CurrentVelocity.sqrMagnitude > 0.001f)
        {
            LastMoveDirection = CurrentVelocity.normalized;
        }
    }

    /*
     * 스폰 포인트 이동 시 플레이어 좌표와 물리 상태를 함께 초기화합니다.
     */
    public void SetWorldPosition(Vector3 position)
    {
        transform.position = position;

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (body != null)
        {
            body.position = new Vector2(position.x, position.y);
            body.linearVelocity = Vector2.zero;
        }
    }

    /*
     * 특정 출처가 주는 이동 배율을 등록하거나 갱신합니다.
     */
    public void SetMovementMultiplierSource(object source, float multiplier)
    {
        if (source == null)
        {
            return;
        }

        float clampedMultiplier = Mathf.Max(0f, multiplier);
        if (Mathf.Approximately(clampedMultiplier, 1f))
        {
            movementMultiplierSources.Remove(source);
            return;
        }

        movementMultiplierSources[source] = clampedMultiplier;
    }

    /*
     * 특정 출처가 주던 이동 배율을 제거합니다.
     */
    public void ClearMovementMultiplierSource(object source)
    {
        if (source == null)
        {
            return;
        }

        movementMultiplierSources.Remove(source);
    }

    /*
     * 바람이나 강제 이동 같은 외부 속도 출처를 등록합니다.
     */
    public void SetExternalVelocitySource(object source, Vector2 velocity)
    {
        if (source == null)
        {
            return;
        }

        if (velocity.sqrMagnitude <= 0.0001f)
        {
            externalVelocitySources.Remove(source);
            return;
        }

        externalVelocitySources[source] = velocity;
    }

    /*
     * 특정 외부 속도 출처를 제거합니다.
     */
    public void ClearExternalVelocitySource(object source)
    {
        if (source == null)
        {
            return;
        }

        externalVelocitySources.Remove(source);
    }

    /*
     * 새 입력 시스템과 레거시 입력 시스템을 모두 고려해 이동 입력을 합칩니다.
     */
    private Vector2 ReadMoveInput()
    {
        Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        // 새 입력 시스템이 켜져 있으면 Action 과 키보드 상태를 함께 읽습니다.
        if (moveAction != null)
        {
            input += moveAction.ReadValue<Vector2>();
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                input.x -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                input.x += 1f;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                input.y -= 1f;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                input.y += 1f;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        // 레거시 입력 시스템도 동시에 지원해 프로젝트 설정 차이를 흡수합니다.
        Vector2 legacyInput = Vector2.zero;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            legacyInput.x -= 1f;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            legacyInput.x += 1f;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            legacyInput.y -= 1f;
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            legacyInput.y += 1f;
        }

        input += legacyInput;
#endif

        input.x = Mathf.Clamp(input.x, -1f, 1f);
        input.y = Mathf.Clamp(input.y, -1f, 1f);
        return input.normalized;
    }

    /*
     * 상호작용 키가 이번 프레임에 눌렸는지 확인합니다.
     */
    private bool ReadInteractPressed()
    {
        bool pressed = false;

#if ENABLE_INPUT_SYSTEM
        if (interactAction != null && interactAction.WasPressedThisFrame())
        {
            pressed = true;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
        {
            pressed = true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return);
#endif

        return pressed;
    }

    /*
     * 현재 등록된 모든 이동 배율 출처를 곱해 최종 배율을 계산합니다.
     */
    private float GetMovementMultiplier()
    {
        float multiplier = 1f;

        foreach (float sourceMultiplier in movementMultiplierSources.Values)
        {
            multiplier *= sourceMultiplier;
        }

        return Mathf.Clamp(multiplier, 0f, 4f);
    }

    /*
     * 현재 등록된 모든 외부 속도 출처를 합쳐 최종 속도를 계산합니다.
     */
    private Vector2 GetExternalVelocity()
    {
        Vector2 combinedVelocity = Vector2.zero;

        foreach (Vector2 velocity in externalVelocitySources.Values)
        {
            combinedVelocity += velocity;
        }

        return combinedVelocity;
    }

#if ENABLE_INPUT_SYSTEM
    /*
     * 키보드 기반 이동 / 상호작용 InputAction 을 동적으로 구성합니다.
     */
    private void SetupInputActions()
    {
        if (moveAction != null && interactAction != null)
        {
            return;
        }

        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");

        interactAction = new InputAction("Interact", InputActionType.Button);
        interactAction.AddBinding("<Keyboard>/e");
        interactAction.AddBinding("<Keyboard>/enter");
    }
#endif
}
