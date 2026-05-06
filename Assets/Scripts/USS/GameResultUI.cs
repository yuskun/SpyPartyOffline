using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class GameResultUI : MonoBehaviour
{
    private VisualElement root;
    
    public void RefreshDisplay()
    {
        
        root = GetComponent<UIDocument>().rootVisualElement;
        if (GameManager.CurrentWinnerData == null) return;
    
        // 1. 抓取贏家實體
        PlayerIdentify winner = GetPlayerByID(GameManager.CurrentWinnerData.winnerID);
        
        // 2. 更新名字
        var winnerNameLabel = root.Q<Label>("WinnerName");
        if (winnerNameLabel != null)
        {
            // 如果抓不到玩家實體，保底顯示 "ID: 1" 之類的
            winnerNameLabel.text = winner != null ? winner.PlayerName : $"Player {GameManager.CurrentWinnerData.winnerID}";
        }
    
        // 3. 更新頭像
        var winnerAvatar = root.Q<Image>("WinnerAvatar");
        if (winnerAvatar != null && winner != null)
        {
            // 載入你存放在 Resources 的資料庫
            var database = Resources.Load<CharacterAvatarData>("Characters/CharacterAvatarData");
            if (database != null)
            {
                // 使用 winner 身上同步的 SkinIndex
                winnerAvatar.sprite = database.GetAvatar(winner.SkinIndex);
            }
        }

        var itemIconsContainer = root.Q<VisualElement>(className: "item-icons-container");
        // 2. 清除舊有的道具圖示
        itemIconsContainer.Clear();

        // 3. 抓取道具使用資料並生成 UI
        // 在 GameResultUI.cs 的 foreach 迴圈內修改
        foreach (var usage in GameManager.CurrentWinnerData.cardUsages)
        {
            VisualElement miniItem = new VisualElement();
            miniItem.AddToClassList("mini-item");

            Image itemImg = new Image();
            itemImg.AddToClassList("item-img");
            // 1. 賦值 Sprite
            itemImg.sprite = usage.image; 

            // 2. 強制指定顯示模式，避免被預設樣式吃掉
            itemImg.style.width = 80; // 確保寬高與 USS 一致
            itemImg.style.height = 80;

            // 這裡很重要：如果 Sprite 有抓到，這行會確保它填滿
            itemImg.scaleMode = ScaleMode.ScaleToFit; 

            miniItem.Add(itemImg);

            Label itemCount = new Label();
            itemCount.AddToClassList("item-count");
            itemCount.text = $"x{usage.useCount}";
            miniItem.Add(itemCount);

            itemIconsContainer.Add(miniItem);
        }
        root.MarkDirtyRepaint();
    }

    private PlayerIdentify GetPlayerByID(int id)
    {
        // 找尋場景中所有的玩家識別腳本
        var players = FindObjectsOfType<PlayerIdentify>();
        foreach (var p in players)
        {
            if (p.PlayerID == id) return p;
        }
        return null;
    }
}