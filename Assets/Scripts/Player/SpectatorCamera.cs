using UnityEngine;
using OodlesEngine;

public class SpectatorCamera : MonoBehaviour
{
    public static SpectatorCamera Instance { get; private set; }

    [Header("Movement")]
    public float moveSpeed = 10f;
    public float minSpeed = 2f;
    public float maxSpeed = 50f;
    public float scrollSensitivity = 5f;

    [Header("Look")]
    public float lookSensitivity = 2f;

    [Header("Follow")]
    public Vector3 followOffset = new Vector3(0f, 3f, -6f);
    public float followSmooth = 8f;

    [Header("Auto Track（給 Streamer 用）")]
    [Tooltip("啟用後忽略所有手動輸入，鏡頭程式控制")]
    public bool autoTrackTarget = false;
    [Tooltip("瞄準點相對玩家身體的偏移（朝這個點 LookAt）")]
    public Vector3 autoTrackPivotOffset = new Vector3(0f, 1.5f, 0f);
    [Tooltip("相機相對瞄準點的偏移（在玩家朝向的局部空間：負 Z = 後方，正 Y = 上方）")]
    public Vector3 autoTrackCameraOffset = new Vector3(0f, 4f, -8f);
    [Tooltip("位置平滑速度（越大越貼）")]
    public float autoTrackSmooth = 6f;
    [Tooltip("水平角(yaw)平滑速度。越小 = 鏡頭轉向越慢、越穩。")]
    public float autoTrackYawSmooth = 2.5f;
    [Tooltip("玩家轉動小於此角度（度）就忽略，避免站著待機 idle 動畫造成微抖")]
    public float autoTrackYawDeadzone = 1.0f;

    private Vector3 _autoTrackSmoothedPos;
    private float   _autoTrackSmoothedYaw = 0f;
    private bool    _autoTrackInitialized = false;

    private float yaw;
    private float pitch;
    private bool cursorLocked = true;

    // 跟隨模式
    private Transform followTarget;
    private int followIndex = -1; // -1 = 自由飛行
    private PlayerIdentify[] cachedPlayers;
    private float cacheTimer = 0f;
    private const float CacheInterval = 2f;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnEnable()
    {
        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = euler.x;
        if (pitch > 180f) pitch -= 360f;

        SetCursorLock(true);
    }

    void OnDisable()
    {
        SetCursorLock(false);
    }

    void Update()
    {
        // 定期刷新場景中的玩家列表
        cacheTimer -= Time.deltaTime;
        if (cacheTimer <= 0f || cachedPlayers == null)
        {
            cachedPlayers = FindObjectsByType<PlayerIdentify>(FindObjectsSortMode.None);
            cacheTimer = CacheInterval;
        }

        // Auto-track（Streamer 模式）：完全不接收手動輸入
        // 滑鼠視角 / 數字鍵切人 / WASD 移動 / 滾輪改速 / Alt 鎖鼠 全部停用
        if (autoTrackTarget) return;

        // Alt 切換滑鼠鎖定
        if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
            SetCursorLock(!cursorLocked);

        HandlePlayerSwitch();
        HandleLook();

        if (followTarget == null)
            HandleFreeMovement();

        HandleSpeedChange();
    }

    void LateUpdate()
    {
        if (followTarget != null)
            HandleFollow();
    }

    // ========= 數字鍵切換跟隨目標 =========

