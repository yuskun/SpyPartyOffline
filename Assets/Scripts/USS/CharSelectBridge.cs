using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using ExitGames.Client.Photon.StructWrapping;

public class CharSelectBridge : MonoBehaviour
{
    private UIDocument _doc;
    private List<Button> _portraitBtns = new List<Button>();

    void OnEnable()
    {
        _doc = GetComponent<UIDocument>();
        var root = _doc.rootVisualElement;

        // 1. 抓取所有頭像按鈕 (class="portrait")
        _portraitBtns = root.Query<Button>(className: "portrait").ToList();
        
        for (int i = 0; i < _portraitBtns.Count; i++)
        {
            int index = i;
            _portraitBtns[i].focusable = false; // 防止空白鍵誤觸
            _portraitBtns[i].clicked += () => {
                // 直接呼叫舊腳本裡的更換皮膚功能
                SkinChange.instance.changeSkin(index);
                UpdateUISelection(index);
            };
        }

        // 2. 確定按鈕
        var selectBtn = root.Q<Button>("SelectBtn");
        if (selectBtn != null) {
            selectBtn.focusable = false;
            selectBtn.clicked += () => {
                // 模擬點擊原本舊 UI 的確定鈕
                MenuUIManager.instance.ConfirmCharcterBtn.onClick.Invoke();
                // 關閉新面板
                this.gameObject.GetComponent<UniversalUIController>().HideCurrentUI();
            };
        }

        // 3. 返回按鈕
        var backBtn = root.Q<Button>("BackBtn");
        if (backBtn != null) {
            backBtn.focusable = false;
            //backBtn.clicked += () => this.gameObject.SetActive(false);
            backBtn.clicked += () => {
                // 如果返回需要先恢復成上次存檔的皮膚，可以加這行：
                // SkinChange.instance.changeSkin(PlayerPrefs.GetInt("Choosenindex", 0));
                SkinChange.instance.BackAndCloseAllUI();
            };
        }
    }

    // 更新新版 UI 的選中框顏色
    void UpdateUISelection(int selectedIndex)
    {
        for (int i = 0; i < _portraitBtns.Count; i++)
        {
            if (i == selectedIndex) _portraitBtns[i].AddToClassList("selected");
            else _portraitBtns[i].RemoveFromClassList("selected");
        }
    }

}