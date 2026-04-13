using System;
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


}

