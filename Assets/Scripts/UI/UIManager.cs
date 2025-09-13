using System;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public GameObject PeekUI;
    public GameObject GiveUI;
    public GameObject SwapUI;
    public GameObject BacnPackUI;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    public void SetPeekUISprites(Sprite[] sprites)
    {
        if (PeekUI == null)
        {
            Debug.LogError("PeekUI 沒有指定!");
            return;
        }

        // 找到所有子物件的 Image (排除按鈕上的Image)
        Image[] images = PeekUI.GetComponentsInChildren<Image>(true);

        int count = Mathf.Min(sprites.Length, 6);

        int replaced = 0;
        for (int i = 0; i < images.Length; i++)
        {
            // 假設你只要前6張圖片（而不是按鈕的背景）
            if (replaced < count)
            {
                images[i].sprite = sprites[replaced];
                replaced++;
            }
            else break;
        }
    }
        public void SetGiveUIButtons(Sprite[] sprites, Action<int> onClick)
    {
        if (GiveUI == null)
        {
            Debug.LogError("GiveUI 沒有指定!");
            return;
        }

        // 找到所有子物件的 Button
        Button[] buttons = GiveUI.GetComponentsInChildren<Button>(true);

        int count = Mathf.Min(sprites.Length, 6, buttons.Length);

        for (int i = 0; i < count; i++)
        {
            Image btnImage = buttons[i].GetComponent<Image>();
            if (btnImage != null)
                btnImage.sprite = sprites[i];

            int index = i; // 避免閉包問題

            // 清掉舊的 listener
            buttons[i].onClick.RemoveAllListeners();

            // 新增 listener
            buttons[i].onClick.AddListener(() =>
            {
                onClick?.Invoke(index);
            });
        }
    }
}

