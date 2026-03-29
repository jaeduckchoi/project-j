using UnityEngine;

// 플레이어가 E 키로 실행할 수 있는 상호작용 대상 규약이다.
namespace Interaction
{
public interface IInteractable
{
    // UI 프롬프트와 거리 계산에 사용할 기준 위치다.
    string InteractionPrompt { get; }
    Transform InteractionTransform { get; }

    /*
     * 현재 상호작용이 허용되는지 검사한다.
     */
    bool CanInteract(GameObject interactor);

    /*
     * 실제 상호작용 결과를 실행한다.
     */
    void Interact(GameObject interactor);
}
}
