using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;
using System.Linq; // 必須引用以使用 Cast

public class CardUseUIManager_UIToolkit : MonoBehaviour
{
    [Header("UI Documents")]
    public UIDocument gameHUDDocument;
    public UIDocument giveUIDocument;
    public UIDocument peekUIDocument;
    public UIDocument swapUIDocument;

    [Header("失敗提示 UI")]
    public GameObject giveFailUI;
    public GameObject peekFailUI;
    public GameObject swapFailUI;

    public GameObject localBackpack;
    public float failMessageDuration = 2f;

    private int selectedUserIndex = -1;
    private int selectedTargetIndex = -1;
    private FunctionCard currentFunctionCard;
    private PlayerInventory currentUser, currentTarget;
    private int currentUseCardIndex;

    // ────────────────────────────────────────────────
    // 公開入口
    // ────────────────────────────────────────────────

    public void TryUseFunctionCard(FunctionCard card, PlayerInventory user, PlayerInventory target, int useCardIndex)
    {
        if (card == null) return;

        currentFunctionCard = card;
        currentUser = user;
        currentTarget = target;
        currentUseCardIndex = useCardIndex;
        selectedUserIndex = -1;
        selectedTargetIndex = -1;

        if (card is Give)
        {
            if (!((Give)card).CanUse(user, target)) { ShowFailUI(giveFailUI); return; }
            OpenGiveUI();
        }
        else if (card is Peek)
        {
            if (!((Peek)card).CanUse(user, target)) { ShowFailUI(peekFailUI); return; }
            OpenPeekUI();
        }
        else if (card is Swap)
        {
            if (!((Swap)card).CanUse(user, target)) { ShowFailUI(swapFailUI); return; }
            OpenSwapUI();
        }
    }

    // ────────────────────────────────────────────────
    // 介面開啟邏輯
    // ────────────────────────────────────────────────

    private void OpenGiveUI()
    {
        giveUIDocument.gameObject.SetActive(true);
        VisualElement root = giveUIDocument.rootVisualElement;

        UpdateUIInfo(root, currentUser, currentTarget);

        //道具
        List<Button> slots = root.Query<Button>(className: "item-slot").ToList();
        VisualElement preselectIcon = root.Q<VisualElement>("SelectedGiftIcon");
        if (preselectIcon != null) preselectIcon.style.backgroundImage = StyleKeyword.None;

        RefreshInventorySlots(currentUser, slots, (idx) => {
            selectedUserIndex = idx;
            if (preselectIcon != null)
            {
                Image icon = slots[idx].Q<Image>();
                if (icon != null) preselectIcon.style.backgroundImage = icon.style.backgroundImage;
                preselectIcon.style.display = DisplayStyle.Flex;
            }
            UpdateSelectionUI(slots, idx);
        }, true);

        BindCommonButtons(root, giveUIDocument);
        SetOpenState();
    }

    private void OpenPeekUI()
    {
        peekUIDocument.gameObject.SetActive(true);
        VisualElement root = peekUIDocument.rootVisualElement;

        var targetIdentify = currentTarget.GetComponent<PlayerIdentify>();
        var db = SkinChange.instance?.characterAvatarDatabase;
    
        // 設定對方頭像與名字
        if (db != null)
            root.Q<Image>(className: "avatar-target").sprite = db.GetAvatar(targetIdentify.SkinIndex);
        
        var nameLabel = root.Q<Label>(className: "player-name-bottom");
        if (nameLabel != null) nameLabel.text = targetIdentify.PlayerName;

        //道具
        List<Button> slots = root.Query<Button>(className: "item-slot").ToList();
        RefreshInventorySlots(currentTarget, slots, (idx) => { }, false);

        Button closeBtn = root.Q<Button>("CloseBtn");
        if (closeBtn != null) 
        { 
            closeBtn.clicked -= () => CloseUI(peekUIDocument); 
            closeBtn.clicked += () => CloseUI(peekUIDocument); 
        }

        SetOpenState();
        GameManager.instance.Rpc_RequestUseCard(BuildParameters());
    }

    private void OpenSwapUI()
    {
        swapUIDocument.gameObject.SetActive(true);
        VisualElement root = swapUIDocument.rootVisualElement;

        UpdateUIInfo(root, currentUser, currentTarget);

        List<VisualElement> panels = root.Query<VisualElement>(className: "item-panel").ToList();
        if (panels.Count >= 2)
        {
            List<Button> userSlots = panels[0].Query<Button>(className: "item-slot").ToList();
            RefreshInventorySlots(currentUser, userSlots, (idx) => {
                selectedUserIndex = idx;
                UpdateSelectionUI(userSlots, idx);
            }, true);

            List<Button> targetSlots = panels[1].Query<Button>(className: "item-slot").ToList();
            RefreshInventorySlots(currentTarget, targetSlots, (idx) => {
                selectedTargetIndex = idx;
                UpdateSelectionUI(targetSlots, idx);
            }, false);
        }

        BindCommonButtons(root, swapUIDocument);
        SetOpenState();
    }

