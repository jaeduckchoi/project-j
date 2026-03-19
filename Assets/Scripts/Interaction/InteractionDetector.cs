using System;
using System.Collections.Generic;
using UnityEngine;

// 플레이어 주변의 상호작용 후보를 관리하고 가장 가까운 대상을 현재 대상으로 선택한다.
[RequireComponent(typeof(Collider2D))]
public class InteractionDetector : MonoBehaviour
{
    private readonly List<IInteractable> nearbyInteractables = new();
    private Collider2D triggerCollider;

    public event Action<IInteractable> CurrentInteractableChanged;

    public IInteractable CurrentInteractable { get; private set; }

    /*
     * 감지용 콜라이더를 트리거로 강제한다.
     */
    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    /*
     * 매 프레임 누락된 대상을 정리하고 최적의 상호작용 대상을 다시 고른다.
     */
    private void Update()
    {
        CleanupMissingInteractables();
        RefreshCurrentInteractable();
    }

    /*
     * 현재 선택된 대상을 실제로 실행하고 선택 상태를 갱신한다.
     */
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

    /*
     * 감지 범위에 들어온 상호작용 대상을 후보 목록에 추가한다.
     */
    private void OnTriggerEnter2D(Collider2D other)
    {
        IInteractable interactable = FindInteractable(other);
        if (interactable == null || nearbyInteractables.Contains(interactable))
        {
            return;
        }

        nearbyInteractables.Add(interactable);
        RefreshCurrentInteractable();
    }

    /*
     * 감지 범위를 벗어난 대상을 후보 목록에서 제거한다.
     */
    private void OnTriggerExit2D(Collider2D other)
    {
        IInteractable interactable = FindInteractable(other);
        if (interactable == null)
        {
            return;
        }

        nearbyInteractables.Remove(interactable);
        RefreshCurrentInteractable();
    }

    /*
     * 충돌체 부모 계층에서 상호작용 인터페이스를 구현한 컴포넌트를 찾는다.
     */
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

    /*
     * 파괴되었거나 기준 위치를 잃은 대상을 목록에서 정리한다.
     */
    private void CleanupMissingInteractables()
    {
        for (int index = nearbyInteractables.Count - 1; index >= 0; index--)
        {
            IInteractable interactable = nearbyInteractables[index];
            if (interactable == null || interactable.InteractionTransform == null)
            {
                nearbyInteractables.RemoveAt(index);
            }
        }
    }

    /*
     * 프롬프트가 있는 후보 중 가장 가까운 대상을 현재 상호작용 대상으로 선택한다.
     */
    private void RefreshCurrentInteractable()
    {
        IInteractable bestInteractable = null;
        float bestDistance = float.MaxValue;
        Vector3 detectorPosition = transform.position;

        foreach (IInteractable interactable in nearbyInteractables)
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
