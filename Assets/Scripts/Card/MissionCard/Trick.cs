using System;
using UnityEngine;
[CreateAssetMenu(menuName = "Card/MissionCard/Trick")]
public class Trick : MissionCard
{
       public override void UseSkill(CardUseParameters parameters)
    {
        PlayerInventory Target = PlayerInventoryManager.Instance.GetPlayer(parameters.TargetId).GetComponent<PlayerInventory>();
        PlayerInventory User = PlayerInventoryManager.Instance.GetPlayer(parameters.UserId).GetComponent<PlayerInventory>();
        Target.dropAll = true;
        
      
         if (CardHistoryManager.Instance != null)
        {
            CardHistoryManager.Instance.Record(
                new CardHistoryEntry(
                    parameters.UserId,
                    parameters.TargetId,
                    "Trick",
                    CardType.Mission
                    //result
                )
            );
        }
        User.SetCooldownEnd(this.cardData);
    }
}
