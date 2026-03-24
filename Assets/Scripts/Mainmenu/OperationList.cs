using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MissionUIManager : MonoBehaviour
{
    [Header("Prefab & 容器")]
    public GameObject missionSlotPrefab;
    public Transform missionContainer;

    private Dictionary<int, MissionSlot> missionDict = new Dictionary<int, MissionSlot>();
    private List<int> missionOrder = new List<int>();
    private int focusIndex = 0;



    void Update()
    {
        var missionStates = LocalBackpack.Instance.userInventory.MissionStates;

        // 1️⃣ 更新與新增
        foreach (var mission in missionStates)
        {
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

    // ✅ 更新任務進度MissionStates
   public void UpdateMissionProgress(int id, int setValue)
{
    if (!missionDict.ContainsKey(id)) return;

    MissionSlot slot = missionDict[id];
    slot.data.current = Mathf.Min(setValue, slot.data.goal); // ← 直接設，不累加
    slot.Refresh();

    if (slot.data.current >= slot.data.goal)
    {
        slot.MarkAsCompleted();
    }
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
