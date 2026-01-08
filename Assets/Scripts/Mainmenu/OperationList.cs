using System.Collections.Generic;
using UnityEngine;

public class MissionUIManager : MonoBehaviour
{
    [Header("Prefab & 容器")]
    public GameObject missionSlotPrefab;
    public Transform missionContainer;

    private Dictionary<int, MissionSlot> missionDict = new Dictionary<int, MissionSlot>();
    private List<int> missionOrder = new List<int>();
    private int focusIndex = 0;

    public static MissionUIManager Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        foreach(var Mission in LocalBackpack.Instance.userInventory.MissionStates)
        {
             UpdateMissions(Mission.Key, Mission.Value);
        }
        
        if (Input.GetKeyDown(KeyCode.Tab))
            FocusNextMission();
    }
    public void UpdateMissions(int missionID,int addValue)
    {
        if (missionDict.ContainsKey(missionID))
        {
            UpdateMissionProgress(missionID, addValue);
           
        }else
        {
            AddMission(CardManager.Instance.GetMissionCard(missionID).data);
        }
    }

    // ✅ 新增任務
    public void AddMission(MissionData data)
    {
        if (missionDict.ContainsKey(data.id))
        {
            Debug.LogWarning($"任務ID {data.id} 已存在！");
            return;
        }

        GameObject obj = Instantiate(missionSlotPrefab);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(missionContainer, false); // ✅ 確保尺寸正確

        // ❌ 不用再設定縮放
        // rect.localScale = new Vector3(0.4f, 0.4f, 1f);

        MissionSlot slot = obj.GetComponent<MissionSlot>();
        slot.Setup(data);

        missionDict.Add(data.id, slot);
        missionOrder.Add(data.id);
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

    // ✅ 更新任務進度
    public void UpdateMissionProgress(int id, int addValue)
    {
        Debug.LogWarning("UpdateMissionProgress呼叫");
        if (!missionDict.ContainsKey(id)) return;
        Debug.LogWarning("!missionDict.ContainsKey(id)");
        MissionSlot slot = missionDict[id];
        slot.data.current = Mathf.Min(slot.data.current + addValue, slot.data.goal);
        slot.Refresh();
        Debug.LogWarning("slot.Refresh()");
        if (slot.data.current >= slot.data.goal)
        {
            slot.MarkAsCompleted();
            Debug.LogWarning("MarkAsCompleted()");
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
