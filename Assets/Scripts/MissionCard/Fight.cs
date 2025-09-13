using OodlesEngine;
using UnityEngine;
[CreateAssetMenu(menuName = "Card/MissionCard/Fight")]
public class Fight : MissionCard
{

    public override void UseSkill(CardUseParameters parameters)
    {
        PlayerInventoryManager.Instance.GetPlayer(parameters.UserId).GetComponent<OodlesCharacter>().UpdateAttack();
    }
    public override void CheckMission(CardUseParameters parameters)
    {
        Count--;
        if (Count == 0)
        {
            Debug.Log("任務完成: " + name);
            
         }
    }
}
