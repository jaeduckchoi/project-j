using System.Collections.Generic;
using Core;
using Player;
using Tools;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// 플레이어가 영역 안에 있는 동안 이동 속도 배율을 변경한다.
namespace World
{
    [RequireComponent(typeof(Collider2D))]
    [MovedFrom(false, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "MovementModifierZone")]
    public class MovementModifierZone : MonoBehaviour
    {
    [SerializeField, Range(0.1f, 2f)] private float movementMultiplier = 0.6f;
    [SerializeField] private ToolType ignorePenaltyWithTool = ToolType.None;
    [SerializeField, TextArea] private string guideText = string.Empty;
    [SerializeField] private string hintId = "movement_zone";

    private readonly HashSet<PlayerController> _playersInZone = new();
    private Collider2D _triggerCollider;

    /*
     * 이동 보정 지대를 트리거 영역으로 고정합니다.
     */
    private void Awake()
    {
        _triggerCollider = GetComponent<Collider2D>();
        _triggerCollider.isTrigger = true;
    }

    /*
     * 런타임 보강 과정에서 감속 수치와 힌트 문구를 다시 설정합니다.
     */
    public void Configure(float multiplier, ToolType ignoreWithTool = ToolType.None, string text = "", string id = "")
    {
        movementMultiplier = Mathf.Clamp(multiplier, 0.1f, 2f);
        ignorePenaltyWithTool = ignoreWithTool;
        guideText = text ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(id))
        {
            hintId = id;
        }
    }

    /*
     * 진입 시 감속 등록을 시도합니다.
     */
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryRegisterPlayer(other);
    }

    /*
     * 체류 중에도 도구 조건 변화에 대응할 수 있게 다시 검사합니다.
     */
    private void OnTriggerStay2D(Collider2D other)
    {
        TryRegisterPlayer(other);
    }

    /*
     * 지대를 벗어나면 이 지대가 건 속도 배율을 제거합니다.
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
     * 비활성화 시 남아 있는 속도 배율 출처를 정리합니다.
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
     * 플레이어를 감속 대상으로 등록하고 필요 시 힌트 문구를 노출합니다.
     */
    private void TryRegisterPlayer(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        _playersInZone.Add(player);

        // 특정 도구가 있으면 감속을 무시하도록 열어둔 확장 포인트입니다.
        if (CanIgnorePenalty())
        {
            player.ClearMovementMultiplierSource(this);
            return;
        }

        player.SetMovementMultiplierSource(this, movementMultiplier);

        if (!string.IsNullOrWhiteSpace(guideText))
        {
            GameManager.Instance?.DayCycle?.ShowHintOnce(hintId, guideText);
        }
    }

    /*
     * 지정한 도구가 있으면 감속을 무시할 수 있는지 확인합니다.
     */
    private bool CanIgnorePenalty()
    {
        return ignorePenaltyWithTool != ToolType.None
            && GameManager.Instance != null
            && GameManager.Instance.Tools != null
            && GameManager.Instance.Tools.HasTool(ignorePenaltyWithTool);
    }
    }
}