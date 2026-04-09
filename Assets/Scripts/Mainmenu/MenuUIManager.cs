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
    public GameObject RoomCode;
    
    

//元件

    public TMP_InputField PlayerNameInput;
    public TMP_InputField RoomCodeInput;
    public GameObject[] Ai;
    public PlayerListManager playerlistmanager;
    public Button StartButton;
    public MissionUIManager missionUIManager;
    public GameObject PlayerList;
    public Button ConfirmCharcterBtn;
    public Button[] CharacterButtons;
     public TextMeshProUGUI RoomCodeText;
    [Header("UI Documents")]
    public UnityEngine.UIElements.UIDocument hostRoomDocument;
    public UnityEngine.UIElements.UIDocument chooseCharacterDocument;
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
        // RoomCode.SetActive(false);
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

    public void ShowRoomCodePanel()
    {
        showUI(RoomCode);
        if (RoomCodeInput != null) RoomCodeInput.text = "";
    }

    /// <summary>從「旁觀」按鈕呼叫：快速搜尋房間並以旁觀者加入</summary>
    public void QuickJoinAsSpectator()
    {
        NetworkManager2.Instance.QuickJoinAsSpectator();
    }

    public void ConfirmJoinByCode()
    {
        if (RoomCodeInput == null) return;
        string code = RoomCodeInput.text.Trim();
        if (string.IsNullOrEmpty(code)) return;
        NetworkManager2.Instance.JoinByCode(code);
    }
    public void ExitGame()
    {
        Application.Quit();
    }
    public void ShowGameroom(GameMode mode, string roomCode = "")
    {
        if (RoomCodeText != null)
            RoomCodeText.text = roomCode;

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
        if (NetworkManager2.IsSpectator) return;
        LocalBackpack.Instance.userInventory.gameObject.GetComponent<NetworkPlayer>().AllowInput = allow;
    }
}
