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
    private float _triggerCooldown = 0f;
    [Networked, Capacity(8)]
    public NetworkArray<NetworkObject> SpawnedPlayers => default;

    public CharacterAvatarData characterAvatarDatabase;

    public override void Spawned()
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

        // 每個 Client（含 Host）生成時，告知 Server 自己的 skin index 與名字
        int skinIndex = PlayerPrefs.GetInt("Choosenindex", 0);
        string playerName = NetworkManager2.Instance != null ? NetworkManager2.Instance.PlayerName : "Player";
        Rpc_RegisterAndSpawn(skinIndex, playerName);

        // 初始化角色外观
        currentSkinIndex = PlayerPrefs.GetInt("Choosenindex");
        MenuUIManager.instance.ConfirmCharcterBtn.onClick.AddListener(() =>
        {
            if (PlayerPrefs.GetInt("Choosenindex") != currentSkinIndex)
            {
                SettingSkinColor(currentSkinIndex, SkinColor);
                Rpc_ChangeSkin(Runner.LocalPlayer, currentSkinIndex, PlayerPrefs.GetString("Color"));
                if (MenuUIManager.instance.playerlistmanager != null)
                {
                    MenuUIManager.instance.playerlistmanager.UpdateSkinIndex(Runner.LocalPlayer, currentSkinIndex);
                }
                FindObjectOfType<PracticeUIManager>()?.RefreshAvatar();
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
            _triggerCooldown = 3f;

            PlayHideOrShow(true);
            MenuUIManager.instance.Gameroom.SetActive(true);

            // PlayHideOrShow(true) 之後 player 已恢復 active，統一 rebind Camera
            StartCoroutine(RebindCameraNextFrame());

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
        if (_triggerCooldown > 0f) return;
        if (collision.gameObject.tag == "Player")
        {
            Rpc_ChangeSkinProcess(true, collision.gameObject.transform.parent.GetComponent<NetworkObject>());
        }
    }
    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            Rpc_ChangeSkinProcess(false, collision.gameObject.transform.parent.GetComponent<NetworkObject>());
        }
    }

    void Update()
    {
        if (_triggerCooldown > 0f)
            _triggerCooldown -= Time.deltaTime;

        if (GameUIManager.Instance.progressBar.activeSelf)
        {
            GameUIManager.Instance.progressfill.fillAmount += Time.deltaTime;
            if (GameUIManager.Instance.progressfill.fillAmount >= 1)
            {
                GameUIManager.Instance.progressBar.SetActive(false);
                MenuUIManager.instance.Gameroom.SetActive(false);
                CameraFollow.Get().enable = false;
                StartCoroutine(MoveCamera());
                Skins[currentSkinIndex].SetActive(true);
                MenuUIManager.instance.ChooseCharacterUI.SetActive(true);
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
    void changeSkin(int index)
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
    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void Rpc_RegisterAndSpawn(int skinIndex, string playerName, RpcInfo info = default)
    {
        if (!Runner.IsServer) return;
        if (PlayerSpawner.instance == null) { Debug.LogWarning("[SkinChange] PlayerSpawner.instance is null"); return; }
        PlayerSpawner.instance.SpawnPlayer(Runner, skinIndex, info.Source, playerName, true);
        StartCoroutine(RegisterWhenReady(info.Source, playerName, skinIndex));
    }

    private IEnumerator RebindCameraNextFrame()
    {
        yield return null; // 等一幀，確保 PlayHideOrShow(true) 已讓 player active

        Transform physicsBody = null;
        foreach (var playerObj in SpawnedPlayers)
        {
            if (playerObj == null) continue;
            var np = playerObj.GetComponent<NetworkPlayer>();
            if (np != null && np.PlayerId == Runner.LocalPlayer)
            {
                var ch = playerObj.GetComponent<OodlesCharacter>();
                if (ch != null) physicsBody = ch.GetPhysicsBody().transform;
                break;
            }
        }

        if (physicsBody != null)
        {
            CameraFollow.Get().player = physicsBody;
            CameraFollow.Get().enable = true;
        }
        else
        {
            Debug.LogWarning("[SkinChange] RebindCamera: 找不到本地玩家的 physics body");
        }
    }

    private IEnumerator RegisterWhenReady(PlayerRef player, string playerName, int skinIndex)
    {
        while (MenuUIManager.instance == null || MenuUIManager.instance.playerlistmanager == null)
            yield return null;
        MenuUIManager.instance.playerlistmanager.RegisterPlayer(player, playerName, skinIndex);
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





}
