using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 卡片歷程紀錄管理器
/// 負責保存所有卡片使用行為，並提供統計與查詢功能
/// </summary>
public class CardHistoryManager : MonoBehaviour
{
    /// <summary>
    /// 單例實例，可在任何腳本透過 CardHistoryManager.Instance 呼叫
    /// </summary>
    public static CardHistoryManager Instance;

    /// <summary>
    /// 所有歷程的儲存清單
    /// </summary>
    private List<CardHistoryEntry> history = new List<CardHistoryEntry>();

    //==========================================================
    // 基本初始化
    //==========================================================
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 可跨場景保存
        }
        else
        {
            Destroy(gameObject);
        }
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKey(KeyCode.P))
        {
            PrintSummary();
        }
    }
#endif

    //==========================================================
    // 主要功能：新增紀錄
    //==========================================================
    /// <summary>
    /// 新增一筆卡片使用紀錄
    /// </summary>
    public void Record(CardHistoryEntry entry)
    {
        history.Add(entry);
        Debug.Log($"[歷程紀錄] 玩家 {entry.userId} 對 玩家 {entry.targetId}使用「{entry.cardName}」({entry.cardType}) " +
                  $"於 時間: {entry.timeStamp:HH:mm:ss}");

        if (TraceMission.Instance != null)
            TraceMission.Instance.ProcessPlayerCards();
        else
            Debug.LogWarning("TraceMission.Instance 尚未初始化");
    }

    //==========================================================
    // 取得所有紀錄
    //==========================================================
    public List<CardHistoryEntry> GetAllHistory() => history;

    //==========================================================
    // 清空所有紀錄
    //==========================================================
    public void ClearHistory()
    {
        history.Clear();
        Debug.Log("[歷程紀錄] 所有歷程已清除");
    }

    //==========================================================
    // 簡易輸出總覽
    //==========================================================
    /// <summary>
    /// 在 Console 印出所有紀錄的摘要
    /// </summary>
    public void PrintSummary()
    {
        Debug.Log("===== 🎴 卡片使用歷程總覽 =====");
        if (history.Count == 0)
        {
            Debug.Log("目前沒有任何紀錄。");
            return;
        }

        foreach (var entry in history)
        {
            Debug.Log($"玩家 {entry.userId} 使用「{entry.cardName}」({entry.cardType}) 對 玩家 {entry.targetId} " +
                      $"於 時間: {entry.timeStamp}");
        }
    }

    //==========================================================
    // 分類統計功能
    //==========================================================
    /// <summary>
    /// 統計每種類型的卡片使用次數（Mission / Function / Item）
    /// </summary>
    public void GetSummaryByType()
    {
        Debug.Log("===== 📊 卡片類型使用統計 =====");

        if (history.Count == 0)
        {
            Debug.Log("目前沒有紀錄可統計。");
            return;
        }

        var grouped = history.GroupBy(h => h.cardType);

        foreach (var group in grouped)
        {
            int successCount = group.Count(g => g.canUseResult);
            int failCount = group.Count(g => !g.canUseResult);
            Debug.Log($"類型: {group.Key} → 成功: {successCount} 次, 失敗: {failCount} 次, 總共: {group.Count()} 次");
        }
    }

    //==========================================================
    // 玩家別統計
    //==========================================================
    /// <summary>
    /// 輸出每位玩家使用過的卡與次數
    /// </summary>
    public void GetSummaryByPlayer()
    {
        Debug.Log("===== 🧍 玩家使用卡片統計 =====");

        if (history.Count == 0)
        {
            Debug.Log("目前沒有紀錄可統計。");
            return;
        }

        var grouped = history.GroupBy(h => h.userId);

        foreach (var group in grouped)
        {
            Debug.Log($"玩家 {group.Key}：共使用 {group.Count()} 張卡");

            var cardGroups = group.GroupBy(h => h.cardName);
            foreach (var cardGroup in cardGroups)
            {
                int success = cardGroup.Count(h => h.canUseResult);
                int fail = cardGroup.Count(h => !h.canUseResult);
                Debug.Log($"　　→ {cardGroup.Key}：成功 {success} 次，失敗 {fail} 次");
            }
        }
    }
}
