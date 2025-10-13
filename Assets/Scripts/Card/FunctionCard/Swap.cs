using UnityEngine;

[CreateAssetMenu(menuName = "Card/FunctionCard/Swap")]
public class Swap : FunctionCard
{
    public override bool CanUse(PlayerInventory user, PlayerInventory target)
    {
        if (target == null)
            return false;

        int userCount = 0,
            targetCount = 0;
        foreach (var c in user.slots)
            if (!c.IsEmpty())
                userCount++;
        foreach (var c in target.slots)
            if (!c.IsEmpty())
                targetCount++;
        return userCount >= 2 && targetCount >= 1;
    }

    public override void Execute(CardUseParameters parameters)
    {
        PlayerInventory User = PlayerInventoryManager
            .Instance.GetPlayer(parameters.UserId)
            .GetComponent<PlayerInventory>();
        PlayerInventory Target = PlayerInventoryManager
            .Instance.GetPlayer(parameters.TargetId)
            .GetComponent<PlayerInventory>();
        var tmp = Target.GetCard(parameters.TargetSelectIndex);
        Target.ReplaceCard(parameters.TargetSelectIndex, User.GetCard(parameters.SelectIndex));
        User.ReplaceCard(parameters.SelectIndex, tmp);
    }
}
