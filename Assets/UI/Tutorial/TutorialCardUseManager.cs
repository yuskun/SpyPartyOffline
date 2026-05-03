using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 教學版的卡片使用 UI 控制器。對應場景上既有的 PeekUIPanel / GiveUIPanel / SwapUIPanel。
/// 跟原本的 CardUseUIManager_UIToolkit 不同處：
///   - 不使用 PlayerInventory（NetworkBehaviour）→ 改用 TutorialInventory
///   - 不依賴 PlayerIdentify / SkinChange / 網路 RPC
///   - 卡片使用後本地立即生效（移除自己卡 + 對 target 做事）
/// </summary>
public class TutorialCardUseManager : MonoBehaviour
{
    public static TutorialCardUseManager Instance { get; private set; }

    [Header("UI Panels（留空會用 GameObject.Find 自動抓）")]
    [SerializeField] private UniversalUIController peekUI;
    [SerializeField] private UniversalUIController giveUI;
    [SerializeField] private UniversalUIController swapUI;

    [Header("Optional 顯示用")]
    [SerializeField] private Sprite questionMarkIcon;
    [SerializeField] private string targetDisplayName = "假人";

    /// <summary>(card, user, target) — 卡片真正被使用（confirm）後 fire</summary>
    public event Action<Card, TutorialInventory, TutorialInventory> OnCardUsed;
    public event Action OnUIClosed;

    private Card _currentCard;
    private TutorialInventory _user;
    private TutorialInventory _target;
    private int _userCardIdx;
    private int _selectedUserIdx = -1;
    private bool _isOpen;

    void Awake()
    {
        Instance = this;
        if (peekUI == null) peekUI = FindPanel("PeekUIPanel");
        if (giveUI == null) giveUI = FindPanel("GiveUIPanel");
        if (swapUI == null) swapUI = FindPanel("SwapUIPanel");
    }

    private static UniversalUIController FindPanel(string name)
    {
        var go = GameObject.Find(name);
        return go != null ? go.GetComponent<UniversalUIController>() : null;
    }

    /// <summary>外部入口：嘗試對 target 使用 user 第 useCardIdx 格的卡片</summary>
    public bool TryUseFunctionCard(Card card, TutorialInventory user, TutorialInventory target, int useCardIdx)
    {
        if (_isOpen) return false;
        if (card == null || user == null || target == null) return false;

        _currentCard = card;
        _user = user; _target = target; _userCardIdx = useCardIdx;
        _selectedUserIdx = -1;

        if (card is Peek) { OpenPeek(); return true; }
        if (card is Give) { OpenGive(); return true; }
        if (card is Swap) { OpenSwap(); return true; }

        Debug.LogWarning($"[TutorialCardUse] 不支援的卡片類型：{card.GetType().Name}");
        return false;
    }

    // ─────────────────────────────────────────
    // Peek：純展示 target 的卡，按關閉就結束
    // ─────────────────────────────────────────
    private void OpenPeek()
    {
        if (peekUI == null) { Debug.LogWarning("[TutorialCardUse] PeekUIPanel 未設定"); return; }
        peekUI.ShowCurrentUI();
        _isOpen = true;

        var root = peekUI.GetComponent<UIDocument>().rootVisualElement;
        SetTargetName(root);
        var slots = root.Query<Button>(className: "item-slot").ToList();
        FillSlots(slots, _target, null, false);

        var closeBtn = root.Q<Button>("CloseBtn");
        if (closeBtn != null) closeBtn.clickable = new Clickable(() => Confirm(peekUI));
    }

    // ─────────────────────────────────────────
    // Give：玩家選一張 → 從自己背包移除 + 加到 target
    // ─────────────────────────────────────────
    private void OpenGive()
    {
        if (giveUI == null) { Debug.LogWarning("[TutorialCardUse] GiveUIPanel 未設定"); return; }
        giveUI.ShowCurrentUI();
        _isOpen = true;

        var root = giveUI.GetComponent<UIDocument>().rootVisualElement;
        SetTargetName(root);
        var slots = root.Query<Button>(className: "item-slot").ToList();
        var preselectIcon = root.Q<Image>("SelectedGiftIcon");
        if (preselectIcon != null) preselectIcon.style.backgroundImage = StyleKeyword.None;

        FillSlots(slots, _user, idx =>
        {
            _selectedUserIdx = idx;
            UpdateSelected(slots, idx);
            if (preselectIcon != null)
            {
                var icon = slots[idx].Q<Image>();
                if (icon != null) preselectIcon.style.backgroundImage = icon.style.backgroundImage;
                preselectIcon.style.display = DisplayStyle.Flex;
            }
        }, true);

        BindConfirmCancel(root, giveUI, () =>
        {
            if (_selectedUserIdx < 0) return;
            var card = _user.GetCard(_selectedUserIdx);
            if (card.IsEmpty()) return;
            _user.RemoveAt(_selectedUserIdx);
            _target.AddCard(card);
            Confirm(giveUI);
        });
    }

