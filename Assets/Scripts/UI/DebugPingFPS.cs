using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 顯示 FPS / Ping 的小工具。
/// 按 F12（可在 Inspector 改）切換顯示。
/// 兩個 Text 欄位擇一拉 Inspector 上即可（UGUI Text 或 TextMeshPro）。
/// </summary>
public class DebugPingFPS : MonoBehaviour
{
    [Header("文字顯示（兩個擇一拉）")]
    [SerializeField] private Text legacyText;
    [SerializeField] private TMP_Text tmpText;
    [Header("遊戲開始人數(玩家不足該人數自動補滿電腦玩家)")]
     [SerializeField] private GameObject SettingPlayerCount;

    [SerializeField] private TMP_Text PlayerCountText;
     [SerializeField] public int PlayerCount = 0;
    [SerializeField] private Button ADDPlayerButton;
    [SerializeField] private Button increasePlayerButton;
    [Header("行為設定")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F12;
    [Tooltip("多久更新一次顯示（秒）")]
    [SerializeField, Range(0.05f, 2f)] private float updateInterval = 0.5f;
    [Tooltip("啟動時是否預設顯示")]
    [SerializeField] private bool showOnStart = false;

    [Header("文字格式")]
    [SerializeField] private string format = "FPS: {0:F0}    Ping: {1}";

    [Header("FPS 鎖定")]
    [Tooltip("打勾後啟動時把 Application.targetFrameRate 設為下方數值（並關 VSync 才能生效）")]
    [SerializeField] private bool capFrameRate = true;
    [SerializeField] private int targetFrameRate = 60;



    // === FPS 統計（以 unscaledDeltaTime 平均）===
    private float _accumDeltaTime;
    private int _accumFrames;
    private float _timeSinceUpdate;
    private float _displayedFps;

    private bool _isVisible;

    void Awake()
    {
        SetVisible(showOnStart);

        if (capFrameRate)
        {
            // VSync 開著時 targetFrameRate 會被忽略，要先關 VSync
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFrameRate;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            SetVisible(!_isVisible);

        if (!_isVisible) return;

        _accumDeltaTime += Time.unscaledDeltaTime;
        _accumFrames++;
        _timeSinceUpdate += Time.unscaledDeltaTime;
        if (PlayerCountText != null)
            PlayerCountText.text = $"玩家數: {PlayerCount}";

        if (_timeSinceUpdate < updateInterval) return;

        _displayedFps = (_accumDeltaTime > 0f) ? (_accumFrames / _accumDeltaTime) : 0f;
        _accumDeltaTime = 0f;
        _accumFrames = 0;
        _timeSinceUpdate = 0f;

        int pingMs = TryGetPingMs();
        string pingStr = (pingMs >= 0) ? $"{pingMs} ms" : "-- ms";
        string output = string.Format(format, _displayedFps, pingStr);

        if (legacyText != null) legacyText.text = output;
        if (tmpText != null) tmpText.text = output;
    }

    private void SetVisible(bool show)
    {
        _isVisible = show;
        if (legacyText != null) legacyText.gameObject.SetActive(show);
        if (tmpText != null) tmpText.gameObject.SetActive(show);
        if(SettingPlayerCount != null) SettingPlayerCount.SetActive(show);
    }

    /// <summary>
    /// 嘗試從 Fusion 拿 RTT。失敗回 -1。
    /// </summary>
    private int TryGetPingMs()
    {
        try
        {
            var nm = NetworkManager2.Instance;
            if (nm == null || nm.runner == null || !nm.runner.IsRunning) return -1;

            float rttSec = (float)nm.runner.GetPlayerRtt(nm.runner.LocalPlayer);
            if (rttSec <= 0f) return -1;
            return Mathf.RoundToInt(rttSec * 1000f);
        }
        catch
        {
            return -1;
        }
    }
    public void PlayerAdd()
    {
        if (PlayerCount < 8)
            PlayerCount++;

    }
    public void Playerdecrease()
    {
        if (PlayerCount > 0)
            PlayerCount--;

    }
}
