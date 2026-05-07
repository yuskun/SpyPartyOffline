using System.Collections;
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
    [HideInInspector] public NetworkPlayer networkPlayer;
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

    // ========= 背包變動 FX =========
    [Header("背包變動 FX 設定")]
    [SerializeField] private Color pickupFlashColor  = new Color(1f, 0.95f, 0.4f, 1f);   // 暖黃 = 撿到
    [SerializeField] private Color selfUseFlashColor = new Color(0.5f, 1f, 0.7f, 1f);    // 青綠 = 自用
    [SerializeField] private Color stolenFlashColor  = new Color(1f, 0.3f, 0.3f, 1f);    // 紅 = 被偷
    [SerializeField] private Color swapFlashColor    = new Color(0.4f, 0.9f, 1f, 1f);    // 青 = 換卡
    [SerializeField] private float pickupDuration    = 0.45f;
    [SerializeField] private float selfUseDuration   = 0.55f;
    [SerializeField] private float swapDuration      = 0.45f;
    [Tooltip("玩家按 E 自用卡片後，這秒數內背包變空 → 視為自用而非被偷")]
    [SerializeField] private float selfUseGraceSeconds = 1.5f;

    private CardData[] _lastSlots;
    private bool _slotsInitialized = false;
    private Coroutine[] _slotFx;
    private bool[] _slotStolenLock;       // FX 期間阻擋一般 image 隱藏邏輯
    private float _selfUseGraceUntil = 0f; // 玩家自用卡片的寬限時間戳
    private Vector3[] _imgBasePos;
    private Vector3[] _imgBaseScale;
    private Quaternion[] _imgBaseRot;
    private Color[] _imgBaseColor;
    private Color[] _frameBaseColor;


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
                data.hintObject = btn.transform.Find("Hint")?.gameObject;
                if (data.hintObject != null) data.hintObject.SetActive(false);

                buttons.Add(data);
            }
        }

        SlotCount = buttons.Count;

        // ===== FX 用的快取 =====
        _lastSlots       = new CardData[PlayerInventory.MaxSlots];
        _slotFx          = new Coroutine[buttons.Count];
        _slotStolenLock  = new bool[buttons.Count];
        _imgBasePos      = new Vector3[buttons.Count];
        _imgBaseScale    = new Vector3[buttons.Count];
        _imgBaseRot      = new Quaternion[buttons.Count];
        _imgBaseColor    = new Color[buttons.Count];
        _frameBaseColor  = new Color[buttons.Count];
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i].image != null)
            {
                _imgBasePos[i]   = buttons[i].image.transform.localPosition;
                _imgBaseScale[i] = buttons[i].image.transform.localScale;
                _imgBaseRot[i]   = buttons[i].image.transform.localRotation;
                _imgBaseColor[i] = buttons[i].image.color;
            }
            else
            {
                _imgBaseScale[i] = Vector3.one;
                _imgBaseRot[i]   = Quaternion.identity;
                _imgBaseColor[i] = Color.white;
            }
            _frameBaseColor[i] = (buttons[i].frameImage != null) ? buttons[i].frameImage.color : Color.white;
        }
    }
    public void AllowInput(bool allow)
    {
        if (networkPlayer != null)
            networkPlayer.AllowInput = allow;
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
        UpdateItemPreview(); // Focus 落在 ItemCard 時自動顯示預覽
    }

    /// <summary>
    /// 依目前 FocusIndex 的卡片自動顯示/隱藏 ItemCard 預覽。
    /// - Focus 在 ItemCard 上 → 顯示預覽
    /// - Focus 移到空格 / FunctionCard / MissionCard / UI 開啟時 → 隱藏預覽
    /// </summary>
    void UpdateItemPreview()
    {
        // 基本 guard
        if (userInventory == null || CardManager.Instance == null) { EnsurePreviewHidden(); return; }
        if (FocusIndex < 0 || FocusIndex >= PlayerInventory.MaxSlots) { EnsurePreviewHidden(); return; }
        if (Cursor.lockState != CursorLockMode.Locked) { EnsurePreviewHidden(); return; }

        var data = userInventory.slotsNetworked[FocusIndex];
        if (data.IsEmpty()) { EnsurePreviewHidden(); return; }

        var card = CardManager.Instance.Catalog.cards.Find(c => c.cardData.id == data.id && c.cardData.type == data.type);
        if (!(card is ItemCard)) { EnsurePreviewHidden(); return; }

        // 需要顯示：如果尚未顯示或換格了，重新 Show
        if (!isPreviewingItem || previewingIndex != FocusIndex)
        {
            CardPreviewSystem.Instance.ShowPreview(card);
            isPreviewingItem = true;
            previewingIndex = FocusIndex;
        }
    }

    void EnsurePreviewHidden()
    {
        if (!isPreviewingItem) return;
        CardPreviewSystem.Instance.HidePreview();
        isPreviewingItem = false;
        previewingIndex = -1;
    }
    void HandleCardInput()
    {
        // 取得目前 Focus 的卡（如果 Focus 不合法，直接不做事）
        if (FocusIndex < 0 || FocusIndex >= buttons.Count) return;
        if (!buttons[FocusIndex].button.interactable) return;

        // 滑鼠沒鎖定（UI 開啟中）時不處理道具輸入
        if (Cursor.lockState != CursorLockMode.Locked) return;

        // 道具使用鍵：E（左鍵保留給武器攻擊，不衝突）
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
            // 先檢查所有可能的失敗原因，若有則顯示對應的失敗 UI
            var failReason = CheckFunctionCardFailure(functionCard, userInventory, targetInventory);
            if (failReason != GameUIManager.CardUseFailReason.None)
            {
                PlayAnimationViaRpc("DoScratch");
                GameUIManager.Instance?.ShowCardFailUI(failReason);
                Debug.Log($"[UseCard] FunctionCard 無法使用，原因：{failReason}");
                return;
            }

            if (cardUseUIManagerUIToolkit != null)
                cardUseUIManagerUIToolkit.TryUseFunctionCard(functionCard, userInventory, targetInventory, FocusIndex);
            else
                cardUseUIManager.TryUseFunctionCard(functionCard, userInventory, targetInventory, FocusIndex);
            return;
        }

        // ItemCard：預覽由 UpdateItemPreview 自動顯示，E 直接使用
        if (card is ItemCard itemCard)
        {
            if (itemCard.needTarget && targetInventory == null)
            {
                PlayAnimationViaRpc("DoScratch");
                GameUIManager.Instance?.ShowCardFailUI(GameUIManager.CardUseFailReason.NoTarget);
                Debug.Log("此物品卡需要目標，但沒有掃描到有效目標");
                return;
            }

            // 直接使用；使用完隱藏預覽（下次 Focus 到 ItemCard 時會再自動顯示）
            CardPreviewSystem.Instance.HidePreview();
            SendUseCardRpc(data, FocusIndex, targetInventory);
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
        // 標記自用：本玩家主動用卡，下一個 grace 視窗內變空的格子算「自用」而非「被偷」
        _selfUseGraceUntil = Time.time + selfUseGraceSeconds;

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

        // 把當下 Camera/SpawnObject 的世界座標帶給 Host，讓 ItemCard 在這個點生成
        useCard.SpawnPosition = GetSpawnWorldPosition();

        GameManager.instance.Rpc_RequestUseCard(useCard);
    }

    /// <summary>取得 Camera.main 底下 "SpawnObject" 的世界座標；找不到則退回玩家位置。</summary>
    Vector3 GetSpawnWorldPosition()
    {
        if (Camera.main != null)
        {
            var anchor = Camera.main.transform.Find("SpawnObject");
            if (anchor != null) return anchor.position;
        }
        // fallback：用玩家的位置（避免回傳 (0,0,0) 讓物件掉進地圖原點）
        if (character != null) return character.transform.position;
        return Vector3.zero;
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

    /// <summary>
    /// 檢查 FunctionCard 使用失敗的原因
    /// 依序檢查：沒目標 → 自己不夠 → 對方滿 / 對方空
    /// </summary>
    GameUIManager.CardUseFailReason CheckFunctionCardFailure(FunctionCard card, PlayerInventory user, PlayerInventory target)
    {
        // 1. 沒鎖定到玩家
        if (card.needTarget && target == null)
            return GameUIManager.CardUseFailReason.NoTarget;

        // 統計雙方背包數量（不含作為「使用中」的那張卡嗎？這裡簡單用總數）
        int userCount = 0, targetCount = 0;
        if (user != null)
            foreach (var c in user.slots) if (!c.IsEmpty()) userCount++;
        if (target != null)
            foreach (var c in target.slots) if (!c.IsEmpty()) targetCount++;

        // 各卡片特殊檢查
        if (card is Give)
        {
            // 4. 自身沒有多的物品（只有 Give 卡本身）
            if (userCount < 2) return GameUIManager.CardUseFailReason.SelfNotEnough;
            // 2. 對方背包滿（6 格全滿，>5 = 6）
            if (targetCount > 5) return GameUIManager.CardUseFailReason.TargetFull;
        }
        else if (card is Swap)
        {
            // 4. 自身沒有多的物品（只有 Swap 卡本身）
            if (userCount < 2) return GameUIManager.CardUseFailReason.SelfNotEnough;
            // 3. 對方沒有物品可交換
            if (targetCount < 1) return GameUIManager.CardUseFailReason.TargetEmpty;
        }
        // Peek / Wiretap 之類：只需要 target 不為 null（上方已檢查過）

        return GameUIManager.CardUseFailReason.None;
    }

    void SendStealObjectRpc(int cardIndex, StealTargetObject target)
    {
        if (target == null) return;
        // 標記自用：Steal 卡屬於主動使用
        _selfUseGraceUntil = Time.time + selfUseGraceSeconds;

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

        // 只要背包任一格有 Steal 卡，就啟動掃描（不限焦點）
        scanner.enableStealScan = LocalPlayerHasStealCard();
    }

    /// <summary>判斷某個 slot 是不是 Steal 卡</summary>
    bool IsStealCardAt(int i)
    {
        if (userInventory == null) return false;
        if (i < 0 || i >= userInventory.slotsNetworked.Length) return false;
        var data = userInventory.slotsNetworked[i];
        if (data.IsEmpty()) return false;
        var card = CardManager.Instance?.Catalog?.cards?.Find(c => c.cardData.id == data.id && c.cardData.type == data.type);
        return card is Steal;
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
        // Steal 卡的 hint 規則：只要 scanner 掃到 Steal Object 就亮（不論焦點）
        bool stealTargetVisible = scanner != null && scanner.currentStealTarget != null;

        for (int i = 0; i < buttons.Count; i++)
        {
            bool isFocus = (i == FocusIndex);
            bool hasCard = userInventory != null && i < userInventory.slotsNetworked.Length && !userInventory.slotsNetworked[i].IsEmpty();
            bool isStealSlot = hasCard && IsStealCardAt(i);

            // 焦點視覺：框 + 放大
            buttons[i].frameImage.sprite = isFocus ? selectedFrame : normalFrame;
            buttons[i].button.transform.localScale = isFocus ? Vector3.one * 1.15f : Vector3.one;

            // Hint 顯示
            bool showHint;
            if (isStealSlot)
            {
                // Steal 卡：scanner 有掃到才開（與焦點無關）
                showHint = stealTargetVisible;
            }
            else
            {
                // 其他卡：原邏輯，焦點且有卡才開
                showHint = isFocus && hasCard;
            }

            if (buttons[i].hintObject != null) buttons[i].hintObject.SetActive(showHint);
        }
    }



    /// <summary>透過 RPC 請求 Host 播放動畫（Host 端 SetTrigger 後自動同步回所有端）</summary>
    private void PlayAnimationViaRpc(string triggerName)
    {
        if (playerIdentify == null) return;
        GameManager.instance.Rpc_PlayFailAnimation(playerIdentify.PlayerID, triggerName);
    }

    // ========= Emote 動畫觸發（Trigger 參數）=========
    /// <summary>揮手</summary>
    public void DoWaveHand() { PlayAnimationViaRpc("DoWaveHand"); }
    /// <summary>大笑</summary>
    public void DoBigLaugh() { PlayAnimationViaRpc("DoBigLaugh"); }
    /// <summary>肌肉</summary>
    public void DoMuscle()   { PlayAnimationViaRpc("DoMuscle"); }
    /// <summary>舉手</summary>
    public void DoHandUp()   { PlayAnimationViaRpc("DoHandUp"); }

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
        int slotMax = Mathf.Min(buttons.Count, userInventory.slotsNetworked.Length);

        for (int i = 0; i < buttons.Count; i++)
        {
            CardData data = (i < userInventory.slotsNetworked.Length)
                ? userInventory.slotsNetworked[i]
                : CardData.Empty();

            // === 變動偵測：empty→has = 撿到；has→empty = 被偷走；A→B = 被換 ===
            if (_slotsInitialized && i < slotMax)
            {
                CardData old = _lastSlots[i];
                bool wasEmpty = old.IsEmpty();
                bool nowEmpty = data.IsEmpty();
                bool sameCard = !wasEmpty && !nowEmpty && old.id == data.id && old.type == data.type;

                if (wasEmpty && !nowEmpty)
                {
                    TriggerSlotFx(i, PickupFx(i));
                }
                else if (!wasEmpty && nowEmpty)
                {
                    Sprite oldSprite = GetSpriteForData(old);
                    if (Time.time < _selfUseGraceUntil)
                        TriggerSlotFx(i, SelfUseFx(i, oldSprite));
                    else
                        TriggerSlotFx(i, StolenFx(i, oldSprite));
                }
                else if (!wasEmpty && !nowEmpty && !sameCard)
                {
                    TriggerSlotFx(i, SwapFx(i));
                }
            }

            // === 圖片更新（若 stolen FX 正在播，先不要清掉 sprite，留給協程處理）===
            if (!_slotStolenLock[i])
            {
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

            // 紀錄這一幀狀態，給下一幀比對
            if (i < _lastSlots.Length) _lastSlots[i] = data;
        }
        _slotsInitialized = true;
    }

    // ========= 背包 FX 協程與工具 =========

    Sprite GetSpriteForData(CardData data)
    {
        if (data.IsEmpty()) return null;
        if (CardManager.Instance == null) return null;
        var card = CardManager.Instance.Catalog.cards.Find(c =>
            c.cardData.id == data.id && c.cardData.type == data.type);
        return (card != null) ? card.image : null;
    }

    void TriggerSlotFx(int i, IEnumerator routine)
    {
        if (i < 0 || i >= _slotFx.Length) return;
        if (_slotFx[i] != null)
        {
            StopCoroutine(_slotFx[i]);
            ResetSlotVisual(i);
        }
        _slotFx[i] = StartCoroutine(routine);
    }

    void ResetSlotVisual(int i)
    {
        if (i < 0 || i >= buttons.Count) return;
        _slotStolenLock[i] = false;
        var img = buttons[i].image;
        if (img != null)
        {
            img.transform.localPosition = _imgBasePos[i];
            img.transform.localScale    = _imgBaseScale[i];
            img.transform.localRotation = _imgBaseRot[i];
            img.color = _imgBaseColor[i];
        }
        if (buttons[i].frameImage != null)
            buttons[i].frameImage.color = _frameBaseColor[i];
    }

    /// <summary>撿到新道具：Scale 0.5 → 1.2 overshoot → 1.0，邊框暖黃閃光</summary>
    IEnumerator PickupFx(int i)
    {
        if (i < 0 || i >= buttons.Count) yield break;
        var imgT  = (buttons[i].image != null) ? buttons[i].image.transform : null;
        var frame = buttons[i].frameImage;

        float duration = pickupDuration;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);
            if (imgT != null) imgT.localScale = _imgBaseScale[i] * SpringPop(n);
            if (frame != null) frame.color = Color.Lerp(pickupFlashColor, _frameBaseColor[i], n);
            yield return null;
        }
        ResetSlotVisual(i);
        _slotFx[i] = null;
    }

    /// <summary>被偷走：紅色閃 + 抖動 0.5s，再 fade 落下 0.4s，最後清除</summary>
    IEnumerator StolenFx(int i, Sprite oldSprite)
    {
        if (i < 0 || i >= buttons.Count) yield break;
        var img   = buttons[i].image;
        var imgT  = (img != null) ? img.transform : null;
        var frame = buttons[i].frameImage;

        _slotStolenLock[i] = true;
        if (img != null && oldSprite != null)
        {
            img.sprite = oldSprite;
            img.gameObject.SetActive(true);
            img.color = _imgBaseColor[i];
        }

        // Phase 1: 紅閃 + 抖動 (0.5s)
        float phase1 = 0.5f;
        float t = 0f;
        while (t < phase1)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / phase1);
            float pulse = Mathf.Abs(Mathf.Sin(n * Mathf.PI * 3f));
            if (frame != null) frame.color = Color.Lerp(_frameBaseColor[i], stolenFlashColor, pulse);
            if (imgT != null)
                imgT.localPosition = _imgBasePos[i] + new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f), 0f);
            yield return null;
        }

        // Phase 2: fade out + drop (0.4s)
        float phase2 = 0.4f;
        t = 0f;
        Color baseImgColor = _imgBaseColor[i];
        while (t < phase2)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / phase2);
            if (img != null)
                img.color = new Color(baseImgColor.r, baseImgColor.g, baseImgColor.b, baseImgColor.a * (1f - n));
            if (imgT != null)
                imgT.localPosition = _imgBasePos[i] + new Vector3(0f, -25f * n, 0f);
            yield return null;
        }

        // Cleanup
        _slotStolenLock[i] = false;
        if (img != null)
        {
            img.color = baseImgColor;
            img.sprite = null;
            img.gameObject.SetActive(false);
        }
        if (imgT != null) imgT.localPosition = _imgBasePos[i];
        if (frame != null) frame.color = _frameBaseColor[i];
        _slotFx[i] = null;
    }

    /// <summary>玩家自用：Scale 1.0 → 1.35 + 上升 + 淡出，邊框青綠閃（正向回饋）</summary>
    IEnumerator SelfUseFx(int i, Sprite oldSprite)
    {
        if (i < 0 || i >= buttons.Count) yield break;
        var img   = buttons[i].image;
        var imgT  = (img != null) ? img.transform : null;
        var frame = buttons[i].frameImage;

        _slotStolenLock[i] = true;
        if (img != null && oldSprite != null)
        {
            img.sprite = oldSprite;
            img.gameObject.SetActive(true);
            img.color = _imgBaseColor[i];
        }

        Color baseImgColor = _imgBaseColor[i];
        float duration = selfUseDuration;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);
            // 影像：放大到 1.35 + 上升 25 + 淡出
            if (imgT != null)
            {
                float scale = Mathf.Lerp(1f, 1.35f, n);
                imgT.localScale = _imgBaseScale[i] * scale;
                imgT.localPosition = _imgBasePos[i] + new Vector3(0f, 25f * n, 0f);
            }
            if (img != null)
                img.color = new Color(baseImgColor.r, baseImgColor.g, baseImgColor.b, baseImgColor.a * (1f - n));
            // 邊框：前半段青綠閃光，後半段回到原色
            if (frame != null)
            {
                float flash = Mathf.Sin(n * Mathf.PI);
                frame.color = Color.Lerp(_frameBaseColor[i], selfUseFlashColor, flash);
            }
            yield return null;
        }

        _slotStolenLock[i] = false;
        if (img != null)
        {
            img.color = baseImgColor;
            img.sprite = null;
            img.gameObject.SetActive(false);
        }
        if (imgT != null)
        {
            imgT.localPosition = _imgBasePos[i];
            imgT.localScale    = _imgBaseScale[i];
        }
        if (frame != null) frame.color = _frameBaseColor[i];
        _slotFx[i] = null;
    }

    /// <summary>被交換：Y 軸翻轉 360 + scale pulse + 邊框青光閃</summary>
    IEnumerator SwapFx(int i)
    {
        if (i < 0 || i >= buttons.Count) yield break;
        var imgT  = (buttons[i].image != null) ? buttons[i].image.transform : null;
        var frame = buttons[i].frameImage;

        float duration = swapDuration;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);
            if (imgT != null)
            {
                imgT.localRotation = Quaternion.Euler(0f, n * 360f, 0f) * _imgBaseRot[i];
                float scale = 1f + 0.2f * Mathf.Sin(n * Mathf.PI);
                imgT.localScale = _imgBaseScale[i] * scale;
            }
            if (frame != null)
            {
                float flash = Mathf.Sin(n * Mathf.PI);
                frame.color = Color.Lerp(_frameBaseColor[i], swapFlashColor, flash);
            }
            yield return null;
        }
        ResetSlotVisual(i);
        _slotFx[i] = null;
    }

    /// <summary>0~1 → 0.5 → 1.2 (overshoot) → 1.0 的彈跳曲線</summary>
    static float SpringPop(float n)
    {
        if (n < 0.4f)
        {
            float k = n / 0.4f;
            float ease = 1f - Mathf.Pow(1f - k, 3f); // EaseOutCubic
            return Mathf.Lerp(0.5f, 1.2f, ease);
        }
        else
        {
            float k = (n - 0.4f) / 0.6f;
            float ease = 1f - (1f - k) * (1f - k); // EaseOutQuad
            return Mathf.Lerp(1.2f, 1.0f, ease);
        }
    }
    public void ClearCardImages()
    {
        // 停所有 FX 並重置視覺
        if (_slotFx != null)
        {
            for (int i = 0; i < _slotFx.Length; i++)
            {
                if (_slotFx[i] != null) StopCoroutine(_slotFx[i]);
                _slotFx[i] = null;
                if (i < _slotStolenLock.Length) _slotStolenLock[i] = false;
                ResetSlotVisual(i);
            }
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].image.sprite = null;
            buttons[i].image.gameObject.SetActive(false);
        }

        // 重置追蹤狀態，下次第一次 update 不會誤觸發 FX
        if (_lastSlots != null)
        {
            for (int i = 0; i < _lastSlots.Length; i++) _lastSlots[i] = CardData.Empty();
        }
        _slotsInitialized = false;

        enableUpdate = false; // ✅ 清空後關閉 Update

        // 清除 Steal Outline
        SetAllStealOutlines(false);
        hadStealCard = false;
    }

    /// <summary>持有 Steal 卡時，所有 StealTarget 持續顯示 Outline，鎖定中的顯示綠色，未鎖定的紅色</summary>
    void UpdateStealOutlines()
    {
        if (userInventory == null) return;

        bool hasSteal = LocalPlayerHasStealCard();

        // 狀態沒變且沒持有 → 不需更新
        if (!hasSteal && !hadStealCard) return;

        // 狀態變化：失去 Steal 卡 → 關掉全部 Outline
        if (!hasSteal && hadStealCard)
        {
            SetAllStealOutlines(false);
            hadStealCard = false;
            return;
        }

        hadStealCard = hasSteal;

        // 持有 Steal 卡 → 更新 Outline 顏色
        if (hasSteal)
        {
            // 取得目前鎖定的 StealTargetObject
            var scanner = GetComponent<PlayerScanner>();
            StealTargetObject lockedTarget = scanner != null ? scanner.currentStealTarget : null;

            foreach (var obj in StealTargetObject.All)
            {
                if (obj == null) continue;
                obj.SetHighlight(true);

                // 鎖定中 → 綠色，未鎖定 → 紅色
                var outlines = obj.GetComponentsInChildren<Outline>(true);
                Color c = (lockedTarget != null && obj == lockedTarget) ? Color.green : Color.red;
                foreach (var outline in outlines)
                    if (outline != null) outline.OutlineColor = c;
            }
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
    public GameObject hintObject; // Hint UI
}
