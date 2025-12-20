using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CardUseUIManager : MonoBehaviour
{
    [System.Serializable]
    public struct FunctionCardUIBlock
    {
        public GameObject ui;
        public GameObject failUI;
        public Image[] userImages;
        public Image[] targetImages;
        public Button confirmButton;
        public Image preselectImage; //give預選圖片
    }

    public FunctionCardUIBlock giveBlock;
    public FunctionCardUIBlock peekBlock;
    public FunctionCardUIBlock swapBlock;

    public GameObject localBackpack;
    public float failMessageDuration = 2f;

    // 玩家選的卡片 index
    private int selectedUserIndex = -1;
    private int selectedTargetIndex = -1;
    private FunctionCard currentFunctionCard;
    private PlayerInventory currentUser, currentTarget;
    private int currentUseCardIndex;


    public void TryUseFunctionCard(FunctionCard card, PlayerInventory user, PlayerInventory target, int useCardIndex)
    {
        if (card == null)
            return;

        currentFunctionCard = card;
        currentUser = user;
        currentTarget = target;
        currentUseCardIndex = useCardIndex;

        if (card is Give giveCard)
        {
            if (giveCard.CanUse(user, target))
            {
                UpdateImagesByInventory(user, giveBlock.userImages);
                OpenUI(giveBlock.ui);
                DisableUsedCardButton(giveBlock);
                ChooseCard(giveBlock);
                BindConfirmButton(giveBlock);
            }
            else
            {
                ShowFailUI(giveBlock.failUI);
            }

        }
        else if (card is Peek peekCard)
        {
            if (peekCard.CanUse(user, target))
            {
                UpdateImagesByInventory(target, peekBlock.targetImages);
                OpenUI(peekBlock.ui);

                CardUseParameters parameters = new CardUseParameters();
                // parameters.UserId = user.playerId;
                parameters.UserId = user.GetComponent<PlayerIdentify>().PlayerID;
                parameters.UseCardIndex = currentUseCardIndex;
                parameters.Card = currentUser.slots[currentUseCardIndex];
                parameters.TargetId = target.GetComponent<PlayerIdentify>().PlayerID;
                GameManager.instance.Rpc_RequestUseCard(parameters);
            }
            else
                ShowFailUI(peekBlock.failUI);
        }
        else if (card is Swap swapCard)
        {
            if (swapCard.CanUse(user, target))
            {
                UpdateImagesByInventory(user, swapBlock.userImages);
                UpdateImagesByInventory(target, swapBlock.targetImages);
                OpenUI(swapBlock.ui);
                DisableUsedCardButton(swapBlock);
                ChooseCard(swapBlock);
                BindConfirmButton(swapBlock);
            }
            else
                ShowFailUI(swapBlock.failUI);
        }
    }
       public bool TryUseMissionCard(MissionCard card, PlayerInventory user, PlayerInventory target)
    {
        if (card == null)
            return false;

        if (card is Steal StealCard)
        {
            if (!StealCard.CanUse(user, target, user.slots[currentUseCardIndex]))
                return false;
        }
        else if (card is Catch CatchCard)
        {
            if (!CatchCard.CanUse(user, target, user.slots[currentUseCardIndex]))
                return false;
        }
        return true;

    }

    // 選哪張卡
    private void ChooseCard(FunctionCardUIBlock block)
    {
        for (int i = 0; i < block.userImages.Length; i++)
        {
            int idx = i;
            block.userImages[i].GetComponent<Button>().onClick.RemoveAllListeners();
            //block.userImages[i].GetComponent<Button>().onClick.AddListener(() => selectedUserIndex = idx);

            block.userImages[i].GetComponent<Button>().onClick.AddListener(() =>
            {
                selectedUserIndex = idx;
                if (block.ui == giveBlock.ui && block.preselectImage != null) //只有give需要顯示預選圖片
                {
                    block.preselectImage.sprite = block.userImages[idx].sprite;
                    block.preselectImage.gameObject.SetActive(true);
                }
                Debug.Log($"[User Image Clicked] Index: {idx}");
            });
        }
        for (int i = 0; i < block.targetImages.Length; i++)
        {
            int idx = i;
            block.targetImages[i].GetComponent<Button>().onClick.RemoveAllListeners();
            //block.targetImages[i].GetComponent<Button>().onClick.AddListener(() => selectedTargetIndex = idx);
            block.targetImages[i].GetComponent<Button>().onClick.AddListener(() =>
            {
                selectedTargetIndex = idx;
                Debug.Log($"[Target Image Clicked] Index: {idx}");
            });
        }
    }
    // 綁定確認按鈕
    private void BindConfirmButton(FunctionCardUIBlock block)
    {
        block.confirmButton.onClick.RemoveAllListeners();
        block.confirmButton.onClick.AddListener(OnConfirmButtonClicked);
    }

    // 確認按鈕事件（Give/Swap 共用）
    public void OnConfirmButtonClicked()
    {
        CardUseParameters parameters = new CardUseParameters();
        // parameters.UserId = currentUser.playerId;
        // parameters.TargetId = currentTarget.playerId;
        parameters.UserId = currentUser.GetComponent<PlayerIdentify>().PlayerID;
        parameters.TargetId = currentTarget.GetComponent<PlayerIdentify>().PlayerID;
        parameters.UseCardIndex = currentUseCardIndex;
        parameters.SelectIndex = selectedUserIndex;
        parameters.TargetSelectIndex = selectedTargetIndex;
        parameters.Card = currentUser.slots[currentUseCardIndex];
        GameManager.instance.Rpc_RequestUseCard(parameters);

        // 關閉UI、重設選擇
        if (currentFunctionCard is Give)
            CloseUI(giveBlock.ui);
        else if (currentFunctionCard is Swap)
            CloseUI(swapBlock.ui);
        selectedUserIndex = -1;
        selectedTargetIndex = -1;
    }

    private void OpenUI(GameObject ui)
    {
        localBackpack.SetActive(false);
        ui.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void ShowFailUI(GameObject failUI) // fail文字顯示幾秒後關閉
    {
        failUI.SetActive(true);
        StartCoroutine(CloseAfterSeconds(failUI, failMessageDuration));
    }

    private IEnumerator CloseAfterSeconds(GameObject ui, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        ui.SetActive(false);
    }

    public void CloseUI(GameObject ui)
    {
        ui.SetActive(false);
        localBackpack.SetActive(true);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void UpdateImagesByInventory(PlayerInventory inv, Image[] images)
    {
        Sprite[] sprites = CardManager.Instance.GetCardInfo(inv.GetAllCards());
        for (int i = 0; i < images.Length; i++)
        {
            if (i < sprites.Length && sprites[i] != null)
            {
                images[i].sprite = sprites[i];
                images[i].gameObject.SetActive(true);
            }
            else
            {
                images[i].sprite = null;
                images[i].gameObject.SetActive(false);
            }
        }
    }
    private void DisableUsedCardButton(FunctionCardUIBlock block)
    {
        for (int i = 0; i < block.userImages.Length; i++)
        {
            var btn = block.userImages[i].GetComponent<Button>();
            btn.interactable = (i != currentUseCardIndex);
        }
    }
}