    // ────────────────────────────────────────────────
    // 核心工具方法
    // ────────────────────────────────────────────────

    private void RefreshInventorySlots(PlayerInventory inv, List<Button> slots, System.Action<int> onSelect, bool isUser)
    {
        Sprite[] sprites = CardManager.Instance.GetCardInfo(inv.GetAllCards());

        for (int i = 0; i < slots.Count; i++)
        {
            int idx = i;
            Button slotBtn = slots[i];
            Image icon = slotBtn.Q<Image>();

            slotBtn.RemoveFromClassList("selected");
            slotBtn.style.display = DisplayStyle.Flex;

            if (i < sprites.Length && sprites[i] != null)
            {
                if (icon != null)
                {
                    icon.style.backgroundImage = new StyleBackground(sprites[i]);
                    icon.style.display = DisplayStyle.Flex;
                }

                bool isBeingUsed = isUser && i == currentUseCardIndex;
                if (isBeingUsed)
                {
                    slotBtn.SetEnabled(false);
                    slotBtn.style.opacity = 0.5f;
                }
                else
                {
                    slotBtn.SetEnabled(true);
                    slotBtn.style.opacity = 1f;
                    slotBtn.clicked -= () => onSelect(idx);
                    slotBtn.clicked += () => onSelect(idx);
                }
            }
            else
            {
                if (icon != null) icon.style.backgroundImage = StyleKeyword.None;
                slotBtn.SetEnabled(false);
                slotBtn.style.opacity = 1f;
            }
        }
    }

    private void UpdateSelectionUI(List<Button> slots, int selectedIdx)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (i == selectedIdx) slots[i].AddToClassList("selected");
            else slots[i].RemoveFromClassList("selected");
        }
    }

    private void BindCommonButtons(VisualElement root, UIDocument doc)
    {
        Button confirmBtn = root.Q<Button>("ConfirmBtn");
        if (confirmBtn != null) 
        { 
            confirmBtn.clicked -= OnConfirmButtonClicked; 
            confirmBtn.clicked += OnConfirmButtonClicked; 
        }

        Button cancelBtn = root.Q<Button>("CancelBtn");
        if (cancelBtn != null) 
        { 
            cancelBtn.clicked -= () => CloseUI(doc); 
            cancelBtn.clicked += () => CloseUI(doc); 
        }
    }

    // ────────────────────────────────────────────────
    // 系統功能
    // ────────────────────────────────────────────────

    public void OnConfirmButtonClicked()
    {
        GameManager.instance.Rpc_RequestUseCard(BuildParameters());
        if (currentFunctionCard is Give) CloseUI(giveUIDocument);
        else if (currentFunctionCard is Swap) CloseUI(swapUIDocument);
    }

    private CardUseParameters BuildParameters()
    {
        return new CardUseParameters
        {
            UserId = currentUser.GetComponent<PlayerIdentify>().PlayerID,
            TargetId = currentTarget.GetComponent<PlayerIdentify>().PlayerID,
            UseCardIndex = currentUseCardIndex,
            SelectIndex = selectedUserIndex,
            TargetSelectIndex = selectedTargetIndex,
            Card = currentUser.slots[currentUseCardIndex],
        };
    }

    private void SetOpenState()
    {
        localBackpack.SetActive(false);
        if (gameHUDDocument != null) 
            gameHUDDocument.gameObject.SetActive(false);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        CharacterSFXManager.Instance?.PlayOpenUI();
    }

    public void CloseUI(UIDocument doc)
    {
        if (doc != null) doc.gameObject.SetActive(false);

        if (gameHUDDocument != null) 
            gameHUDDocument.gameObject.SetActive(true);
        localBackpack.SetActive(true);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        selectedUserIndex = -1;
        selectedTargetIndex = -1;
    }

    private void ShowFailUI(GameObject failUI)
    {
        if (failUI == null) return;
        failUI.SetActive(true);
        StartCoroutine(CloseAfterSeconds(failUI, failMessageDuration));
    }

    private IEnumerator CloseAfterSeconds(GameObject ui, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        ui.SetActive(false);
    }

    private void UpdateUIInfo(VisualElement root, PlayerInventory user, PlayerInventory target)
    {
        var userIdentify = user.GetComponent<PlayerIdentify>();
        var targetIdentify = target.GetComponent<PlayerIdentify>();
        var db = SkinChange.instance?.characterAvatarDatabase;

        if (db != null)
        {
            // 設定自己的頭像
            var selfImg = root.Q<Image>(className: "avatar-self");
            if (selfImg != null) selfImg.sprite = db.GetAvatar(userIdentify.SkinIndex);

            // 設定對方的頭像
            var targetImg = root.Q<Image>(className: "avatar-target");
            if (targetImg != null) targetImg.sprite = db.GetAvatar(targetIdentify.SkinIndex);
        }

        // 設定對方的名字 (對應 UXML 中的 Label)
        var targetNameLabel = root.Query<Label>(className: "player-name-bottom").Last(); // 通常最後一個是目標
        if (targetNameLabel != null) targetNameLabel.text = targetIdentify.PlayerName;
    }
}