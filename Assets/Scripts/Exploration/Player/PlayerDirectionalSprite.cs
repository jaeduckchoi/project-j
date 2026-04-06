using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Shared;

// Player 네임스페이스
namespace Exploration.Player
{
    [RequireComponent(typeof(PlayerController))]
    [MovedFrom(false, sourceNamespace: "Player", sourceAssembly: "Assembly-CSharp", sourceClassName: "PlayerDirectionalSprite")]
    public sealed class PlayerDirectionalSprite : MonoBehaviour
    {
        private static PrototypeGeneratedAssetSettings AssetSettings => PrototypeGeneratedAssetSettings.GetCurrent();
        private static float PlayerSpritePixelsPerUnit => AssetSettings.PlayerSpritePixelsPerUnit;
        private static readonly Vector2 SpritePivot = new(0.5f, 0.08f);

        private static Vector3 DefaultPlayerVisualScale
        {
            get
            {
                float scale = AssetSettings.PlayerVisualScale;
                return new Vector3(scale, scale, 0f);
            }
        }


        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite frontSprite;
        [SerializeField] private Sprite backSprite;
        [SerializeField] private Sprite sideSprite;
        [SerializeField] private bool sideSpriteFacesLeft = true;
        [SerializeField, Min(0.0001f)] private float movementThreshold = 0.0001f;

        private PlayerController playerController;

        /// <summary>
        /// 외부에서 방향 스프라이트를 주입할 때는 null이 아닌 값만 갱신합니다.
        /// 이미 씬에 저장된 참조는 지우지 않고, 필요한 경우만 보정합니다.
        /// </summary>
        public void Configure(SpriteRenderer targetSpriteRenderer, Sprite front, Sprite back, Sprite side)
        {
            if (targetSpriteRenderer != null)
            {
                targetRenderer = targetSpriteRenderer;
            }

            if (front != null)
            {
                frontSprite = NormalizeSprite(front);
            }

            if (back != null)
            {
                backSprite = NormalizeSprite(back);
            }

            if (side != null)
            {
                sideSprite = NormalizeSprite(side);
            }

            InitializeVisuals();
        }

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            InitializeVisuals();
        }

        private void LateUpdate()
        {
            RefreshSprite();
        }

        /// <summary>
        /// 물리 루트와 비주얼 루트를 정리하고, 직렬화된 방향 스프라이트를 우선 사용합니다.
        /// </summary>
        private void InitializeVisuals()
        {
            targetRenderer = EnsureVisualRenderer();
            EnsureSpritesLoaded();
            RefreshSprite();
        }

        /// <summary>
        /// 현재 이동 방향에 따라 정면, 후면, 측면 스프라이트를 선택합니다.
        /// </summary>
        private void RefreshSprite()
        {
            if (playerController == null || targetRenderer == null)
            {
                return;
            }

            Vector2 facing = ResolveFacingDirection();
            if (Mathf.Abs(facing.x) > Mathf.Abs(facing.y))
            {
                targetRenderer.sprite = sideSprite != null ? sideSprite : frontSprite;
                targetRenderer.flipX = sideSpriteFacesLeft ? facing.x > 0f : facing.x < 0f;
            }
            else if (facing.y > 0f)
            {
                targetRenderer.sprite = backSprite != null ? backSprite : frontSprite;
                targetRenderer.flipX = false;
            }
            else
            {
                targetRenderer.sprite = frontSprite != null ? frontSprite : sideSprite;
                targetRenderer.flipX = false;
            }

            targetRenderer.color = Color.white;
        }

        /// <summary>
        /// 멈춰 있을 때는 마지막 이동 방향을 유지해 캐릭터가 갑자기 정면으로 돌아오지 않게 합니다.
        /// </summary>
        private Vector2 ResolveFacingDirection()
        {
            Vector2 direction = playerController.CurrentVelocity.sqrMagnitude > movementThreshold
                ? playerController.CurrentVelocity
                : playerController.LastMoveDirection;

            if (direction.sqrMagnitude <= movementThreshold)
            {
                direction = Vector2.down;
            }

            return direction;
        }

