using System.Collections;
using System.Collections.Generic;
using Fusion;
using OodlesEngine;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class SkinChange : NetworkBehaviour
{
    public Sprite SkinChangeImage;
    public static SkinChange instance;
    public GameObject[] Skins;
    private int currentSkinIndex;
    private Color SkinColor;
    [Networked, Capacity(8)]
    public NetworkArray<NetworkObject> SpawnedPlayers => default;

    public CharacterAvatarData characterAvatarDatabase;
    [Networked, Capacity(8)]
    public NetworkDictionary<int, int> PlayerSkinIndex { get; }

    public override void Spawned()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // 如果欄位是空的，就從 Resources 資料夾抓取
            if (characterAvatarDatabase == null)
            {
                characterAvatarDatabase = Resources.Load<CharacterAvatarData>("Characters/CharacterAvatarData");
            }
        }
        else
        {
            Destroy(gameObject);
        }
        // 初始化角色外观
        currentSkinIndex = PlayerPrefs.GetInt("Choosenindex");
        MenuUIManager.instance.ConfirmCharcterBtn.onClick.AddListener(() =>
        {
            if (PlayerPrefs.GetInt("Choosenindex") != currentSkinIndex)
            {
                SettingSkinColor(currentSkinIndex, SkinColor);
                Rpc_ChangeSkin(Runner.LocalPlayer, currentSkinIndex, PlayerPrefs.GetString("Color"));

                PlayerPrefs.SetInt("Choosenindex", currentSkinIndex);
                PlayerPrefs.Save();

                if (MenuUIManager.instance.playerlistmanager != null)
                {
                    //MenuUIManager.instance.playerlistmanager.UpdateSkinIndex(Runner.LocalPlayer, currentSkinIndex);
                    MenuUIManager.instance.playerlistmanager.Rpc_RequestSkinUpdate(Runner.LocalPlayer, currentSkinIndex);
                }
                
                //FindObjectOfType<PracticeUIManager>()?.RefreshAvatar();
            }
            else
            {
                SettingSkinColor(currentSkinIndex, SkinColor);
            }

            MenuUIManager.instance.ChooseCharacterUI.SetActive(false);

            foreach (var Skin in Skins)
            {
                Skin.SetActive(false);
            }
            GameUIManager.Instance.progressfill.fillAmount = 0;

            PlayHideOrShow(true);
            MenuUIManager.instance.Gameroom.SetActive(true);
            
            var practiceRoomUI = FindObjectOfType<PracticeUIManager>(true);
            if (practiceRoomUI != null)
            {
                practiceRoomUI.gameObject.SetActive(true); // 重新打開
            }

        });
        foreach (var btn in MenuUIManager.instance.CharacterButtons)
        {
            int index = System.Array.IndexOf(MenuUIManager.instance.CharacterButtons, btn);
            btn.onClick.AddListener(() =>
            {
                changeSkin(index);
            });
        }
    }




    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            Rpc_ChangeSkinProcess(true,collision.gameObject.transform.parent.GetComponent<NetworkObject>());

        }
    }
    void OnCollisionExit(Collision collision)
    {
         Rpc_ChangeSkinProcess(true,collision.gameObject.transform.parent.GetComponent<NetworkObject>());

    }

    void Update()
    {
        if (GameUIManager.Instance.progressBar.activeSelf)
        {
            GameUIManager.Instance.progressfill.fillAmount += Time.deltaTime;
            if (GameUIManager.Instance.progressfill.fillAmount >= 1)
            {
                GameUIManager.Instance.progressBar.SetActive(false);
                MenuUIManager.instance.Gameroom.SetActive(false);
                var practiceRoomUI = FindObjectOfType<PracticeUIManager>(true);
                if (practiceRoomUI != null) practiceRoomUI.gameObject.SetActive(false);
                CameraFollow.Get().enable = false;
                StartCoroutine(MoveCamera());
                Skins[currentSkinIndex].SetActive(true);
                //MenuUIManager.instance.ChooseCharacterUI.SetActive(true);

                MenuUIManager.instance.chooseCharacterDocument.gameObject.SetActive(true); 
                

                PlayHideOrShow(false);

            }
        }
    }
    IEnumerator MoveCamera()
    {
        Transform cam = Camera.main.transform;

        Vector3 startPos = cam.position;
        Quaternion startRot = cam.rotation;

        Vector3 targetPos = new Vector3(47.5f, 13f, 7f);
        Quaternion targetRot = Quaternion.Euler(0, -90, 0);

        float duration = 1f; // 過渡時間
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            cam.position = Vector3.Lerp(startPos, targetPos, t);
            cam.rotation = Quaternion.Lerp(startRot, targetRot, t);

            yield return null;
        }

        cam.position = targetPos;
        cam.rotation = targetRot;
    }

    void SettingSkinColor(int index, Color color)
    {
        PlayerPrefs.SetInt("Choosenindex", index);
        string hex = "#" + UnityEngine.ColorUtility.ToHtmlStringRGB(color);
        PlayerPrefs.SetString("Color", hex);
    }
    void ChangeSkinColor(Color color)
    {
        Skins[currentSkinIndex].GetComponent<Renderer>().material.color = color;
    }
    public void changeSkin(int index)
    {
        Skins[currentSkinIndex].SetActive(false);
        Skins[index].SetActive(true);
        currentSkinIndex = index;
    }
    void PlayHideOrShow(bool show)
    {
        foreach (var player in SpawnedPlayers)
        {
            if (player != null)
                player.gameObject.SetActive(show);
        }
    }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void Rpc_ChangeSkin(PlayerRef PlayerId, int index, string colorHex)
    {
        if (Runner.IsServer)
        {

            foreach (var player in SpawnedPlayers)
            {
                if (player.GetComponent<NetworkPlayer>().PlayerId == PlayerId)
                {
                    Runner.Despawn(player);
                    PlayerSpawner.instance.SpawnPlayer(Runner, index, PlayerId, "ChangeSkinPlayer");
                    break;
                }
            }
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All, HostMode = RpcHostMode.SourceIsHostPlayer)]
    void Rpc_ChangeSkinProcess( bool open,NetworkObject Player)
    {
        if(Player.GetComponent<NetworkPlayer>().PlayerId==Runner.LocalPlayer){
            GameUIManager.Instance.UserCardUI.sprite=SkinChangeImage;
            GameUIManager.Instance.progressBar.SetActive(open);
                
            }
            
    }
    public void SetSpawnedPlayer(NetworkObject playerObj)
    {
        for (int index = 0; index < SpawnedPlayers.Length; index++)
        {
            var player = SpawnedPlayers.Get(index);
            if (player == null)
            {
                SpawnedPlayers.Set(index, playerObj);
                break;
            } 
        }

    }

    // 在 SkinChange.cs 內部增加
    public void BackAndCloseAllUI()
    {

        int originalIndex = PlayerPrefs.GetInt("Choosenindex", 0);
        Rpc_ChangeSkin(Runner.LocalPlayer, originalIndex, PlayerPrefs.GetString("Color", "#FFFFFF"));
        // 2. 隱藏 3D 預覽模型 
        foreach (var skin in Skins)
        {
            if (skin != null) skin.SetActive(false); 
        }
        GameUIManager.Instance.progressfill.fillAmount = 0;

        // 3. 恢復玩家物件與房間介面 
        PlayHideOrShow(true); 
        if (MenuUIManager.instance != null)
        {
            MenuUIManager.instance.Gameroom.SetActive(true); 

            // 4. 關鍵：同時關閉新舊兩種 UI 面板 
            MenuUIManager.instance.ChooseCharacterUI.SetActive(false); 
            if (MenuUIManager.instance.chooseCharacterDocument != null)
            {
                MenuUIManager.instance.chooseCharacterDocument.gameObject.SetActive(false); 
            }
        }
        
        var practiceRoomUI = FindObjectOfType<PracticeUIManager>(true);
        if (practiceRoomUI != null)
        {
            practiceRoomUI.gameObject.SetActive(true); // 重新打開
        }


    }

}
