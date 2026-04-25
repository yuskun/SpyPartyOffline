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

    [Header("角色預覽拖曳旋轉")]
    [Tooltip("水平拖曳 1 像素轉幾度（角度/像素）")]
    [SerializeField] private float skinDragSensitivity = 0.4f;
    [Tooltip("打勾代表「向右拖 = 角色順時針旋轉」；不勾則相反")]
    [SerializeField] private bool skinDragInvert = false;
    private bool _isDraggingSkin = false;
    private float _lastDragMouseX;
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

        // 旁觀者不生成角色、不註冊、不綁 UI
        if (NetworkManager2.IsSpectator) return;

        // Host 延遲生成（用 TickTimer 等場景穩定），Client 立即生成
        if (NetworkManager2.Instance.mode == NetworkManager2.NetMode.Host)
        {
            instance.StartHostSpawnDelay();
        }
        else
        {
            int skinIndex = PlayerPrefs.GetInt("Choosenindex", 0);
            string playerName = NetworkManager2.Instance != null ? NetworkManager2.Instance.PlayerName : "Player";
            Rpc_RegisterAndSpawn(skinIndex, playerName);
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
            _triggerCooldown = 3f;

            PlayHideOrShow(true);
            MenuUIManager.instance.Gameroom.SetActive(true);

            var practiceRoomUI = FindObjectOfType<PracticeUIManager>(true);
            if (practiceRoomUI != null)
            {
                practiceRoomUI.gameObject.SetActive(true); // 重新打開
            }

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
               PickCharacterUI();

            }
        }

        HandleSkinDragRotate();
    }

    /// <summary>
    /// 角色挑選 UI 開啟時，按住左鍵左右拖曳可旋轉預覽角色。
    /// </summary>
    [Header("拖曳除錯")]
    [SerializeField] private bool skinDragDebugLog = true;
    [Tooltip("打勾後，按下左鍵時不檢查是否點到 UI 按鈕，純粹只要 UI 開著就能拖")]
    [SerializeField] private bool skinDragIgnoreUIBlock = false;

    private void HandleSkinDragRotate()
    {
        if (Skins == null || currentSkinIndex < 0 || currentSkinIndex >= Skins.Length) return;
        var currentSkin = Skins[currentSkinIndex];

        if (currentSkin == null || !currentSkin.activeInHierarchy)
        {
            if (_isDraggingSkin && skinDragDebugLog)
                Debug.Log($"[SkinDrag] 中止：currentSkin 失效 (null={currentSkin == null}, activeInHierarchy={(currentSkin != null ? currentSkin.activeInHierarchy : false)})");
            _isDraggingSkin = false;
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            bool overSelectable = !skinDragIgnoreUIBlock && IsPointerOverInteractiveUI();

            if (skinDragDebugLog)
                Debug.Log($"[SkinDrag] MouseDown 偵測到。overInteractiveUI={overSelectable}, skin={currentSkin.name}, skinActive={currentSkin.activeInHierarchy}");

            if (!overSelectable)
            {
                _isDraggingSkin = true;
                _lastDragMouseX = Input.mousePosition.x;
                if (skinDragDebugLog) Debug.Log("[SkinDrag] ▶ 開始拖曳");
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (_isDraggingSkin && skinDragDebugLog)
                Debug.Log("[SkinDrag] ■ 結束拖曳");
            _isDraggingSkin = false;
        }

        if (_isDraggingSkin && Input.GetMouseButton(0))
        {
            float deltaX = Input.mousePosition.x - _lastDragMouseX;
            _lastDragMouseX = Input.mousePosition.x;

            if (Mathf.Abs(deltaX) > 0.01f)
            {
                // 反轉預設方向：向左拖 = 角色向右轉，向右拖 = 角色向左轉
                float yawDelta = -deltaX * skinDragSensitivity;
                if (skinDragInvert) yawDelta = -yawDelta;
                currentSkin.transform.Rotate(0f, yawDelta, 0f, Space.World);
            }
        }
    }

    /// <summary>
    /// 改用「raycast 後檢查是否打到 Selectable」的判斷，避免被全螢幕背景 Panel 阻擋。
    /// 也支援 UI Toolkit / EventSystem 兩種情況。
    /// </summary>
    private static readonly List<UnityEngine.EventSystems.RaycastResult> _uiRaycastBuffer = new List<UnityEngine.EventSystems.RaycastResult>();
    private static bool IsPointerOverInteractiveUI()
    {
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es == null) return false;

        var data = new UnityEngine.EventSystems.PointerEventData(es) { position = Input.mousePosition };
        _uiRaycastBuffer.Clear();
        es.RaycastAll(data, _uiRaycastBuffer);

        foreach (var r in _uiRaycastBuffer)
        {
            // 只把「真正的互動元件」算成阻擋（Button、Toggle、Slider...）
            if (r.gameObject != null && r.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>() != null)
                return true;
        }
        return false;
    }
   public void PickCharacterUI()
    {
        GameUIManager.Instance.progressBar.SetActive(false);
        MenuUIManager.instance.Gameroom.SetActive(false);
        MenuUIManager.instance.PracticePanel.HideCurrentUI();
         MenuUIManager.instance.HostRoomPanel.HideCurrentUI();

        CameraFollow.Get().enable = false;
        StartCoroutine(MoveCamera());
        Skins[currentSkinIndex].SetActive(true);
        Skins[currentSkinIndex].transform.localRotation = Quaternion.Euler(0f, 90f, 0f); // 開啟 UI 時 reset 成向右 90 度
        MenuUIManager.instance.CharSelectPanel.ShowCurrentUI();
        PlayHideOrShow(false);
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;

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
    void ChangeSkinColor(Color color)   {
        Skins[currentSkinIndex].GetComponent<Renderer>().material.color = color;
    }
    public void changeSkin(int index)
    {
        Skins[currentSkinIndex].SetActive(false);
        Skins[index].SetActive(true);
        Skins[index].transform.localRotation = Quaternion.Euler(0f, 90f, 0f); // 切換角色時 reset 成向右 90 度
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
    [Header("生成延遲（僅 Host）")]
    public float spawnDelay = 1.5f;

    [Networked] private TickTimer HostSpawnDelay { get; set; }
    private bool hostPendingSpawn = false;

    private void StartHostSpawnDelay()
    {
        HostSpawnDelay = TickTimer.CreateFromSeconds(Runner, spawnDelay);
        hostPendingSpawn = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!hostPendingSpawn) return;
        if (!HostSpawnDelay.Expired(Runner)) return;

        hostPendingSpawn = false;

        int skinIndex = PlayerPrefs.GetInt("Choosenindex", 0);
        string playerName = NetworkManager2.Instance != null ? NetworkManager2.Instance.PlayerName : "Player";
        Rpc_RegisterAndSpawn(skinIndex, playerName);
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

        // Host 生成完該玩家的角色後，通知該 Client 關閉 Loading UI
        Rpc_PlayerSpawnComplete(player);
    }

    /// <summary>
    /// Host → All：通知指定玩家角色已生成完畢，該 Client 可以關閉 Loading。
    /// 每個 Client 收到後檢查是不是自己，是的話關掉 LoadingScreen。
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_PlayerSpawnComplete(PlayerRef targetPlayer)
    {
        // 只有目標玩家自己才關 Loading
        if (Runner.LocalPlayer != targetPlayer) return;

        Debug.Log($"[SkinChange] 收到 Rpc_PlayerSpawnComplete，關閉 Loading");
        if (MenuUIManager.instance != null && MenuUIManager.instance.LoadingScreen != null)
            MenuUIManager.instance.LoadingScreen.SetActive(false);
            PickCharacterUI();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void Rpc_ChangeSkin(PlayerRef PlayerId, int index, string colorHex)
    {
        if (Runner.IsServer)
            StartCoroutine(ChangeSkinRoutine(PlayerId, index));
    }

    /// <summary>分幀處理 Despawn / Spawn，避免同一幀同時銷毀+建立造成全體卡頓</summary>
    private IEnumerator ChangeSkinRoutine(PlayerRef playerId, int skinIndex)
    {
        NetworkObject oldObj = null;
        int oldSlot = -1;
        string playerName = "Player";

        // 1) 找到舊角色，記下名字和欄位
        for (int i = 0; i < SpawnedPlayers.Length; i++)
        {
            var obj = SpawnedPlayers.Get(i);
            if (obj == null) continue;
            var np = obj.GetComponent<NetworkPlayer>();
            if (np != null && np.PlayerId == playerId)
            {
                oldObj = obj;
                oldSlot = i;
                playerName = obj.GetComponent<PlayerIdentify>()?.PlayerName ?? "Player";
                break;
            }
        }

        if (oldObj == null) yield break;

        // 2) Frame A：先生成新角色（Client 端只需要 Instantiate）
        SpawnedPlayers.Set(oldSlot, null); // 清空舊欄位給新角色用
        PlayerSpawner.instance.SpawnPlayer(Runner, skinIndex, playerId, playerName);

        yield return null; // 等一幀

        // 3) Frame B：再銷毀舊角色（Client 端只需要 Destroy）
        if (oldObj != null && oldObj.IsValid)
            Runner.Despawn(oldObj);
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All, HostMode = RpcHostMode.SourceIsHostPlayer)]
    void Rpc_ChangeSkinProcess(bool open, NetworkObject Player)
    {
        if (Player.GetComponent<NetworkPlayer>().PlayerId == Runner.LocalPlayer)
        {
            GameUIManager.Instance.UserCardUI.sprite = SkinChangeImage;
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
            if (MenuUIManager.instance.CharSelectPanel != null)
            {
                MenuUIManager.instance.CharSelectPanel.HideCurrentUI();
            }
        }

        var practiceRoomUI = FindObjectOfType<PracticeUIManager>(true);
        if (practiceRoomUI != null)
        {
            practiceRoomUI.gameObject.SetActive(true); // 重新打開
        }


    }

}