    // ─────────────────────────────────────────
    // Swap：玩家選一張 + 從 target 隨機抽一張交換
    // ─────────────────────────────────────────
    private void OpenSwap()
    {
        if (swapUI == null) { Debug.LogWarning("[TutorialCardUse] SwapUIPanel 未設定"); return; }
        swapUI.ShowCurrentUI();
        _isOpen = true;

        var root = swapUI.GetComponent<UIDocument>().rootVisualElement;
        SetTargetName(root);
        var panels = root.Query<VisualElement>(className: "item-panel").ToList();
        if (panels.Count < 2) return;

        var userSlots   = panels[0].Query<Button>(className: "item-slot").ToList();
        var targetSlots = panels[1].Query<Button>(className: "item-slot").ToList();

        FillSlots(userSlots, _user, idx =>
        {
            _selectedUserIdx = idx;
            UpdateSelected(userSlots, idx);
        }, true);

        // target side：固定問號（隨機交換不可選）
        foreach (var btn in targetSlots)
        {
            btn.SetEnabled(false);
            btn.style.opacity = 0.7f;
            var icon = btn.Q<Image>();
            if (icon != null)
            {
                if (questionMarkIcon != null) icon.style.backgroundImage = new StyleBackground(questionMarkIcon);
                else icon.style.backgroundImage = StyleKeyword.None;
                icon.style.display = DisplayStyle.Flex;
            }
        }

        BindConfirmCancel(root, swapUI, () =>
        {
            if (_selectedUserIdx < 0) return;
            var userCard = _user.GetCard(_selectedUserIdx);
            if (userCard.IsEmpty()) return;

            int targetIdx = PickRandomNonEmpty(_target);
            if (targetIdx < 0)
            {
                // target 全空 → 直接給
                _user.RemoveAt(_selectedUserIdx);
                _target.AddCard(userCard);
            }
            else
            {
                var targetCard = _target.GetCard(targetIdx);
                _target.SetCardAt(targetIdx, userCard);
                _user.SetCardAt(_selectedUserIdx, targetCard);
            }
            Confirm(swapUI);
        });
    }

    // ─────────────────────────────────────────
    // 共用工具
    // ─────────────────────────────────────────
    private void FillSlots(List<Button> slots, TutorialInventory inv, Action<int> onSelect, bool selectable)
    {
        Sprite[] sprites = (CardManager.Instance != null)
            ? CardManager.Instance.GetCardInfo(inv.GetAllCards())
            : new Sprite[0];

        for (int i = 0; i < slots.Count; i++)
        {
            int idx = i;
            var btn = slots[i];
            btn.RemoveFromClassList("selected");
            btn.style.display = DisplayStyle.Flex;

            var icon = btn.Q<Image>();
            bool hasCard = i < sprites.Length && sprites[i] != null;
            if (hasCard)
            {
                if (icon != null)
                {
                    // Image 元件用 .sprite field 才會真的渲染（單靠 style.backgroundImage 對 Image 不可靠）
                    icon.sprite = sprites[i];
                    icon.style.backgroundImage = new StyleBackground(sprites[i]);
                    icon.style.display = DisplayStyle.Flex;
                }
                bool disabled = (inv == _user && i == _userCardIdx);
                btn.SetEnabled(selectable && !disabled);
                btn.style.opacity = disabled ? 0.5f : 1f;
                if (selectable && !disabled && onSelect != null)
                    btn.clickable = new Clickable(() => onSelect(idx));
            }
            else
            {
                if (icon != null)
                {
                    icon.sprite = null;
                    icon.style.backgroundImage = StyleKeyword.None;
                }
                btn.SetEnabled(false);
                btn.style.opacity = 1f;
            }
        }
    }

    private void UpdateSelected(List<Button> slots, int selectedIdx)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (i == selectedIdx) slots[i].AddToClassList("selected");
            else slots[i].RemoveFromClassList("selected");
        }
    }

    private void BindConfirmCancel(VisualElement root, UniversalUIController ui, Action onConfirm)
    {
        var confirm = root.Q<Button>("ConfirmBtn");
        var cancel  = root.Q<Button>("CancelBtn");
        if (confirm != null) confirm.clickable = new Clickable(() => onConfirm());
        if (cancel  != null) cancel.clickable  = new Clickable(() => Cancel(ui));
    }

    private void SetTargetName(VisualElement root)
    {
        var nameLbls = root.Query<Label>(className: "player-name-bottom").ToList();
        if (nameLbls.Count == 0) return;
        // Peek 只有一個 player-name → 是 target
        // Give/Swap 有兩個（自己 + 對方）→ 取最後一個當 target
        nameLbls.Last().text = targetDisplayName;
    }

    private int PickRandomNonEmpty(TutorialInventory inv)
    {
        var indices = new List<int>();
        for (int i = 0; i < TutorialInventory.MaxSlots; i++)
            if (!inv.GetCard(i).IsEmpty()) indices.Add(i);
        if (indices.Count == 0) return -1;
        return indices[UnityEngine.Random.Range(0, indices.Count)];
    }

    /// <summary>確認使用 — 移除使用的卡 + fire OnCardUsed event</summary>
    private void Confirm(UniversalUIController ui)
    {
        // 把使用中的那張從玩家身上移除（消耗一張）
        if (_user != null && _userCardIdx >= 0)
        {
            var used = _user.GetCard(_userCardIdx);
            if (!used.IsEmpty()) _user.RemoveAt(_userCardIdx);
        }
        var card = _currentCard;
        var u = _user; var t = _target;
        Close(ui);
        OnCardUsed?.Invoke(card, u, t);
    }

    private void Cancel(UniversalUIController ui)
    {
        Close(ui);
    }

    private void Close(UniversalUIController ui)
    {
        if (ui != null) ui.HideCurrentUI();
        _selectedUserIdx = -1;
        _currentCard = null;
        _isOpen = false;
        OnUIClosed?.Invoke();
    }

    public void ForceCloseAll()
    {
        if (peekUI != null) peekUI.HideCurrentUI();
        if (giveUI != null) giveUI.HideCurrentUI();
        if (swapUI != null) swapUI.HideCurrentUI();
        _selectedUserIdx = -1;
        _currentCard = null;
        _isOpen = false;
    }
}
