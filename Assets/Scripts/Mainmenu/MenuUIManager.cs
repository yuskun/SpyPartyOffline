using TMPro;
using UnityEngine;
using Fusion;

public class MenuUIManager : MonoBehaviour
{
    static public MenuUIManager instance;
    public GameObject Menu;
    public GameObject Gameroom;
    public GameObject Host;
    public GameObject Client;
    public GameObject BulidOrJoin;
    public GameObject LoadingScreen;
    public GameObject JoinRoomList;
    public TextMeshProUGUI PlayerNameInput;
    public GameObject[] Ai;
    public PlayerListManager playerlistmanager;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            if (playerlistmanager == null)
                DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        NetworkManager.instance.PlayerName = PlayerNameInput.text;
        Menu.SetActive(true);
        Gameroom.SetActive(false);
        BulidOrJoin.SetActive(false);
        LoadingScreen.SetActive(false);
        JoinRoomList.SetActive(false);
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
}
