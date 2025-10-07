using TMPro;
using UnityEngine;
using UnityEngine.UI;   // 如果你用 TextMeshPro，改用 TMPro

public class CountdownTimer : MonoBehaviour
{
    [Header("UI 設定")]
    public TextMeshProUGUI timerText;   // 綁定 Unity UI 的 Text (或 TMP_Text)

    [Header("時間設定")]
    public int totalMinutes = 15; // 預設 15 分鐘

    private float remainingTime; // 剩餘秒數
    private bool isRunning = false;

    void Start()
    {
        remainingTime = totalMinutes * 60f;
        isRunning = true;
    }

    void Update()
    {
        if (!isRunning) return;

        remainingTime -= Time.deltaTime;
        if (remainingTime < 0)
        {
            remainingTime = 0;
            isRunning = false;
            OnTimerEnd();
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);

        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    /*private void OnTimerEnd()
    {
       MissionWinSystem.Instance.GameOver();
    }*/

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
