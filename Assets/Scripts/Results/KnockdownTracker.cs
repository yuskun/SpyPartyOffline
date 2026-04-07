using System.Collections.Generic;

/// <summary>
/// 全玩家擊倒記錄（靜態類，僅 Host 端）
/// 記錄：攻擊者 → 被擊倒者 → 次數
/// </summary>
public static class KnockdownTracker
{
    // Key = attackerId, Value = { targetId → 擊倒次數 }
    private static readonly Dictionary<int, Dictionary<int, int>> records = new();

    /// <summary>記錄一次擊倒</summary>
    public static void RecordKnockdown(int attackerId, int targetId)
    {
        if (!records.ContainsKey(attackerId))
            records[attackerId] = new Dictionary<int, int>();

        var targets = records[attackerId];
        if (targets.ContainsKey(targetId))
            targets[targetId]++;
        else
            targets[targetId] = 1;
    }

    /// <summary>取得某玩家的所有擊倒記錄（targetId → 次數）</summary>
    public static Dictionary<int, int> GetRecords(int attackerId)
    {
        return records.TryGetValue(attackerId, out var dict) ? dict : new Dictionary<int, int>();
    }

    /// <summary>取得某玩家的總擊倒次數</summary>
    public static int GetTotalCount(int attackerId)
    {
        if (!records.TryGetValue(attackerId, out var dict)) return 0;
        int total = 0;
        foreach (var count in dict.Values) total += count;
        return total;
    }

    /// <summary>重置（新遊戲開始時呼叫）</summary>
    public static void Reset()
    {
        records.Clear();
    }
}
