using System;
using System.Xml.Serialization;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private const int MaxSlots = 6; // 6 格背包
    public int playerId;
    public CardData[] slots = new CardData[MaxSlots];

    private void Awake()
    {
        // 初始化 6 格為 Empty
        for (int i = 0; i < MaxSlots; i++)
            slots[i] = CardData.Empty();
    }

    /// <summary>將卡片加入第一個空格</summary>
    public void AddCard(CardData card)
    {
        for (int i = 0; i < MaxSlots; i++)
        {
            if (slots[i].IsEmpty())
            {
                slots[i] = card;
                Debug.Log($"[Inventory] 加到 {i}: {card.type}, ID={card.id}");
                NotifyChanged();
                return; // 找到第一個空格後就直接結束
            }
        }

        // 如果跑完都沒有空格
        Debug.LogWarning("[Inventory] 滿了，無法加入新卡");
    }

    /// <summary>指定索引取得卡片（越界回 Empty）</summary>
    public CardData GetCard(int index)
    {
        if (index >= 0 && index < MaxSlots)
            return slots[index];
        Debug.LogError("[Inventory] 索引超出範圍");
        return CardData.Empty();
    }

    /// <summary>移除指定格的卡片</summary>
    public void RemoveCard(int index)
    {
        if (index < 0 || index >= MaxSlots)
        {
            Debug.LogError("[Inventory] 索引超出範圍");
            return;
        }
        if (slots[index].IsEmpty())
            return;

        Debug.Log($"[Inventory] 移除第 {index} 格: {slots[index].type}, ID={slots[index].id}");
        slots[index] = CardData.Empty();
        NotifyChanged();
    }

    /// <summary>將 index 的卡片置換成 newCard</summary>
    public void ReplaceCard(int index, CardData newCard)
    {
        if (index < 0 || index >= MaxSlots)
        {
            Debug.LogError("[Inventory] 索引超出範圍");
            return;
        }
        slots[index] = newCard;
        Debug.Log($"[Inventory] 置換第 {index} 格為: {newCard.type}, ID={newCard.id}");
        NotifyChanged();
    }

    public void ClearAll()
    {
        bool changed = false;
        for (int i = 0; i < MaxSlots; i++)
        {
            if (!slots[i].IsEmpty())
            {
                slots[i] = CardData.Empty();
                changed = true;
            }
        }
        if (changed)
        {
            Debug.Log("[Inventory] 清空完成");
            NotifyChanged();
        }
    }

    /// <summary>是否已滿</summary>
    public bool IsFull()
    {
        for (int i = 0; i < MaxSlots; i++)
            if (slots[i].IsEmpty())
                return false;
        return true;
    }

    /// <summary>統一對外廣播的入口（可在此做 debounce/batch）</summary>
    private void NotifyChanged()
    {
        PlayerInventoryManager.Instance.Refresh();
        LocalBackpack.Instance.UpdateCardImagesByInventory(this,CardManager.Instance.Catalog.cards);
    }

    public CardData[] GetAllCards()
    {
        return slots;
    }

    public CardData RandomGetCard()
    {
        System.Random rand = new System.Random();
        int index = rand.Next(0, MaxSlots);
        if (slots[index].IsEmpty())
        {
            for (int i = 0; i < MaxSlots; i++)
            {
                if (!slots[i].IsEmpty())
                {
                    return slots[i];
                }
            }
            return CardData.Empty();
        }
        return slots[index];
    }

    public bool HasCard(CardData Card)
    {
        for (int i = 0; i < MaxSlots; i++)
        {
            if (slots[i].id == Card.id && slots[i].type == Card.type)
            {
                return true;
            }
        }
        return false;
    }
}
