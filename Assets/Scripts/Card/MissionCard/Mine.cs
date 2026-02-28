using System;
using UnityEngine;
[CreateAssetMenu(menuName = "Card/MissionCard/Mine")]
public class Mine : MissionCard
{
    
    public override void UseSkill(CardUseParameters parameters)
    {
        PlayerInventory User = PlayerInventoryManager.Instance.GetPlayer(parameters.UserId).GetComponent<PlayerInventory>();
        User.Protect(true);
         if (CardHistoryManager.Instance != null)
        {
            CardHistoryManager.Instance.Record(
                new CardHistoryEntry(
                    parameters.UserId,
                    parameters.TargetId,
                    "Mine",
                    CardType.Mission
                    //result
                )
            );
        }
        User.SetCooldownEnd(this.cardData);
    }
}
