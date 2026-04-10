using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MissionPanelController : MonoBehaviour
{
    private UIDocument _uiDocument;
    private List<VisualElement> _tasks;
    private int _currentIndex = -1; 

    private void OnEnable()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null) return;

        var root = _uiDocument.rootVisualElement;

        // 1. 抓取任務列表
        _tasks = root.Query<VisualElement>(className: "mission-task").ToList();

        // 2. 綁定滑鼠點擊
        for (int i = 0; i < _tasks.Count; i++)
        {
            int index = i; 
            _tasks[i].RegisterCallback<ClickEvent>(evt => ToggleTask(index));
        }

        // 3. 綁定鍵盤事件
        root.focusable = true;
        root.RegisterCallback<KeyDownEvent>(OnKeyDown);

        // --- 新增部分：預設邏輯 ---
        if (_tasks.Count > 0)
        {
            // 預設展開第一個 (索引為 0)
            ToggleTask(0);
            
            // 強制讓根節點獲得焦點，這樣一進遊戲按 Tab 才有反應
            root.Focus();
        }
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
        // 偵測 Tab 鍵
        if (evt.keyCode == KeyCode.Tab)
        {
            // 計算下一個索引 (循環邏輯)
            // 如果當前是 -1 (全關)，按下 Tab 會從 0 開始
            int nextIndex = (_currentIndex + 1) % _tasks.Count;
            ToggleTask(nextIndex);

            // 阻止事件冒泡，避免干擾其他 UI 組件
            evt.StopPropagation();
        }
    }

    private void ToggleTask(int index)
    {
        // 如果 index 超出範圍則不處理
        if (index < 0 || index >= _tasks.Count) return;

        // A. 檢查是否點擊了同一個已經展開的 (點第二次就關掉)
        bool isAlreadyActive = (index == _currentIndex);

        // B. 清除所有任務的 active 狀態
        foreach (var t in _tasks)
        {
            t.RemoveFromClassList("active");
        }

        // C. 切換狀態
        if (!isAlreadyActive)
        {
            _tasks[index].AddToClassList("active");
            _currentIndex = index;
        }
        else
        {
            _currentIndex = -1; // 全關狀態
        }
    }
}