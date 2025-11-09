using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class MissionSlot : MonoBehaviour
{
    [Header("UI 結構")]
    public GameObject focusRoot;   // 對應 Mission/Focus
    public GameObject normalRoot;  // 對應 Mission/Normal
    private MissionUIManager missionUIManager;

    [Header("Focus 狀態元件")]
    public TextMeshProUGUI focusTitleText;
    public TextMeshProUGUI focusDescText;
    public TextMeshProUGUI focusProgressText;
    public Slider focusProgressBar;

    [Header("Normal 狀態元件")]
    public TextMeshProUGUI normalTitleText;
    public TextMeshProUGUI normalDescText;
    public TextMeshProUGUI normalProgressText;
    public Slider normalProgressBar;

    [HideInInspector] public MissionData data;

    public void Start()
    {
        missionUIManager = GetComponentInParent<MissionUIManager>();
    }

    public void Setup(MissionData newData)
    {
        data = newData;
        Refresh();
    }

    public void Refresh()
    {
        bool isComplete = data.current >= data.goal;

        // Focus 顯示內容
        focusTitleText.text = data.title;
        focusDescText.text = data.description;
        focusProgressBar.value = Mathf.Clamp01((float)data.current / data.goal);
        focusProgressText.text = isComplete ? "完成" : $"{data.current}/{data.goal}";

        // Normal 顯示內容
        normalTitleText.text = data.title;
        normalDescText.text = data.description;
        normalProgressBar.value = Mathf.Clamp01((float)data.current / data.goal);
        normalProgressText.text = isComplete ? "完成" : $"{data.current}/{data.goal}";
    }

    public void SetFocus(bool focus)
    {
        // ✅ 直接切換顯示狀態，不用調整 scale
        focusRoot.SetActive(focus);
        normalRoot.SetActive(!focus);
    }

    // ✅ 呼叫此函式可立即標記任務完成（顯示「完成」文字）
    public void MarkAsCompleted()
    {
        data.current = data.goal;
        Refresh();
    }
    public void Update()
    {
        if (data != null && data.triggerKey.Any(key => Input.GetKeyDown(key)))
        {
            missionUIManager.UpdateMissionProgress(data.id, 1);
        }
    }
}
