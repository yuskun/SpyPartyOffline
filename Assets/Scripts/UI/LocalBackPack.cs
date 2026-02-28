using System.Collections.Generic;
using OodlesEngine;
using TMPro;
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
    // ========= 長按設定 =========
    [SerializeField] private float holdSeconds = 1f;

    // 先用白名單：只有特定任務卡要長按


    // ========= 長按狀態 =========
    private bool isHolding = false;
    private float holdTimer = 0f;
    private int holdingIndex = -1;
    private MissionCard holdingMissionCard = null;
    private PlayerInventory holdingTargetInventory = null;



    public CardUseUIManager cardUseUIManager; // UI 控制器

    // ✅ 新增：可控制 Update 是否執行
    [Header("控制項")]
    public bool enableUpdate = false;
    public bool canUseCard = true;

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning("LocalBackpack 已存在，請檢查場景中是否有重複的 LocalBackpack！");
            Destroy(gameObject);
            return;
        }
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
                data.cooldownText = btn.transform.Find("CooldownText")?.GetComponent<TextMeshProUGUI>();
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

        CardCooldownUpdate();
        HandleMouseScroll();
        HandleNumberKeys();
        UpdateButtonHighlight();
        HandleCardInput();
    }
    void HandleCardInput()
    {
        // 取得目前 Focus 的卡（如果 Focus 不合法，直接不做事）
        if (FocusIndex < 0 || FocusIndex >= buttons.Count) return;
        if (!buttons[FocusIndex].button.interactable) return;

        if (Input.GetMouseButtonDown(0))
        {
            OnMouseDownUse();
        }

        if (Input.GetMouseButton(0))
        {
            OnMouseHoldUse();
        }

        if (Input.GetMouseButtonUp(0))
        {
            OnMouseUpUse();
        }
    }
    void OnMouseDownUse()
    {
        if (!canUseCard)
            return;

        // 找 targetInventory（你原本的邏輯）
        PlayerInventory targetInventory = null;
        if (scanner != null && scanner.currentTarget != null)
            targetInventory = scanner.currentTarget.GetComponent<PlayerInventory>();

        // 基本檢查
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
        if (!IsCardReady(FocusIndex))
        {
            Debug.Log("卡片冷卻中，無法使用");
            return;
        }

        var data = userInventory.slots[FocusIndex];
        var card = CardManager.Instance.Catalog.cards.Find(c =>
            c.cardData.id == data.id && c.cardData.type == data.type
        );


        // FunctionCard：仍然瞬發（你原本邏輯）
        if (card is FunctionCard functionCard)
        {
            cardUseUIManager.TryUseFunctionCard(functionCard, userInventory, targetInventory, FocusIndex);
            return;
        }

        // ItemCard：仍然瞬發（你原本邏輯）
        if (card is ItemCard)
        {
            SendUseCardRpc(data, FocusIndex, targetInventory);
            return;
        }

        // MissionCard：判斷要不要長按
        if (card is MissionCard missionCard)
        {
            bool requireHold = missionCard.isHoldingUse; // 直接從卡片屬性讀取是否需要長按

            if (!requireHold)
            {
                // 不需要長按：照原本瞬發流程
                if (!cardUseUIManager.TryUseMissionCard(missionCard, userInventory, targetInventory)|| !IsCardReady(FocusIndex))
                    return;

                SendUseCardRpc(data, FocusIndex, targetInventory);
                return;
            }

            // 需要長按：進入蓄力狀態（先不要用卡）
            // 注意：蓄力 UI 你自己接，這裡先留 hook
            isHolding = true;
            holdTimer = 0f;
            holdingIndex = FocusIndex;
            holdingMissionCard = missionCard;
            holdingTargetInventory = targetInventory;
            GameUIManager.Instance.UserCardUI.sprite = card.image;
            GameUIManager.Instance.progressBar.SetActive(true);

            // TODO: 顯示長按進度 UI（例如開始顯示圓環 0%）
            // cardUseUIManager.ShowHoldUI(true);
            // cardUseUIManager.SetHoldProgress(0f);
        }
    }

    void OnMouseHoldUse()
    {
        if (!isHolding) return;

        // 長按期間狀態改變 → 直接取消
        if (!canUseCard)
        {
            CancelHold("canUseCard 為 false，取消長按");
            return;
        }

        // Focus 改變 → 取消
        if (FocusIndex != holdingIndex)
        {
            CancelHold("FocusIndex 改變，取消長按");
            return;
        }

        // 檢查該卡是否還可用（例如冷卻中）
        if (!IsCardReady(holdingIndex))
        {
            // 冷卻期間不計算時間（選一種策略）
            // 1) 不累積但不取消
            return;

            // 或 2) 直接取消（比較乾淨）
            // CancelHold("卡片進入冷卻，取消長按");
            // return;
        }

        holdTimer += Time.deltaTime;
        GameUIManager.Instance.progressfill.fillAmount = holdTimer / holdSeconds;

        if (holdTimer >= holdSeconds)
        {
            var data = userInventory.slots[holdingIndex];

            if (!cardUseUIManager.TryUseMissionCard(
                holdingMissionCard,
                userInventory,
                holdingTargetInventory))
            {
                CancelHold("TryUseMissionCard 失敗");
                return;
            }

            SendUseCardRpc(data, holdingIndex, holdingTargetInventory);

            FinishHold();
        }
    }

    void OnMouseUpUse()
    {
        if (!isHolding) return;

        // 放開但沒滿秒：取消
        if (holdTimer < holdSeconds)
            CancelHold("提早放開，取消長按");
    }

    void SendUseCardRpc(CardData data, int index, PlayerInventory targetInventory)
    {
        CardUseParameters useCard = new CardUseParameters();
        useCard.Card = data;
        useCard.UserId = playerIdentify.PlayerID;
        useCard.UseCardIndex = index;

        if (targetInventory != null)
        {
            var id = targetInventory.GetComponent<PlayerIdentify>();
            if (id != null)
                useCard.TargetId = id.PlayerID;
        }

        GameManager.instance.Rpc_RequestUseCard(useCard);
    }

    void FinishHold()
    {
        GameUIManager.Instance.progressBar.SetActive(false);
        isHolding = false;
        holdTimer = 0f;
        holdingIndex = -1;
        holdingMissionCard = null;
        holdingTargetInventory = null;

        // TODO: 關閉長按 UI
        // cardUseUIManager.ShowHoldUI(false);
    }

    void CancelHold(string reason)
    {
        Debug.Log($"[Hold] {reason}");
        isHolding = false;
        holdTimer = 0f;
        holdingIndex = -1;
        holdingMissionCard = null;
        holdingTargetInventory = null;

        // TODO: 關閉長按 UI / 重置進度
        // cardUseUIManager.ShowHoldUI(false);
        // cardUseUIManager.SetHoldProgress(0f);
    }
    public void SetUpdateEnabled(bool state)
    {
        enableUpdate = state;
    }
    void CardCooldownUpdate()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            float cooldown = userInventory.GetRemainingCooldown(i);
            if (cooldown > 0)
            {
                buttons[i].cooldownText.text = Mathf.CeilToInt(cooldown).ToString();
                buttons[i].cooldownText.gameObject.SetActive(true);
                buttons[i].button.interactable = false;
            }
            else
            {
                buttons[i].cooldownText.text = "";
                buttons[i].cooldownText.gameObject.SetActive(false);
                buttons[i].button.interactable = true;
            }
        }
    }

    // 以下保持原本邏輯不變
    bool IsCardReady(int index)
    {
        if (index < 0 || index >= userInventory.slots.Length)
            return false;

        var slot = userInventory.slots[index];
        return userInventory.CanUse(slot); // 確保冷卻狀態更新


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
[System.Serializable]
public class ButtionData
{
    public Button button;
    public Image image;
    public UnityEngine.UI.Outline outline;
    public Shadow shadow;
    public TextMeshProUGUI cooldownText;
}
