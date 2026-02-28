using System;
using TMPro;
using UnityEngine;
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

