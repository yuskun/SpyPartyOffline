using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(menuName = "Card/FunctionCard/Give")]
public class Give : FunctionCard
{
    public override bool CanUse(PlayerInventory user, PlayerInventory target)
    {
        if (target == null)
            return false;
        else if (user.CanUse(this.cardData) == false)
        {
            return false;
        }
        int userCount = 0,
            targetCount = 0;
        foreach (var c in user.slots)
            if (!c.IsEmpty())
                userCount++;
        foreach (var c in target.slots)
            if (!c.IsEmpty())
                targetCount++;
        return userCount >= 2 && targetCount <= 5;
    }

    public override void Execute(CardUseParameters parameters)
    {
        Debug.Log("Give Execute");
        PlayerInventory User = PlayerInventoryManager.Instance.GetPlayer(parameters.UserId).GetComponent<PlayerInventory>();
        PlayerInventory Target = PlayerInventoryManager.Instance.GetPlayer(parameters.TargetId).GetComponent<PlayerInventory>();
        Target.AddCard(User.slots[parameters.SelectIndex]);
        User.RemoveCard(parameters.SelectIndex);

        User.RemoveCard(parameters.UseCardIndex);

        if (CardHistoryManager.Instance != null)
        {
            CardHistoryManager.Instance.Record(
                new CardHistoryEntry(
                    parameters.UserId,
                    parameters.TargetId,
                    "give",
                    CardType.Function,
                    null
                //result
                )
            );
        }
        User.SetCooldownEnd(this.cardData);
    }
}
