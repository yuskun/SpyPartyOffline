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
        if (ResultsPanel != null) ResultsPanel.SetActive(true);
        if (ResultsBgPlane.Instance != null) ResultsBgPlane.Instance.SlideIn();
    }

    void FixedUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (HUDUI.activeSelf)
            {
                if (PauseUI.activeSelf)
                {
                    PauseUI.SetActive(false);
                }
                else
                {
                    PauseUI.SetActive(true);
                }
            }
        }
    }


}