    void HandlePlayerSwitch()
    {
        // 0 或 ` 回到自由飛行
        if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.BackQuote))
        {
            SetFollowTarget(-1);
            return;
        }

        // 1~9 跟隨對應玩家
        for (int i = 1; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
            {
                SetFollowTarget(i - 1);
                return;
            }
        }
    }

    public void SetFollowTarget(int index)
    {
        // 換目標 → 下次 auto-track 直接 snap 到新位置，不會從舊玩家平滑移過去
        _autoTrackInitialized = false;

        if (index < 0)
        {
            followTarget = null;
            followIndex = -1;
            Debug.Log("[Spectator] 自由飛行模式");
            return;
        }

        if (cachedPlayers == null || index >= cachedPlayers.Length) return;

        var identify = cachedPlayers[index];
        if (identify == null) return;

        // 找到角色的物理 body（跟 CameraFollow 一樣的跟隨點）
        var character = identify.GetComponent<OodlesCharacter>();
        if (character != null)
            followTarget = character.GetPhysicsBody().transform;
        else
            followTarget = identify.transform;

        followIndex = index;

        string name = !string.IsNullOrEmpty(identify.PlayerName) ? identify.PlayerName : $"Player {index + 1}";
        Debug.Log($"[Spectator] 跟隨玩家 {index + 1}: {name}");
    }

    // ========= Streamer / 程式控制用 API =========

    /// <summary>當前場景內可跟隨的玩家數量（自動更新）</summary>
    public int PlayerCount => cachedPlayers != null ? cachedPlayers.Length : 0;

    /// <summary>當前正在跟隨的索引（-1 表示自由飛行）</summary>
    public int CurrentFollowIndex => followIndex;

    /// <summary>取得指定索引的玩家（給 Director 讀名字用）</summary>
    public PlayerIdentify GetPlayer(int index)
    {
        if (cachedPlayers == null || index < 0 || index >= cachedPlayers.Length) return null;
        return cachedPlayers[index];
    }

    /// <summary>強制立刻 refresh cachedPlayers，不用等 2 秒輪詢</summary>
    public void RefreshPlayerList()
    {
        cachedPlayers = FindObjectsByType<PlayerIdentify>(FindObjectsSortMode.None);
        cacheTimer = CacheInterval;
    }

    // ========= 相機控制 =========

    void SetCursorLock(bool locked)
    {
        cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void HandleLook()
    {
        // Auto track 模式：忽略滑鼠輸入，由 HandleFollow 接管 yaw
        if (autoTrackTarget && followTarget != null) return;

        yaw   += Input.GetAxisRaw("Mouse X") * lookSensitivity;
        pitch -= Input.GetAxisRaw("Mouse Y") * lookSensitivity;
        pitch  = Mathf.Clamp(pitch, -89f, 89f);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void HandleFollow()
    {
        // 跟隨目標被銷毀 → 回到自由飛行
        if (followTarget == null)
        {
            SetFollowTarget(-1);
            _autoTrackInitialized = false;
            return;
        }

        if (autoTrackTarget)
        {
            DoAutoTrackFollow();
            return;
        }

        // 一般模式：用當前視角旋轉計算 offset 方向
        Quaternion rotManual = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = followTarget.position + rotManual * followOffset;
    }

    /// <summary>
    /// 簡單直接的第三人稱跟隨：
    ///   pivot       = followTarget.position + pivotOffset       （瞄準點，預設頭部高度）
    ///   yaw         = atan2(fwd.x, fwd.z)                        （依玩家面向算水平角）
    ///   cameraPos   = pivot + Yaw旋轉 * cameraOffset             （cameraOffset 預設 (0,4,-8)）
    ///   cameraRot   = LookAt(pivot)                              （嚴格對準瞄準點）
    /// 不繞負角度，不依賴特定 Inspector 數值，看起來就是第三人稱後上方視角。
    /// </summary>
    void DoAutoTrackFollow()
    {
        // 1) 由 followTarget.forward 取水平面 forward
        Vector3 fwd = followTarget.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
        else fwd.Normalize();

        // 2) 瞄準點（玩家頭部）
        Vector3 pivot = followTarget.position + autoTrackPivotOffset;

        // 3) 計算「目標 yaw」：玩家正面方向；deadzone 內就忽略，避免微抖
        float targetYaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;

        if (!_autoTrackInitialized)
        {
            // 換玩家：直接 snap 到目標 yaw / 位置
            _autoTrackSmoothedYaw = targetYaw;
            Quaternion snapRot = Quaternion.Euler(0f, _autoTrackSmoothedYaw, 0f);
            _autoTrackSmoothedPos = pivot + snapRot * autoTrackCameraOffset;
            _autoTrackInitialized = true;
        }
        else
        {
            // Yaw 平滑（用 LerpAngle 自動處理 -180/+180 wrap）
            float delta = Mathf.DeltaAngle(_autoTrackSmoothedYaw, targetYaw);
            if (Mathf.Abs(delta) > autoTrackYawDeadzone)
            {
                _autoTrackSmoothedYaw = Mathf.LerpAngle(_autoTrackSmoothedYaw, targetYaw,
                    Time.deltaTime * autoTrackYawSmooth);
            }
            // deadzone 內 → _autoTrackSmoothedYaw 不動

            // 用平滑後的 yaw 算 desired pos
            Quaternion yawRot = Quaternion.Euler(0f, _autoTrackSmoothedYaw, 0f);
            Vector3 desiredPos = pivot + yawRot * autoTrackCameraOffset;

            // 位置平滑
            _autoTrackSmoothedPos = Vector3.Lerp(_autoTrackSmoothedPos, desiredPos,
                Time.deltaTime * autoTrackSmooth);
        }

        // 4) 避免穿牆：從 pivot 到相機位置 SphereCast，命中就拉近
        Vector3 castDir = _autoTrackSmoothedPos - pivot;
        float castLen = castDir.magnitude;
        if (castLen > 0.01f)
        {
            if (Physics.SphereCast(pivot, 0.25f, castDir.normalized, out var hit, castLen,
                                   ~0, QueryTriggerInteraction.Ignore))
            {
                _autoTrackSmoothedPos = pivot + castDir.normalized * Mathf.Max(hit.distance - 0.1f, 1f);
            }
        }

        // 5) 套到 transform
        transform.position = _autoTrackSmoothedPos;
        transform.LookAt(pivot);

        // 同步 yaw/pitch（讓未來切回手動模式時鏡頭不跳）
        yaw = _autoTrackSmoothedYaw;
        pitch = transform.eulerAngles.x;
        if (pitch > 180f) pitch -= 360f;
    }

    void HandleFreeMovement()
    {
        Vector3 dir = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) dir += transform.forward;
        if (Input.GetKey(KeyCode.S)) dir -= transform.forward;
        if (Input.GetKey(KeyCode.D)) dir += transform.right;
        if (Input.GetKey(KeyCode.A)) dir -= transform.right;
        if (Input.GetKey(KeyCode.Space))      dir += Vector3.up;
        if (Input.GetKey(KeyCode.LeftShift))   dir -= Vector3.up;

        if (dir.sqrMagnitude > 0f)
            transform.position += dir.normalized * moveSpeed * Time.deltaTime;
    }

    void HandleSpeedChange()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            moveSpeed = Mathf.Clamp(moveSpeed + scroll * scrollSensitivity, minSpeed, maxSpeed);
        }
    }
}
