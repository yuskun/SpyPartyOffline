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

        var data = GameManager.CurrentWinnerData;
        var winnerIds = (data.winnerIDs != null && data.winnerIDs.Count > 0)
            ? data.winnerIDs
            : new List<int> { data.winnerID };
        bool isMulti = winnerIds.Count > 1;

        var winnerSection = root.Q<VisualElement>("WinnerSection");
        var statsRow = root.Q<VisualElement>("StatsRow");
        var avatarDb = Resources.Load<CharacterAvatarData>("Characters/CharacterAvatarData");

        // 重建 WinnerSection（避免上一場單人/多人佈局殘留）
        if (winnerSection != null)
        {
            winnerSection.Clear();
            winnerSection.RemoveFromClassList("multi-winners");
        }

        if (isMulti)
        {
            // 多人勝利：列出所有勝利者頭像 + 名字，隱藏下方統計
            if (statsRow != null) statsRow.style.display = DisplayStyle.None;
            if (winnerSection != null)
            {
                winnerSection.AddToClassList("multi-winners");
                foreach (var wid in winnerIds)
                {
                    var pi = GetPlayerByID(wid);
                    winnerSection.Add(BuildWinnerCard(pi, wid, avatarDb, withCrown: false));
                }
            }
            root.MarkDirtyRepaint();
            return;
        }

        // ── 單人勝利 ──
        if (statsRow != null) statsRow.style.display = DisplayStyle.Flex;
        if (winnerSection != null)
        {
            var winner = GetPlayerByID(data.winnerID);
            winnerSection.Add(BuildWinnerCard(winner, data.winnerID, avatarDb, withCrown: true));
        }

        // 完成任務列表
        var taskList = root.Q<VisualElement>(className: "task-list");
        if (taskList != null)
        {
            taskList.Clear();
            foreach (var p in data.missionProgress)
            {
                var row = new VisualElement();
                row.AddToClassList("task-item-row");

                var nameLbl = new Label(p.title);
                nameLbl.AddToClassList("task-name");

                var valueLbl = new Label();
                valueLbl.AddToClassList("task-value");
                valueLbl.text = (p.goal <= 0)
                    ? "完成"
                    : (p.current >= p.goal ? "完成" : $"{p.current}/{p.goal}");

                row.Add(nameLbl);
                row.Add(valueLbl);
                taskList.Add(row);
            }
        }

        // 道具使用
        var itemIconsContainer = root.Q<VisualElement>(className: "item-icons-container");
        if (itemIconsContainer != null)
        {
            itemIconsContainer.Clear();

            if (data.cardUsages.Count == 0)
            {
                var emptyLbl = new Label("沒有使用任何道具");
                emptyLbl.AddToClassList("item-empty-text");
                itemIconsContainer.Add(emptyLbl);
            }
            else
            {
                foreach (var usage in data.cardUsages)
                {
                    var miniItem = new VisualElement();
                    miniItem.AddToClassList("mini-item");

                    var itemImg = new Image();
                    itemImg.AddToClassList("item-img");
                    itemImg.sprite = usage.image;
                    itemImg.style.width = 80;
                    itemImg.style.height = 80;
                    itemImg.scaleMode = ScaleMode.ScaleToFit;
                    miniItem.Add(itemImg);

                    var itemCount = new Label();
                    itemCount.AddToClassList("item-count");
                    itemCount.text = $"x{usage.useCount}";
                    miniItem.Add(itemCount);

                    itemIconsContainer.Add(miniItem);
                }
            }
        }

        root.MarkDirtyRepaint();
    }

    /// <summary>建立一張「頭像 + 王冠 + 名字」卡片，與原 UXML 的 winner-section 子結構對齊。</summary>
    private VisualElement BuildWinnerCard(PlayerIdentify pi, int winnerId, CharacterAvatarData db, bool withCrown)
    {
        var card = new VisualElement();
        card.AddToClassList("winner-card");

        var wrap = new VisualElement();
        wrap.AddToClassList("winner-avatar-wrap");

        var avatar = new Image();
        avatar.AddToClassList("winner-avatar");
        if (pi != null && db != null)
            avatar.sprite = db.GetAvatar(pi.SkinIndex);
        wrap.Add(avatar);

        card.Add(wrap);

        if (withCrown)
        {
            var crown = new VisualElement();
            crown.AddToClassList("crown-icon");
            card.Add(crown);
        }

        var nameLbl = new Label();
        nameLbl.AddToClassList("winner-badge");
        nameLbl.text = pi != null ? pi.PlayerName : $"Player {winnerId}";
        card.Add(nameLbl);

        return card;
    }

    private PlayerIdentify GetPlayerByID(int id)
    {
        var players = FindObjectsOfType<PlayerIdentify>();
        foreach (var p in players)
        {
            if (p.PlayerID == id) return p;
        }
        return null;
    }
}
