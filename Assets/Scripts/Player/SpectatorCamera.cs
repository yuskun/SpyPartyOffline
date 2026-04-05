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

        // Alt 切換滑鼠鎖定
        if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
            SetCursorLock(!cursorLocked);

        HandlePlayerSwitch();
        HandleLook();

        if (followTarget != null)
            HandleFollow();
        else
            HandleFreeMovement();

        HandleSpeedChange();
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

    void SetFollowTarget(int index)
    {
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

    // ========= 相機控制 =========

    void SetCursorLock(bool locked)
    {
        cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void HandleLook()
    {
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
            return;
        }

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 targetPos = followTarget.position + rot * followOffset;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSmooth);
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
