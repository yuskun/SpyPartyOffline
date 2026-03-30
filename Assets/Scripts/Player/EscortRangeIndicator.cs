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
    }

    // ── 實體 ─────────────────────────────────────────────────────
    private LineRenderer   lr;
    private PlayerIdentify identify;

    private const int   Segments  = 48;
    private const float LineWidth = 0.07f;
    private const float GroundY   = 0.05f;   // 貼地高度

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
        if (identify == null || MissionWinSystem.Instance == null)
        {
            if (lr != null) lr.enabled = false;
            return;
        }

        // 直接比對 PlayerIdentify 元件參照：最可靠的「本地玩家物件」判斷
        bool isLocalPlayerObject = LocalBackpack.Instance?.playerIdentify != null
                                   && LocalBackpack.Instance.playerIdentify == identify;
        bool iAmCatcher = IsEscortActive && isLocalPlayerObject && identify.PlayerID == CatcherID;

        if (IsEscortActive && isLocalPlayerObject)
            Debug.Log($"[EscortRange] IsActive={IsEscortActive} isLocal={isLocalPlayerObject} myID={identify.PlayerID} CatcherID={CatcherID} iAmCatcher={iAmCatcher}");

        if (!iAmCatcher)
        {
            lr.enabled = false;
            return;
        }

        // 圓圈以小偷（目標）為圓心，只有警察（抓人者）看得到
        if (PlayerInventoryManager.Instance == null) { lr.enabled = false; return; }
        GameObject target = PlayerInventoryManager.Instance.GetPlayer(TargetID);
        if (target == null) { lr.enabled = false; return; }

        lr.enabled = true;

        Vector3 center = GetRagdollPos(target);
        center.y = GroundY;

        float radius = MissionWinSystem.Instance.escortRadius;
        DrawCircle(center, radius);
        UpdateColor(center, radius, gameObject);
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

    // ── 顏色：小偷（目標）在圓心範圍內→綠；超出→紅 ──

    private void UpdateColor(Vector3 targetGroundPos, float radius, GameObject catcher)
    {
        // 取警察自己的位置，判斷小偷是否在範圍內
        Vector3 catcherPos = GetRagdollPos(catcher);
        float dist = Vector2.Distance(
            new Vector2(targetGroundPos.x, targetGroundPos.z),
            new Vector2(catcherPos.x,      catcherPos.z)
        );

        SetColor(dist <= radius ? ColorSafe : ColorWarn);
    }

    private void SetColor(Color c)
    {
        lr.startColor = c;
        lr.endColor   = c;
    }

    // ── Helpers ──────────────────────────────────────────────────

    private Vector3 GetRagdollPos(GameObject go)
    {
        Transform r = go.transform.Find("Ragdoll");
        return r != null ? r.position : go.transform.position;
    }
}
