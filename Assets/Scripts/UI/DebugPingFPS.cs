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

        string pingStr = TryGetPingDisplay();
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
    /// 嘗試從 Fusion 拿 RTT 並組成顯示字串。
    /// - 沒連線 → "offline"
    /// - Host 自己 → "Host"（自己對自己沒 ping）
    /// - Client → "{ms} ms"（對 Host 的 RTT）
    /// </summary>
    private bool _pingDebugLogged;
    private string TryGetPingDisplay()
    {
        try
        {
            var nm = NetworkManager2.Instance;
            if (nm == null || nm.runner == null || !nm.runner.IsRunning)
                return "offline";

            var runner = nm.runner;

            // Host：自己對自己沒有 RTT，直接標成 Host
            if (runner.IsServer)
                return "Host";

            // Client：對自己 RTT 通常是 0，要對「其他玩家（Host）」拿 RTT
            float rttSec = (float)runner.GetPlayerRtt(runner.LocalPlayer);

            // 如果是 0，遍歷其他 active player（通常就是 Host）拿 RTT
            if (rttSec <= 0f)
            {
                foreach (var p in runner.ActivePlayers)
                {
                    if (p == runner.LocalPlayer) continue;
                    float r = (float)runner.GetPlayerRtt(p);
                    if (r > 0f) { rttSec = r; break; }
                }
            }

            if (!_pingDebugLogged)
            {
                _pingDebugLogged = true;
                Debug.Log($"[DebugPingFPS] IsServer={runner.IsServer}, LocalPlayer={runner.LocalPlayer}, RTT(local)={runner.GetPlayerRtt(runner.LocalPlayer):F4}s, finalRtt={rttSec:F4}s");
            }

            if (rttSec <= 0f) return "-- ms";
            return $"{Mathf.RoundToInt(rttSec * 1000f)} ms";
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[DebugPingFPS] TryGetPingDisplay 失敗: {e.Message}");
            return "err";
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
