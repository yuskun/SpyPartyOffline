using UnityEngine;

/// <summary>
/// 教學用本地 6 格背包，玩家跟假人都掛一份。
/// 沿用既有的 CardData struct（含 cardId/id/type/cooldown），
/// 但完全不靠 Fusion 同步，純 MonoBehaviour。
/// </summary>
public class TutorialInventory : MonoBehaviour
{
    public const int MaxSlots = 6;

    [SerializeField] private CardData[] slots = new CardData[MaxSlots];
    [SerializeField] private int selectedIndex = 0;

    public int SelectedIndex => selectedIndex;
    public int Capacity => MaxSlots;

    /// <summary>內容變動（增/移/換）</summary>
    public event System.Action OnInventoryChanged;
    /// <summary>選擇格切換</summary>
    public event System.Action<int> OnSelectedChanged;
    /// <summary>使用一張卡（外部 trigger 用）</summary>
    public event System.Action<int, CardData> OnCardUsed;

    void Awake()
    {
        // 全部初始化成 Empty()。CardData 預設值是 (id=0, type=Mission)，
        // 跟「空格」不是同一回事，會誤判，所以這邊統一砸成 Empty()。
        slots = new CardData[MaxSlots];
        for (int i = 0; i < MaxSlots; i++) slots[i] = CardData.Empty();
    }

    void Start()
    {
        // 保險：再 fire 一次事件，因為 UI 的 OnEnable 可能在這支 Awake 之前跑
        // （那時讀到的是 [SerializeField] 預設值而不是 Empty()）
        OnInventoryChanged?.Invoke();
    }

    // ---------- 存取 ----------
    public CardData GetCard(int index)
    {
        if (index < 0 || index >= MaxSlots) return CardData.Empty();
        return slots[index];
    }

    public CardData GetSelected() => GetCard(selectedIndex);

    public CardData[] GetAllCards()
    {
        var arr = new CardData[MaxSlots];
        System.Array.Copy(slots, arr, MaxSlots);
        return arr;
    }

    public bool IsFull()
    {
        for (int i = 0; i < MaxSlots; i++) if (slots[i].IsEmpty()) return false;
        return true;
    }

    public bool IsEmpty()
    {
        for (int i = 0; i < MaxSlots; i++) if (!slots[i].IsEmpty()) return false;
        return true;
    }

    public bool HasCard(CardData card)
    {
        for (int i = 0; i < MaxSlots; i++)
            if (slots[i].id == card.id && slots[i].type == card.type) return true;
        return false;
    }

    public int FirstEmptySlot()
    {
        for (int i = 0; i < MaxSlots; i++) if (slots[i].IsEmpty()) return i;
        return -1;
    }

    // ---------- 修改 ----------
    public bool AddCard(CardData card)
    {
        int idx = FirstEmptySlot();
        if (idx < 0) return false;
        slots[idx] = card;
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool SetCardAt(int index, CardData card)
    {
        if (index < 0 || index >= MaxSlots) return false;
        slots[index] = card;
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveAt(int index)
    {
        if (index < 0 || index >= MaxSlots) return false;
        if (slots[index].IsEmpty()) return false;
        slots[index] = CardData.Empty();
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>使用選中的那張卡：若不空，移除 + 觸發 OnCardUsed</summary>
    public bool UseSelected()
    {
        var card = GetSelected();
        if (card.IsEmpty()) return false;
        slots[selectedIndex] = CardData.Empty();
        OnCardUsed?.Invoke(selectedIndex, card);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void Clear()
    {
        bool any = false;
        for (int i = 0; i < MaxSlots; i++)
        {
            if (!slots[i].IsEmpty()) { slots[i] = CardData.Empty(); any = true; }
        }
        if (any) OnInventoryChanged?.Invoke();
    }

    // ---------- 選擇 ----------
    public void SelectIndex(int index)
    {
        if (index < 0 || index >= MaxSlots) return;
        if (selectedIndex == index) return;
        selectedIndex = index;
        OnSelectedChanged?.Invoke(index);
    }

    public void SelectNext() { SelectIndex((selectedIndex + 1) % MaxSlots); }
    public void SelectPrev() { SelectIndex((selectedIndex - 1 + MaxSlots) % MaxSlots); }

    /// <summary>處理鍵盤+滾輪輸入，切換選格。回傳是否有切換動作（可給 TutorialFlow 用來計次）</summary>
    public bool HandleSwitchInput()
    {
        // 1-6 數字鍵
        for (int k = 0; k < MaxSlots; k++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + k))
            {
                if (selectedIndex != k) { SelectIndex(k); return true; }
                return false; // 按到當前，視為沒切換
            }
        }
        // 滾輪
        float sy = Input.mouseScrollDelta.y;
        if (sy > 0.01f)  { SelectPrev(); return true; }
        if (sy < -0.01f) { SelectNext(); return true; }
        return false;
    }
}
