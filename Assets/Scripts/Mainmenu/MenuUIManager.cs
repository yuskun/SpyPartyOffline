using TMPro;
using UnityEngine;
using Fusion;
using UnityEngine.UI;
using System;
using UnityEngine.UIElements;

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
    public UnityEngine.UI.Button StartButton;
    public MissionUIManager missionUIManager;
    public GameObject PlayerList;
    public UnityEngine.UI.Button ConfirmCharcterBtn;
    public UnityEngine.UI.Button[] CharacterButtons;
    public TextMeshProUGUI RoomCodeText;
    [Header("UI Documents")]
    public UnityEngine.UIElements.UIDocument hostRoomDocument;
    public UnityEngine.UIElements.UIDocument chooseCharacterDocument;
    public UnityEngine.UIElements.UIDocument createRoomDocument;

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
        InitCreateRoomUI();
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

    private void InitCreateRoomUI()
    {
        if (createRoomDocument == null) return;
        var root = createRoomDocument.rootVisualElement;

        // 找到 UXML 裡的 TextField
        var nameField = root.Q<UnityEngine.UIElements.TextField>("PlayerNameInput");

        if (nameField != null)
        {
            // 優先讀取 NetworkManager 裡的名字，如果那邊是空的，才給預設值
            string defaultName = string.IsNullOrEmpty(NetworkManager2.Instance.PlayerName) 
                                 ? "玩家" + UnityEngine.Random.Range(1000, 9999) 
                                 : NetworkManager2.Instance.PlayerName;
    
            // 顯示到 UI 上
            nameField.value = defaultName;
            
            // 同步回 NetworkManager，確保變數不是空的
            NetworkManager2.Instance.PlayerNamgeChanged(defaultName);
    
            // 綁定輸入事件
            nameField.RegisterValueChangedCallback(evt => {
                NetworkManager2.Instance.PlayerNamgeChanged(evt.newValue);
            });
        }
    }
}
