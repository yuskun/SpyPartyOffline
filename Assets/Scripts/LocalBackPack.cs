using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LocalBackpack : MonoBehaviour
{
    public static LocalBackpack Instance;
    public int FocusIndex = 0;
    public int SlotCount = 0;
    public List<Button> button = new List<Button>();
    public List<Image> images = new List<Image>();
    private bool canInvoke = true;

    private PlayerInventory userInventory; // 本地玩家
    public CardUseUIManager cardUseUIManager; // UI 控制器

    void Awake()
    {
        Instance = this;
        // 自動抓取子物件的 Button
        button.Clear();
        foreach (Transform child in transform)
        {
            Button btn = child.GetComponent<Button>();
            if (btn != null)
                button.Add(btn);
        }

        images.Clear();
        foreach (var btn in button)
        {
            var img = btn.transform.Find("CardImage")?.GetComponent<Image>();
            images.Add(img);
        }

        SlotCount = button.Count;

        if (userInventory == null)
        {
            var localPlayer = OodlesEngine.LocalPlayer.Instance;
            if (localPlayer != null)
            {
                userInventory = localPlayer.GetComponent<PlayerInventory>();
            }
        }
    }

    void Update()
    {
        HandleMouseScroll();
        HandleNumberKeys();
        UpdateButtonHighlight();
        HandleMouseClick();
    }

    void HandleMouseScroll()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > 0f)
        {
            FocusIndex--;
            if (FocusIndex < 0)
                FocusIndex = SlotCount - 1;
        }
        else if (scroll < 0f)
        {
            FocusIndex++;
            if (FocusIndex >= SlotCount)
                FocusIndex = 0;
        }
    }

    void HandleNumberKeys()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
                FocusIndex = i;
        }
    }

    void UpdateButtonHighlight()
    {
        for (int i = 0; i < button.Count; i++)
        {
            ColorBlock cb = button[i].colors;
            cb.normalColor = (i == FocusIndex) ? Color.yellow : Color.white;
            button[i].colors = cb;
        }
    }

    void HandleMouseClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (FocusIndex >= 0 && FocusIndex < button.Count && button[FocusIndex].interactable)
            {
                // 取得目標玩家（用 PlayerScanner）
                var scanner = FindObjectOfType<PlayerScanner>();
                PlayerInventory targetInventory = null;
                if (scanner != null && scanner.currentTarget != null)
                    targetInventory = scanner.currentTarget.GetComponent<PlayerInventory>();

                if (userInventory == null)
                {
                    Debug.LogError("userInventory 為 null，請檢查初始化！");
                    return;
                }
                if (cardUseUIManager == null)
                {
                    Debug.LogError("cardUseUIManager 為 null，請在 Inspector 拖曳！");
                    return;
                }

                // 取得目前選中的卡片資料
                var data = userInventory.slots[FocusIndex];
                var card = CardManager.Instance.Catalog.cards.Find(c =>
                    c.cardData.id == data.id && c.cardData.type == data.type
                );

                if (card is FunctionCard functionCard)
                {
                    cardUseUIManager.TryUseFunctionCard(
                        functionCard,
                        userInventory,
                        targetInventory
                    );
                }
                else
                {
                    // 不是功能卡就直接觸發原本的按鈕事件
                    button[FocusIndex].onClick.Invoke();
                }
            }
        }
    }

    public void DisableInteractable()
    {
        for (int i = 0; i < button.Count; i++)
        {
            button[i].interactable = false;
        }
    }

    public void EnableInteractable()
    {
        for (int i = 0; i < button.Count; i++)
        {
            button[i].interactable = true;
        }
    }

    /// <summary>
    /// 根據 PlayerInventory 的 slots 內容，顯示對應卡片圖片
    /// </summary>
    /// <param name="inv">玩家背包</param>
    /// <param name="allCards">所有 Card ScriptableObject 的 List</param>
    public void UpdateCardImagesByInventory(PlayerInventory inv, List<Card> allCards)
    {
        if (inv != userInventory)
            return;

        for (int i = 0; i < images.Count; i++)
        {
            var data = inv.slots[i];
            if (!data.IsEmpty())
            {
                // 找到對應的 Card
                var card = allCards.Find(c =>
                    c.cardData.id == data.id && c.cardData.type == data.type
                );
                if (card != null && card.image != null)
                {
                    images[i].sprite = card.image;
                    images[i].gameObject.SetActive(true);
                }
                else
                {
                    images[i].sprite = null;
                    images[i].gameObject.SetActive(false);
                }
            }
            else
            {
                images[i].sprite = null;
                images[i].gameObject.SetActive(false);
            }
        }
    }
}
