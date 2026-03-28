using TMPro;
using UnityEngine;
using Fusion;
using UnityEngine.UI;
using System;

public class MenuUIManager : MonoBehaviour
{

    static public MenuUIManager instance;
    //介面UI
    public GameObject Menu;
    public GameObject Gameroom;
    public GameObject Host;
    public GameObject Client;
    public GameObject BulidOrJoin;
    public GameObject LoadingScreen;
    public GameObject JoinRoomList;
    public GameObject ChooseCharacterUI;
    public GameObject Practice;
    

//元件

    public TextMeshProUGUI PlayerNameInput;
    public GameObject[] Ai;
   [HideInInspector] public PlayerListManager playerlistmanager;
    public Button StartButton;
    public MissionUIManager missionUIManager;
    public GameObject PlayerList;
    public Button ConfirmCharcterBtn;
    public Button[] CharacterButtons;
    [Header("UI Documents")]
    public UnityEngine.UIElements.UIDocument hostRoomDocument;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Start()
    {

        Menu.SetActive(true);
        Gameroom.SetActive(false);
        BulidOrJoin.SetActive(false);
        LoadingScreen.SetActive(false);
        JoinRoomList.SetActive(false);
    }
    void Update()
    {
        if (playerlistmanager != null)
            playerlistmanager.Check();
    }
    public void showUI(GameObject target)
    {
        Menu.SetActive(false);
        Gameroom.SetActive(false);
        BulidOrJoin.SetActive(false);
        LoadingScreen.SetActive(false);
        JoinRoomList.SetActive(false);
        target.SetActive(true);
    }
    public void ExitGame()
    {
        Application.Quit();
    }
    public void ShowGameroom(GameMode mode)
    {
        if (mode == GameMode.Host)
        {
            Host.SetActive(true);
            Client.SetActive(false);

            showUI(Gameroom);
        }
        else if (mode == GameMode.Client)
        {
            Host.SetActive(false);
            Client.SetActive(true);

            showUI(Gameroom);
        }
    }
    public void CloseAi()
    {
        foreach (var item in Ai)
        {
            item.SetActive(false);
        }
    }
    public void AllowInput(bool allow)
    {

        LocalBackpack.Instance.userInventory.gameObject.GetComponent<NetworkPlayer>().AllowInput = allow;

    }
}
