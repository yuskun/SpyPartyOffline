using System;
using UnityEngine;

/// <summary>
/// 紀錄一筆卡片使用歷程的資料結構
/// 用於追蹤誰對誰使用了什麼卡、何時使用、結果如何
/// </summary>
[Serializable]
public class CardHistoryEntry
{
    /// <summary>
    /// 使用卡片的玩家 ID
    /// （用來辨識是哪位玩家發動了技能或效果）
    /// </summary>
    public int userId;

    /// <summary>
    /// 被使用（或攻擊）目標的玩家 ID  
    /// （若卡片無目標，則可設為 -1 或相同玩家 ID）
    /// </summary>
    public int targetId;

    /// <summary>
    /// 被使用的卡片名稱  
    /// （例如：「陷阱任務卡」、「治療道具卡」、「偷取功能卡」等）
    /// </summary>
    public string cardName;

    /// <summary>
    /// 卡片的類型  
    /// 可能值：
    /// - <see cref="CardType.Mission"/>：任務卡，通常與目標或條件有關  
    /// - <see cref="CardType.Function"/>：功能卡，通常是即時效果（如偷取、交換）  
    /// - <see cref="CardType.Item"/>：物品卡，提供道具或特殊增益  
    /// - <see cref="CardType.None"/>：無效或暫無類型
    /// </summary>
    public CardType cardType;

    /// <summary>
    /// 任務卡的具體任務類型（Trigger / Collect 等）  
    /// 非任務卡時可為 null  
    /// - Trigger：觸發型（例如某事件達成時啟動）  
    /// - Collect：收集型（需要收集特定物件或條件）
    /// </summary>
    public MissionType? missionType; // 可為 null

    /// <summary>
    /// CanUse() 的檢查結果  
    /// - true：允許使用  
    /// - false：使用失敗（條件不符、冷卻中、目標錯誤等）
    /// </summary>
    public bool canUseResult;

    /// <summary>
    /// 此次卡片操作的時間戳記  
    /// （記錄動作發生的實際時間，可用於排序或統計）
    /// </summary>
    public DateTime timeStamp;

    /// <summary>
    /// 建構子，用於初始化一筆完整的歷程資料
    /// </summary>
    public CardHistoryEntry(int userId, int targetId, string cardName, CardType cardType, MissionType? missionType) //, bool canUseResult
    {
        this.userId = userId;
        this.targetId = targetId;
        this.cardName = cardName;
        this.cardType = cardType;
        this.missionType = missionType;
        //this.canUseResult = canUseResult;
        this.timeStamp = DateTime.Now; // 自動記錄當下時間
    }
}
