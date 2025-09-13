using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LocalBackpack : MonoBehaviour
{
    public static LocalBackpack Instance;
    public int FocusIndex = 0;
    public int SlotCount = 0;
    public List<Button> button = new List<Button>();
    private bool canInvoke = true;

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

        SlotCount = button.Count;
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
        if (Input.GetMouseButtonDown(0)) // 左鍵按下
        {
            if (FocusIndex >= 0 && FocusIndex < button.Count&&button[FocusIndex].interactable)
            {
                button[FocusIndex].onClick.Invoke(); // 觸發該按鈕事件
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
}
