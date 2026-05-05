using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class GameResultUI : MonoBehaviour
{
    private VisualElement root;
    
    public void RefreshDisplay()
    {
        
        root = GetComponent<UIDocument>().rootVisualElement;

        Debug.Log("嘗試更新結算 UI...");
    
        if (GameManager.CurrentWinnerData == null)
        {
            Debug.LogError("更新失敗：GameManager.CurrentWinnerData 是 Null！");
            return;
        }

        Debug.Log($"找到贏家資料：ID={GameManager.CurrentWinnerData.winnerID}, 道具數量={GameManager.CurrentWinnerData.cardUsages.Count}");
        
        // 1. 更新贏家名稱
        var winnerNameLabel = root.Q<Label>("WinnerName");
        if (winnerNameLabel != null)
        {
            // 這裡可以根據 winnerID 從 PlayerInventoryManager 抓取玩家名稱
            winnerNameLabel.text = $"Player {GameManager.CurrentWinnerData.winnerID}";
        }


        var itemIconsContainer = root.Q<VisualElement>("item-icons-container");
        // 2. 清除舊有的道具圖示
        itemIconsContainer.Clear();

        // 3. 抓取道具使用資料並生成 UI
        foreach (var usage in GameManager.CurrentWinnerData.cardUsages)
        {
            // 建立單個道具的容器
            VisualElement miniItem = new VisualElement();
            miniItem.AddToClassList("mini-item");

            // 建立道具圖片
            Image itemImg = new Image();
            itemImg.AddToClassList("item-img");
            itemImg.sprite = usage.image; // 使用 BuildWinnerData 抓取的圖片
            miniItem.Add(itemImg);

            // 建立使用次數標籤
            Label itemCount = new Label();
            itemCount.AddToClassList("item-count");
            itemCount.text = $"x{usage.useCount}";
            miniItem.Add(itemCount);

            // 加入到 ScrollView 的容器中[cite: 4]
            itemIconsContainer.Add(miniItem);
        }
        root.MarkDirtyRepaint();
    }
}