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
    }
    public void Draw()
    {
        DrawUI.SetActive(true);
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

