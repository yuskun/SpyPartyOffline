using System;
using UnityEngine;
[CreateAssetMenu(menuName = "Card/MissionCard/Steal")]
public class Steal : MissionCard
{
    public override bool CanUse(PlayerInventory user, PlayerInventory target, CardData card)
    {
        if (target == null)
            return false;
        else if (user.CanUse(this.cardData) == false)
            return false;

        int userCount = 0,
            targetCount = 0;
        foreach (var c in user.slots)
            if (!c.IsEmpty())
                userCount++;
        foreach (var c in target.slots)
            if (!c.IsEmpty())
                targetCount++;

        return userCount <= 5 && targetCount >= 1 ;
    }
    public override void UseSkill(CardUseParameters parameters)
    {
        PlayerInventory Target = PlayerInventoryManager.Instance.GetPlayer(parameters.TargetId).GetComponent<PlayerInventory>();
        PlayerInventory User = PlayerInventoryManager.Instance.GetPlayer(parameters.UserId).GetComponent<PlayerInventory>();
        CardData card = Target.RandomGetCard();
        Debug.Log($"[Steal] 玩家 {parameters.UserId} 從 玩家 {parameters.TargetId} 偷到卡片 ID={card.id}, type={card.type} ");
        User.AddCard(card);
         if (CardHistoryManager.Instance != null)
        {
            CardHistoryManager.Instance.Record(
                new CardHistoryEntry(
                    parameters.UserId,
                    parameters.TargetId,
                    "Steal",
                    CardType.Mission
                    //result
                )
            );
        }
        User.SetCooldownEnd(this.cardData);
    }
}
