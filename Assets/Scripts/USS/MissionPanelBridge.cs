using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MissionPanelBridge : MonoBehaviour
{
    // 任務資料（與 UI 分離，重啟用時可重建）
    private class MissionEntry
    {
        public int id;
        public string title;
        public string desc;
        public int current;
        public int goal;
    }

    private UIDocument _uiDocument;
    private VisualElement _missionPanel;

    // key = 任意 ID（你傳什麼都行，例如 0/1/2）
    private Dictionary<int, VisualElement> _rows = new();
    private Dictionary<int, MissionEntry> _data = new();
    private List<int> _order = new();
    private int _focusIndex = 0;

    private void OnEnable()
    {
        // UIDocument 在 OnEnable 才建立 rootVisualElement，而元件 OnEnable 執行順序不固定，
        // 所以延後一幀再跑，確保視覺樹已就緒
        StartCoroutine(InitNextFrame());
    }

    private IEnumerator InitNextFrame()
    {
        yield return null;
        EnsurePanelSynced();
    }

    /// <summary>
    /// 每次操作前呼叫：確保 _missionPanel 是當前 UIDocument 的，
    /// 並把 _data 裡的任務全部 rebuild 出 row。
    /// 解決兩個問題：
    /// (1) Client 端 AddMission 早於 OnEnable 協程跑完 → _missionPanel 還是 null
    /// (2) UIDocument 重建後快取的 _missionPanel 變孤兒參照（與 PlayerListManager 同樣的坑）
    /// </summary>
    private void EnsurePanelSynced()
    {
        if (_uiDocument == null) _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null) return;

        var root = _uiDocument.rootVisualElement;
        if (root == null) return;

        var currentPanel = root.Q<VisualElement>("MissionPanel");
        if (currentPanel == null) return;

        // 已是最新且 row 數量正確 → 不用做事
        if (currentPanel == _missionPanel && _rows.Count == _order.Count) return;

        // panel 變了（UIDocument 重建）或 row 缺漏 → 整個從 _data 重建
        _missionPanel = currentPanel;
        _missionPanel.Clear();
        _rows.Clear();

        foreach (var id in _order)
        {
            if (!_data.TryGetValue(id, out var entry)) continue;
            var row = BuildRow(entry.id, entry.title, entry.desc, entry.current, entry.goal);
            _missionPanel.Add(row);
            _rows[id] = row;
        }
        RefreshFocus();
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
        // 資料先寫入 _data（即使此刻 panel 尚未就緒也不會掉資料）
        if (!_data.ContainsKey(id))
        {
            _data[id] = new MissionEntry { id = id, title = title, desc = desc, current = current, goal = goal };
            _order.Add(id);
        }

        // 第一列自動 focus
        if (_order.Count == 1) _focusIndex = 0;

        // 嘗試取得最新的 panel；若 panel 變了或 row 缺漏會自動 rebuild
        EnsurePanelSynced();

        if (_missionPanel == null) return;
        if (_rows.ContainsKey(id)) { RefreshFocus(); return; }

        var row = BuildRow(id, title, desc, current, goal);
        _missionPanel.Add(row);
        _rows[id] = row;
        RefreshFocus();
    }

    /// <summary>更新進度數字</summary>
    public void UpdateProgress(int id, int current, int goal)
    {
        if (_data.TryGetValue(id, out var entry))
        {
            entry.current = current;
            entry.goal = goal;
        }

        // 確保 panel 是當前的；如果之前 row 沒建出來，這裡會補建
        EnsurePanelSynced();

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
        if (_data.TryGetValue(id, out var entry))
        {
            entry.title = title;
            entry.desc = desc;
            entry.current = current;
            entry.goal = goal;
        }

        // 補建漏掉的 row（與 AddMission 同理）
        EnsurePanelSynced();

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
        _data.Remove(id);
        _order.Remove(id);

        if (_rows.TryGetValue(id, out var row))
        {
            if (_missionPanel != null && row.parent == _missionPanel)
                _missionPanel.Remove(row);
            _rows.Remove(id);
        }

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