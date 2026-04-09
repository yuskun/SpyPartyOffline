using UnityEngine;

public class Givebanana : MonoBehaviour
{
    private bool hasGiven = false;

    void Start()
    {
        
    }

    void Update()
    {
        
    }


    private void OnTriggerEnter(Collider other)
    {
        if (hasGiven) return;

        if (!other.CompareTag("Player")) return;


        GiveItem();

        hasGiven = true;

        // ⭐ 通知教學系統
        NotifyTutorial();

        // 可選：消失
        gameObject.SetActive(false);
    }

    void GiveItem()
    {
        
    }
    // ⭐ 通知教學流程
    void NotifyTutorial()
    {
        TutorialManager.Instance.OnPickBanana(); 
    }
}
