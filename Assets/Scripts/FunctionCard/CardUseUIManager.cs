using System.Collections;
using UnityEngine;

public class CardUseUIManager : MonoBehaviour
{
    public GameObject giveUI;
    public GameObject giveFailUI;
    public GameObject peekUI;
    public GameObject peekFailUI;
    public GameObject swapUI;
    public GameObject swapFailUI;

    public float failMessageDuration = 2f;

    public void TryUseFunctionCard(FunctionCard card, PlayerInventory user, PlayerInventory target)
    {
        if (card == null)
            return;

        if (card is Give giveCard)
        {
            if (giveCard.CanUse(user, target))
                OpenUI(giveUI);
            else
                ShowFailUI(giveFailUI);
        }
        else if (card is Peek peekCard)
        {
            if (peekCard.CanUse(user, target))
                OpenUI(peekUI);
            else
                ShowFailUI(peekFailUI);
        }
        else if (card is Swap swapCard)
        {
            if (swapCard.CanUse(user, target))
                OpenUI(swapUI);
            else
                ShowFailUI(swapFailUI);
        }
    }

    private void OpenUI(GameObject ui)
    {
        ui.SetActive(true);
        ShowCursor(true);
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

    private void ShowCursor(bool show)
    {
        Cursor.visible = show;
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public void CloseUI(GameObject ui)
    {
        ui.SetActive(false);

        // 如果沒有任何 UI 還開著，就隱藏滑鼠
        if (!IsAnyUIOpen())
        {
            ShowCursor(false);
        }
    }

    private bool IsAnyUIOpen()
    {
        return giveUI.activeSelf || peekUI.activeSelf || swapUI.activeSelf;
    }
}
