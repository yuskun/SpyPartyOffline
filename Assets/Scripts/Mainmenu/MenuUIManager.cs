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
    [Tooltip("新版 UIDocument 主選單面板（斷線返回時要重新啟用）")]
    public GameObject MainMenuPanel;

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
    // 追蹤 createRoomDocument 的啟用狀態，只在轉為啟用的那一幀初始化
    private bool _createRoomPrevActive = false;
    private UnityEngine.UIElements.TextField _nameFieldCached;

    void Update()
    {
        if (playerlistmanager != null)
            playerlistmanager.Check();

        // 只在 createRoomDocument 從未啟用 → 啟用 的瞬間做一次初始化
        bool isActive = createRoomDocument != null && createRoomDocument.gameObject.activeInHierarchy;
        if (isActive && !_createRoomPrevActive)
        {
            // 延後一幀確保 UIDocument 已重建 rootVisualElement
            StartCoroutine(InitCreateRoomUINextFrame());
        }
        _createRoomPrevActive = isActive;
    }

    private System.Collections.IEnumerator InitCreateRoomUINextFrame()
    {
        yield return null;
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

    /// <summary>斷線/離開房間後重置回主選單狀態：關掉所有遊戲中 UI，打開新版 MainMenuPanel</summary>
    public void ResetToMainMenu()
    {
        // 關掉所有大廳/遊戲中相關的新版 UIDocument
        if (hostRoomDocument != null)        hostRoomDocument.gameObject.SetActive(false);
        if (createRoomDocument != null)      createRoomDocument.gameObject.SetActive(false);
        if (chooseCharacterDocument != null) chooseCharacterDocument.gameObject.SetActive(false);

        // 打開新版主選單
        if (MainMenuPanel != null && !MainMenuPanel.activeSelf)
            MainMenuPanel.SetActive(true);
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

        // ✅ 關鍵修正：StartGameBtn 會把 HostRoomPanel SetActive(false)，
        // 第二次進入 Gameroom 時必須主動重新啟用，
        // 否則 UIDocument 永遠不會再觸發 OnEnable，畫面就不會出現。
        if (hostRoomDocument != null && !hostRoomDocument.gameObject.activeSelf)
            hostRoomDocument.gameObject.SetActive(true);

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
        if (root == null)
        {
            Debug.LogWarning("createRoomDocument.rootVisualElement 尚未就緒");
            return;
        }

        // 找到 UXML 裡的 TextField
        var nameField = root.Q<UnityEngine.UIElements.TextField>("PlayerNameInput");
        if (nameField == null)
        {
            Debug.LogError("找不到 PlayerNameInput");
            return;
        }

        // 優先讀取 NetworkManager 裡的名字，如果那邊是空的，才給預設值
        string defaultName = string.IsNullOrEmpty(NetworkManager2.Instance.PlayerName)
                             ? "玩家" + UnityEngine.Random.Range(1000, 9999)
                             : NetworkManager2.Instance.PlayerName;

        // 先解掉舊的 callback 再設值，避免觸發並堆疊註冊
        nameField.UnregisterValueChangedCallback(OnPlayerNameFieldChanged);
        nameField.SetValueWithoutNotify(defaultName);

        // 同步回 NetworkManager，確保變數不是空的
        NetworkManager2.Instance.PlayerNamgeChanged(defaultName);

        // 綁定輸入事件（具名方法，Unregister 才抓得到同一個委派）
        nameField.RegisterValueChangedCallback(OnPlayerNameFieldChanged);

        _nameFieldCached = nameField;
    }

    private void OnPlayerNameFieldChanged(UnityEngine.UIElements.ChangeEvent<string> evt)
    {
        NetworkManager2.Instance.PlayerNamgeChanged(evt.newValue);
    }
}
