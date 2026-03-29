using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// 골드와 평판을 관리하는 최소 경제 시스템이다.
namespace Economy
{
    [MovedFrom(false, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "EconomyManager")]
    public class EconomyManager : MonoBehaviour
    {
    // 하루 루프에서 누적할 시작 재화 값이다.
    [SerializeField, Min(0)] private int startingGold = 0;
    [SerializeField] private int startingReputation = 0;

    private bool _initialized;

    // UI가 골드와 평판 변화를 바로 반영할 수 있도록 이벤트를 노출한다.
    public event Action<int> GoldChanged;
    public event Action<int> ReputationChanged;

    public int CurrentGold { get; private set; }
    public int CurrentReputation { get; private set; }

    /*
     * 시작 시점에 기본 재화 상태를 초기화한다.
     */
    private void Awake()
    {
        InitializeIfNeeded();
    }

    /*
     * 시작 골드와 평판을 한 번만 적용하고 초기 이벤트를 발생시킨다.
     */
    public void InitializeIfNeeded()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        CurrentGold = Mathf.Max(0, startingGold);
        CurrentReputation = startingReputation;
        GoldChanged?.Invoke(CurrentGold);
        ReputationChanged?.Invoke(CurrentReputation);
    }

    /*
     * 양수 골드를 누적하고 변경 이벤트를 알린다.
     */
    public void AddGold(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        InitializeIfNeeded();
        CurrentGold += amount;
        GoldChanged?.Invoke(CurrentGold);
    }

    /*
     * 보유 골드가 충분할 때만 비용을 차감한다.
     */
    public bool TrySpendGold(int amount)
    {
        InitializeIfNeeded();

        if (amount <= 0 || CurrentGold < amount)
        {
            return false;
        }

        CurrentGold -= amount;
        GoldChanged?.Invoke(CurrentGold);
        return true;
    }

    /*
     * 평판 변화량을 누적하고 관련 UI에 알린다.
     */
    public void AddReputation(int amount)
    {
        if (amount == 0)
        {
            return;
        }

        InitializeIfNeeded();
        CurrentReputation += amount;
        ReputationChanged?.Invoke(CurrentReputation);
    }
    }
}