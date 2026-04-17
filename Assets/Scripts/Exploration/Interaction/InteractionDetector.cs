using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// Interaction 네임스페이스
namespace Exploration.Interaction
{
    /// <summary>
    /// 플레이어 주변의 상호작용 후보를 관리하고 가장 가까운 대상을 현재 대상으로 선택한다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [MovedFrom(false, sourceNamespace: "Interaction", sourceAssembly: "Assembly-CSharp", sourceClassName: "InteractionDetector")]
    public class InteractionDetector : MonoBehaviour
    {
        private readonly List<IInteractable> nearbyInteractables = new();
        private readonly Dictionary<IInteractable, float> nearbyInteractableDistances = new();
        private readonly Collider2D[] overlapBuffer = new Collider2D[32];
        private ContactFilter2D overlapFilter;
        private Collider2D triggerCollider;

        public event Action<IInteractable> CurrentInteractableChanged;

        public IInteractable CurrentInteractable { get; private set; }

        /// <summary>
        /// 감지용 콜라이더를 트리거로 강제한다.
        /// </summary>
        private void Awake()
        {
            overlapFilter = ContactFilter2D.noFilter;

            triggerCollider = GetComponent<Collider2D>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        /// <summary>
        /// 매 프레임 누락된 대상을 정리하고 최적의 상호작용 대상을 다시 고른다.
        /// </summary>
        private void Update()
        {
            RefreshNearbyOverlapCandidates();
            RefreshCurrentInteractable();
        }

        /// <summary>
        /// 현재 선택된 대상을 실제로 실행하고 선택 상태를 갱신한다.
        /// </summary>
        public bool TryInteract(GameObject interactor)
        {
            if (CurrentInteractable == null || !CurrentInteractable.CanInteract(interactor))
            {
                return false;
            }

            CurrentInteractable.Interact(interactor);
            RefreshCurrentInteractable();
            return true;
        }

        /// <summary>
        /// 감지 범위에 들어온 상호작용 대상을 후보 목록에 추가한다.
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            RefreshNearbyOverlapCandidates();
            RefreshCurrentInteractable();
        }

        /// <summary>
        /// 감지 범위를 벗어난 대상을 후보 목록에서 제거한다.
        /// </summary>
        private void OnTriggerExit2D(Collider2D other)
        {
            RefreshNearbyOverlapCandidates();
            RefreshCurrentInteractable();
        }

        private void RefreshNearbyOverlapCandidates()
        {
            nearbyInteractables.Clear();
            nearbyInteractableDistances.Clear();

            if (triggerCollider == null)
            {
                return;
            }

            int overlapCount = triggerCollider.Overlap(overlapFilter, overlapBuffer);
            for (int i = 0; i < overlapCount; i++)
            {
                Collider2D overlapCollider = overlapBuffer[i];
                IInteractable interactable = FindInteractable(overlapCollider);
                if (interactable == null || interactable.InteractionTransform == null)
                {
                    continue;
                }

                float candidateDistance = GetColliderDistance(overlapCollider);
                if (nearbyInteractableDistances.TryGetValue(interactable, out float currentDistance)
                    && candidateDistance >= currentDistance)
                {
                    continue;
                }

                nearbyInteractableDistances[interactable] = candidateDistance;
                if (!nearbyInteractables.Contains(interactable))
                {
                    nearbyInteractables.Add(interactable);
                }
            }
        }

        /// <summary>
        /// 충돌체 부모 계층에서 상호작용 인터페이스를 구현한 컴포넌트를 찾는다.
        /// </summary>
        private static IInteractable FindInteractable(Collider2D targetCollider)
        {
            if (targetCollider == null)
            {
                return null;
            }

            MonoBehaviour[] behaviours = targetCollider.GetComponentsInParent<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IInteractable interactable)
                {
                    return interactable;
                }
            }

