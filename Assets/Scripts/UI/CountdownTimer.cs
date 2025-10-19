using Fusion;
using TMPro;
using UnityEngine;

public class CountdownTimer : NetworkBehaviour
{
    [Header("UI 設定")]
    
    [Networked] public string TimeText { get; set; }
    [Networked] private NetworkBool IsRunning { get; set; }

    [Header("時間設定")]
    public int totalMinutes = 15;
    private float remainingTime;

    public override void Spawned()
    {
        if (Runner.IsServer)
        {
            remainingTime = totalMinutes * 60f;
            UpdateTimeText();
        }
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

            UpdateTimeText();
        }

        GameUIManager.Instance.timerText.text= TimeText;
    }

    public void StartTimer()
    {
        if (Runner.IsServer)
        {
            remainingTime = totalMinutes * 60f;
            IsRunning = true;
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
    // 1. 呼叫 TraceMission 的方法
    if (TraceMission.Instance != null)
    {
        TraceMission.Instance.ProcessPlayerCards();
    }
    else
    {
        Debug.LogWarning("TraceMission.Instance 尚未初始化");
    }

    // 2. 將 StealWin 設為 true
    if (MissionWinSystem.Instance != null)
    {
        MissionWinSystem.Instance.StealWin = true;
    }
    else
    {
        Debug.LogWarning("MissionWinSystem.Instance 尚未初始化");
    }

    // 3. 呼叫 GameOver 判定
    if (MissionWinSystem.Instance != null)
    {
        MissionWinSystem.Instance.GameOver();
    }
}
}
