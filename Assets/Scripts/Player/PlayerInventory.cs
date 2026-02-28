using System;
using System.Collections.Generic;
using Fusion;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour // ✅ 必須繼承 NetworkBehaviour 才能使用 [Networked]
{
    public const int MaxSlots = 6; // 6 格背包

    [Networked, Capacity(MaxSlots)]
    public NetworkArray<CardData> slotsNetworked => default;
    [Networked, OnChangedRender(nameof(NotifyChange))] public int InventoryVersion { get; set; }

    public CardData[] slots = new CardData[MaxSlots];

    public List<CardData> lostCards = new List<CardData>();
    public bool IsProtecting = false;
    private float ProtectingTime = 0f;
    public bool dropAll = false;



    public override void Spawned()
    {

        if (Object.HasStateAuthority) // 只有 Host 初始化欄位，Client 會自動收到同步值
        {
            for (int i = 0; i < MaxSlots; i++)
            {
                slotsNetworked.Set(i, CardData.Empty());
            }
            UpdateLocalSlot();  // 本地初始化
            NotifyChange();
            InventoryVersion = 0;

        }


    }
    public override void FixedUpdateNetwork()
    {
        if (IsProtecting)
        {
            ProtectingTime += Runner.DeltaTime;
            if (ProtectingTime >= 10f)
            {
                IsProtecting = false;
                ProtectingTime = 0f;
            }

        }
    }



    /// <summary>將卡片加入第一個空格（僅 Host 可寫）</summary>
    public bool AddCard(CardData card)
    {
        if (!Object.HasStateAuthority) { Debug.LogWarning("[Inventory] AddCard 僅 Host 可寫"); return false; }

        for (int i = 0; i < MaxSlots; i++)
        {
            if (slots[i].IsEmpty())
            {
                slotsNetworked.Set(i, card);
                Debug.Log($"[Inventory] 加到 {i}: {card.type}, ID={card.id}");
                NotifyChange();
                InventoryVersion++;

                return true;
            }
        }
        return false; // 滿了
    }

    /// <summary>指定索引取得卡片（Client 可讀）</summary>
    public CardData GetCard(int index)
    {
        if (index >= 0 && index < MaxSlots) return slots[index];
        Debug.LogError("[Inventory] 索引超出範圍");
        return CardData.Empty();
    }

    /// <summary>移除指定格的卡片（僅 Host 可寫）</summary>
    public void RemoveCard(int index)
    {
        if (!Object.HasStateAuthority) { Debug.LogWarning("[Inventory] RemoveCard 僅 Host 可寫"); return; }
        if (index < 0 || index >= MaxSlots) { Debug.LogError("[Inventory] 索引超出範圍"); return; }
        if (slots[index].IsEmpty()) return;

        Debug.Log($"[Inventory] 移除第 {index} 格: {slots[index].type}, ID={slots[index].id}");
        slotsNetworked.Set(index, CardData.Empty());
        NotifyChange();
        InventoryVersion++;

    }

    /// <summary>將 index 的卡片置換成 newCard（僅 Host 可寫）</summary>
    public void ReplaceCard(int index, CardData newCard)
    {
        if (!Object.HasStateAuthority) { Debug.LogWarning("[Inventory] ReplaceCard 僅 Host 可寫"); return; }
        if (index < 0 || index >= MaxSlots) { Debug.LogError("[Inventory] 索引超出範圍"); return; }

        slotsNetworked.Set(index, newCard);
        Debug.Log($"[Inventory] 置換第 {index} 格為: {newCard.type}, ID={newCard.id}");
        NotifyChange();
        InventoryVersion++;

    }

    /// <summary>清空全部（僅 Host 可寫）</summary>
    public void ClearAll()
    {
        if (!Object.HasStateAuthority) { Debug.LogWarning("[Inventory] ClearAll 僅 Host 可寫"); return; }

        bool changed = false;
        for (int i = 0; i < MaxSlots; i++)
        {
            if (!slots[i].IsEmpty())
            {

                slotsNetworked.Set(i, CardData.Empty());
                changed = true;
            }
        }
        if (changed)
        {
            Debug.Log("[Inventory] 清空完成");

        }
    }

    /// <summary>是否已滿（Client 可讀）</summary>
    public bool IsFull()
    {
        for (int i = 0; i < MaxSlots; i++)
            if (slots[i].IsEmpty())
                return false;
        return true;
    }

    /// <summary>回傳複本陣列（Client 可讀）</summary>
    public CardData[] GetAllCards()
    {
        var arr = new CardData[MaxSlots];
        for (int i = 0; i < MaxSlots; i++) arr[i] = slots[i];
        return arr;
    }

    /// <summary>隨機取一張卡（Client 可讀）</summary>
    public CardData RandomGetCard()
    {
        List<int> validIndices = new List<int>();

        for (int i = 0; i < MaxSlots; i++)
        {
            if (!slots[i].IsEmpty())
                validIndices.Add(i);
        }

        if (validIndices.Count == 0)
            return CardData.Empty();

        int randomIndex = UnityEngine.Random.Range(0, validIndices.Count);
        int slotIndex = validIndices[randomIndex];

        CardData result = slots[slotIndex];
        RemoveCard(slotIndex);

        return result;
    }

    /// <summary>是否擁有該卡（Client 可讀）</summary>
    public bool HasCard(CardData card)
    {
        for (int i = 0; i < MaxSlots; i++)
            if (slots[i].id == card.id && slots[i].type == card.type)
                return true;
        return false;
    }

    /// <summary>遺失 1~3 張卡並丟到場景（僅 Host 可寫）</summary>
    public void LostCard()
    {
        if (!Object.HasStateAuthority) { Debug.LogWarning("[Inventory] LostCard 僅 Host 可寫"); return; }
        if (IsProtecting)
        {
            Debug.Log("[Inventory] 正在保護中，無法遺失卡片");
            Protect(false);
            return;
        }

        // 清除上次紀錄
        lostCards.Clear();
        List<int> nonEmptyIndexes = new List<int>();
        for (int i = 0; i < MaxSlots; i++)
            if (!slots[i].IsEmpty()) nonEmptyIndexes.Add(i);

        if (nonEmptyIndexes.Count == 0)
        {
            Debug.Log("[Inventory] 沒有卡可以遺失");
            return;
        }

        System.Random rand = new System.Random();
        int lostCount;

        if (dropAll)
        {
            // ✅ 噴光全部
            lostCount = nonEmptyIndexes.Count;
            dropAll = false; 
        }
        else
        {

            lostCount = Mathf.Min(rand.Next(1, 4), nonEmptyIndexes.Count);
        }


        for (int i = 0; i < lostCount; i++)
        {
            int randomIdx = rand.Next(nonEmptyIndexes.Count);
            int slotIndex = nonEmptyIndexes[randomIdx];
            nonEmptyIndexes.RemoveAt(randomIdx);

            CardData lostCard = slots[slotIndex];
            lostCards.Add(lostCard); // ✅ 儲存遺失的卡資料

            Debug.Log($"[Inventory] 遺失第 {slotIndex} 格: {lostCard.type}, ID={lostCard.id}");

            slotsNetworked.Set(slotIndex, CardData.Empty());
        }

        // 丟到場景（你原本的流程）
        var ragdoll = this.gameObject.transform.Find("Ragdoll");
        if (ragdoll != null)
            ObjectSpawner.Instance.LostCard(ragdoll, lostCards);
        else
            Debug.LogWarning("[Inventory] 找不到 Ragdoll 變換節點，跳過掉落生成");

        Debug.Log($"[Inventory] 本次遺失了 {lostCards.Count} 張卡");
        NotifyChange();
        InventoryVersion++;
        TraceMission.Instance.ProcessPlayerCards();
    }
    private void NotifyChange()
    {
        Debug.Log("更新UI");
        UpdateLocalSlot();
        if (PlayerInventoryManager.Instance != null)
            PlayerInventoryManager.Instance.Refresh();
        if (Runner.LocalPlayer == this.GetComponent<NetworkPlayer>().PlayerId)
            LocalBackpack.Instance.UpdateCardImagesByInventory(this, CardManager.Instance.Catalog.cards);
    }
    private void UpdateLocalSlot()
    {
        for (int i = 0; i < MaxSlots; i++)
        {
            slots[i] = slotsNetworked[i];
        }
    }
    // Cooldown System------------------------------------------------------------------------
    [Networked, Capacity(10)]
    public NetworkDictionary<int, int> CardCooldownEndTick => default;
    public void SetCooldownEnd(CardData card)
    {
        if (!Object.HasStateAuthority)
            return;

        int cooldownTicks =
            Mathf.CeilToInt(card.cooldown / Runner.DeltaTime);

        int endTick = Runner.Tick + cooldownTicks;

        Debug.Assert(card.cardId >= 0 && card.cardId < 50,
            $"非法 cardId: {card.cardId}");
        CardCooldownEndTick.Set(card.cardId, endTick);
        Debug.Log($"[Inventory] 卡片 {card.type} (ID={card.id}) 進入冷卻，結束於 Tick {endTick}");
    }
    public bool CanUse(CardData card)
    {
        if (!CardCooldownEndTick.TryGet(card.cardId, out int endTick))
            return true; // 從未進 CD

        return Runner.Tick >= endTick;
    }
    public float GetRemainingCooldown(int index)
    {
        if (!CardCooldownEndTick.TryGet(slotsNetworked[index].cardId, out int endTick))
            return 0f;

        int remainingTicks = Mathf.Max(0, endTick - Runner.Tick);
        return remainingTicks * Runner.DeltaTime;
    }
    public void Protect(bool value)
    {
        if (value)
        {
            IsProtecting = true;
            ProtectingTime = 0f;
        }
        else
        {
            IsProtecting = false;
            ProtectingTime = 0f;
        }
    }
    //Mission UI------------------------------------------------------------------------
    [Networked, Capacity(20)]
    public NetworkDictionary<int, int> MissionStates => default;




}
