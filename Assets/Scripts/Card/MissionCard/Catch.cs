using UnityEngine;
[CreateAssetMenu(menuName = "Card/MissionCard/Catch")]
public class Catch : MissionCard
{
    public Card targetCard;
      public override bool CanUse(PlayerInventory user, PlayerInventory target, CardData card)
    {
        if (target == null && user.CanUse(card) == false)
            return false;
        return true;
    }


    public override void UseSkill(CardUseParameters parameters)
    {   
        CheckMission(parameters);
    }
    public override void CheckMission(CardUseParameters parameters)
    {

        if (PlayerInventoryManager.Instance.GetPlayer(parameters.TargetId).GetComponent<PlayerInventory>().HasCard(targetCard.cardData))
        {
            MissionWinSystem.Instance.CatchWin = true;
            MissionWinSystem.Instance.GameOver();
        }
        else
        {
            PlayerInventoryManager.Instance.GetPlayer(parameters.UserId).GetComponent<PlayerInventory>().SetCooldownEnd(parameters.Card);
        }
         if (CardHistoryManager.Instance != null)
        {
            CardHistoryManager.Instance.Record(
                new CardHistoryEntry(
                    parameters.UserId,
                    parameters.TargetId,
                    "Catch",
                    CardType.Mission,
                    null
                    //result
                )
            );
        }
        
        
    }
}