        /// <summary>
        /// 씬에 저장된 스프라이트가 없을 때만 Resources 폴백을 사용합니다.
        /// </summary>
        private void EnsureSpritesLoaded()
        {
            frontSprite = frontSprite != null ? NormalizeSprite(frontSprite) : LoadSprite(AssetSettings.PlayerFrontSpriteResourcePath);
            backSprite = backSprite != null ? NormalizeSprite(backSprite) : LoadSprite(AssetSettings.PlayerBackSpriteResourcePath);
            sideSprite = sideSprite != null ? NormalizeSprite(sideSprite) : LoadSprite(AssetSettings.PlayerSideSpriteResourcePath);

            if (targetRenderer != null && targetRenderer.sprite == null)
            {
                targetRenderer.sprite = frontSprite != null ? frontSprite : sideSprite;
            }
        }

        /// <summary>
        /// 비주얼은 PlayerVisual 자식에만 두고, 루트에 남아 있던 예전 SpriteRenderer는 비활성화합니다.
        /// </summary>
        private SpriteRenderer EnsureVisualRenderer()
        {
            Transform visualRoot = transform.Find("PlayerVisual");
            SpriteRenderer visualRenderer = visualRoot != null ? visualRoot.GetComponent<SpriteRenderer>() : null;
            SpriteRenderer rootRenderer = GetComponent<SpriteRenderer>();

            if (visualRoot == null)
            {
                GameObject visualObject = new("PlayerVisual");
                visualRoot = visualObject.transform;
                visualRoot.SetParent(transform, false);
                visualRoot.localScale = DefaultPlayerVisualScale;
            }

            visualRoot.localPosition = Vector3.zero;

            if (visualRenderer == null)
            {
                visualRenderer = visualRoot.gameObject.AddComponent<SpriteRenderer>();
            }

            if (rootRenderer != null && rootRenderer != visualRenderer)
            {
                CopyRendererState(rootRenderer, visualRenderer);
                rootRenderer.enabled = false;
                rootRenderer.sprite = null;
            }

            visualRenderer.color = Color.white;
            if (visualRenderer.sortingOrder == 0)
            {
                visualRenderer.sortingOrder = 12;
            }

            return visualRenderer;
        }

        /// <summary>
        /// 예전 루트 렌더러의 시각 상태를 비주얼 전용 렌더러로 옮깁니다.
        /// </summary>
        private static void CopyRendererState(SpriteRenderer source, SpriteRenderer target)
        {
            if (source == null || target == null)
            {
                return;
            }

            target.sprite = NormalizeSprite(source.sprite);
            target.color = Color.white;
            target.flipX = source.flipX;
            target.flipY = source.flipY;
            target.sortingLayerID = source.sortingLayerID;
            target.sortingOrder = source.sortingOrder;
            target.sharedMaterial = source.sharedMaterial;
        }

        /// <summary>
        /// PPU와 pivot이 이미 올바르면 그대로 쓰고, 아니라면 원본 rect를 유지한 채 다시 만듭니다.
        /// </summary>
        private static Sprite NormalizeSprite(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                return sprite;
            }

            ApplyTexturePresentation(sprite.texture);

            Vector2 normalizedPivot = new Vector2(
                sprite.pivot.x / sprite.rect.width,
                sprite.pivot.y / sprite.rect.height);
            if (Mathf.Abs(sprite.pixelsPerUnit - PlayerSpritePixelsPerUnit) < 0.01f
                && Approximately(normalizedPivot, SpritePivot))
            {
                return sprite;
            }

            return Sprite.Create(
                sprite.texture,
                sprite.rect,
                SpritePivot,
                PlayerSpritePixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
        }

        /// <summary>
        /// Resources에서는 Sprite를 우선으로 읽고, 실패할 때만 Texture2D 폴백을 사용합니다.
        /// </summary>
        private static Sprite LoadSprite(string resourcePath)
        {
            Sprite importedSprite = Resources.Load<Sprite>(resourcePath);
            if (importedSprite != null)
            {
                return NormalizeSprite(importedSprite);
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                ApplyTexturePresentation(texture);
                return Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    SpritePivot,
                    PlayerSpritePixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect);
            }

            return null;
        }

        /// <summary>
        /// 비픽셀 아트 원본이라 작은 크기로 줄여도 거칠지 않도록 필터와 랩 모드를 고정합니다.
        /// </summary>
        private static void ApplyTexturePresentation(Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
        }

        private static bool Approximately(Vector2 left, Vector2 right)
        {
            return Mathf.Abs(left.x - right.x) < 0.001f && Mathf.Abs(left.y - right.y) < 0.001f;
        }
    }
}
