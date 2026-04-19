using System.Collections.Generic;
using Exploration.Interaction;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Player 네임스페이스
namespace Exploration.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [MovedFrom(false, sourceNamespace: "Player", sourceAssembly: "Assembly-CSharp", sourceClassName: "PlayerController")]
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

        /// <summary>
        /// 이동에 필요한 물리/상호작용 참조를 캐시하고, 방향 스프라이트를 보정합니다.
        /// </summary>
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();

            if (interactionDetector == null)
            {
                interactionDetector = GetComponentInChildren<InteractionDetector>();
            }

            EnsureDirectionalSprite();

#if ENABLE_INPUT_SYSTEM
            SetupInputActions();
#endif
        }

        /// <summary>
        /// 동적 입력 액션을 씬 활성화 상태와 맞춰 함께 켭니다.
        /// </summary>
        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            moveAction?.Enable();
            interactAction?.Enable();
#endif
        }

        /// <summary>
        /// 씬 비활성화 중에는 외부 속도 보정과 입력 리스너를 함께 비웁니다.
        /// </summary>
        private void OnDisable()
        {
            movementMultiplierSources.Clear();
            externalVelocitySources.Clear();

#if ENABLE_INPUT_SYSTEM
            moveAction?.Disable();
            interactAction?.Disable();
#endif
        }

        /// <summary>
        /// 런타임에 만든 입력 액션은 파괴 시점에 함께 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
#if ENABLE_INPUT_SYSTEM
            moveAction?.Dispose();
            interactAction?.Dispose();
#endif
        }

        /// <summary>
        /// 매 프레임 이동 입력과 상호작용 키 입력을 읽습니다.
        /// </summary>
        private void Update()
        {
            moveInput = ReadMoveInput();

            if (ReadInteractPressed() && interactionDetector != null)
            {
                interactionDetector.TryInteract(gameObject);
            }
        }

        /// <summary>
        /// Rigidbody2D에는 입력 이동과 외부 힘 보정을 합친 최종 속도만 반영합니다.
        /// </summary>
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

        /// <summary>
        /// 스폰 직후 위치 보정이 필요할 때 Transform과 Rigidbody를 함께 맞춥니다.
        /// </summary>
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

        /// <summary>
        /// 늪, 강풍 같은 구역이 이동 속도 배율을 소스별로 등록하도록 열어 둡니다.
        /// </summary>
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

        /// <summary>
        /// 특정 구역 보정이 끝나면 등록한 이동 배율을 제거합니다.
        /// </summary>
        public void ClearMovementMultiplierSource(object source)
        {
            if (source == null)
            {
                return;
            }

            movementMultiplierSources.Remove(source);
        }

        /// <summary>
        /// 밀쳐짐이나 바람처럼 입력과 별개인 외부 속도를 소스별로 누적합니다.
        /// </summary>
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

        /// <summary>
        /// 외부 힘 효과가 끝나면 해당 소스의 속도 보정을 해제합니다.
        /// </summary>
        public void ClearExternalVelocitySource(object source)
        {
            if (source == null)
            {
                return;
            }

            externalVelocitySources.Remove(source);
        }

        /// <summary>
        /// 새 입력 시스템과 레거시 입력 시스템을 모두 읽어 프로젝트 설정 차이를 흡수합니다.
        /// </summary>
        private Vector2 ReadMoveInput()
        {
            Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
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

        /// <summary>
        /// 상호작용 키도 두 입력 시스템에서 모두 읽어 동일하게 처리합니다.
        /// </summary>
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

        /// <summary>
        /// 여러 구역 배율이 겹칠 수 있으므로 곱셈으로 합치고 과한 값은 제한합니다.
        /// </summary>
        private float GetMovementMultiplier()
        {
            float multiplier = 1f;

            foreach (float sourceMultiplier in movementMultiplierSources.Values)
            {
                multiplier *= sourceMultiplier;
            }

            return Mathf.Clamp(multiplier, 0f, 4f);
        }

        /// <summary>
        /// 외부 속도는 벡터 합으로 합쳐 한 번에 Rigidbody에 적용합니다.
        /// </summary>
        private Vector2 GetExternalVelocity()
        {
            Vector2 combinedVelocity = Vector2.zero;

            foreach (Vector2 velocity in externalVelocitySources.Values)
            {
                combinedVelocity += velocity;
            }

            return combinedVelocity;
        }

        /// <summary>
        /// PlayerVisual 자식이 있으면 그 렌더러를 우선 사용해 그림자나 보조 스프라이트를 피합니다.
        /// </summary>
        private void EnsureDirectionalSprite()
        {
            SpriteRenderer spriteRenderer = transform.Find("PlayerVisual")?.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            PlayerDirectionalSprite directionalSprite = GetComponent<PlayerDirectionalSprite>();
            if (directionalSprite == null)
            {
                directionalSprite = gameObject.AddComponent<PlayerDirectionalSprite>();
            }

            directionalSprite.Configure(spriteRenderer, null, null, null);
        }

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// 이동과 상호작용에 필요한 최소 InputAction만 코드에서 직접 만듭니다.
        /// </summary>
        private void SetupInputActions()
        {
            if (moveAction != null && interactAction != null)
            {
                return;
            }

            moveAction = new InputAction("Move");
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
}
