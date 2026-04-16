using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance;

    public GameObject GameoverText;
    public GameObject WinText;
    public GameObject HUDUI;
    public TextMeshProUGUI timerText;
    public MissionUIManager missionUIManager;
    public GameObject PauseUI;
    public Image progressfill;
    public GameObject progressBar;
    public Image UserCardUI;
    public GameObject Notification;
    public GameObject DrawUI;
    public GameObject ResultsPanel;
    public GameObject CaughtUI;
    public GameObject BackBtn;

    [Header("新版 UI Panel（UniversalUIController）")]
    public UniversalUIController GameHUDPanel;
    public UniversalUIController GameMenuPanel;
    public UniversalUIController GameConfirmExit;
    public UniversalUIController SettingsPanel_Scene2;
    public UniversalUIController ItemIntroPanel;

    public UniversalUIController GameHudUI;

    // ────────────────────────────────────────────────
    // 使用道具失敗提示 UI
    // ────────────────────────────────────────────────
    public enum CardUseFailReason
    {
        None,
        NoTarget,       // 1. 沒有鎖定到玩家
        TargetFull,     // 2. 對方背包滿了
        TargetEmpty,    // 3. 對方沒有物品
        SelfNotEnough   // 4. 自身沒有多的物品
    }

    [Header("使用道具失敗提示 UI")]
    public GameObject FailUI_NoTarget;
    public GameObject FailUI_TargetFull;
    public GameObject FailUI_TargetEmpty;
    public GameObject FailUI_SelfNotEnough;
    [SerializeField] private float failUIDuration = 2f;

    private Coroutine _failUICoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    public void Gameover()
    {
        GameoverText.SetActive(true);
    }
    public void Win()
    {
        WinText.SetActive(true);
    }
    public void init()
    {
         WinText.SetActive(false);
         GameoverText.SetActive(false);
         HUDUI.SetActive(false);
         if (GameHUDPanel != null) GameHUDPanel.HideCurrentUI();
         PauseUI.SetActive(false);
         if (GameHudUI != null) GameHudUI.SetVisible(false);
         if (ResultsPanel != null) ResultsPanel.SetActive(false);
    }
    public void Draw()
    {
        DrawUI.SetActive(true);
    }

    public void ShowResultsPanel()
    {
        WinText.SetActive(false);
        GameoverText.SetActive(false);
        HUDUI.SetActive(false);
        if (GameHUDPanel != null) GameHUDPanel.HideCurrentUI();
        if (ResultsPanel != null) ResultsPanel.SetActive(true);
        if (ResultsBgPlane.Instance != null) ResultsBgPlane.Instance.SlideIn();
    }

    /// <summary>GameScene 初始化：只顯示 GameHUDPanel，其他全部 Hide，滑鼠隱藏鎖定</summary>
    public void GameSceneInit()
    {
        if (GameHUDPanel != null)        GameHUDPanel.ShowCurrentUI();
        if (GameMenuPanel != null)       GameMenuPanel.HideCurrentUI();
        if (GameConfirmExit != null)     GameConfirmExit.HideCurrentUI();
        if (SettingsPanel_Scene2 != null) SettingsPanel_Scene2.HideCurrentUI();
        if (ItemIntroPanel != null)      ItemIntroPanel.HideCurrentUI();

        // Menu 面板也全關
        if (MenuUIManager.instance != null)
        {
            if (MenuUIManager.instance.MainMenuPanel != null)   MenuUIManager.instance.MainMenuPanel.HideCurrentUI();
            if (MenuUIManager.instance.SettingsPanel_1 != null) MenuUIManager.instance.SettingsPanel_1.HideCurrentUI();
            if (MenuUIManager.instance.CreateRoomPanel != null) MenuUIManager.instance.CreateRoomPanel.HideCurrentUI();
            if (MenuUIManager.instance.ConfirmExit != null)     MenuUIManager.instance.ConfirmExit.HideCurrentUI();
            if (MenuUIManager.instance.PracticePanel != null)   MenuUIManager.instance.PracticePanel.HideCurrentUI();
            if (MenuUIManager.instance.HostRoomPanel != null)   MenuUIManager.instance.HostRoomPanel.HideCurrentUI();
            if (MenuUIManager.instance.CharSelectPanel != null) MenuUIManager.instance.CharSelectPanel.HideCurrentUI();
        }

        // 滑鼠：遊戲中隱藏並鎖定
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>輔助：Game 面板全部 Hide（給 MenuUIManager 的 Init 呼叫）</summary>
    public void GameSceneInit_HideAll()
    {
        if (GameHUDPanel != null)        GameHUDPanel.HideCurrentUI();
        if (GameMenuPanel != null)       GameMenuPanel.HideCurrentUI();
        if (GameConfirmExit != null)     GameConfirmExit.HideCurrentUI();
        if (SettingsPanel_Scene2 != null) SettingsPanel_Scene2.HideCurrentUI();
        if (ItemIntroPanel != null)      ItemIntroPanel.HideCurrentUI();
    }

    // ────────────────────────────────────────────────
    // 使用道具失敗 UI：依原因顯示對應提示，自動關閉
    // ────────────────────────────────────────────────
    public void ShowCardFailUI(CardUseFailReason reason)
    {
        HideAllFailUIs();

        GameObject target = null;
        switch (reason)
        {
            case CardUseFailReason.NoTarget:      target = FailUI_NoTarget;      break;
            case CardUseFailReason.TargetFull:    target = FailUI_TargetFull;    break;
            case CardUseFailReason.TargetEmpty:   target = FailUI_TargetEmpty;   break;
            case CardUseFailReason.SelfNotEnough: target = FailUI_SelfNotEnough; break;
        }
        if (target == null) return;

        target.SetActive(true);

        if (_failUICoroutine != null) StopCoroutine(_failUICoroutine);
        _failUICoroutine = StartCoroutine(CloseFailUIAfter(target, failUIDuration));
    }

    private void HideAllFailUIs()
    {
        if (FailUI_NoTarget      != null) FailUI_NoTarget.SetActive(false);
        if (FailUI_TargetFull    != null) FailUI_TargetFull.SetActive(false);
        if (FailUI_TargetEmpty   != null) FailUI_TargetEmpty.SetActive(false);
        if (FailUI_SelfNotEnough != null) FailUI_SelfNotEnough.SetActive(false);
    }

    private IEnumerator CloseFailUIAfter(GameObject ui, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (ui != null) ui.SetActive(false);
        _failUICoroutine = null;
    }
}

