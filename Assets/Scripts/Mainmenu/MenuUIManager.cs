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
    [Header("新版 UI Panel（UniversalUIController）")]
    public UniversalUIController MainMenuPanel;
    public UniversalUIController SettingsPanel_1;
    public UniversalUIController CreateRoomPanel;
    public UniversalUIController ConfirmExit;
    public UniversalUIController PracticePanel;
    public UniversalUIController HostRoomPanel;
    public UniversalUIController CharSelectPanel;

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
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.None;
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

    /// <summary>主選單初始化：只顯示 MainMenuPanel，其他全部 Hide，滑鼠顯示</summary>
    public void MainMenuInit()
    {
        // Menu 面板
        if (MainMenuPanel != null)   MainMenuPanel.ShowCurrentUI();
        if (SettingsPanel_1 != null) SettingsPanel_1.HideCurrentUI();
        if (CreateRoomPanel != null) CreateRoomPanel.HideCurrentUI();
        if (ConfirmExit != null)     ConfirmExit.HideCurrentUI();
        if (PracticePanel != null)   PracticePanel.HideCurrentUI();
        if (HostRoomPanel != null)   HostRoomPanel.HideCurrentUI();
        if (CharSelectPanel != null) CharSelectPanel.HideCurrentUI();

        // Game 面板
        if (GameUIManager.Instance != null)
            GameUIManager.Instance.GameSceneInit_HideAll();

        // 滑鼠：主選單顯示
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>PrepareRoom 初始化：只顯示 HostRoomPanel，其他全部 Hide，滑鼠顯示</summary>
    public void PrepareInit()
    {
        // Menu 面板
        if (MainMenuPanel != null)   MainMenuPanel.HideCurrentUI();
        if (SettingsPanel_1 != null) SettingsPanel_1.HideCurrentUI();
        if (CreateRoomPanel != null) CreateRoomPanel.HideCurrentUI();
        if (ConfirmExit != null)     ConfirmExit.HideCurrentUI();
        if (PracticePanel != null)   PracticePanel.HideCurrentUI();
        if (HostRoomPanel != null)   HostRoomPanel.HideCurrentUI();
        // if (CharSelectPanel != null) CharSelectPanel.ShowCurrentUI();

        // Game 面板
        if (GameUIManager.Instance != null)
            GameUIManager.Instance.GameSceneInit_HideAll();

        // 滑鼠：準備房間顯示
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>根據當前場景 Index 自動呼叫對應的 Init</summary>
    public void ESCByCurrentScene()
    {
        
        int sceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        switch (sceneIndex)
        {
            case 0:
                MainMenuInit();
                break;
            case 1:
                PracticePanel.ShowCurrentUI();

                break;
            case 2:
            case 3:
                if (GameUIManager.Instance != null)
                    GameUIManager.Instance.GameMenuPanel.ShowCurrentUI();
                break;
        }
    }

    private void InitCreateRoomUI()
    {
        if (CreateRoomPanel == null) return;
        var root = CreateRoomPanel.GetComponent<UIDocument>().rootVisualElement;

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
