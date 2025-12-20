using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MissionPanelController : MonoBehaviour
{
    private UIDocument _uiDocument;

    private void OnEnable()
    {
        // 1. 抓取掛在這個物件上的 UIDocument
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
        {
            Debug.LogError("請將此腳本掛在有 UIDocument 的 GameObject 上！");
            return;
        }

        // 2. 獲取根節點
        var root = _uiDocument.rootVisualElement;

        // 3. 抓取所有 class 為 "mission-task" 的元素 (那三個長條)
        List<VisualElement> tasks = root.Query<VisualElement>(className: "mission-task").ToList();

        // 4. 為每一個任務欄綁定點擊事件
        foreach (var task in tasks)
        {
            // RegisterCallback<ClickEvent> 等同於 HTML 的 addEventListener("click")
            task.RegisterCallback<ClickEvent>(evt => OnTaskClicked(task, tasks));
        }
    }

    // 點擊處理邏輯
    private void OnTaskClicked(VisualElement clickedTask, List<VisualElement> allTasks)
    {
        // A. 檢查這個被點的任務，現在是不是已經打開了？
        bool isAlreadyActive = clickedTask.ClassListContains("active");

        // B. 先把「所有」任務的 active 關掉 (實現手風琴效果：一次只開一個)
        // 如果你想允許同時開多個，就把這段 B 刪掉
        foreach (var t in allTasks)
        {
            t.RemoveFromClassList("active");
        }

        // C. 如果原本沒開，就幫它加上 active class
        // (因為上面步驟 B 已經全關了，所以這裡只要判斷原本狀態即可)
        if (!isAlreadyActive)
        {
            clickedTask.AddToClassList("active");
        }
    }
}