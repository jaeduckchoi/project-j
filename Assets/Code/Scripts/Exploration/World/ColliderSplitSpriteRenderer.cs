using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Code.Scripts.Exploration.World
{
    /// <summary>
    /// 미리 슬라이스된 FrontCounter 스프라이트 조각을 서로 다른 정렬값의 렌더러로 배치합니다.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ColliderSplitSpriteRenderer : MonoBehaviour
    {
        private const string GeneratedPartPrefix = "ColliderSplit";
        private const string SplitSpriteResourcePath = "Generated/Sprites/Hub/front_counter";
        private const string ColliderAreaSpriteName = "FrontCounterOutsideArea";
        private const string OutsideTopSpriteName = "FrontCounterOutsideTop";
        private const string OutsideBottomSpriteName = "FrontCounterOutsideBottom";
        private const string OutsideLeftSpriteName = "FrontCounterOutsideLeft";
        private const string OutsideRightSpriteName = "FrontCounterOutsideRight";
        private const string KitchenCounterAreaObjectName = "KitchenCounterArea";
        private const string KitchenCounterTopObjectName = "KitchenCounterTop";
        private const string KitchenCounterBottomObjectName = "KitchenCounterBottom";
        private const string KitchenCounterLeftObjectName = "KitchenCounterLeft";
        private const string KitchenCounterRightObjectName = "KitchenCounterRight";

        [SerializeField] private SpriteRenderer sourceRenderer;
        [SerializeField] private BoxCollider2D splitCollider;
        [SerializeField] private int colliderPartSortingOrder = 11;
        [SerializeField] private int outsidePartSortingOrder = 13;
        [SerializeField] private Sprite colliderAreaSprite;
        [SerializeField] private Sprite outsideTopSprite;
        [SerializeField] private Sprite outsideBottomSprite;
        [SerializeField] private Sprite outsideLeftSprite;
        [SerializeField] private Sprite outsideRightSprite;
        [SerializeField] private bool showEditorPreview = true;
        [SerializeField] private bool showGeneratedPartsInHierarchy = true;
        [SerializeField] private bool keepGeneratedPartsInScene = true;

        private bool hasCapturedSourceRendererState;
        private bool sourceRendererForceRenderingOff;
        private bool hasLastSplitStateHash;
        private int lastSplitStateHash;
        private int lastGeneratedPartCount;

#if UNITY_EDITOR
        private bool rebuildScheduled;
        private bool scheduledForceRebuild;
#endif

        /// <summary>
        /// 마지막 재빌드에서 활성화된 분할 조각 수입니다.
        /// </summary>
        public int LastGeneratedPartCount => lastGeneratedPartCount;

        /// <summary>
        /// 마지막으로 반영된 분할 입력 상태 해시입니다.
        /// </summary>
        public int LastSplitStateHash => lastSplitStateHash;

        /// <summary>
        /// 현재 활성 분할 프리뷰나 런타임 조각이 있는지 나타냅니다.
        /// </summary>
        public bool HasActivePreview => lastGeneratedPartCount > 0;

        private void Reset()
        {
            AutoBindReferences();
            AutoBindSplitSprites(persistBindings: true);
            RequestRebuild(force: true);
        }

        private void OnValidate()
        {
            AutoBindReferences();
            AutoBindSplitSprites(persistBindings: false);
            RequestRebuild(force: true);
        }

        private void OnEnable()
        {
            AutoBindReferences();
            RequestRebuild(force: true);
        }

        private void LateUpdate()
        {
            RebuildIfStateChanged(force: false);
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            CancelScheduledRebuild();
            if (!Application.isPlaying)
            {
                RestoreSourceRendererVisibility();
                return;
            }
#endif
            ClearSplitPreviewInternal(rememberCurrentState: false, markDirty: false);
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            CancelScheduledRebuild();
            if (!Application.isPlaying)
            {
                RestoreSourceRendererVisibility();
                return;
            }
#endif
            ClearSplitPreviewInternal(rememberCurrentState: false, markDirty: false);
        }

        /// <summary>
        /// 현재 설정으로 분할 프리뷰를 즉시 다시 만듭니다.
        /// </summary>
        [ContextMenu("Rebuild Split Preview")]
        public void RebuildSplitPreview()
        {
            AutoBindSplitSprites(persistBindings: true);
            RebuildIfStateChanged(force: true, markDirty: true);
        }

        /// <summary>
        /// 현재 생성된 분할 프리뷰를 정리하고 원본 렌더러 표시 상태를 복구합니다.
        /// </summary>
        [ContextMenu("Clear Split Preview")]
        public void ClearSplitPreview()
        {
            ClearSplitPreviewInternal(rememberCurrentState: true, markDirty: true);
        }

        private void AutoBindReferences()
        {
            if (sourceRenderer == null)
            {
                sourceRenderer = GetComponent<SpriteRenderer>();
            }

            if (splitCollider == null)
            {
                splitCollider = GetComponentInChildren<BoxCollider2D>();
            }
        }

        private void AutoBindSplitSprites(bool persistBindings)
        {
            if (!persistBindings)
            {
                return;
            }

            Sprite[] availableSprites = LoadAvailableSplitSprites();
            if (availableSprites == null || availableSprites.Length == 0)
            {
                return;
            }

            bool changed = false;
            BindSpriteIfMissing(ref colliderAreaSprite, ColliderAreaSpriteName, availableSprites, ref changed);
            BindSpriteIfMissing(ref outsideTopSprite, OutsideTopSpriteName, availableSprites, ref changed);
            BindSpriteIfMissing(ref outsideBottomSprite, OutsideBottomSpriteName, availableSprites, ref changed);
            BindSpriteIfMissing(ref outsideLeftSprite, OutsideLeftSpriteName, availableSprites, ref changed);
            BindSpriteIfMissing(ref outsideRightSprite, OutsideRightSpriteName, availableSprites, ref changed);

            if (changed)
            {
                MarkSceneObjectDirty(gameObject, markDirty: true);
            }
        }

        private Sprite[] LoadAvailableSplitSprites()
        {
#if UNITY_EDITOR
            if (sourceRenderer != null && sourceRenderer.sprite != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(sourceRenderer.sprite);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                    List<Sprite> sprites = new();
                    for (int i = 0; i < assets.Length; i++)
                    {
                        if (assets[i] is Sprite sprite)
                        {
                            sprites.Add(sprite);
                        }
                    }

                    if (sprites.Count > 0)
                    {
                        return sprites.ToArray();
                    }
                }
            }
#endif

            return Resources.LoadAll<Sprite>(SplitSpriteResourcePath);
        }

        private static void BindSpriteIfMissing(
            ref Sprite target,
            string spriteName,
            IReadOnlyList<Sprite> availableSprites,
            ref bool changed)
        {
            if (target != null)
            {
                return;
            }

            Sprite foundSprite = FindSpriteByName(spriteName, availableSprites);
            if (foundSprite == null)
            {
                return;
            }

            target = foundSprite;
            changed = true;
        }

        private static Sprite FindSpriteByName(string spriteName, IReadOnlyList<Sprite> availableSprites)
        {
            for (int i = 0; i < availableSprites.Count; i++)
            {
                Sprite sprite = availableSprites[i];
                if (sprite != null && string.Equals(sprite.name, spriteName, StringComparison.Ordinal))
                {
                    return sprite;
                }
            }

            return null;
        }

        /// <summary>
        /// OnValidate 중 직접 생성/삭제하지 않도록 에디터에서는 delayCall로 재빌드합니다.
        /// </summary>
        private void RequestRebuild(bool force)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ScheduleEditorRebuild(force);
                return;
            }
