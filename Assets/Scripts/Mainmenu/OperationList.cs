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
    private static readonly HashSet<int> DisplayKeys = new HashSet<int> { 0, 1, 2, 99 };

    [Header("UIDocument Bridge（新任務欄）")]
    public MissionPanelBridge missionPanelBridge;

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
                if (mission.Key == MissionWinSystem.NormalMissionKey)
                    AddNormalMission();
                else
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

        missionPanelBridge?.AddMission(missionId, card.data.title, card.data.description, 0, goal);
    }
    
    // ✅ 新增凡人任務
    public void AddNormalMission()
    {
        int missionId = MissionWinSystem.NormalMissionKey;
        if (missionDict.ContainsKey(missionId)) return;

        int goal = 480; // 預設 8 分鐘
        LocalBackpack.Instance.userInventory.MissionGoals.TryGet(missionId, out goal);

        string title = "凡人";
        string desc = "試著拿到其他任務或乖乖等到遊戲結束";
        MissionData displayData = new MissionData(title, desc, goal);

        GameObject obj = Instantiate(missionSlotPrefab);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(missionContainer, false);

        MissionSlot slot = obj.GetComponent<MissionSlot>();
        slot.Setup(displayData);

        missionDict.Add(missionId, slot);
        missionOrder.Add(missionId);
        RefreshFocus();

        missionPanelBridge?.AddMission(missionId, title, desc, 0, goal);
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

        missionPanelBridge?.RemoveMission(id);
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
                missionPanelBridge?.UpdateDisplay(id,
                    slot.data.title, slot.data.description,
                    slot.data.current, slot.data.goal);
                break;
            }

            case 1: // Steal：偷取場景物件 / 被押送時切換為「逃離」
            {
                // 檢查是否正在被押送（MissionStates[12] 存在）
                bool isBeingEscorted = inv.MissionStates.TryGet(12, out int escortProgress);

                if (isBeingEscorted)
                {
                    int escortGoal = 10;
                    if (MissionWinSystem.Instance != null)
                        escortGoal = (int)MissionWinSystem.Instance.escapeDuration;

                    slot.data.title       = "逃離";
                    slot.data.description = "逃離警察抓捕" + escortGoal + "秒";
                    slot.data.current     = escortProgress;
                    slot.data.goal        = escortGoal;
                    slot.Refresh();

                    missionPanelBridge?.UpdateDisplay(id, slot.data.title, slot.data.description, escortProgress, escortGoal);
                }
                else
                {
                    var stealCard = CardManager.Instance.GetMissionCard(1);
                    slot.data.title       = stealCard != null ? stealCard.data.title : "小偷";
                    slot.data.description = "靠近場景物件長按 E 偷走它";
                    slot.data.current     = setValue;
                    slot.data.goal        = 3;
                    slot.Refresh();

                    missionPanelBridge?.UpdateDisplay(id, slot.data.title, slot.data.description, setValue, 3);
                }
                break;
            }

            case 99: // 凡人：倒數計時進度
            {
                int goal = slot.data.goal;
                inv.MissionGoals.TryGet(99, out goal);
                slot.data.goal = goal;
                slot.data.current = Mathf.Min(setValue, goal);
                slot.data.title = "凡人";
                slot.data.description = "試著拿到其他任務或乖乖等到遊戲結束";
                slot.Refresh();

                missionPanelBridge?.UpdateDisplay(id, slot.data.title, slot.data.description, slot.data.current, goal);
                break;
            }

            default: // Fight（id=2）及其他：原有邏輯
            {
                slot.data.current = Mathf.Min(setValue, slot.data.goal);
                slot.Refresh();
                if (slot.data.current >= slot.data.goal)
                    slot.MarkAsCompleted();

                missionPanelBridge?.UpdateProgress(id, slot.data.current, slot.data.goal);
                break;
            }
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
