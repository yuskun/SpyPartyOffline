using Fusion;
using TMPro;
using UnityEngine;

public class CountdownTimer : NetworkBehaviour
{
    [Header("UI 設定")]

    [Networked]
    public string TimeText { get; set; }
    [Networked] private NetworkBool IsRunning { get; set; }

    [Networked, OnChangedRender(nameof(OnLastMinuteChanged))]
    public NetworkBool IsLastMinute { get; set; }

    [Header("時間設定")]
    public int totalMinutes = 15;
    public float remainingTime;

    public override void Spawned()
    {
        if (Runner.IsServer)
        {
            remainingTime = totalMinutes * 60f;
            UpdateTimeText();
        }
    }
    public void OnTimeTextChanged()
    {
        if (GameUIManager.Instance != null && GameUIManager.Instance.timerText != null)
            GameUIManager.Instance.timerText.text = TimeText;
        if (GameHUDManager.Instance != null)
            GameHUDManager.Instance.SetTopTime(TimeText);
    }

    public void OnLastMinuteChanged()
    {
        if (IsLastMinute && MissionWinSystem.Instance != null)
            MissionWinSystem.Instance.SwitchStealToTimerMode();
    }

    public override void FixedUpdateNetwork()
    {
        if (Runner.IsServer && IsRunning)
        {
            remainingTime -= Runner.DeltaTime;

            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                IsRunning = false;
                OnTimerEnd();
            }

            if (!IsLastMinute && remainingTime <= 60f)
                IsLastMinute = true;

            UpdateTimeText();
        }
    }

    /// <summary>每幀直接同步 UI，不依賴 OnChangedRender</summary>
    public override void Render()
    {
        if (string.IsNullOrEmpty(TimeText)) return;

        if (GameUIManager.Instance != null && GameUIManager.Instance.timerText != null)
            GameUIManager.Instance.timerText.text = TimeText;

        if (GameHUDManager.Instance != null)
            GameHUDManager.Instance.SetTopTime(TimeText);
    }

    public void StartTimer()
    {
        if (Runner.IsServer)
        {
            remainingTime = totalMinutes * 60f;
            IsLastMinute = false;

            // 先設一個不同的值，強制 Fusion 偵測到變化觸發 OnChangedRender
            TimeText = "";
            IsRunning = true;
            UpdateTimeText();
        }
    }

    public void StopTimer()
    {
        if (Runner.IsServer)
        {
            IsRunning = false;
        }
    }

    private void UpdateTimeText()
    {
        int m = Mathf.FloorToInt(remainingTime / 60);
        int s = Mathf.FloorToInt(remainingTime % 60);
        TimeText = $"{m:00}:{s:00}";
    }

    private void OnTimerEnd()
    {
        if (MissionWinSystem.Instance != null)
        {
            MissionWinSystem.Instance.StealTimerWin();
        }
        else
        {
            Debug.LogWarning("MissionWinSystem.Instance 尚未初始化");
        }
    }
}
