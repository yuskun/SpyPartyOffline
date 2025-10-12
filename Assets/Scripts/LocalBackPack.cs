using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LocalBackpack : MonoBehaviour
{
    public static LocalBackpack Instance;
    public int FocusIndex = 0;
    public int SlotCount = 0;
    public List<ButtionData> buttons = new List<ButtionData>();

    private bool canInvoke = true;

    private PlayerInventory userInventory; // 本地玩家
    public CardUseUIManager cardUseUIManager; // UI 控制器

    void Awake()
    {
        Instance = this;
        buttons.Clear();

        foreach (Transform child in transform)
        {
            Button btn = child.GetComponent<Button>();
            if (btn != null)
            {
                ButtionData data = new ButtionData();
                data.button = btn;

                // 嘗試抓 CardImage
                data.image = btn.transform.Find("CardImage")?.GetComponent<Image>();

                // 確保 Outline 存在
                data.outline = btn.gameObject.GetComponent<UnityEngine.UI.Outline>();
                if (data.outline == null) data.outline = btn.gameObject.AddComponent<UnityEngine.UI.Outline>();
                data.outline.enabled = false;

                // 嘗試抓 Shadow
                data.shadow = btn.gameObject.GetComponent<Shadow>();
                if (data.shadow != null) data.shadow.enabled = true;

                buttons.Add(data);
            }
        }

        SlotCount = buttons.Count;

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
        for (int i = 0; i < buttons.Count; i++)
        {

            if (i == FocusIndex)
            {
                // 開啟 Outline
                buttons[i].outline.enabled = true;
                buttons[i].shadow.enabled = false;

                // 放大
                buttons[i].button.transform.localScale = Vector3.one * 1.15f;
            }
            else
            {
                // 關閉 Outline
                buttons[i].outline.enabled = false;
                buttons[i].shadow.enabled = true;
                // 恢復原始大小
                buttons[i].button.transform.localScale = Vector3.one;
            }
        }
    }

    void HandleMouseClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (FocusIndex >= 0 && FocusIndex < buttons.Count && buttons[FocusIndex].button.interactable)
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
                    cardUseUIManager.TryUseFunctionCard(functionCard, userInventory, targetInventory, FocusIndex);
                }
                else if (card is MissionCard missionCard)
                {
                    //cardUseUIManager.TryUseMissionCard(missionCard, userInventory, targetInventory, FocusIndex);
                    if (missionCard.CanUse(userInventory, targetInventory, card.cardData))
                    {
                        Debug.Log("MissionCard CanUse");
                        card.cardData.cooldown = card.cardData.cooldown;
                    }
                }
                else
                {
                    // 不是功能卡就直接觸發原本的按鈕事件
                    buttons[FocusIndex].button.onClick.Invoke();
                }
            }
        }
    }

    public void DisableInteractable()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].button.interactable = false;
        }
    }

    public void EnableInteractable()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].button.interactable = true;
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

        for (int i = 0; i < buttons.Count; i++)
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
                    buttons[i].image.sprite = card.image;
                    buttons[i].image.gameObject.SetActive(true);
                }
                else
                {
                    buttons[i].image.sprite = null;
                    buttons[i].image.gameObject.SetActive(false);
                }
            }
            else
            {
                buttons[i].image.sprite = null;
                buttons[i].image.gameObject.SetActive(false);
            }
        }
    }
}

public class ButtionData
{
    public Button button;
    public Image image;
    public UnityEngine.UI.Outline outline;
    public Shadow shadow;
}