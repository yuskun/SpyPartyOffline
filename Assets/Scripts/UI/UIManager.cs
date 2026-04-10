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

    [Header("結算舊版 UGUI 面板（遊戲結算時顯示）")]
    public GameObject EndUI;

    [Header("多人勝利結算面板（MULTIPYWIN）")]
    public GameObject MultipleWinUI;

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
         PauseUI.SetActive(false);
         if (GameHudUI != null) GameHudUI.SetVisible(false);
         if (ResultsPanel != null) ResultsPanel.SetActive(false);
         if (EndUI != null) EndUI.SetActive(false);
         if (MultipleWinUI != null) MultipleWinUI.SetActive(false);
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
        // 關掉新版 UIDocument 的 HUD（任務欄、卡片欄等）
        if (GameHudUI != null) GameHudUI.SetVisible(false);
        if (ResultsPanel != null) ResultsPanel.SetActive(true);
        if (ResultsBgPlane.Instance != null) ResultsBgPlane.Instance.SlideIn();
        // ⚠️ 注意：EndUI（舊版 UGUI 結算面板）不在這裡開，
        // 必須等結算動畫跑完才跳出來，由 ShowEndUI() 在 ResultsSequence 的最後呼叫。
    }

    /// <summary>
    /// 結算動畫結束後才呼叫，跳出舊版 UGUI 結算面板（EndUI）。
    /// 由 GameManager.ResultsSequence / SpectatorResultsSequence 在動畫完成後觸發。
    /// </summary>
    public void ShowEndUI()
    {
        if (EndUI != null) EndUI.SetActive(true);
    }

    /// <summary>
    /// 多人勝利時用這個 UI 取代 EndUI。
    /// winnerIDs 會先寫進 MultiWinnerPanelPopulator.PendingWinnerIDs，
    /// SetActive(true) 之後 Populator 的 OnEnable 會自動填充玩家列表。
    /// </summary>
    public void ShowMultiWinUI(int[] winnerIDs)
    {
        if (MultipleWinUI == null) return;
        MultiWinnerPanelPopulator.PendingWinnerIDs = winnerIDs;
        MultipleWinUI.SetActive(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            /*if (GameHudUI.activeSelf)
            {
                if (PauseUI.activeSelf)
                {
                    PauseUI.SetActive(false);
                    if (GameHudUI != null) GameHudUI.SetVisible(false);
                    HUDUI.SetActive(false);
                }
                else
                {
                    PauseUI.SetActive(true);
                    if (GameHudUI != null) GameHudUI.SetVisible(true);
                    HUDUI.SetActive(true);
                }
            }*/

            if (GameHudUI != null && GameHudUI.gameObject.activeSelf) 
            {
                if (PauseUI.activeSelf)
                {
                    PauseUI.SetActive(false);
                    if (GameHudUI != null) GameHudUI.SetVisible(true);
                    HUDUI.SetActive(true);
                }
                else
                {
                    GameHudUI.SetVisible(false);
                    HUDUI.SetActive(false);
                    PauseUI.SetActive(true);
                }
            }
        }

        
    }


}

