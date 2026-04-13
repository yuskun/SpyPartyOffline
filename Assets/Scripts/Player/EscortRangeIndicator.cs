using UnityEngine;

/// <summary>
/// 押送範圍視覺指示器（純 Client MonoBehaviour）。
/// 掛在 Player Prefab 根節點上；押送期間只對本地「抓人者（警察）」玩家顯示地面圓圈。
/// 圓圈以小偷（目標）為圓心，讓警察看清楚小偷是否超出押送範圍。
/// 押送狀態由 GameManager.RPC_EscortStart / RPC_EscortEnd 廣播後設定。
/// </summary>
public class EscortRangeIndicator : MonoBehaviour
{
    // ── 靜態押送狀態（RPC 廣播後所有 Client 同步）────────────────
    public static bool IsEscortActive = false;
    public static int  CatcherID      = -1;
    public static int  TargetID       = -1;

    public static void SetEscort(int catcher, int target)
    {
        IsEscortActive = true;
        CatcherID      = catcher;
        TargetID       = target;
    }

    public static void ClearEscort()
    {
        IsEscortActive = false;
        CatcherID      = -1;
        TargetID       = -1;
        // 快取會在下次 SetEscort 時因為 TargetID 改變自動重新搜尋
    }

    // ── 實體 ─────────────────────────────────────────────────────
    private LineRenderer   lr;
    private PlayerIdentify identify;
    private GameObject     cachedTarget;
    private int            cachedTargetID = -1;
    private GameObject     cachedCatcher;
    private int            cachedCatcherID = -1;

    private const int   Segments  = 48;
    private const float LineWidth = 0.07f;
    private const float GroundY   = 0.05f;   // 貼地高度
    private const float DefaultEscortRadius = 5f; // 預設押送半徑（與 MissionWinSystem.escortRadius 一致）

    private static readonly Color ColorSafe = new Color(0.1f, 1f,   0.1f, 0.9f);  // 綠：目標在範圍內
    private static readonly Color ColorWarn = new Color(1f,   0.2f, 0f,   0.9f);  // 紅：目標超出範圍

    // ─────────────────────────────────────────────────────────────

    void Start()
    {
        identify = GetComponent<PlayerIdentify>();

        // 建立專屬 Child GameObject + LineRenderer
        GameObject child = new GameObject("EscortCircle");
        child.transform.SetParent(transform, false);

        lr = child.AddComponent<LineRenderer>();
        lr.useWorldSpace     = true;
        lr.loop              = true;
        lr.positionCount     = Segments;
        lr.widthMultiplier   = LineWidth;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows    = false;
        lr.generateLightingData = false;

        // 使用最基本的 Unlit shader（Built-in / URP 通用）
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader != null)
            lr.material = new Material(shader);

        lr.enabled = false;
    }

    void Update()
    {
        if (identify == null)
        {
            if (lr != null) lr.enabled = false;
            return;
        }

        // 直接比對 PlayerIdentify 元件參照：最可靠的「本地玩家物件」判斷
        bool isLocalPlayerObject = LocalBackpack.Instance?.playerIdentify != null
                                   && LocalBackpack.Instance.playerIdentify == identify;
        bool iAmCatcher = IsEscortActive && isLocalPlayerObject && identify.PlayerID == CatcherID;
        bool iAmTarget  = IsEscortActive && isLocalPlayerObject && identify.PlayerID == TargetID;

        if (!iAmCatcher && !iAmTarget)
        {
            lr.enabled = false;
            return;
        }

        // 圓圈以小偷（目標）為圓心，警察和小偷都看得到
        // 快取目標玩家，避免每幀搜尋
        // 快取目標（小偷）
        if (cachedTarget == null || cachedTargetID != TargetID)
        {
            cachedTarget = FindPlayerByID(TargetID);
            cachedTargetID = TargetID;
        }
        if (cachedTarget == null) { lr.enabled = false; return; }

        // 快取警察
        if (cachedCatcher == null || cachedCatcherID != CatcherID)
        {
            cachedCatcher = FindPlayerByID(CatcherID);
            cachedCatcherID = CatcherID;
        }
        if (cachedCatcher == null) { lr.enabled = false; return; }

        lr.enabled = true;

        // 圓心永遠在小偷（目標）位置
        Vector3 center = GetRagdollPos(cachedTarget);
        center.y = GroundY;

        float radius = MissionWinSystem.Instance != null
            ? MissionWinSystem.Instance.escortRadius
            : DefaultEscortRadius;
        DrawCircle(center, radius);
        // 顏色用警察和小偷的距離判斷，小偷看到的顏色相反
        UpdateColor(center, radius, cachedCatcher, iAmTarget);
    }

    // ── 圓圈繪製 ─────────────────────────────────────────────────

    private void DrawCircle(Vector3 center, float radius)
    {
        for (int i = 0; i < Segments; i++)
        {
            float angle = (float)i / Segments * Mathf.PI * 2f;
            lr.SetPosition(i, center + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            ));
        }
    }

    // ── 顏色判斷 ──

    private void UpdateColor(Vector3 targetGroundPos, float radius, GameObject catcher, bool invertColor)
    {
        Vector3 catcherPos = GetRagdollPos(catcher);
        float dist = Vector2.Distance(
            new Vector2(targetGroundPos.x, targetGroundPos.z),
            new Vector2(catcherPos.x,      catcherPos.z)
        );

        bool inRange = dist <= radius;

        // 警察：在範圍內=綠，超出=紅
        // 小偷：在範圍內=紅（危險），超出=綠（安全）
        if (invertColor)
            SetColor(inRange ? ColorWarn : ColorSafe);
        else
            SetColor(inRange ? ColorSafe : ColorWarn);
    }

    private void SetColor(Color c)
    {
        lr.startColor = c;
        lr.endColor   = c;
    }

    // ── Helpers ──────────────────────────────────────────────────

    /// <summary>透過 PlayerIdentify.PlayerID 找玩家，Host / Client 都能用</summary>
    private GameObject FindPlayerByID(int playerID)
    {
        foreach (var pi in FindObjectsByType<PlayerIdentify>(FindObjectsSortMode.None))
        {
            if (pi != null && pi.PlayerID == playerID)
                return pi.gameObject;
        }
        return null;
    }

    private Vector3 GetRagdollPos(GameObject go)
    {
        Transform r = go.transform.Find("Ragdoll");
        return r != null ? r.position : go.transform.position;
    }
}
