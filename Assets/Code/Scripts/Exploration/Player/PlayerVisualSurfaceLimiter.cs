using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Tilemaps;

namespace Code.Scripts.Exploration.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1000)]
    [MovedFrom(false, sourceNamespace: "Player", sourceAssembly: "Assembly-CSharp", sourceClassName: "PlayerVisualSurfaceLimiter")]
    public sealed class PlayerVisualSurfaceLimiter : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform walkableSurfaceRoot;
        [SerializeField] private SpriteRenderer baseSurfaceRenderer;

        private readonly List<Renderer> walkableSurfaces = new();
        private Rigidbody2D body;
        private Vector2 lastValidPosition;
        private bool hasLastValidPosition;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            RefreshWalkableSurfaces();
        }

        private void OnEnable()
        {
            RefreshWalkableSurfaces();
            Vector2 currentPosition = body != null ? body.position : transform.position;

            if (IsOnWalkableSurface(currentPosition))
            {
                SaveValidPosition(currentPosition);
                return;
            }

            if (TryFindNearestWalkablePosition(currentPosition, out Vector2 nearestPosition))
            {
                MoveTo(nearestPosition);
                SaveValidPosition(nearestPosition);
            }
        }

        private void LateUpdate()
        {
            if (walkableSurfaces.Count == 0)
            {
                RefreshWalkableSurfaces();
            }

            Vector2 currentPosition = body != null ? body.position : transform.position;
            if (IsOnWalkableSurface(currentPosition))
            {
                SaveValidPosition(currentPosition);
                return;
            }

            if (!hasLastValidPosition)
            {
                if (!TryFindNearestWalkablePosition(currentPosition, out Vector2 nearestPosition))
                {
                    return;
                }

                SaveValidPosition(nearestPosition);
            }

            MoveTo(lastValidPosition);
        }

        private void RefreshWalkableSurfaces()
        {
            walkableSurfaces.Clear();
            Transform surfaceRoot = walkableSurfaceRoot != null ? walkableSurfaceRoot : visualRoot;
            if (surfaceRoot == null)
            {
                return;
            }

            Renderer[] surfaceRenderers = surfaceRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer surfaceRenderer in surfaceRenderers)
            {
                if (surfaceRenderer == null || !surfaceRenderer.enabled || !surfaceRenderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!IsSupportedWalkableRenderer(surfaceRenderer))
                {
                    continue;
                }

                if (surfaceRenderer == baseSurfaceRenderer)
                {
                    continue;
                }

                walkableSurfaces.Add(surfaceRenderer);
            }
        }

        private bool IsOnWalkableSurface(Vector2 position)
        {
            foreach (Renderer surfaceRenderer in walkableSurfaces)
            {
                if (surfaceRenderer == null || !surfaceRenderer.enabled || !surfaceRenderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Bounds bounds = surfaceRenderer.bounds;
                if (position.x >= bounds.min.x
                    && position.x <= bounds.max.x
                    && position.y >= bounds.min.y
                    && position.y <= bounds.max.y)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryFindNearestWalkablePosition(Vector2 position, out Vector2 nearestPosition)
        {
            nearestPosition = position;
            float nearestDistance = float.PositiveInfinity;

            foreach (Renderer surfaceRenderer in walkableSurfaces)
            {
                if (surfaceRenderer == null || !surfaceRenderer.enabled || !surfaceRenderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Bounds bounds = surfaceRenderer.bounds;
                Vector2 candidate = new(
                    Mathf.Clamp(position.x, bounds.min.x, bounds.max.x),
                    Mathf.Clamp(position.y, bounds.min.y, bounds.max.y));
                float distance = (candidate - position).sqrMagnitude;
                if (distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = distance;
                nearestPosition = candidate;
            }

            return !float.IsPositiveInfinity(nearestDistance);
        }

        private static bool IsSupportedWalkableRenderer(Renderer renderer)
        {
            return renderer is SpriteRenderer || renderer is TilemapRenderer;
        }

        private void MoveTo(Vector2 position)
        {
            if (body != null)
            {
                body.position = position;
                body.linearVelocity = Vector2.zero;
            }

            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }

        private void SaveValidPosition(Vector2 position)
        {
            lastValidPosition = position;
            hasLastValidPosition = true;
        }
    }
}