            return null;
        }

        /// <summary>
        /// 감지 범위 콜라이더와 후보 콜라이더 사이의 실제 거리를 구한다.
        /// 겹친 상태면 음수가 되므로, 값이 더 작을수록 현재 플레이어와 더 직접 맞닿은 대상이다.
        /// </summary>
        private float GetColliderDistance(Collider2D targetCollider)
        {
            if (triggerCollider == null || targetCollider == null)
            {
                return float.MaxValue;
            }

            return triggerCollider.Distance(targetCollider).distance;
        }

        /// <summary>
        /// 프롬프트가 있는 후보 중 실제 콜라이더 거리가 가장 가까운 대상을 현재 상호작용 대상으로 선택한다.
        /// </summary>
        private void RefreshCurrentInteractable()
        {
            Vector3 detectorPosition = transform.position;
            IInteractable bestInteractable = null;
            float bestDistance = float.MaxValue;
            int bestPriority = int.MinValue;
            float bestCenterDistance = float.MaxValue;

            if (TryGetSelectionMetrics(CurrentInteractable, detectorPosition, out float currentDistance, out int currentPriority, out float currentCenterDistance))
            {
                bestInteractable = CurrentInteractable;
                bestDistance = currentDistance;
                bestPriority = currentPriority;
                bestCenterDistance = currentCenterDistance;
            }

            foreach (IInteractable interactable in nearbyInteractables)
            {
                if (ReferenceEquals(interactable, bestInteractable))
                {
                    continue;
                }

                if (!TryGetSelectionMetrics(interactable, detectorPosition, out float candidateDistance, out int candidatePriority, out float candidateCenterDistance))
                {
                    continue;
                }

                if (!IsBetterCandidate(candidateDistance, candidatePriority, candidateCenterDistance, bestDistance, bestPriority, bestCenterDistance))
                {
                    continue;
                }

                bestDistance = candidateDistance;
                bestPriority = candidatePriority;
                bestCenterDistance = candidateCenterDistance;
                bestInteractable = interactable;
            }

            if (ReferenceEquals(CurrentInteractable, bestInteractable))
            {
                return;
            }

            CurrentInteractable = bestInteractable;
            CurrentInteractableChanged?.Invoke(CurrentInteractable);
        }

        private bool TryGetSelectionMetrics(IInteractable interactable, Vector3 detectorPosition, out float colliderDistance, out int selectionPriority, out float centerSqrDistance)
        {
            colliderDistance = float.MaxValue;
            selectionPriority = int.MinValue;
            centerSqrDistance = float.MaxValue;

            if (interactable == null || interactable.InteractionTransform == null)
            {
                return false;
            }

            // 빈 프롬프트는 UI에 노출하지 않기 위해 선택 대상에서 제외한다.
            if (string.IsNullOrWhiteSpace(interactable.InteractionPrompt))
            {
                return false;
            }

            if (!nearbyInteractableDistances.TryGetValue(interactable, out colliderDistance))
            {
                return false;
            }

            selectionPriority = GetInteractionPriority(interactable);
            centerSqrDistance = (interactable.InteractionTransform.position - detectorPosition).sqrMagnitude;
            return true;
        }

        private static bool IsBetterCandidate(
            float candidateDistance,
            int candidatePriority,
            float candidateCenterDistance,
            float bestDistance,
            int bestPriority,
            float bestCenterDistance)
        {
            const float distanceEpsilon = 0.0001f;

            if (candidateDistance < bestDistance - distanceEpsilon)
            {
                return true;
            }

            if (Mathf.Abs(candidateDistance - bestDistance) > distanceEpsilon)
            {
                return false;
            }

            if (candidatePriority != bestPriority)
            {
                return candidatePriority > bestPriority;
            }

            return candidateCenterDistance < bestCenterDistance - distanceEpsilon;
        }

        private static int GetInteractionPriority(IInteractable interactable)
        {
            return interactable switch
            {
                Restaurant.Kitchen.RefrigeratorStation => 3,
                Restaurant.Kitchen.FrontCounterStation => 2,
                Restaurant.ServiceCounterStation => 0,
                _ => 1
            };
        }
    }
}
