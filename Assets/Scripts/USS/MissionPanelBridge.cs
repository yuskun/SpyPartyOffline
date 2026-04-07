using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MissionPanelBridge : MonoBehaviour
{
    private UIDocument _doc;
    private VisualElement _panel;
    private Dictionary<int, VisualElement> _taskElements = new Dictionary<int, VisualElement>();
    private List<int> _order = new List<int>();
    private int _focusIndex = -1;

    void OnEnable()
    {
        _doc = GetComponent<UIDocument>();
        _panel = _doc.rootVisualElement.Q<VisualElement>("MissionPanel");
        if (_panel != null) _panel.Clear();
    }

    // 確保有 4 個參數：id, title, desc, goal
    public void AddMission(int id, string title, string desc, int goal)
    {
        if (_taskElements.ContainsKey(id)) return;

        // 建立 UI 節點
        VisualElement taskNode = new VisualElement();
        taskNode.AddToClassList("mission-task");

        VisualElement leftBox = new VisualElement();
        leftBox.AddToClassList("task-left");

        Label titleLabel = new Label(title) { name = "MissionTitle" };
        titleLabel.AddToClassList("task-title");

        Label descLabel = new Label(desc) { name = "MissionDesc" };
        descLabel.AddToClassList("task-desc");

        leftBox.Add(titleLabel);
        leftBox.Add(descLabel);

        Label progressLabel = new Label("") { name = "MissionProgress" };
        progressLabel.AddToClassList("task-progress");

        taskNode.Add(leftBox);
        taskNode.Add(progressLabel);
        taskNode.RegisterCallback<ClickEvent>(evt => OnTaskClicked(id));

        _panel.Add(taskNode);
        _taskElements.Add(id, taskNode);
        _order.Add(id);
        
        UpdateDisplay(id, title, desc, 0, goal); // 初始更新
        RefreshFocus();
    }

    // 新增此函式以解決 CS1061 報錯
    public void UpdateDisplay(int id, string title, string desc, int current, int goal)
    {
        if (!_taskElements.TryGetValue(id, out var el)) return;

        el.Q<Label>("MissionTitle").text = title;
        el.Q<Label>("MissionDesc").text = desc;
        
        var progressLabel = el.Q<Label>("MissionProgress");
        if (goal <= 0) progressLabel.text = "";
        else progressLabel.text = (current >= goal) ? "完成" : $"{current}/{goal}";
    }

    public void UpdateProgress(int id, int current, int goal)
    {
        UpdateDisplay(id, null, null, current, goal);
    }

    public void RemoveMission(int id)
    {
        if (_taskElements.TryGetValue(id, out var el))
        {
            _panel.Remove(el);
            _taskElements.Remove(id);
            _order.Remove(id);
            RefreshFocus();
        }
    }

    private void OnTaskClicked(int id)
    {
        _focusIndex = _order.IndexOf(id);
        RefreshFocus();
    }

    private void RefreshFocus()
    {
        for (int i = 0; i < _order.Count; i++)
        {
            int id = _order[i];
            if (i == _focusIndex) _taskElements[id].AddToClassList("active");
            else _taskElements[id].RemoveFromClassList("active");
        }
    }
}