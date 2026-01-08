using Fusion;
using TMPro;
using UnityEngine;

public class CountdownTimer : NetworkBehaviour
{
    [Header("UI 設定")]

    [Networked, OnChangedRender(nameof(OnTimeTextChanged))]
    public string TimeText { get; set; }
    private NetworkBool IsRunning { get; set; }

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
    public void OnTimeTextChanged()
    {
        GameUIManager.Instance.timerText.text = TimeText;
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
        // 2. 將 StealWin 設為 true
        if (MissionWinSystem.Instance != null)
        {
            MissionWinSystem.Instance.StealWin = true;
            PlayerInventoryManager.Instance.playerInventories[MissionWinSystem.Instance.StealID].MissionStates.Set(1, 1);
            MissionWinSystem.Instance.GameOver();
        }
        else
        {
            Debug.LogWarning("MissionWinSystem.Instance 尚未初始化");
        }

    }
}
