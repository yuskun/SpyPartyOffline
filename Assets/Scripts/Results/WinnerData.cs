using System.Collections.Generic;

/// <summary>
/// 勝利玩家的結算數據
/// </summary>
[System.Serializable]
public class WinnerData
{
    /// <summary>勝利者 PlayerID</summary>
    public int winnerID;

    /// <summary>勝利者持有的任務卡 CardData（可能多張）</summary>
    public List<CardData> missionCards = new();

    /// <summary>使用過的道具/功能卡（不含任務卡）— 卡片名稱 → 使用次數</summary>
    public List<CardUsageEntry> cardUsages = new();

    /// <summary>擊倒記錄 — 被擊倒者 PlayerID → 擊倒次數</summary>
    public List<KnockdownEntry> knockdowns = new();
}

/// <summary>卡片使用紀錄（單一卡片）</summary>
[System.Serializable]
public class CardUsageEntry
{
    public int cardId;
    public CardType cardType;
    public string cardName;
    public int useCount;
}

/// <summary>擊倒紀錄（單一目標）</summary>
[System.Serializable]
public class KnockdownEntry
{
    public int targetPlayerId;
    public int knockdownCount;
}
