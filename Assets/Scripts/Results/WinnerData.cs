using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 勝利玩家的結算數據
/// </summary>
[System.Serializable]
public class WinnerData
{
    /// <summary>主勝利者 PlayerID（單人/多人勝利時都填，多人時 = winnerIDs[0]）</summary>
    public int winnerID;

    /// <summary>所有勝利者 PlayerID（單人勝利時 = [winnerID]，多人勝利時包含全部）</summary>
    public List<int> winnerIDs = new();

    /// <summary>勝利者持有的任務卡（可能多張）</summary>
    public List<MissionCardEntry> missionCards = new();

    /// <summary>使用過的道具/功能卡（不含任務卡）</summary>
    public List<CardUsageEntry> cardUsages = new();

    /// <summary>擊倒記錄 — 被擊倒者 PlayerID → 擊倒次數</summary>
    public List<KnockdownEntry> knockdowns = new();

    /// <summary>勝利者每張任務卡的當下進度（結算 UI 用）</summary>
    public List<MissionProgressEntry> missionProgress = new();
}

/// <summary>單一任務卡的進度快照</summary>
[System.Serializable]
public class MissionProgressEntry
{
    public int missionId;
    public string title;
    public int current;
    public int goal;
}

/// <summary>任務卡紀錄</summary>
[System.Serializable]
public class MissionCardEntry
{
    public CardData card;
    public Sprite image;
}

/// <summary>卡片使用紀錄（單一卡片）</summary>
[System.Serializable]
public class CardUsageEntry
{
    public CardData card;
    public Sprite image;
    public int useCount;
}

/// <summary>擊倒紀錄（單一目標）</summary>
[System.Serializable]
public class KnockdownEntry
{
    public int targetPlayerId;
    public int knockdownCount;
}
