using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MissionPanelBridge : MonoBehaviour
{
    private UIDocument _uiDocument;
    private VisualElement _missionPanel;
 
    // key = 任意 ID（你傳什麼都行，例如 0/1/2）
    private Dictionary<int, VisualElement> _rows = new();
    private List<int> _order = new();
    private int _focusIndex = 0;
 
    private void OnEnable()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null) { Debug.LogError("找不到 UIDocument"); return; }
 
        var root = _uiDocument.rootVisualElement;
        _missionPanel = root.Q<VisualElement>("MissionPanel");
        if (_missionPanel == null) { Debug.LogError("找不到 #MissionPanel"); return; }
 
        // 清掉 UXML 裡寫死的示範列，改由程式動態產生
        _missionPanel.Clear();
    }
 
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            FocusNext();
    }
 
    // ═══════════════════════════════════════════════
    //  公開 API
    // ═══════════════════════════════════════════════
 
    /// <summary>新增一列任務</summary>
    public void AddMission(int id, string title, string desc, int current, int goal)
    {
        if (_rows.ContainsKey(id)) return;
 
        var row = BuildRow(id, title, desc, current, goal);
        _missionPanel.Add(row);
        _rows.Add(id, row);
        _order.Add(id);
 
        // 第一列自動 focus
        if (_order.Count == 1) _focusIndex = 0;
        RefreshFocus();
    }
 
    /// <summary>更新進度數字</summary>
    public void UpdateProgress(int id, int current, int goal)
    {
        if (!_rows.TryGetValue(id, out var row)) return;
        var lbl = row.Q<Label>("progress-lbl");
        if (lbl == null) return;
 
        if (goal <= 0)
            lbl.text = "";
        else
            lbl.text = current >= goal ? "完成" : $"{current}/{goal}";
    }
 
    /// <summary>更新標題與描述（多步驟任務用）</summary>
    public void UpdateDisplay(int id, string title, string desc, int current, int goal)
    {
        if (!_rows.TryGetValue(id, out var row)) return;
        var titleLbl = row.Q<Label>("title-lbl");
        var descLbl  = row.Q<Label>("desc-lbl");
        if (titleLbl != null) titleLbl.text = title;
        if (descLbl  != null) descLbl.text  = desc;
        UpdateProgress(id, current, goal);
    }
 
    /// <summary>移除一列任務</summary>
    public void RemoveMission(int id)
    {
        if (!_rows.TryGetValue(id, out var row)) return;
        _missionPanel.Remove(row);
        _rows.Remove(id);
        _order.Remove(id);
        _focusIndex = Mathf.Clamp(_focusIndex, 0, Mathf.Max(0, _order.Count - 1));
        RefreshFocus();
    }
 
    // ═══════════════════════════════════════════════
    //  私有：建立列、焦點切換
    // ═══════════════════════════════════════════════
 
    private VisualElement BuildRow(int id, string title, string desc, int current, int goal)
    {
        // 外層 .mission-task
        var row = new VisualElement();
        row.AddToClassList("mission-task");
 
        // 左側：標題 + 描述
        var left = new VisualElement();
        left.AddToClassList("task-left");
 
        var titleLbl = new Label(title);
        titleLbl.name = "title-lbl";
        titleLbl.AddToClassList("task-title");
        titleLbl.pickingMode = PickingMode.Ignore;
 
        var descLbl = new Label(desc);
        descLbl.name = "desc-lbl";
        descLbl.AddToClassList("task-desc");
        descLbl.pickingMode = PickingMode.Ignore;
 
        left.Add(titleLbl);
        left.Add(descLbl);
 
        // 右側：進度
        var progressLbl = new Label();
        progressLbl.name = "progress-lbl";
        progressLbl.AddToClassList("task-progress");
        progressLbl.pickingMode = PickingMode.Ignore;
 
        if (goal <= 0)
            progressLbl.text = "";
        else
            progressLbl.text = current >= goal ? "完成" : $"{current}/{goal}";
 
        row.Add(left);
        row.Add(progressLbl);
 
        // 點擊：toggle 展開/收合
        row.RegisterCallback<ClickEvent>(_ => OnRowClicked(id));
 
        return row;
    }
 
    private void OnRowClicked(int id)
    {
        int idx = _order.IndexOf(id);
        if (idx < 0) return;
 
        // 點已展開的那列 → 收合（focusIndex = -1）
        // 點其他列 → 展開那列
        _focusIndex = (_focusIndex == idx) ? -1 : idx;
        RefreshFocus();
    }
 
    private void FocusNext()
    {
        if (_order.Count == 0) return;
        _focusIndex = (_focusIndex + 1) % _order.Count;
        RefreshFocus();
    }
 
    private void RefreshFocus()
    {
        for (int i = 0; i < _order.Count; i++)
        {
            int id = _order[i];
            if (!_rows.TryGetValue(id, out var row)) continue;
 
            if (i == _focusIndex)
                row.AddToClassList("active");
            else
                row.RemoveFromClassList("active");
        }
    }
}