using System.Collections.Generic;
using OodlesEngine;
using UnityEngine;
using UnityEngine.UI;

public class LocalBackpack : MonoBehaviour
{
    public static LocalBackpack Instance;
    public int FocusIndex = 0;
    public int SlotCount = 0;
    public List<ButtionData> buttons = new List<ButtionData>();
    public GameObject BackPack;
    public PlayerIdentify playerIdentify;
    public PlayerInventory userInventory; // 本地玩家
    [HideInInspector] public PlayerScanner scanner;
    

    public CardUseUIManager cardUseUIManager; // UI 控制器

    // ✅ 新增：可控制 Update 是否執行
    [Header("控制項")]
    public bool enableUpdate = false;

    void Awake()
    {
        Instance = this;
        buttons.Clear();

        foreach (Transform child in BackPack.transform)
        {
            Button btn = child.GetComponent<Button>();
            if (btn != null)
            {
                ButtionData data = new ButtionData();
                data.button = btn;
                data.image = btn.transform.Find("CardImage")?.GetComponent<Image>();
                data.outline = btn.gameObject.GetComponent<UnityEngine.UI.Outline>();
                if (data.outline == null) data.outline = btn.gameObject.AddComponent<UnityEngine.UI.Outline>();
                data.outline.enabled = false;
                data.shadow = btn.gameObject.GetComponent<Shadow>();
                if (data.shadow != null) data.shadow.enabled = true;

                buttons.Add(data);
            }
        }

        SlotCount = buttons.Count;
    }

    void Update()
    {
        // ✅ 若關閉則不執行任何更新邏輯
        if (!enableUpdate) return;

        HandleMouseScroll();
        HandleNumberKeys();
        UpdateButtonHighlight();
        HandleMouseClick();
    }

    public void SetUpdateEnabled(bool state)
    {
        enableUpdate = state;
    }

    // 以下保持原本邏輯不變
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
                buttons[i].outline.enabled = true;
                buttons[i].shadow.enabled = false;
                buttons[i].button.transform.localScale = Vector3.one * 1.15f;
            }
            else
            {
                buttons[i].outline.enabled = false;
                buttons[i].shadow.enabled = true;
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

                var data = userInventory.slots[FocusIndex];
                var card = CardManager.Instance.Catalog.cards.Find(c =>
                    c.cardData.id == data.id && c.cardData.type == data.type
                );

                if (card is FunctionCard functionCard)
                {
                    cardUseUIManager.TryUseFunctionCard(functionCard, userInventory, targetInventory, FocusIndex);
                }
                else if (card is ItemCard)
                {
                    CardUseParameters UseCard = new CardUseParameters();
                    UseCard.Card = data;
                    UseCard.UserId = playerIdentify.PlayerID;
                     UseCard.UseCardIndex = FocusIndex;
                    GameManager.instance.Rpc_RequestUseCard(UseCard);
                    
                }
                else if (card is MissionCard missioncard)
                {
                    if (!cardUseUIManager.TryUseMissionCard(missioncard, userInventory, targetInventory))
                        return;
                    CardUseParameters UseCard = new CardUseParameters();
                    UseCard.Card = data;
                    UseCard.UserId = playerIdentify.PlayerID;
                    UseCard.UseCardIndex = FocusIndex;
                    if (scanner.currentTarget != null)
                    {
                        UseCard.TargetId = scanner.currentTarget.GetComponent<PlayerIdentify>().PlayerID;
                    }
                    GameManager.instance.Rpc_RequestUseCard(UseCard);
                    data.cooldown = 5;
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

    public void UpdateCardImagesByInventory(PlayerInventory inv, List<Card> allCards)
    {
        if (inv != userInventory)
            return;

        for (int i = 0; i < buttons.Count; i++)
        {
            var data = inv.slots[i];
            if (!data.IsEmpty())
            {
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
