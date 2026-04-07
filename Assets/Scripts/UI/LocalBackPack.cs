using System.Collections.Generic;
using OodlesEngine;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LocalBackpack : MonoBehaviour
{
    public static LocalBackpack Instance;
    public int FocusIndex = 0;
    public int SlotCount = 0;
    public List<ButtionData> buttons = new List<ButtionData>();
    public GameObject BackPack;
    [HideInInspector] public PlayerIdentify playerIdentify;
    [HideInInspector] public PlayerInventory userInventory; // 本地玩家
    [HideInInspector] public OodlesCharacter character;
    [HideInInspector] public PlayerScanner scanner;
    // ========= 長按設定 =========
    [SerializeField] private float holdSeconds = 1f;

    // 先用白名單：只有特定任務卡要長按


    // ========= 長按狀態 =========
    private bool isHolding = false;
    private float holdTimer = 0f;
    private float currentHoldDuration = 1f;
    private int holdingIndex = -1;
    private MissionCard holdingMissionCard = null;
    private PlayerInventory holdingTargetInventory = null;
    private StealTargetObject holdingStealTarget = null;

    // ========= ItemCard 預覽狀態 =========
    private bool isPreviewingItem = false;
    private int previewingIndex = -1;
    private ItemCard previewingItemCard = null;
    private CardData previewingCardData;
    private PlayerInventory previewingTargetInventory = null;

    // ========= Steal Outline 狀態 =========
    private bool hadStealCard = false;



    public CardUseUIManager cardUseUIManager; // UI 控制器
    public CardUseUIManager_UIToolkit cardUseUIManagerUIToolkit;
    // 在欄位區加這兩個
    public Sprite normalFrame;    // 拖入 inventory.png
    public Sprite selectedFrame;  // 拖入 inventory_select.png

    // ✅ 新增：可控制 Update 是否執行
    [Header("控制項")]
    public bool enableUpdate = false;


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
                data.frameImage = btn.gameObject.GetComponent<Image>();
                data.cooldownText = btn.transform.Find("CooldownText")?.GetComponent<TextMeshProUGUI>();
                data.outline = btn.gameObject.GetComponent<UnityEngine.UI.Outline>();
                if (data.outline == null) data.outline = btn.gameObject.AddComponent<UnityEngine.UI.Outline>();
                data.outline.enabled = false;
                data.shadow = btn.gameObject.GetComponent<Shadow>();
                if (data.shadow != null) data.shadow.enabled = true;
                data.forbidImage = btn.transform.Find("Forbid")?.GetComponent<Image>();
                if (data.forbidImage != null) data.forbidImage.gameObject.SetActive(false);

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
        UpdateCardImagesByInventory();
        UpdateStealScan();
        UpdateStealOutlines();
    }
    void HandleCardInput()
    {
        // 取得目前 Focus 的卡（如果 Focus 不合法，直接不做事）
        if (FocusIndex < 0 || FocusIndex >= buttons.Count) return;
        if (!buttons[FocusIndex].button.interactable) return;

        // FocusIndex 換格子時取消 ItemCard 預覽
        if (isPreviewingItem && FocusIndex != previewingIndex)
            CancelItemPreview();

        if (Input.GetKeyDown(KeyCode.E))
        {
            OnMouseDownUse();
        }

        if (Input.GetKey(KeyCode.E))
        {
            OnMouseHoldUse();
        }

        if (Input.GetKeyUp(KeyCode.E))
        {
            OnMouseUpUse();
        }
    }
    void OnMouseDownUse()
    {
        if (!userInventory.CanUseCard)
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

        var data = userInventory.slotsNetworked[FocusIndex];
        var card = CardManager.Instance.Catalog.cards.Find(c =>
            c.cardData.id == data.id && c.cardData.type == data.type
        );


        // FunctionCard：仍然瞬發（你原本邏輯）
        if (card is FunctionCard functionCard)
        {
            if (functionCard.needTarget && targetInventory == null)
            {
                PlayAnimationViaRpc("DoScratch");
                Debug.Log("此功能卡需要目標，但沒有掃描到有效目標");
                return;
            }
            //cardUseUIManager.TryUseFunctionCard(functionCard, userInventory, targetInventory, FocusIndex);

            if (cardUseUIManagerUIToolkit != null)
                cardUseUIManagerUIToolkit.TryUseFunctionCard(functionCard, userInventory, targetInventory, FocusIndex);
            else
                cardUseUIManager.TryUseFunctionCard(functionCard, userInventory, targetInventory, FocusIndex);
            return;
            return;
        }

        // ItemCard：第一次 E 顯示預覽，第二次 E 才使用
        if (card is ItemCard itemCard)
        {
            if (itemCard.needTarget && targetInventory == null)
            {
                PlayAnimationViaRpc("DoScratch");
                Debug.Log("此物品卡需要目標，但沒有掃描到有效目標");
                return;
            }

            if (!isPreviewingItem || previewingIndex != FocusIndex)
            {
                // 第一次按：顯示預覽
                CancelItemPreview(); // 先取消舊的（換格子的情況）
                isPreviewingItem = true;
                previewingIndex = FocusIndex;
                previewingItemCard = itemCard;
                previewingCardData = data;
                previewingTargetInventory = targetInventory;
                CardPreviewSystem.Instance.ShowPreview(card);
                return;
            }

            // 第二次按：真正使用
            CardPreviewSystem.Instance.HidePreview();
            SendUseCardRpc(previewingCardData, previewingIndex, previewingTargetInventory);
            ClearItemPreviewState();
            return;
        }

        // MissionCard：判斷要不要長按
        if (card is MissionCard missionCard)
        {
            bool isSteal = missionCard is Steal;

            if (isSteal)
            {
                if (scanner.currentStealTarget == null)
                {
                    PlayAnimationViaRpc("DoScratch");
                    Debug.Log("附近沒有可竊盜的物件");
                    return;
                }
            }
            else if (missionCard.needTarget && targetInventory == null)
            {
                PlayAnimationViaRpc("DoScratch");
                Debug.Log("此任務卡需要目標，但沒有掃描到有效目標");
                return;
            }

            bool requireHold = missionCard.isHoldingUse;

            if (!requireHold)
            {
                // 不需要長按：照原本瞬發流程
                if (!cardUseUIManager.TryUseMissionCard(missionCard, userInventory, targetInventory, FocusIndex) || !IsCardReady(FocusIndex))
                    return;

                SendUseCardRpc(data, FocusIndex, targetInventory);
                return;
            }

            // 需要長按：進入蓄力狀態
            isHolding = true;
            holdTimer = 0f;
            currentHoldDuration = missionCard.holdDuration > 0f ? missionCard.holdDuration : holdSeconds;
            holdingIndex = FocusIndex;
            holdingMissionCard = missionCard;
            holdingTargetInventory = isSteal ? null : targetInventory;
            holdingStealTarget = isSteal ? scanner.currentStealTarget : null;
            GameUIManager.Instance.UserCardUI.sprite = card.image;
            GameUIManager.Instance.progressBar.SetActive(true);
        }
    }

    void OnMouseHoldUse()
    {
        if (!isHolding) return;

        bool isSteal = holdingMissionCard is Steal;

        // 目標消失檢查
        if (isSteal)
        {
            if (scanner.currentStealTarget == null)
            {
                PlayAnimationViaRpc("DoScratch");
                CancelHold("steal target 消失，取消長按");
                return;
            }
        }
        else
        {
            if (scanner.currentTarget == null)
            {
                PlayAnimationViaRpc("DoScratch");
                CancelHold("scanner.currentTarget 為 null，取消長按");
                return;
            }
        }

        if (!userInventory.CanUseCard)
        {
            CancelHold("canUseCard 為 false，取消長按");
            return;
        }

        if (FocusIndex != holdingIndex)
        {
            CancelHold("FocusIndex 改變，取消長按");
            return;
        }

        if (!IsCardReady(holdingIndex))
            return;

        holdTimer += Time.deltaTime;
        GameUIManager.Instance.progressfill.fillAmount = holdTimer / currentHoldDuration;

        if (holdTimer >= currentHoldDuration)
        {
            if (!cardUseUIManager.TryUseMissionCard(holdingMissionCard, userInventory, holdingTargetInventory, holdingIndex))
            {
                CancelHold("TryUseMissionCard 失敗");
                return;
            }

            if (isSteal)
                SendStealObjectRpc(holdingIndex, holdingStealTarget);
            else
                SendUseCardRpc(userInventory.slotsNetworked[holdingIndex], holdingIndex, holdingTargetInventory);

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
        holdingStealTarget = null;
    }

    void CancelItemPreview()
    {
        if (!isPreviewingItem) return;
        CardPreviewSystem.Instance.HidePreview();
        ClearItemPreviewState();
    }

    void ClearItemPreviewState()
    {
        isPreviewingItem = false;
        previewingIndex = -1;
        previewingItemCard = null;
        previewingCardData = default;
        previewingTargetInventory = null;
    }

    void CancelHold(string reason)
    {
        Debug.Log($"[Hold] {reason}");
        GameUIManager.Instance.progressBar.SetActive(false);
        isHolding = false;
        holdTimer = 0f;
        holdingIndex = -1;
        holdingMissionCard = null;
        holdingTargetInventory = null;
        holdingStealTarget = null;
    }

    void SendStealObjectRpc(int cardIndex, StealTargetObject target)
    {
        if (target == null) return;
        CardUseParameters p = new CardUseParameters();
        p.Card = userInventory.slotsNetworked[cardIndex];
        p.UserId = playerIdentify.PlayerID;
        p.UseCardIndex = cardIndex;
        p.TargetId = target.StealIndex;  // 傳序號（0/1/2），Host 端用靜態表查找
        GameManager.instance.Rpc_RequestUseCard(p);
    }

    void UpdateStealScan()
    {
        if (scanner == null || userInventory == null) return;
        if (FocusIndex < 0 || FocusIndex >= PlayerInventory.MaxSlots) { scanner.enableStealScan = false; return; }

        var data = userInventory.slotsNetworked[FocusIndex];
        if (data.IsEmpty()) { scanner.enableStealScan = false; return; }

        var card = CardManager.Instance?.Catalog?.cards?.Find(c => c.cardData.id == data.id && c.cardData.type == data.type);
        scanner.enableStealScan = card is Steal;
    }
    public void SetUpdateEnabled(bool state)
    {
        enableUpdate = state;
    }
    void CardCooldownUpdate()
    {
        if (userInventory == null)
        return;
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
        if (index < 0 || index >= PlayerInventory.MaxSlots)
        {
            PlayAnimationViaRpc("DoShake");
            return false;
        }
        var slot = userInventory.slotsNetworked[index];
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
                //buttons[i].outline.enabled = true;
                //buttons[i].shadow.enabled = false;
                buttons[i].frameImage.sprite = selectedFrame;
                buttons[i].button.transform.localScale = Vector3.one * 1.15f;
            }
            else
            {
                //buttons[i].outline.enabled = false;
                //buttons[i].shadow.enabled = true;
                buttons[i].frameImage.sprite = normalFrame;
                buttons[i].button.transform.localScale = Vector3.one;
            }
        }
    }



    /// <summary>透過 RPC 請求 Host 播放動畫（Host 端 SetTrigger 後自動同步回所有端）</summary>
    private void PlayAnimationViaRpc(string triggerName)
    {
        if (playerIdentify == null) return;
        GameManager.instance.Rpc_PlayFailAnimation(playerIdentify.PlayerID, triggerName);
    }

    private void ShowForbid(bool show)
    {
        foreach (var btn in buttons)
            if (btn.forbidImage != null) btn.forbidImage.gameObject.SetActive(show);
    }

    public void OnEscortStart(int catcherID, int targetID)
    {
        if (playerIdentify == null) return;
        int localID = playerIdentify.PlayerID;

        // 只有被抓者禁用卡片，抓人者可以繼續使用
        if (localID == targetID)
        {
            userInventory.CanUseCard = false;
            ShowForbid(true);
            if (GameUIManager.Instance != null && GameUIManager.Instance.CaughtUI != null)
                GameUIManager.Instance.CaughtUI.SetActive(true);
        }
    }

    public void OnEscortEnd()
    {
        if (userInventory != null) userInventory.CanUseCard = true;
        ShowForbid(false);
        if (GameUIManager.Instance != null && GameUIManager.Instance.CaughtUI != null)
            GameUIManager.Instance.CaughtUI.SetActive(false);
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

    public void UpdateCardImagesByInventory()
    {
        if (userInventory == null)
            return;
        if (CardManager.Instance == null)
            return;

        var allCards = CardManager.Instance.Catalog.cards;
        for (int i = 0; i < buttons.Count; i++)
        {
            var data = userInventory.slotsNetworked[i]; // 直接讀網路狀態，確保 Client 端同步正確
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
    public void ClearCardImages()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].image.sprite = null;
            buttons[i].image.gameObject.SetActive(false);
        }
        enableUpdate = false; // ✅ 清空後關閉 Update

        // 清除 Steal Outline
        SetAllStealOutlines(false);
        hadStealCard = false;
    }

    /// <summary>持有 Steal 卡時，所有 StealTarget 持續顯示 Outline</summary>
    void UpdateStealOutlines()
    {
        if (userInventory == null) return;

        bool hasSteal = LocalPlayerHasStealCard();

        // 狀態沒變且沒持有 → 不需更新
        if (!hasSteal && !hadStealCard) return;

        // 狀態變化 → 全部切換
        if (hasSteal != hadStealCard)
        {
            SetAllStealOutlines(hasSteal);
            hadStealCard = hasSteal;
            return;
        }

        // 持續持有 → 確保新生成的 target 也亮起來
        if (hasSteal)
        {
            foreach (var obj in StealTargetObject.All)
                if (obj != null) obj.SetHighlight(true);
        }
    }

    bool LocalPlayerHasStealCard()
    {
        var allCards = CardManager.Instance?.Catalog?.cards;
        if (allCards == null) return false;

        for (int i = 0; i < PlayerInventory.MaxSlots; i++)
        {
            var data = userInventory.slotsNetworked[i];
            if (data.IsEmpty()) continue;
            var card = allCards.Find(c => c.cardData.id == data.id && c.cardData.type == data.type);
            if (card is Steal) return true;
        }
        return false;
    }

    void SetAllStealOutlines(bool on)
    {
        foreach (var obj in StealTargetObject.All)
            if (obj != null) obj.SetHighlight(on);
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

    public Image frameImage; // 父物件的 Image
    public Image forbidImage;
}
