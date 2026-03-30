using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MissionUIManager : MonoBehaviour
{
    [Header("Prefab & 容器")]
    public GameObject missionSlotPrefab;
    public Transform missionContainer;

    private Dictionary<int, MissionSlot> missionDict = new Dictionary<int, MissionSlot>();
    private List<int> missionOrder = new List<int>();
    private int focusIndex = 0;

    // 只有這些 key 代表「任務顯示槽」，其餘為 metadata
    private static readonly HashSet<int> DisplayKeys = new HashSet<int> { 0, 1, 2 };

    void Update()
    {
        var missionStates = LocalBackpack.Instance.userInventory.MissionStates;

        // 1️⃣ 更新與新增（只處理顯示用 key）
        foreach (var mission in missionStates)
        {
            if (!DisplayKeys.Contains(mission.Key)) continue; // 跳過 metadata key（11 等）

            if (missionDict.ContainsKey(mission.Key))
            {
                UpdateMissionProgress(mission.Key, mission.Value);
            }
            else
            {
                AddMission(CardManager.Instance.GetMissionCard(mission.Key));
            }
        }

        // 2️⃣ 找出需要被移除的 Mission
        var removeList = missionDict.Keys
            .Where(id => !missionStates.ContainsKey(id))
            .ToList();

        // 3️⃣ 執行移除
        foreach (var missionID in removeList)
        {
            RemoveMission(missionID);
        }

        if (Input.GetKeyDown(KeyCode.Tab))
            FocusNextMission();
    }
    public void UpdateMissions(int missionID, int addValue)
    {
        if (missionDict.ContainsKey(missionID))
        {
            UpdateMissionProgress(missionID, addValue);

        }
        else
        {
            AddMission(CardManager.Instance.GetMissionCard(missionID));
        }
    }

    // ✅ 新增任務
    public void AddMission(MissionCard card)
    {
        int missionId = card.cardData.id;
        if (missionDict.ContainsKey(missionId))
        {
            Debug.LogWarning($"任務ID {missionId} 已存在！");
            return;
        }

        // goal 從本地玩家的 MissionGoals 讀取（由 Host 分配任務時寫入）
        // 若尚未收到網路資料則 fallback 為 1
        int goal = 1;
        LocalBackpack.Instance.userInventory.MissionGoals.TryGet(missionId, out goal);

        MissionData displayData = new MissionData(card.data.title, card.data.description, goal);

        GameObject obj = Instantiate(missionSlotPrefab);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(missionContainer, false);

        MissionSlot slot = obj.GetComponent<MissionSlot>();
        slot.Setup(displayData);

        missionDict.Add(missionId, slot);
        missionOrder.Add(missionId);
        RefreshFocus();
    }
    
    // ✅ 移除任務
    public void RemoveMission(int id)
    {
        if (!missionDict.ContainsKey(id)) return;

        Destroy(missionDict[id].gameObject);
        missionOrder.Remove(id);
        missionDict.Remove(id);

        focusIndex = Mathf.Clamp(focusIndex, 0, missionOrder.Count - 1);
        RefreshFocus();
    }

    // ✅ 更新任務進度（依任務ID特殊處理顯示邏輯）
    public void UpdateMissionProgress(int id, int setValue)
    {
        if (!missionDict.ContainsKey(id)) return;
        MissionSlot slot = missionDict[id];
        var inv = LocalBackpack.Instance.userInventory;

        switch (id)
        {
            case 0: // Catch：兩步驟顯示
            {
                var catchCard = CardManager.Instance.GetMissionCard(0) as Catch;
                int escortGoal = 20;
                inv.MissionGoals.TryGet(0, out escortGoal);

                if (setValue < 0) // 步驟0：尋找小偷
                {
                    slot.data.title       = catchCard != null ? catchCard.step0Title : "尋找小偷";
                    slot.data.description = catchCard != null ? catchCard.step0Desc  : "抓到持有 Steal 卡的玩家";
                    slot.data.current     = 0;
                    slot.data.goal        = 1;
                }
                else // 步驟1：押送中
                {
                    slot.data.title       = catchCard != null ? catchCard.step1Title : "押送小偷";
                    slot.data.description = catchCard != null ? catchCard.step1Desc  : "在小偷附近待20秒";
                    slot.data.current     = Mathf.Min(setValue, escortGoal);
                    slot.data.goal        = escortGoal;
                }
                slot.Refresh();
                break;
            }

            case 1: // Steal：收集模式 or 計時模式
            {
                int mode = 0;
                inv.MissionStates.TryGet(11, out mode);

                if (mode == 1) // 計時模式（倒數1分鐘後）
                {
                    slot.data.title       = "存活";
                    slot.data.description = "等待時間結束存活獲勝";
                    slot.data.current     = 0;
                    slot.data.goal        = 0; // goal=0 → MissionSlot 隱藏進度條
                }
                else // 收集模式
                {
                    var stealCard = CardManager.Instance.GetMissionCard(1);
                    slot.data.title       = stealCard != null ? stealCard.data.title : "小偷";
                    slot.data.description = BuildStealItemDesc();
                    slot.data.current     = setValue;
                    slot.data.goal        = 3;
                }
                slot.Refresh();
                break;
            }

            default: // Fight（id=2）及其他：原有邏輯
            {
                slot.data.current = Mathf.Min(setValue, slot.data.goal);
                slot.Refresh();
                if (slot.data.current >= slot.data.goal)
                    slot.MarkAsCompleted();
                break;
            }
        }
    }

    /// <summary>讀取 MissionGoals[11~13] 的道具 CardID，組合成描述字串</summary>
    private string BuildStealItemDesc()
    {
        var inv = LocalBackpack.Instance.userInventory;
        var lines = new System.Text.StringBuilder();
        for (int i = 0; i < 3; i++)
        {
            int itemId = -1;
            inv.MissionGoals.TryGet(11 + i, out itemId);
            if (itemId < 0) continue;

            var item = CardManager.Instance.GetItemCard(itemId);
            string itemName = item != null ? item.name : $"道具{itemId}";
            lines.AppendLine($"• {itemName}");
        }
        return lines.Length > 0 ? lines.ToString().TrimEnd() : "收集3個指定道具";
    }

    // ✅ TAB 切換焦點
    private void FocusNextMission()
    {
        if (missionOrder.Count <= 1) return;
        focusIndex = (focusIndex + 1) % missionOrder.Count;
        RefreshFocus();
    }

    // ✅ 更新所有 Slot 顯示狀態
    private void RefreshFocus()
    {
        for (int i = 0; i < missionOrder.Count; i++)
        {
            int id = missionOrder[i];
            bool isFocus = (i == focusIndex);
            missionDict[id].SetFocus(isFocus);
        }
    }
}
