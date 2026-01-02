using System;
using UnityEngine;
[CreateAssetMenu(menuName = "Card/MissionCard/Steal")]
public class Steal : MissionCard
{
    public override bool CanUse(PlayerInventory user, PlayerInventory target, CardData card)
    {
        if (target == null)
            return false;
        else if (user.CanUse(card) == false)
            return false;

        int userCount = 0,
            targetCount = 0;
        foreach (var c in user.slots)
            if (!c.IsEmpty())
                userCount++;
        foreach (var c in target.slots)
            if (!c.IsEmpty())
                targetCount++;

        return userCount <= 5 && targetCount >= 1 && card.cooldown <= 0;
    }
    public override void UseSkill(CardUseParameters parameters)
    {
        PlayerInventory Target = PlayerInventoryManager.Instance.GetPlayer(parameters.TargetId).GetComponent<PlayerInventory>();
        PlayerInventory User = PlayerInventoryManager.Instance.GetPlayer(parameters.UserId).GetComponent<PlayerInventory>();
        CardData card = Target.RandomGetCard();
        User.AddCard(card);
         if (CardHistoryManager.Instance != null)
        {
            CardHistoryManager.Instance.Record(
                new CardHistoryEntry(
                    parameters.UserId,
                    parameters.TargetId,
                    "Steal",
                    CardType.Mission,
                    null
                    //result
                )
            );
        }
        User.SetCooldownEnd(card);
    }
}
