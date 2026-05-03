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

    // ===== Skin 預覽色票（menu 預覽用，不走網路） =====
    [Header("Skin 預覽色票設定")]
    [Tooltip("只動材質名稱等於這個的 material（與 PlayerIdentify 邏輯一致）")]
    [SerializeField] private string skinMaterialName = "Skin";

    private bool  _previewColorActive = false; // 玩家有沒有選自訂色？
    private Color _currentPreviewColor = Color.white;
    // 重複使用同一個 block 物件減少 GC（per call 重設 + GetPropertyBlock 即可）
    private static MaterialPropertyBlock _mpb;

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

        // 新流程：玩家先跑選擇角色 + 顏色，按 Confirm / 開始遊戲後才生成
        // → Client 不再加入後立即 Rpc_RegisterAndSpawn
        // → Host 也不走 StartHostSpawnDelay 自動生成
        // 角色生成統一交由 GameManager1.SpawnAllPlayers（Start Game 時）/ Rpc_ChangeSkin（換 skin 時）
        // 必須先讀 PlayerPrefs 設定 currentSkinIndex，再呼叫 PickCharacterUI
        // 否則 PickCharacterUI 會用 currentSkinIndex=0 啟用錯的 skin，後續 changeSkin
        // 想 deactive 時找不到正確目標，造成多隻 skin 同時可見
        currentSkinIndex = PlayerPrefs.GetInt("Choosenindex");

        // 既然已不走 spawn → Rpc_PlayerSpawnComplete 流程，這裡接管原本它做的兩件事：
        //   1) 關掉 LoadingScreen（不再等 spawn 完成）
        //   2) 開啟選角面板（順便 ResetSkinPreviewColor 在 PickCharacterUI 內）
        if (MenuUIManager.instance != null && MenuUIManager.instance.LoadingScreen != null)
            MenuUIManager.instance.LoadingScreen.SetActive(false);
        PickCharacterUI();
        MenuUIManager.instance.ConfirmCharcterBtn.onClick.AddListener(() =>
        {
            // 寫入 PlayerPrefs（skinIndex / color）
            // Reset 狀態（沒套自訂色）→ Color 寫空字串，host 端 HexToColor 回 null = 不套色 = 用 prefab 原色
            // 套了自訂色      → Color 寫 hex，host 端會把角色染這個色
            PlayerPrefs.SetInt("Choosenindex", currentSkinIndex);
            string colorToSave = _previewColorActive
                ? "#" + UnityEngine.ColorUtility.ToHtmlStringRGB(SkinColor)
                : "";
            PlayerPrefs.SetString("Color", colorToSave);
            PlayerPrefs.Save();

            // 永遠呼叫 Rpc_ChangeSkin：
            //   - 第一次（無舊角色）→ host 端會 SpawnPlayer + 註冊
            //   - 之後（有舊角色）  → host 端會 despawn 舊 + spawn 新
            string pName = NetworkManager2.Instance != null ? NetworkManager2.Instance.PlayerName : "Player";
            Rpc_ChangeSkin(Runner.LocalPlayer, currentSkinIndex, colorToSave, pName);

            if (MenuUIManager.instance.playerlistmanager != null)
                MenuUIManager.instance.playerlistmanager.Rpc_RequestSkinUpdate(Runner.LocalPlayer, currentSkinIndex);

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

        // 每次開啟選角畫面（= 每次開始遊戲）→ 預覽色票回到 RESET 狀態
        ResetSkinPreviewColor();

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
    // ============================================================
    // Skin 預覽色票（給 menu UI 用，純本地不走網路）
    // ============================================================

    /// <summary>
    /// 取得當前已套用的預覽色。沒套自訂色（reset 狀態）時讀目前展示 skin 的 asset 原色。
    /// 若也讀不到（找不到 Skin material）回傳 white。
    /// </summary>
    public Color CurrentPreviewColor
    {
        get
        {
            if (_previewColorActive) return _currentPreviewColor;
            // reset 狀態：直接從 sharedMaterial 讀目前 skin 的 asset 原色
            var skin = GetCurrentSkin();
            if (skin == null) return Color.white;
            var renderers = skin.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                var mats = r.sharedMaterials;
                for (int j = 0; j < mats.Length; j++)
                {
                    var m = mats[j];
                    if (m == null || !IsSkinMaterial(m)) continue;
                    if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
                    if (m.HasProperty("_Color"))     return m.GetColor("_Color");
                }
            }
            return Color.white;
        }
    }

    /// <summary>套用顏色到目前展示的 skin（並記住，切到下一個 skin 也會套）</summary>
    public void SetSkinPreviewColor(Color color)
    {
        _previewColorActive = true;
        _currentPreviewColor = color;
        SkinColor = color; // 讓 confirm 時的 SettingSkinColor 寫進 PlayerPrefs 是這個色
        ApplyColorToSkin(GetCurrentSkin(), color);
    }

    /// <summary>Hex overload：吃 "#FF6464" / "FF6464" 等格式；解析失敗就忽略</summary>
    public void SetSkinPreviewColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return;
        string h = hex.Trim();
        if (h[0] != '#') h = "#" + h;
        if (UnityEngine.ColorUtility.TryParseHtmlString(h, out Color c))
            SetSkinPreviewColor(c);
        else
            Debug.LogWarning($"[SkinChange] 無法解析 hex 色票：'{hex}'");
    }

    /// <summary>
    /// 還原所有 Skins[] 的 "Skin" 材質回 asset 原色 — 用 PropertyBlock 清空，
    /// 不 clone material、不依賴快取，每隻 skin 各自顯示自己 prefab 設計色。
    /// </summary>
    public void ResetSkinPreviewColor()
    {
        _previewColorActive = false;
        if (Skins == null) return;

        for (int i = 0; i < Skins.Length; i++)
        {
            ClearColorOnSkin(Skins[i]);
        }
        // _currentPreviewColor / SkinColor reset 後不再被使用，留空即可（白）
        _currentPreviewColor = Color.white;
        SkinColor = Color.white;
    }

    // ============================================================
    // 測試用 ContextMenu（Inspector 右上角齒輪 → 點即可觸發）
    // 沒接 ColorPicker UI 之前先用這個試 SetSkinPreview / Reset 流程
    // ============================================================

    [ContextMenu("Test/Set Preview Red (#FF0000)")]
    private void TestSetRed() => SetSkinPreviewColor("#FF0000");

    [ContextMenu("Test/Set Preview Blue (#3399FF)")]
    private void TestSetBlue() => SetSkinPreviewColor("#3399FF");

    [ContextMenu("Test/Set Preview Yellow (#FFCC33)")]
    private void TestSetYellow() => SetSkinPreviewColor("#FFCC33");

    [ContextMenu("Test/Reset Preview to Original")]
    private void TestReset() => ResetSkinPreviewColor();

    [ContextMenu("Test/Print Current Preview Color")]
    private void TestPrintCurrent()
    {
        Debug.Log($"[SkinChange] currentSkinIndex={currentSkinIndex} active={_previewColorActive} " +
                  $"current={_currentPreviewColor} (#{UnityEngine.ColorUtility.ToHtmlStringRGB(_currentPreviewColor)})");
    }

    public void changeSkin(int index)
    {
        if (Skins == null || index < 0 || index >= Skins.Length) return;

        // 先把全部 deactive 再啟用目標：避免 currentSkinIndex 跟畫面狀態不一致時殘留多隻 skin
        for (int i = 0; i < Skins.Length; i++)
        {
            if (Skins[i] != null) Skins[i].SetActive(false);
        }
        Skins[index].SetActive(true);
        Skins[index].transform.localRotation = Quaternion.Euler(0f, 90f, 0f); // 切換角色時 reset 成向右 90 度
        currentSkinIndex = index;

        // B 模式：玩家有套過自訂色 → 新 skin 也套同色
        // 沒套色（reset 狀態）→ 不要碰，讓新 skin 顯示自己 asset 的原色
        if (_previewColorActive)
        {
            ApplyColorToSkin(Skins[index], _currentPreviewColor);
        }
    }

    // ---- Skin 預覽色票 helpers ----

    GameObject GetCurrentSkin()
    {
        if (Skins == null) return null;
        if (currentSkinIndex < 0 || currentSkinIndex >= Skins.Length) return null;
        return Skins[currentSkinIndex];
    }

    /// <summary>
    /// 用 MaterialPropertyBlock 對指定 skin 的「Skin」材質套 _BaseColor 覆寫。
    /// 不 clone material，不影響 asset；只在這個 renderer 上做 per-instance 顏色覆寫。
    /// </summary>
    void ApplyColorToSkin(GameObject skinGo, Color color)
    {
        if (skinGo == null) return;
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        var renderers = skinGo.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            var sharedMats = r.sharedMaterials; // 不會 clone
            for (int j = 0; j < sharedMats.Length; j++)
            {
                var m = sharedMats[j];
                if (m == null) continue;
                if (!IsSkinMaterial(m)) continue;

                _mpb.Clear();
                r.GetPropertyBlock(_mpb, j); // 取既有 block（沒有就保持空）
                if (m.HasProperty("_BaseColor")) _mpb.SetColor("_BaseColor", color);
                else if (m.HasProperty("_Color")) _mpb.SetColor("_Color", color);
                r.SetPropertyBlock(_mpb, j);
            }
        }
    }

    /// <summary>清掉指定 skin 的「Skin」材質 PropertyBlock 覆寫，回到 asset 原色。</summary>
    void ClearColorOnSkin(GameObject skinGo)
    {
        if (skinGo == null) return;
        var renderers = skinGo.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            var sharedMats = r.sharedMaterials;
            for (int j = 0; j < sharedMats.Length; j++)
            {
                var m = sharedMats[j];
                if (m == null) continue;
                if (!IsSkinMaterial(m)) continue;
                r.SetPropertyBlock(null, j); // null = 清空，這個 sub-material 渲染回 asset 原樣
            }
        }
    }

    bool IsSkinMaterial(Material m)
    {
        if (m == null || string.IsNullOrEmpty(skinMaterialName)) return false;
        string n = m.name;
        int idx = n.IndexOf(" (Instance)");
        if (idx >= 0) n = n.Substring(0, idx);
        return n == skinMaterialName;
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
        // 跟 skinIndex 同樣讀本地 PlayerPrefs 的色票，沒設定就傳空字串
        string colorHex = PlayerPrefs.GetString("Color", "");
        Rpc_RegisterAndSpawn(skinIndex, playerName, colorHex);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void Rpc_RegisterAndSpawn(int skinIndex, string playerName, string colorHex, RpcInfo info = default)
    {
        if (!Runner.IsServer) return;
        if (PlayerSpawner.instance == null) { Debug.LogWarning("[SkinChange] PlayerSpawner.instance is null"); return; }

        // Streamer 客端：跳過生成 / 註冊（防呆，正常情況 streamer 也不會發這個 RPC）
        if (StreamerToken.IsStreamer(Runner, info.Source))
        {
            Debug.Log($"📺 [SkinChange] streamer client ({info.Source}) 跳過 SpawnPlayer");
            return;
        }

        // 用 client 傳來的 PlayerPrefs Color；空字串 → SpawnPlayer 內部的 HexToColor 會回 null → 不套色
        PlayerSpawner.instance.SpawnPlayer(Runner, skinIndex, info.Source, playerName, true, colorHex);
        StartCoroutine(RegisterWhenReady(info.Source, playerName, skinIndex));
    }

    private IEnumerator RebindCameraNextFrame()
    {
        // 第一次 Confirm 後角色才透過 Rpc_ChangeSkin 在 host 端 spawn，
        // 對 client 端可能要好幾幀網路同步才看得到 SpawnedPlayers，
        // 因此這裡用 retry timeout 持續找，而不是只等一幀。
        const float TIMEOUT = 5f;
        float elapsed = 0f;
        Transform physicsBody = null;

        while (elapsed < TIMEOUT)
        {
            yield return null;
            elapsed += Time.deltaTime;

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

            if (physicsBody != null) break;
        }

        if (physicsBody != null)
        {
            CameraFollow.Get().player = physicsBody;
            CameraFollow.Get().enable = true;
        }
        else
        {
            Debug.LogWarning("[SkinChange] RebindCamera: 找不到本地玩家的 physics body (timeout)");
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
    /// Host → All：通知指定玩家角色已生成完畢。
    /// 新流程下 LoadingScreen 已在 Spawned() 直接關掉、選角面板也是 Spawned() 直接開，
    /// 所以這裡不再做 LoadingScreen.SetActive / PickCharacterUI 之類的副作用，
    /// 只當作 spawn 完成的事件 hook（之後若需要做 spawn-done 動作可加在這）。
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_PlayerSpawnComplete(PlayerRef targetPlayer)
    {
        if (Runner.LocalPlayer != targetPlayer) return;
        Debug.Log($"[SkinChange] Rpc_PlayerSpawnComplete (no-op)");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void Rpc_ChangeSkin(PlayerRef PlayerId, int index, string colorHex, string playerName)
    {
        if (Runner.IsServer)
            StartCoroutine(ChangeSkinRoutine(PlayerId, index, colorHex, playerName));
    }

    /// <summary>
    /// 統一的「按 Confirm 後生成 / 換 skin」流程：
    ///  - 沒有舊角色 → 第一次生成（含註冊到 playerlistmanager）
    ///  - 有舊角色   → 分幀 spawn 新 + despawn 舊（避免同一幀同時 create / destroy 卡頓）
    /// </summary>
    private IEnumerator ChangeSkinRoutine(PlayerRef playerId, int skinIndex, string colorHex, string playerName)
    {
        NetworkObject oldObj = null;
        int oldSlot = -1;

        // 1) 找舊角色（如有）
        for (int i = 0; i < SpawnedPlayers.Length; i++)
        {
            var obj = SpawnedPlayers.Get(i);
            if (obj == null) continue;
            var np = obj.GetComponent<NetworkPlayer>();
            if (np != null && np.PlayerId == playerId)
            {
                oldObj = obj;
                oldSlot = i;
                // 已存在的角色用既有名字（避免被 client 端傳來的覆寫）
                string existing = obj.GetComponent<PlayerIdentify>()?.PlayerName;
                if (!string.IsNullOrEmpty(existing)) playerName = existing;
                break;
            }
        }

        // 2) 生成新角色
        if (oldObj != null) SpawnedPlayers.Set(oldSlot, null); // 騰空舊欄位
        PlayerSpawner.instance.SpawnPlayer(Runner, skinIndex, playerId, playerName, false, colorHex);

        // 3) 分支：沒有舊角色 → 第一次生成，註冊 playerlist 後完成
        if (oldObj == null)
        {
            StartCoroutine(RegisterWhenReady(playerId, playerName, skinIndex));
            yield break;
        }

        // 4) 有舊角色：等一幀後 despawn 舊（避免同一幀生成+銷毀）
        yield return null;
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
        string pName = NetworkManager2.Instance != null ? NetworkManager2.Instance.PlayerName : "Player";
        Rpc_ChangeSkin(Runner.LocalPlayer, originalIndex, PlayerPrefs.GetString("Color", "#FFFFFF"), pName);
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
