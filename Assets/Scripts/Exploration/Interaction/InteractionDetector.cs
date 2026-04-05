using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// Interaction 네임스페이스
namespace Interaction
{
    /// <summary>
    /// 플레이어 주변의 상호작용 후보를 관리하고 가장 가까운 대상을 현재 대상으로 선택한다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [MovedFrom(false, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "InteractionDetector")]
    public class InteractionDetector : MonoBehaviour
    {
        private readonly List<IInteractable> _nearbyInteractables = new();
        private Collider2D _triggerCollider;

        public event Action<IInteractable> CurrentInteractableChanged;

        public IInteractable CurrentInteractable { get; private set; }

        /// <summary>
        /// 감지용 콜라이더를 트리거로 강제한다.
        /// </summary>
        private void Awake()
        {
            _triggerCollider = GetComponent<Collider2D>();
            if (_triggerCollider != null)
            {
                _triggerCollider.isTrigger = true;
            }
        }

        /// <summary>
        /// 매 프레임 누락된 대상을 정리하고 최적의 상호작용 대상을 다시 고른다.
        /// </summary>
        private void Update()
        {
            CleanupMissingInteractables();
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
            IInteractable interactable = FindInteractable(other);
            if (interactable == null || _nearbyInteractables.Contains(interactable))
            {
                return;
            }

            _nearbyInteractables.Add(interactable);
            RefreshCurrentInteractable();
        }

        /// <summary>
        /// 감지 범위를 벗어난 대상을 후보 목록에서 제거한다.
        /// </summary>
        private void OnTriggerExit2D(Collider2D other)
        {
            IInteractable interactable = FindInteractable(other);
            if (interactable == null)
            {
                return;
            }

            _nearbyInteractables.Remove(interactable);
            RefreshCurrentInteractable();
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
        /// 파괴되었거나 기준 위치를 잃은 대상을 목록에서 정리한다.
        /// </summary>
        private void CleanupMissingInteractables()
        {
            for (int index = _nearbyInteractables.Count - 1; index >= 0; index--)
            {
                IInteractable interactable = _nearbyInteractables[index];
                if (interactable == null || interactable.InteractionTransform == null)
                {
                    _nearbyInteractables.RemoveAt(index);
                }
            }
        }

        /// <summary>
        /// 프롬프트가 있는 후보 중 가장 가까운 대상을 현재 상호작용 대상으로 선택한다.
        /// </summary>
        private void RefreshCurrentInteractable()
        {
            IInteractable bestInteractable = null;
            float bestDistance = float.MaxValue;
            Vector3 detectorPosition = transform.position;

            foreach (IInteractable interactable in _nearbyInteractables)
            {
                if (interactable == null || interactable.InteractionTransform == null)
                {
                    continue;
                }

                // 빈 프롬프트는 UI에 노출하지 않기 위해 선택 대상에서 제외한다.
                if (string.IsNullOrWhiteSpace(interactable.InteractionPrompt))
                {
                    continue;
                }

                float sqrDistance = (interactable.InteractionTransform.position - detectorPosition).sqrMagnitude;
                if (sqrDistance >= bestDistance)
                {
                    continue;
                }

                bestDistance = sqrDistance;
                bestInteractable = interactable;
            }

            if (ReferenceEquals(CurrentInteractable, bestInteractable))
            {
                return;
            }

            CurrentInteractable = bestInteractable;
            CurrentInteractableChanged?.Invoke(CurrentInteractable);
        }
    }
}
