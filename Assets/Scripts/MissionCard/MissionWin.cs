using System.Collections.Generic;
using UnityEngine;

public class MissionWin : MonoBehaviour
{
    public static MissionWin Instance;

    // 需要追蹤的所有 MissionCard（不分類型）
    private List<MissionCard> trackedMissionCards = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildTrackedMissions();
    }

    /// <summary>
    /// 從 CardManager 的 missionDictionary 讀取所有任務
    /// </summary>
    private void BuildTrackedMissions()
    {
        trackedMissionCards.Clear();

        if (CardManager.Instance == null)
        {
            Debug.LogWarning("[MissionWinSystem] CardManager 尚未初始化");
            return;
        }

        foreach (var kvp in CardManager.Instance.GetAllMissions())
        {
            var mission = kvp.Value;
            if (mission != null)
            {
                trackedMissionCards.Add(mission);

            }
        }
    }

    /// <summary>
    /// 取得所有需要追蹤的任務卡
    /// </summary>
    public List<MissionCard> GetTrackedMissions()
    {
        return trackedMissionCards;
    }

    public void GameOver()
    {
        Debug.Log("遊戲結束，玩家完成所有任務！");
        // TODO: 在這裡觸發結算畫面/廣播事件
    }
}
