using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// 放在父物件上（需有 NetworkObject）。
/// 遊戲開始時由 Host 隨機從子物件中挑 3 個設為 Steal 目標，其餘不啟用。
/// </summary>
public class StealTargetSelector : NetworkBehaviour
{
    public static StealTargetSelector Instance { get; private set; }

    [SerializeField] private Transform poolParent;

    private readonly List<StealTargetObject> selectedTargets = new();

    public override void Spawned()
    {
        Instance = this;

        if (!Runner.IsServer) return;

        var candidates = new List<StealTargetObject>();
        foreach (Transform child in poolParent)
        {
            var sto = child.GetComponent<StealTargetObject>();
            if (sto != null) candidates.Add(sto);
        }

        if (candidates.Count < 3)
        {
            Debug.LogWarning($"[StealTargetSelector] 父物件下只有 {candidates.Count} 個候選，需要至少 3 個");
        }

        // Fisher-Yates shuffle
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        int count = Mathf.Min(3, candidates.Count);
        selectedTargets.Clear();
        for (int i = 0; i < count; i++)
        {
            candidates[i].StealIndex = i;
            candidates[i].IsTarget   = true;
            candidates[i].IsStolen   = false;
            selectedTargets.Add(candidates[i]);
        }

        Debug.Log($"[StealTargetSelector] 已隨機選出 {count} 個 Steal 目標");
    }

    /// <summary>
    /// Host 呼叫：重置所有已偷走的 Steal 目標（Steal 卡換手時使用）。
    /// </summary>
    public void ResetAllTargets()
    {
        if (!Runner.IsServer) return;
        foreach (var target in selectedTargets)
        {
            if (target != null && target.IsStolen)
                target.Reset();
        }
        Debug.Log("[StealTargetSelector] 所有 Steal 目標已重置");
    }
}
