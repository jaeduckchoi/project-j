using System.Collections.Generic;
using Interaction;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [MovedFrom(false, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "PlayerController")]
    public class PlayerController : MonoBehaviour
    {
    [SerializeField, Min(0.1f)] private float moveSpeed = 4f;
    [SerializeField] private InteractionDetector interactionDetector;

    private Rigidbody2D _body;
    private Vector2 _moveInput;
    private readonly Dictionary<object, float> _movementMultiplierSources = new();
    private readonly Dictionary<object, Vector2> _externalVelocitySources = new();

#if ENABLE_INPUT_SYSTEM
    private InputAction _moveAction;
    private InputAction _interactAction;
#endif

    public Vector2 CurrentVelocity { get; private set; }
    public Vector2 LastMoveDirection { get; private set; } = Vector2.down;
    public InteractionDetector InteractionDetector => interactionDetector;
    public float EffectiveMoveSpeed => moveSpeed * GetMovementMultiplier();

    /*
     * 이동에 필요한 물리/상호작용 참조를 캐시하고, 방향 스프라이트를 보정합니다.
     */
    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();

        if (interactionDetector == null)
        {
            interactionDetector = GetComponentInChildren<InteractionDetector>();
        }

        EnsureDirectionalSprite();

#if ENABLE_INPUT_SYSTEM
        SetupInputActions();
#endif
    }

    /*
     * 동적 입력 액션을 씬 활성화 상태와 맞춰 함께 켭니다.
     */
    private void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        _moveAction?.Enable();
        _interactAction?.Enable();
#endif
    }

    /*
     * 씬 비활성화 중에는 외부 속도 보정과 입력 리스너를 함께 비웁니다.
     */
    private void OnDisable()
    {
        _movementMultiplierSources.Clear();
        _externalVelocitySources.Clear();

#if ENABLE_INPUT_SYSTEM
        _moveAction?.Disable();
        _interactAction?.Disable();
#endif
    }

    /*
     * 런타임에 만든 입력 액션은 파괴 시점에 함께 해제합니다.
     */
    private void OnDestroy()
    {
#if ENABLE_INPUT_SYSTEM
        _moveAction?.Dispose();
        _interactAction?.Dispose();
#endif
    }

    /*
     * 매 프레임 이동 입력과 상호작용 키 입력을 읽습니다.
     */
    private void Update()
    {
        _moveInput = ReadMoveInput();

        if (ReadInteractPressed() && interactionDetector != null)
        {
            interactionDetector.TryInteract(gameObject);
        }
    }

    /*
     * Rigidbody2D에는 입력 이동과 외부 힘 보정을 합친 최종 속도만 반영합니다.
     */
    private void FixedUpdate()
    {
        Vector2 desiredVelocity = _moveInput * EffectiveMoveSpeed + GetExternalVelocity();
        _body.linearVelocity = desiredVelocity;
        CurrentVelocity = _body.linearVelocity;

        if (CurrentVelocity.sqrMagnitude > 0.001f)
        {
            LastMoveDirection = CurrentVelocity.normalized;
        }
    }

    /*
     * 스폰 직후 위치 보정이 필요할 때 Transform과 Rigidbody를 함께 맞춥니다.
     */
    public void SetWorldPosition(Vector3 position)
    {
        transform.position = position;

        if (_body == null)
        {
            _body = GetComponent<Rigidbody2D>();
        }

        if (_body != null)
        {
            _body.position = new Vector2(position.x, position.y);
            _body.linearVelocity = Vector2.zero;
        }
    }

    /*
     * 늪, 강풍 같은 구역이 이동 속도 배율을 소스별로 등록하도록 열어 둡니다.
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
            _movementMultiplierSources.Remove(source);
            return;
        }

        _movementMultiplierSources[source] = clampedMultiplier;
    }

    /*
     * 특정 구역 보정이 끝나면 등록한 이동 배율을 제거합니다.
     */
    public void ClearMovementMultiplierSource(object source)
    {
        if (source == null)
        {
            return;
        }

        _movementMultiplierSources.Remove(source);
    }

    /*
     * 밀쳐짐이나 바람처럼 입력과 별개인 외부 속도를 소스별로 누적합니다.
     */
    public void SetExternalVelocitySource(object source, Vector2 velocity)
    {
        if (source == null)
        {
            return;
        }

        if (velocity.sqrMagnitude <= 0.0001f)
        {
            _externalVelocitySources.Remove(source);
            return;
        }

        _externalVelocitySources[source] = velocity;
    }

    /*
     * 외부 힘 효과가 끝나면 해당 소스의 속도 보정을 해제합니다.
     */
    public void ClearExternalVelocitySource(object source)
    {
        if (source == null)
        {
            return;
        }

        _externalVelocitySources.Remove(source);
    }

    /*
     * 새 입력 시스템과 레거시 입력 시스템을 모두 읽어 프로젝트 설정 차이를 흡수합니다.
     */
    private Vector2 ReadMoveInput()
    {
        Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (_moveAction != null)
        {
            input += _moveAction.ReadValue<Vector2>();
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
     * 상호작용 키도 두 입력 시스템에서 모두 읽어 동일하게 처리합니다.
     */
    private bool ReadInteractPressed()
    {
        bool pressed = false;

#if ENABLE_INPUT_SYSTEM
        if (_interactAction != null && _interactAction.WasPressedThisFrame())
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
     * 여러 구역 배율이 겹칠 수 있으므로 곱셈으로 합치고 과한 값은 제한합니다.
     */
    private float GetMovementMultiplier()
    {
        float multiplier = 1f;

        foreach (float sourceMultiplier in _movementMultiplierSources.Values)
        {
            multiplier *= sourceMultiplier;
        }

        return Mathf.Clamp(multiplier, 0f, 4f);
    }

    /*
     * 외부 속도는 벡터 합으로 합쳐 한 번에 Rigidbody에 적용합니다.
     */
    private Vector2 GetExternalVelocity()
    {
        Vector2 combinedVelocity = Vector2.zero;

        foreach (Vector2 velocity in _externalVelocitySources.Values)
        {
            combinedVelocity += velocity;
        }

        return combinedVelocity;
    }

    /*
     * PlayerVisual 자식이 있으면 그 렌더러를 우선 사용해 그림자나 보조 스프라이트를 피합니다.
     */
    private void EnsureDirectionalSprite()
    {
        SpriteRenderer renderer = transform.Find("PlayerVisual")?.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = GetComponent<SpriteRenderer>();
        }

        PlayerDirectionalSprite directionalSprite = GetComponent<PlayerDirectionalSprite>();
        if (directionalSprite == null)
        {
            directionalSprite = gameObject.AddComponent<PlayerDirectionalSprite>();
        }

        directionalSprite.Configure(renderer, null, null, null);
    }

#if ENABLE_INPUT_SYSTEM
    /*
     * 이동과 상호작용에 필요한 최소 InputAction만 코드에서 직접 만듭니다.
     */
    private void SetupInputActions()
    {
        if (_moveAction != null && _interactAction != null)
        {
            return;
        }

        _moveAction = new InputAction("Move", InputActionType.Value);
        _moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        _moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");

        _interactAction = new InputAction("Interact", InputActionType.Button);
        _interactAction.AddBinding("<Keyboard>/e");
        _interactAction.AddBinding("<Keyboard>/enter");
    }
#endif
    }
}