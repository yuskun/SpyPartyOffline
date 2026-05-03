using UnityEngine;
using UnityEngine.UIElements;
using OodlesEngine;

/// <summary>
/// 把玩家身上的 TutorialInventory 連到 UIDocument 裡 #inventory 的 6 個 .slot。
/// 目前只處理選中切換（selected class）。Icon / cooldown 等 visual 之後再加。
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class TutorialInventoryUI : MonoBehaviour
{
    [Tooltip("玩家身上的 TutorialInventory；留空會自動抓場景上有 LocalPlayer 元件的物件")]
    [SerializeField] private TutorialInventory playerInventory;

    [Tooltip("背包整體預設隱藏，撿到第一張卡才顯示")]
    [SerializeField] private bool hideUntilFirstPickup = true;

    private UIDocument _doc;
    private VisualElement   _inventoryRoot;
    private VisualElement[] _slotEls  = new VisualElement[TutorialInventory.MaxSlots];
    private VisualElement[] _iconEls  = new VisualElement[TutorialInventory.MaxSlots];
    private Label[]         _idLabels = new Label[TutorialInventory.MaxSlots];

    void OnEnable()
    {
        _doc = GetComponent<UIDocument>();
        if (_doc == null || _doc.rootVisualElement == null) return;
        var root = _doc.rootVisualElement;

        _inventoryRoot = root.Q<VisualElement>("inventory");

        for (int i = 0; i < TutorialInventory.MaxSlots; i++)
        {
            var slot = root.Q<VisualElement>("slot-" + (i + 1));
            _slotEls[i] = slot;
            if (slot != null)
            {
                _iconEls[i]  = slot.Q<VisualElement>(className: "item-icon");
                _idLabels[i] = slot.Q<Label>(className: "cooldown-label");
            }
        }

        if (playerInventory == null) AutoFindPlayerInventory();
        if (playerInventory != null)
        {
            playerInventory.OnSelectedChanged += HandleSelectedChanged;
            playerInventory.OnInventoryChanged += HandleInventoryChanged;
            // 初始化
            HandleSelectedChanged(playerInventory.SelectedIndex);
            HandleInventoryChanged();
        }
    }

    void OnDisable()
    {
        if (playerInventory != null)
        {
            playerInventory.OnSelectedChanged -= HandleSelectedChanged;
            playerInventory.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    private void AutoFindPlayerInventory()
    {
        // 優先：找 LocalPlayer 上的 inventory
        var local = FindFirstObjectByType<LocalPlayer>();
        if (local != null)
        {
            playerInventory = local.GetComponent<TutorialInventory>();
            if (playerInventory == null)
            {
                // 沒掛就替它掛一個（safe default）
                playerInventory = local.gameObject.AddComponent<TutorialInventory>();
            }
            return;
        }

        // 退而求其次：場景中第一個 TutorialInventory
        var anyInv = FindFirstObjectByType<TutorialInventory>();
        if (anyInv != null) playerInventory = anyInv;
    }

    private void HandleSelectedChanged(int idx)
    {
        for (int i = 0; i < _slotEls.Length; i++)
        {
            if (_slotEls[i] == null) continue;
            if (i == idx) _slotEls[i].AddToClassList("selected");
            else          _slotEls[i].RemoveFromClassList("selected");
        }
    }

    private void HandleInventoryChanged()
    {
        if (playerInventory == null) return;
        for (int i = 0; i < TutorialInventory.MaxSlots; i++)
        {
            var card = playerInventory.GetCard(i);
            ApplySlotVisual(i, card);
        }
        // 重畫選中狀態保險
        HandleSelectedChanged(playerInventory.SelectedIndex);

        // 整個 hotbar 顯隱：空背包就藏起來
        if (_inventoryRoot != null && hideUntilFirstPickup)
        {
            _inventoryRoot.style.display = playerInventory.IsEmpty()
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }
    }

    /// <summary>依卡片內容更新 item-icon。優先用 CardManager 抓 sprite，找不到再 fallback 色塊+id。</summary>
    private void ApplySlotVisual(int slotIdx, CardData card)
    {
        var icon = _iconEls[slotIdx];
        var lbl  = _idLabels[slotIdx];

        if (card.IsEmpty())
        {
            if (icon != null)
            {
                // 空格時整個 item-icon 隱藏，避免 default theme 給的白圓底冒出來
                icon.style.display = DisplayStyle.None;
                icon.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0));
                icon.style.backgroundImage = new StyleBackground((Sprite)null);
            }
            if (lbl != null) { lbl.text = ""; lbl.style.display = DisplayStyle.None; }
            return;
        }
        else if (icon != null)
        {
            icon.style.display = DisplayStyle.Flex;
        }

        // 嘗試從 CardManager 找對應 Card SO 的 image
        Sprite sprite = null;
        if (CardManager.Instance != null)
        {
            var cardSO = CardManager.Instance.GetCardScriptObject(card);
            if (cardSO != null) sprite = cardSO.image;
        }

        if (icon != null)
        {
            if (sprite != null)
            {
                // 有真 sprite：清色塊、放圖
                icon.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0));
                icon.style.backgroundImage = new StyleBackground(sprite);
            }
            else
            {
                // fallback：依 CardType 給暫時色塊
                Color c;
                switch (card.type)
                {
                    case CardType.Mission:  c = new Color(0.95f, 0.40f, 0.40f); break;
                    case CardType.Function: c = new Color(0.36f, 0.78f, 1.00f); break;
                    case CardType.Item:     c = new Color(1.00f, 0.85f, 0.35f); break;
                    default:                c = new Color(0.7f,  0.7f,  0.7f);  break;
                }
                icon.style.backgroundColor = new StyleColor(c);
                icon.style.backgroundImage = new StyleBackground((Sprite)null);
            }
        }

        if (lbl != null)
        {
            // 有 sprite 就不顯示 id 了；fallback 才顯示
            if (sprite != null) { lbl.text = ""; lbl.style.display = DisplayStyle.None; }
            else
            {
                lbl.text = card.id.ToString();
                lbl.style.display = DisplayStyle.Flex;
                lbl.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0));
                lbl.style.color = new StyleColor(new Color(0.05f, 0.22f, 0.34f));
            }
        }
    }
}
