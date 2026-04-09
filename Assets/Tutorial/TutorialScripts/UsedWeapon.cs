using UnityEngine;

public class UsedWeapon : MonoBehaviour
{
    [Header("是否只在特定步驟觸發")]
    public int triggerStep = 6; // 例如 UseBat 那一步

    private void OnDestroy()
    {
        // ⭐ 避免場景切換誤觸
        if (!Application.isPlaying) return;

        if (TutorialManager.Instance == null) return;

        // ⭐ 確認目前教學步驟
        if (TutorialManager.Instance.CurrentStep == triggerStep)
        {
            TutorialManager.Instance.OnUseBat(); 
            // 或直接 NextStep()
            // TutorialManager.Instance.NextStep();
        }
    }
}
