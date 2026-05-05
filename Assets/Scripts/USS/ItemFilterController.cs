using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class ItemFilterController : MonoBehaviour
{
    private VisualElement _root;
    private List<VisualElement> _allCards = new List<VisualElement>();

    void OnEnable()
    {
        // 1. 獲取 UI 根節點
        _root = GetComponent<UIDocument>().rootVisualElement;

        // 2. 找到所有的卡片並存入清單
        _allCards.AddRange(_root.Query<VisualElement>(className: "item-card").ToList());

        // 3. 註冊標籤按鈕的點擊事件
        // 假設你的按鈕 class 分別是 tab-all, tab-mission, tab-trap, tab-means
        RegisterTab("tab-all", "all");
        RegisterTab("tab-mission", "control-card"); // 對應 UXML 的 class
        RegisterTab("tab-trap", "trap-card");
        RegisterTab("tab-means", "means-card");
    }

    private void RegisterTab(string tabClassName, string filterCategory)
    {
        var tab = _root.Q<Button>(className: tabClassName);
        if (tab != null)
        {
            tab.clicked += () => FilterItems(filterCategory);
        }
    }

    private void FilterItems(string category)
    {
        foreach (var card in _allCards)
        {
            if (category == "all" || card.ClassListContains(category))
            {
                // 顯示：將 display 設為 Flex
                card.style.display = DisplayStyle.Flex;
            }
            else
            {
                // 隱藏：將 display 設為 None (不佔空間)
                card.style.display = DisplayStyle.None;
            }
        }
    }
}