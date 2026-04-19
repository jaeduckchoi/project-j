using Code.Scripts.CoreLoop.Core;
using UnityEngine;

namespace Code.Scripts.Exploration.Interaction
{
    /// <summary>
    /// 이동이나 채집 없이 안내 문구만 노출하는 월드 상호작용 지점입니다.
    /// </summary>
    public sealed class GuideOnlyInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string promptLabel = "확인";
        [SerializeField, TextArea] private string guideText = "아직 준비 중입니다.";
        [SerializeField, Min(0.1f)] private float guideDuration = 5f;

        public string InteractionPrompt => string.IsNullOrWhiteSpace(promptLabel) ? string.Empty : $"[E] {promptLabel}";
        public Transform InteractionTransform => transform;

        public void Configure(string label, string guide, float duration = 5f)
        {
            if (!string.IsNullOrWhiteSpace(label))
            {
                promptLabel = label;
            }

            if (!string.IsNullOrWhiteSpace(guide))
            {
                guideText = guide;
            }

            guideDuration = Mathf.Max(0.1f, duration);
        }

        public bool CanInteract(GameObject interactor)
        {
            return !string.IsNullOrWhiteSpace(guideText);
        }

        public void Interact(GameObject interactor)
        {
            GameManager.Instance?.DayCycle?.ShowTemporaryGuide(guideText, guideDuration);
        }
    }
}
