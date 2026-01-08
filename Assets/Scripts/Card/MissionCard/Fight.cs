using OodlesEngine;
using UnityEngine;
[CreateAssetMenu(menuName = "Card/MissionCard/Fight")]
public class Fight : MissionCard
{

    public override void UseSkill(CardUseParameters parameters)
    {
        PlayerInventoryManager.Instance.GetPlayer(parameters.UserId).GetComponent<OodlesCharacter>().Attack();
    }
    public override void CheckMission(CardUseParameters parameters)
    {
       if (CardHistoryManager.Instance != null)
        {
            CardHistoryManager.Instance.Record(
                new CardHistoryEntry(
                    parameters.UserId,
                    parameters.TargetId,
                    "Catch",
                    CardType.Mission
                    //result
                )
            );
        }
    }
}
