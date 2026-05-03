using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Streamer 畫面疊加（雙模式）：
///   - Idle：遊戲還沒開始 → 顯示全螢幕等待 UI（branded）
///   - Live：遊戲進行中（Director.IsCycling）→ 全螢幕 UI 隱藏，只留角落小資訊
/// 切換條件：StreamerCameraDirector.IsCycling == true 視為遊戲已開始。
/// </summary>
public class StreamerOverlay : MonoBehaviour
{
    [Header("外觀")]
    [SerializeField] private int   liveFontSize = 30;
    [SerializeField] private int   idleTitleFontSize = 96;
    [SerializeField] private int   idleStatusFontSize = 44;
    [SerializeField] private int   idleStatsFontSize = 28;
    [SerializeField] private float topMargin = 30f;
    [SerializeField] private float sideMargin = 30f;

    [Header("配色（暗藍 + 青/黃 HUD 同調）")]
    [SerializeField] private Color bgColor      = new Color(0.04f, 0.06f, 0.10f, 0.95f);
    [SerializeField] private Color accentColor  = new Color(0.4f, 0.9f, 1.0f, 1f);   // 青
    [SerializeField] private Color highlightColor = new Color(1f, 0.95f, 0.4f, 1f);  // 黃
    [SerializeField] private Color textColor    = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] private Color subTextColor = new Color(0.65f, 0.72f, 0.80f, 1f);
    [SerializeField] private Color outlineColor = new Color(0f, 0f, 0f, 0.85f);

    private StreamerCameraDirector _director;
    private StreamerAutoJoin       _join;

    // ===== Live 模式 (角落) =====
    private GameObject _liveRoot;
    private TextMeshProUGUI _liveWatching;
    private TextMeshProUGUI _liveCountdown;
    private TextMeshProUGUI _liveStatus;

    // ===== Idle 模式 (全螢幕) =====
    private GameObject _idleRoot;
    private TextMeshProUGUI _idleTitle;
    private TextMeshProUGUI _idleSubtitle;
    private TextMeshProUGUI _idleStatus;
    private TextMeshProUGUI _idleStats;
    private TextMeshProUGUI _idleSession;
    private RectTransform   _idleSpinner;
    private Image           _idleLiveDot;

    void Start()
    {
        _director = GetComponent<StreamerCameraDirector>();
        _join     = GetComponent<StreamerAutoJoin>();
        if (_director == null) _director = FindFirstObjectByType<StreamerCameraDirector>();
        if (_join     == null) _join     = FindFirstObjectByType<StreamerAutoJoin>();
        BuildLiveUI();
        BuildIdleUI();
    }

    void Update()
    {
        bool live = _director != null && _director.IsCycling;

        if (_liveRoot != null) _liveRoot.SetActive(live);
        if (_idleRoot != null) _idleRoot.SetActive(!live);

        if (live) UpdateLive();
        else      UpdateIdle();
    }

    // ──────────────────────────────────────────
    // LIVE
    // ──────────────────────────────────────────
    void UpdateLive()
    {
        if (_director == null) return;
        if (_liveWatching != null)
            _liveWatching.text = $"WATCHING  ▸  {_director.CurrentTargetName}";
        if (_liveCountdown != null)
            _liveCountdown.text = $"NEXT IN  {Mathf.CeilToInt(_director.TimeUntilSwitch)}s";
        if (_liveStatus != null)
        {
            string s = _join != null && _join.IsJoined
                ? $"LIVE  ·  {_director.PlayerCount} players"
                : "live";
            _liveStatus.text = s;
        }
    }

    // ──────────────────────────────────────────
    // IDLE
    // ──────────────────────────────────────────
    void UpdateIdle()
    {
        // 旋轉 spinner
        if (_idleSpinner != null)
            _idleSpinner.localRotation = Quaternion.Euler(0f, 0f, -Time.time * 180f);

        // 紅點呼吸
        if (_idleLiveDot != null)
        {
            float t = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
            var c = _idleLiveDot.color;
            c.a = Mathf.Lerp(0.4f, 1f, t);
            _idleLiveDot.color = c;
        }

        if (_join == null) return;

        string headline, status;
        switch (_join.CurrentState)
        {
            case StreamerAutoJoin.State.Booting:
                headline = "INITIALIZING";
                status = "starting up...";
                break;
            case StreamerAutoJoin.State.ConnectingLobby:
                headline = "CONNECTING";
                status = "joining lobby...";
                break;
            case StreamerAutoJoin.State.ScanningRooms:
                headline = "SCANNING";
                status = (_join.RoomsVisible == 0)
                    ? "looking for matches..."
                    : (_join.RoomsJoinable == 0
                        ? "rooms found, none joinable"
                        : "joining match...");
                break;
            case StreamerAutoJoin.State.Joining:
                headline = "JOINING";
                status = "connecting to host...";
                break;
            case StreamerAutoJoin.State.JoinedWaiting:
                headline = "READY";
                status = "waiting for host to start";
                break;
            default:
                headline = "STAND BY"; status = ""; break;
        }

        if (_idleStatus != null) _idleStatus.text = status;
        if (_idleSubtitle != null) _idleSubtitle.text = headline;

        if (_idleStats != null)
        {
            int mins = (int)(_join.SecondsInState / 60f);
            int secs = (int)(_join.SecondsInState % 60f);
            string elapsed = (mins > 0) ? $"{mins}m {secs}s" : $"{secs}s";
            _idleStats.text =
                $"<color=#{ColorUtility.ToHtmlStringRGB(accentColor)}>◉</color>  {_join.RoomsVisible} rooms visible" +
                $"     <color=#{ColorUtility.ToHtmlStringRGB(accentColor)}>◉</color>  {_join.RoomsJoinable} joinable" +
                $"     <color=#{ColorUtility.ToHtmlStringRGB(accentColor)}>◉</color>  elapsed {elapsed}";
        }

        if (_idleSession != null)
        {
            _idleSession.text = string.IsNullOrEmpty(_join.CurrentSessionName)
                ? ""
                : $"session · {_join.CurrentSessionName}";
        }
    }

    // ──────────────────────────────────────────
    // BUILD
    // ──────────────────────────────────────────
    Canvas MakeCanvas(string name, int sortingOrder)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    void BuildLiveUI()
    {
        var canvas = MakeCanvas("StreamerLiveCanvas", sortingOrder: 32760);
        _liveRoot = canvas.gameObject;

        _liveWatching = MakeText(canvas.transform, "Watching",
            new Vector2(0f, 1f), new Vector2(0.5f, 1f),
            new Vector2(sideMargin, -100f), new Vector2(-sideMargin, -topMargin),
            TextAlignmentOptions.MidlineLeft, "WATCHING  ▸  —", liveFontSize);

        _liveCountdown = MakeText(canvas.transform, "Countdown",
            new Vector2(0.5f, 1f), new Vector2(1f, 1f),
            new Vector2(sideMargin, -100f), new Vector2(-sideMargin, -topMargin),
            TextAlignmentOptions.MidlineRight, "NEXT IN  --s", liveFontSize);

        _liveStatus = MakeText(canvas.transform, "Status",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(sideMargin, topMargin), new Vector2(-sideMargin, topMargin + 50f),
            TextAlignmentOptions.MidlineGeoAligned, "init...", liveFontSize - 6);
    }

    void BuildIdleUI()
    {
        var canvas = MakeCanvas("StreamerIdleCanvas", sortingOrder: 32761); // 比 live 高
        _idleRoot = canvas.gameObject;

        // 1) 全螢幕背景
        var bg = new GameObject("Background", typeof(RectTransform));
        bg.transform.SetParent(canvas.transform, false);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = bgColor;

        // 2) 頂部 LIVE 紅點 + 標題
        var liveBadge = new GameObject("LiveBadge", typeof(RectTransform));
        liveBadge.transform.SetParent(canvas.transform, false);
        var lbRect = liveBadge.GetComponent<RectTransform>();
        lbRect.anchorMin = new Vector2(0.5f, 1f);
        lbRect.anchorMax = new Vector2(0.5f, 1f);
        lbRect.pivot     = new Vector2(0.5f, 1f);
        lbRect.anchoredPosition = new Vector2(0f, -80f);
        lbRect.sizeDelta = new Vector2(220f, 60f);

        var dot = new GameObject("Dot", typeof(RectTransform));
        dot.transform.SetParent(liveBadge.transform, false);
        var dotRect = dot.GetComponent<RectTransform>();
        dotRect.anchorMin = new Vector2(0f, 0.5f);
        dotRect.anchorMax = new Vector2(0f, 0.5f);
        dotRect.pivot = new Vector2(0f, 0.5f);
        dotRect.anchoredPosition = new Vector2(20f, 0f);
        dotRect.sizeDelta = new Vector2(18f, 18f);
        _idleLiveDot = dot.AddComponent<Image>();
        _idleLiveDot.color = new Color(1f, 0.25f, 0.25f, 1f);
        _idleLiveDot.sprite = MakeCircleSprite();

        var liveLabel = MakeText(liveBadge.transform, "LiveLabel",
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(50f, 0f), new Vector2(-10f, 0f),
            TextAlignmentOptions.MidlineLeft, "LIVE STREAM", 30);
        liveLabel.color = textColor;
        liveLabel.fontStyle = FontStyles.Bold;

        // 3) 主標題
        _idleTitle = MakeText(canvas.transform, "Title",
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(0f, 80f), new Vector2(0f, 80f + idleTitleFontSize + 20f),
            TextAlignmentOptions.Center, "SPYPARTY OFFLINE", idleTitleFontSize);
        _idleTitle.color = highlightColor;
        _idleTitle.outlineColor = outlineColor;
        _idleTitle.outlineWidth = 0.2f;
        _idleTitle.fontStyle = FontStyles.Bold;

        // 4) 副標 (狀態 headline，例如 SCANNING / JOINING)
        _idleSubtitle = MakeText(canvas.transform, "Subtitle",
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(0f, 30f), new Vector2(0f, 30f + 50f),
            TextAlignmentOptions.Center, "SCANNING", 36);
        _idleSubtitle.color = accentColor;
        _idleSubtitle.fontStyle = FontStyles.Bold;
        _idleSubtitle.characterSpacing = 12f;

        // 5) Spinner（在副標下方）
        var spinnerGO = new GameObject("Spinner", typeof(RectTransform));
        spinnerGO.transform.SetParent(canvas.transform, false);
        _idleSpinner = spinnerGO.GetComponent<RectTransform>();
        _idleSpinner.anchorMin = new Vector2(0.5f, 0.5f);
        _idleSpinner.anchorMax = new Vector2(0.5f, 0.5f);
        _idleSpinner.pivot     = new Vector2(0.5f, 0.5f);
        _idleSpinner.anchoredPosition = new Vector2(0f, -30f);
        _idleSpinner.sizeDelta = new Vector2(52f, 52f);
        var spinImg = spinnerGO.AddComponent<Image>();
        spinImg.color = accentColor;
        spinImg.sprite = MakeArcSprite();

        // 6) 詳細 status
        _idleStatus = MakeText(canvas.transform, "Status",
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(0f, -100f), new Vector2(0f, -100f + idleStatusFontSize + 10f),
            TextAlignmentOptions.Center, "joining lobby...", idleStatusFontSize);
        _idleStatus.color = textColor;

        // 7) 統計 (rooms, elapsed)
        _idleStats = MakeText(canvas.transform, "Stats",
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(0f, -180f), new Vector2(0f, -180f + idleStatsFontSize + 10f),
            TextAlignmentOptions.Center, "—", idleStatsFontSize);
        _idleStats.color = subTextColor;
        _idleStats.richText = true;

        // 8) Session 名稱（若有）
        _idleSession = MakeText(canvas.transform, "Session",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 80f), new Vector2(0f, 80f + 26f),
            TextAlignmentOptions.Center, "", 22);
        _idleSession.color = subTextColor;

        // 9) 底部 footer
        var footer = MakeText(canvas.transform, "Footer",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 30f), new Vector2(0f, 30f + 20f),
            TextAlignmentOptions.Center, "STREAMER MODE  ·  SPYPARTY OFFLINE", 18);
        footer.color = subTextColor;
        footer.characterSpacing = 8f;
    }

    TextMeshProUGUI MakeText(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax,
        TextAlignmentOptions align, string init, int size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = init;
        txt.fontSize = size;
        txt.alignment = align;
        txt.color = textColor;
        txt.fontStyle = FontStyles.Bold;
        txt.outlineColor = outlineColor;
        txt.outlineWidth = 0.2f;
        txt.enableWordWrapping = false;
        txt.overflowMode = TextOverflowModes.Overflow;
        return txt;
    }

    // 動態生成簡單圓形 sprite（紅點用）
    static Sprite _circleCache;
    static Sprite MakeCircleSprite()
    {
        if (_circleCache != null) return _circleCache;
        const int s = 64;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        var px = new Color[s * s];
        Vector2 c = new Vector2(s * 0.5f, s * 0.5f);
        float r = s * 0.5f - 1f;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                float a = Mathf.Clamp01((r - d) / 1.5f);
                px[y * s + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels(px);
        tex.Apply();
        _circleCache = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        return _circleCache;
    }

    // 動態生成弧形 sprite（spinner 用）
    static Sprite _arcCache;
    static Sprite MakeArcSprite()
    {
        if (_arcCache != null) return _arcCache;
        const int s = 64;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        var px = new Color[s * s];
        Vector2 c = new Vector2(s * 0.5f, s * 0.5f);
        float rOut = s * 0.5f - 1f;
        float rIn  = rOut - 5f;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                Vector2 d = new Vector2(x, y) - c;
                float dist = d.magnitude;
                float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg; // -180~180
                bool inRing = dist <= rOut && dist >= rIn;
                bool inArc  = ang > -10f && ang < 200f; // 約 210 度的弧
                float a = (inRing && inArc) ? Mathf.Clamp01((rOut - dist) / 1.5f) : 0f;
                px[y * s + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels(px);
        tex.Apply();
        _arcCache = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        return _arcCache;
    }
}