#endif

            RebuildIfStateChanged(force, markDirty: false);
        }

#if UNITY_EDITOR
        private void ScheduleEditorRebuild(bool force)
        {
            scheduledForceRebuild |= force;
            if (rebuildScheduled)
            {
                return;
            }

            rebuildScheduled = true;
            EditorApplication.delayCall += RunScheduledRebuild;
        }

        private void RunScheduledRebuild()
        {
            EditorApplication.delayCall -= RunScheduledRebuild;
            rebuildScheduled = false;

            if (this == null)
            {
                scheduledForceRebuild = false;
                return;
            }

            bool force = scheduledForceRebuild;
            scheduledForceRebuild = false;
            RebuildIfStateChanged(force, markDirty: false);
        }

        private void CancelScheduledRebuild()
        {
            if (!rebuildScheduled)
            {
                scheduledForceRebuild = false;
                return;
            }

            EditorApplication.delayCall -= RunScheduledRebuild;
            rebuildScheduled = false;
            scheduledForceRebuild = false;
        }
#endif

        /// <summary>
        /// Renderer와 스프라이트 참조가 달라졌을 때만 분할 조각을 갱신합니다.
        /// </summary>
        private void RebuildIfStateChanged(bool force, bool markDirty = false)
        {
            AutoBindReferences();

            if (!CanRenderSplit())
            {
                ClearSplitPreviewInternal(rememberCurrentState: false, markDirty);
                return;
            }

            int splitStateHash = BuildSplitStateHash();
            if (!force && hasLastSplitStateHash && splitStateHash == lastSplitStateHash)
            {
                if (lastGeneratedPartCount > 0)
                {
                    ApplySourceRendererVisibility(hidden: true);
                }

                return;
            }

            if (!RebuildSplitRenderers(markDirty))
            {
                ClearSplitPreviewInternal(rememberCurrentState: false, markDirty);
                return;
            }

            lastSplitStateHash = splitStateHash;
            hasLastSplitStateHash = true;
        }

        private bool CanRenderSplit()
        {
            if (!Application.isPlaying && !showEditorPreview)
            {
                return false;
            }

            return sourceRenderer != null
                && sourceRenderer.sprite != null
                && sourceRenderer.enabled
                && gameObject.activeInHierarchy
                && HasAnySplitSprite();
        }

        private bool HasAnySplitSprite()
        {
            if (colliderAreaSprite != null
                || outsideTopSprite != null
                || outsideBottomSprite != null
                || outsideLeftSprite != null
                || outsideRightSprite != null)
            {
                return true;
            }

            Sprite[] availableSprites = LoadAvailableSplitSprites();
            return FindSpriteByName(ColliderAreaSpriteName, availableSprites) != null
                || FindSpriteByName(OutsideTopSpriteName, availableSprites) != null
                || FindSpriteByName(OutsideBottomSpriteName, availableSprites) != null
                || FindSpriteByName(OutsideLeftSpriteName, availableSprites) != null
                || FindSpriteByName(OutsideRightSpriteName, availableSprites) != null;
        }

        private bool RebuildSplitRenderers(bool markDirty)
        {
            lastGeneratedPartCount = 0;
            Sprite sourceSprite = sourceRenderer.sprite;
            Sprite[] availableSprites = LoadAvailableSplitSprites();
            HashSet<string> activePieceObjectNames = new();
            ApplyPiece(
                "ColliderArea",
                ResolveSplitSprite(colliderAreaSprite, ColliderAreaSpriteName, availableSprites),
                colliderPartSortingOrder,
                sourceSprite,
                activePieceObjectNames,
                markDirty);
            ApplyPiece(
                "OutsideTop",
                ResolveSplitSprite(outsideTopSprite, OutsideTopSpriteName, availableSprites),
                outsidePartSortingOrder,
                sourceSprite,
                activePieceObjectNames,
                markDirty);
            ApplyPiece(
                "OutsideBottom",
                ResolveSplitSprite(outsideBottomSprite, OutsideBottomSpriteName, availableSprites),
                outsidePartSortingOrder,
                sourceSprite,
                activePieceObjectNames,
                markDirty);
            ApplyPiece(
                "OutsideLeft",
                ResolveSplitSprite(outsideLeftSprite, OutsideLeftSpriteName, availableSprites),
                outsidePartSortingOrder,
                sourceSprite,
                activePieceObjectNames,
                markDirty);
            ApplyPiece(
                "OutsideRight",
                ResolveSplitSprite(outsideRightSprite, OutsideRightSpriteName, availableSprites),
                outsidePartSortingOrder,
                sourceSprite,
                activePieceObjectNames,
                markDirty);
            DisableUnusedManagedPartRenderers(activePieceObjectNames, markDirty);

            if (lastGeneratedPartCount <= 0)
            {
                return false;
            }

            ApplySourceRendererVisibility(hidden: true);
            return true;
        }

        private static Sprite ResolveSplitSprite(
            Sprite configuredSprite,
            string spriteName,
            IReadOnlyList<Sprite> availableSprites)
        {
            return configuredSprite != null
                ? configuredSprite
                : FindSpriteByName(spriteName, availableSprites);
        }

        private void ApplySourceRendererVisibility(bool hidden)
        {
            if (sourceRenderer == null)
            {
                return;
            }

            if (!hasCapturedSourceRendererState)
            {
                sourceRendererForceRenderingOff = sourceRenderer.forceRenderingOff;
                hasCapturedSourceRendererState = true;
            }

            sourceRenderer.forceRenderingOff = hidden || sourceRendererForceRenderingOff;
        }

        private void RestoreSourceRendererVisibility()
        {
            if (sourceRenderer == null || !hasCapturedSourceRendererState)
            {
                hasCapturedSourceRendererState = false;
                return;
            }

            sourceRenderer.forceRenderingOff = sourceRendererForceRenderingOff;
            hasCapturedSourceRendererState = false;
        }

        private void ApplyPiece(
            string pieceName,
            Sprite pieceSprite,
            int sortingOrder,
            Sprite sourceSprite,
            ISet<string> activePieceObjectNames,
            bool markDirty)
        {
            if (pieceSprite == null)
            {
                return;
            }

            GameObject pieceObject = GetOrCreatePieceObject(pieceName, markDirty, out bool createdObject);
            if (pieceObject == null)
            {
                return;
            }

            bool changed = createdObject;
            activePieceObjectNames.Add(pieceObject.name);

            HideFlags targetHideFlags = GetGeneratedObjectHideFlags();
            if (pieceObject.hideFlags != targetHideFlags)
            {
                pieceObject.hideFlags = targetHideFlags;
                changed = true;
            }

            Transform pieceTransform = pieceObject.transform;
            if (pieceTransform.parent != sourceRenderer.transform)
            {
                pieceTransform.SetParent(sourceRenderer.transform, false);
                changed = true;
            }

            changed |= SetLocalPositionIfChanged(pieceTransform, GetPieceLocalPosition(pieceSprite.rect, sourceSprite));
            changed |= SetLocalRotationIfChanged(pieceTransform, Quaternion.identity);
            changed |= SetLocalScaleIfChanged(pieceTransform, Vector3.one);

            bool hasAuthoredRenderer = pieceObject.TryGetComponent(out SpriteRenderer pieceRenderer);
            if (!hasAuthoredRenderer)
            {
                pieceRenderer = pieceObject.AddComponent<SpriteRenderer>();
                changed = true;
            }

            int effectiveSortingOrder = hasAuthoredRenderer && !createdObject
                ? pieceRenderer.sortingOrder
                : sortingOrder;
            changed |= CopyRendererSettingsIfChanged(sourceRenderer, pieceRenderer, effectiveSortingOrder);
            if (pieceRenderer.sprite != pieceSprite)
            {
                pieceRenderer.sprite = pieceSprite;
                changed = true;
            }

            if (!pieceRenderer.enabled)
            {
                pieceRenderer.enabled = true;
                changed = true;
            }

            lastGeneratedPartCount++;
            if (changed)
            {
                MarkSceneObjectDirty(pieceObject, markDirty);
            }
        }

        private GameObject GetOrCreatePieceObject(string pieceName, bool markDirty, out bool createdObject)
        {
            string objectName = GetManagedPieceObjectName(pieceName);
            Transform existingChild = FindDirectGeneratedChild(objectName);
            if (existingChild != null)
            {
                createdObject = false;
                return existingChild.gameObject;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying && keepGeneratedPartsInScene && !markDirty)
            {
                createdObject = false;
                return null;
            }
#endif

            GameObject pieceObject = new(objectName);
            createdObject = true;
#if UNITY_EDITOR
            if (!Application.isPlaying && keepGeneratedPartsInScene)
            {
                Undo.RegisterCreatedObjectUndo(pieceObject, $"Create {objectName}");
            }
#endif
            return pieceObject;
        }

        private static string GetManagedPieceObjectName(string pieceName)
        {
            return pieceName switch
            {
                "ColliderArea" => KitchenCounterAreaObjectName,
                "OutsideTop" => KitchenCounterTopObjectName,
                "OutsideBottom" => KitchenCounterBottomObjectName,
                "OutsideLeft" => KitchenCounterLeftObjectName,
                "OutsideRight" => KitchenCounterRightObjectName,
                _ => $"{GeneratedPartPrefix}{pieceName}"
            };
        }

        private Transform FindDirectGeneratedChild(string objectName)
        {
            if (sourceRenderer == null)
            {
                return null;
            }

            Transform sourceTransform = sourceRenderer.transform;
            for (int i = 0; i < sourceTransform.childCount; i++)
            {
                Transform child = sourceTransform.GetChild(i);
                if (child != null && child.name == objectName)
                {
                    return child;
                }
            }

            return null;
        }

        private HideFlags GetGeneratedObjectHideFlags()
        {
            if (Application.isPlaying)
            {
                return HideFlags.None;
            }

            if (keepGeneratedPartsInScene)
            {
                return showGeneratedPartsInHierarchy ? HideFlags.None : HideFlags.HideInHierarchy;
            }

            return showGeneratedPartsInHierarchy
                ? HideFlags.DontSaveInEditor
                : HideFlags.DontSaveInEditor | HideFlags.HideInHierarchy;
        }

        private static bool CopyRendererSettingsIfChanged(SpriteRenderer source, SpriteRenderer target, int sortingOrder)
        {
            bool changed = false;
            if (target.color != source.color)
            {
                target.color = source.color;
                changed = true;
            }

            if (target.flipX != source.flipX)
            {
                target.flipX = source.flipX;
                changed = true;
            }

            if (target.flipY != source.flipY)
            {
                target.flipY = source.flipY;
                changed = true;
            }

            if (target.maskInteraction != source.maskInteraction)
            {
                target.maskInteraction = source.maskInteraction;
                changed = true;
            }

            if (target.sharedMaterial != source.sharedMaterial)
            {
                target.sharedMaterial = source.sharedMaterial;
                changed = true;
            }

            if (target.sortingLayerID != source.sortingLayerID)
            {
                target.sortingLayerID = source.sortingLayerID;
                changed = true;
            }

            if (target.sortingOrder != sortingOrder)
            {
                target.sortingOrder = sortingOrder;
                changed = true;
            }

            if (target.spriteSortPoint != source.spriteSortPoint)
            {
                target.spriteSortPoint = source.spriteSortPoint;
                changed = true;
            }

            return changed;
        }

        private static bool SetLocalPositionIfChanged(Transform target, Vector3 value)
        {
            if ((target.localPosition - value).sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            target.localPosition = value;
            return true;
        }

        private static bool SetLocalRotationIfChanged(Transform target, Quaternion value)
        {
            if (Quaternion.Dot(target.localRotation, value) > 0.999999f)
            {
                return false;
            }

            target.localRotation = value;
            return true;
        }

        private static bool SetLocalScaleIfChanged(Transform target, Vector3 value)
        {
            if ((target.localScale - value).sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            target.localScale = value;
            return true;
        }

        private static Vector3 GetPieceLocalPosition(Rect pixelRect, Sprite sourceSprite)
        {
            Vector2 pivot = sourceSprite.pivot;
            float pixelsPerUnit = sourceSprite.pixelsPerUnit;
            Rect sourceRect = sourceSprite.rect;
            float localX = ((pixelRect.center.x - sourceRect.xMin) - pivot.x) / pixelsPerUnit;
            float localY = ((pixelRect.center.y - sourceRect.yMin) - pivot.y) / pixelsPerUnit;
            return new Vector3(localX, localY, 0f);
        }

        /// <summary>
        /// 씬에 저장된 조각 오브젝트와 스프라이트 참조는 남기고 SpriteRenderer만 비활성화합니다.
        /// </summary>
        private void DisableUnusedManagedPartRenderers(ISet<string> activePieceObjectNames, bool markDirty)
        {
            if (sourceRenderer == null)
            {
                return;
            }

            Transform sourceTransform = sourceRenderer.transform;
            for (int i = sourceTransform.childCount - 1; i >= 0; i--)
            {
                Transform child = sourceTransform.GetChild(i);
                if (child == null
                    || !IsGeneratedPartName(child.name)
                    || activePieceObjectNames.Contains(child.name))
                {
                    continue;
                }

                bool changed = false;
                HideFlags targetHideFlags = GetGeneratedObjectHideFlags();
                if (child.gameObject.hideFlags != targetHideFlags)
                {
                    child.gameObject.hideFlags = targetHideFlags;
                    changed = true;
                }

                SpriteRenderer pieceRenderer = child.GetComponent<SpriteRenderer>();
                if (pieceRenderer != null && pieceRenderer.enabled)
                {
                    pieceRenderer.enabled = false;
                    changed = true;
                }

                if (changed)
                {
                    MarkSceneObjectDirty(child.gameObject, markDirty);
                }
            }
        }

        /// <summary>
        /// 씬에 저장된 조각 오브젝트와 스프라이트 참조는 남기고 SpriteRenderer만 비활성화합니다.
        /// </summary>
        private void DisableGeneratedPartRenderers(bool markDirty)
        {
            lastGeneratedPartCount = 0;
            if (sourceRenderer == null)
            {
                return;
            }

            Transform sourceTransform = sourceRenderer.transform;
            for (int i = sourceTransform.childCount - 1; i >= 0; i--)
            {
                Transform child = sourceTransform.GetChild(i);
                if (child == null || !IsGeneratedPartName(child.name))
                {
                    continue;
                }

                bool changed = false;
                HideFlags targetHideFlags = GetGeneratedObjectHideFlags();
                if (child.gameObject.hideFlags != targetHideFlags)
                {
                    child.gameObject.hideFlags = targetHideFlags;
                    changed = true;
                }

                SpriteRenderer pieceRenderer = child.GetComponent<SpriteRenderer>();
                if (pieceRenderer != null && pieceRenderer.enabled)
                {
                    pieceRenderer.enabled = false;
                    changed = true;
                }

                if (!changed)
                {
                    continue;
                }

                MarkSceneObjectDirty(child.gameObject, markDirty);
            }
        }

        private void ClearSplitPreviewInternal(bool rememberCurrentState, bool markDirty)
        {
            DisableGeneratedPartRenderers(markDirty);
            RestoreSourceRendererVisibility();

            if (rememberCurrentState && CanRenderSplit())
            {
                lastSplitStateHash = BuildSplitStateHash();
                hasLastSplitStateHash = true;
                return;
            }

            hasLastSplitStateHash = false;
            lastSplitStateHash = 0;
        }

        /// <summary>
        /// 분할에 영향을 주는 입력을 하나의 변경 감지값으로 묶습니다.
        /// </summary>
        private int BuildSplitStateHash()
        {
            unchecked
            {
                int hash = 17;
                AddObjectHash(ref hash, sourceRenderer.sprite);
                AddObjectHash(ref hash, sourceRenderer.sharedMaterial);
                AddObjectHash(ref hash, splitCollider);
                AddObjectHash(ref hash, colliderAreaSprite);
                AddObjectHash(ref hash, outsideTopSprite);
                AddObjectHash(ref hash, outsideBottomSprite);
                AddObjectHash(ref hash, outsideLeftSprite);
                AddObjectHash(ref hash, outsideRightSprite);
                AddVector2Hash(ref hash, sourceRenderer.size);
                AddColorHash(ref hash, sourceRenderer.color);
                AddIntHash(ref hash, sourceRenderer.enabled ? 1 : 0);
                AddIntHash(ref hash, sourceRenderer.flipX ? 1 : 0);
                AddIntHash(ref hash, sourceRenderer.flipY ? 1 : 0);
                AddIntHash(ref hash, (int)sourceRenderer.maskInteraction);
                AddIntHash(ref hash, sourceRenderer.sortingLayerID);
                AddIntHash(ref hash, sourceRenderer.sortingOrder);
                AddIntHash(ref hash, (int)sourceRenderer.spriteSortPoint);
                AddIntHash(ref hash, colliderPartSortingOrder);
                AddIntHash(ref hash, outsidePartSortingOrder);
                AddIntHash(ref hash, showGeneratedPartsInHierarchy ? 1 : 0);
                AddIntHash(ref hash, keepGeneratedPartsInScene ? 1 : 0);
                return hash;
            }
        }

        private static bool IsGeneratedPartName(string objectName)
        {
            return objectName.StartsWith(GeneratedPartPrefix, StringComparison.Ordinal)
                || string.Equals(objectName, KitchenCounterAreaObjectName, StringComparison.Ordinal)
                || string.Equals(objectName, KitchenCounterTopObjectName, StringComparison.Ordinal)
                || string.Equals(objectName, KitchenCounterBottomObjectName, StringComparison.Ordinal)
                || string.Equals(objectName, KitchenCounterLeftObjectName, StringComparison.Ordinal)
                || string.Equals(objectName, KitchenCounterRightObjectName, StringComparison.Ordinal);
        }

        private static void AddObjectHash(ref int hash, UnityEngine.Object targetObject)
        {
            AddIntHash(ref hash, targetObject == null ? 0 : targetObject.GetInstanceID());
        }

        private static void AddVector2Hash(ref int hash, Vector2 value)
        {
            AddFloatHash(ref hash, value.x);
            AddFloatHash(ref hash, value.y);
        }

        private static void AddColorHash(ref int hash, Color value)
        {
            AddFloatHash(ref hash, value.r);
            AddFloatHash(ref hash, value.g);
            AddFloatHash(ref hash, value.b);
            AddFloatHash(ref hash, value.a);
        }

        private static void AddFloatHash(ref int hash, float value)
        {
            AddIntHash(ref hash, value.GetHashCode());
        }

        private static void AddIntHash(ref int hash, int value)
        {
            unchecked
            {
                hash = (hash * 31) + value;
            }
        }

        private static void MarkSceneObjectDirty(GameObject targetObject, bool markDirty)
        {
#if UNITY_EDITOR
            if (!markDirty || Application.isPlaying || targetObject == null)
            {
                return;
            }

            EditorUtility.SetDirty(targetObject);
            if (targetObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(targetObject.scene);
            }
#endif
        }
    }
}
