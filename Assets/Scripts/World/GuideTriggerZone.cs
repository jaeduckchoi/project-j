using Core;
using Player;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// World 네임스페이스
namespace World
{
    /// <summary>
    /// 플레이어가 특정 구역에 들어왔을 때 짧은 안내 문구를 띄우는 트리거다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [MovedFrom(false, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "GuideTriggerZone")]
    public class GuideTriggerZone : MonoBehaviour
    {
        // 한 번만 보여줄 힌트인지, 얼마 동안 유지할지 설정한다.
        [SerializeField] private string hintId = "guide_trigger";
        [SerializeField, TextArea] private string guideText = "안내 문구";
        [SerializeField, Min(1f)] private float duration = 5f;
        [SerializeField] private bool triggerOnlyOnce = true;

        private Collider2D _triggerCollider;

        /// <summary>
        /// 트리거 콜라이더를 강제하고 참조를 캐시한다.
        /// </summary>
        private void Awake()
        {
            _triggerCollider = GetComponent<Collider2D>();
            _triggerCollider.isTrigger = true;
        }

        /// <summary>
        /// 빌더나 런타임 보강 코드에서 힌트 내용을 다시 설정한다.
        /// </summary>
        public void Configure(string id, string text, float hintDuration = 5f, bool once = true)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                hintId = id;
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                guideText = text;
            }

            duration = Mathf.Max(1f, hintDuration);
            triggerOnlyOnce = once;
        }

        /// <summary>
        /// 플레이어 진입 시 1회성 또는 임시 가이드를 표시한다.
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null || string.IsNullOrWhiteSpace(guideText))
            {
                return;
            }

            if (triggerOnlyOnce)
            {
                GameManager.Instance?.DayCycle?.ShowHintOnce(hintId, guideText, duration);
                return;
            }

            GameManager.Instance?.DayCycle?.ShowTemporaryGuide(guideText, duration);
        }
    }
}