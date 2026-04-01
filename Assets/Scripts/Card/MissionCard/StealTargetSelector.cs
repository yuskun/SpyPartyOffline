using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// 放在父物件上（需有 NetworkObject）。
/// 遊戲開始時由 Host 隨機從子物件中挑 3 個設為 Steal 目標，其餘不啟用。
/// </summary>
public class StealTargetSelector : NetworkBehaviour
{
    [SerializeField] private Transform poolParent;

    public override void Spawned()
    {
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
        for (int i = 0; i < count; i++)
        {
            candidates[i].StealIndex = i;
            candidates[i].IsTarget = true;
        }

        Debug.Log($"[StealTargetSelector] 已隨機選出 {count} 個 Steal 目標");
    }
}
