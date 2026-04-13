using UnityEngine;

[CreateAssetMenu(menuName = "Card/FunctionCard/Swap")]
public class Swap : FunctionCard
{
    public override bool CanUse(PlayerInventory user, PlayerInventory target)
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
        return userCount >= 2 && targetCount >= 1;
    }

    public override void Execute(CardUseParameters parameters)
    {
        Debug.Log("Swap Execute");
        PlayerInventory User = PlayerInventoryManager.Instance.GetPlayer(parameters.UserId).GetComponent<PlayerInventory>();
        PlayerInventory Target = PlayerInventoryManager.Instance.GetPlayer(parameters.TargetId).GetComponent<PlayerInventory>();

        // 隨機從對方背包挑一個有物品的欄位
        var validTargetSlots = new System.Collections.Generic.List<int>();
        for (int i = 0; i < Target.slots.Length; i++)
        {
            if (!Target.slots[i].IsEmpty())
                validTargetSlots.Add(i);
        }

        if (validTargetSlots.Count == 0)
        {
            Debug.LogWarning("[Swap] 對方背包沒有物品可交換");
            return;
        }

        int randomTargetIndex = validTargetSlots[Random.Range(0, validTargetSlots.Count)];

        var tmp = Target.GetCard(randomTargetIndex);
        Target.ReplaceCard(randomTargetIndex, User.GetCard(parameters.SelectIndex));
        User.ReplaceCard(parameters.SelectIndex, tmp);

        User.RemoveCard(parameters.UseCardIndex);
         if (CardHistoryManager.Instance != null)
        {
            CardHistoryManager.Instance.Record(
                new CardHistoryEntry(
                    parameters.UserId,
                    parameters.TargetId,
                    "Swap",
                    CardType.Function
                    //result
                )
            );
        }
        User.SetCooldownEnd(this.cardData);
        TraceMission.Instance.ProcessPlayerCards();
    }
}
