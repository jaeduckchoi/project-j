using System.Collections.Generic;
using Core;
using Player;
using Tools;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// 랜턴 보유 여부에 따라 어두운 지역의 이동 난이도를 조절한다.
namespace World
{
    [RequireComponent(typeof(Collider2D))]
    [MovedFrom(false, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "DarknessZone")]
    public class DarknessZone : MonoBehaviour
    {
    [SerializeField, Range(0.1f, 1f)] private float noLanternMovementMultiplier = 0.55f;
    [SerializeField, TextArea] private string noLanternGuideText = "랜턴이 있으면 어두운 지역을 더 안전하게 이동할 수 있습니다.";
    [SerializeField] private string hintId = "darkness_zone";

    private readonly HashSet<PlayerController> _playersInZone = new();
    private Collider2D _triggerCollider;

    /*
     * 어둠 지대를 트리거 영역으로 고정합니다.
     */
    private void Awake()
    {
        _triggerCollider = GetComponent<Collider2D>();
        _triggerCollider.isTrigger = true;
    }

    /*
     * 씬 보강 단계에서 어둠 지대 수치와 안내 문구를 조정합니다.
     */
    public void Configure(float movementMultiplier, string text = "", string id = "")
    {
        noLanternMovementMultiplier = Mathf.Clamp(movementMultiplier, 0.1f, 1f);

        if (!string.IsNullOrWhiteSpace(text))
        {
            noLanternGuideText = text;
        }

        if (!string.IsNullOrWhiteSpace(id))
        {
            hintId = id;
        }
    }

    /*
     * 진입 즉시 플레이어 상태를 다시 계산합니다.
     */
    private void OnTriggerEnter2D(Collider2D other)
    {
        UpdatePlayerState(other);
    }

    /*
     * 체류 중에도 랜턴 해금 여부가 바뀌었을 때를 대비해 상태를 갱신합니다.
     */
    private void OnTriggerStay2D(Collider2D other)
    {
        UpdatePlayerState(other);
    }

    /*
     * 영역을 벗어나면 이 지대가 건 이동 패널티를 제거합니다.
     */
    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        player.ClearMovementMultiplierSource(this);
        _playersInZone.Remove(player);
    }

    /*
     * 오브젝트가 비활성화될 때 남아 있는 패널티 출처를 모두 정리합니다.
     */
    private void OnDisable()
    {
        foreach (PlayerController player in _playersInZone)
        {
            if (player == null)
            {
                continue;
            }

            player.ClearMovementMultiplierSource(this);
        }

        _playersInZone.Clear();
    }

    /*
     * 플레이어의 랜턴 보유 여부에 맞춰 감속과 안내 문구를 적용합니다.
     */
    private void UpdatePlayerState(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        _playersInZone.Add(player);

        // 랜턴이 있으면 어둠 패널티를 제거하고 통과만 시킵니다.
        if (HasLantern())
        {
            player.ClearMovementMultiplierSource(this);
            return;
        }

        player.SetMovementMultiplierSource(this, noLanternMovementMultiplier);
        GameManager.Instance?.DayCycle?.ShowHintOnce(hintId, noLanternGuideText);
    }

    /*
     * 현재 플레이어가 랜턴 도구를 해금했는지 확인합니다.
     */
    private static bool HasLantern()
    {
        return GameManager.Instance != null
            && GameManager.Instance.Tools != null
            && GameManager.Instance.Tools.HasTool(ToolType.Lantern);
    }
    }
}