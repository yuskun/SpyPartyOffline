using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MissionRotator : MonoBehaviour
{
    public Slider slider;

    [System.Serializable]
    public class MissionData
    {
        public TextMeshProUGUI title;
        public TextMeshProUGUI text;
        public TextMeshProUGUI score;
    }

    public MissionData[] missions;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            RotateMissions();
        }
    }

    [ContextMenu("Rotate Missions")]
    public void RotateMissions()
    {
        if (missions.Length < 3) return;

        // ✅ 暫存第三任務的文字資料（而非 UI 物件）
        string tempTitle = missions[0].title.text;
        string tempText = missions[0].text.text;
        string tempScore = missions[0].score.text;
         CopyMissionData(missions[0], missions[1]);
        // 3 → 2
        CopyMissionData(missions[1], missions[2]);
        // 2 → 1
        // 1 → 3（使用暫存文字）
        missions[2].title.text = tempTitle;
        missions[2].text.text = tempText;
        missions[2].score.text = tempScore;

        UpdateFirstMissionProgress();
    }

    private void CopyMissionData(MissionData target, MissionData source)
    {
        target.title.text = source.title.text;
        target.text.text = source.text.text;
        target.score.text = source.score.text;
    }

    private void UpdateFirstMissionProgress()
    {
        var first = missions[0];
        if (first.score == null) return;

        string[] parts = first.score.text.Split('/');
        if (parts.Length != 2) return;

        if (float.TryParse(parts[0], out float current) && float.TryParse(parts[1], out float max))
        {
            slider.value = Mathf.Clamp01(current / max);
        }
    }
}
