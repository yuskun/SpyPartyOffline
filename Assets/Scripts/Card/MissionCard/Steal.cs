using UnityEngine;

[CreateAssetMenu(menuName = "Card/MissionCard/Steal")]
public class Steal : MissionCard
{
    public override bool CanUse(PlayerInventory user, PlayerInventory target, CardData card)
    {
        return user.CanUse(this.cardData);
    }

    public override void UseSkill(CardUseParameters parameters)
    {
        // TargetId 存放 StealIndex（0/1/2），從靜態登錄表找物件
        var stealTarget = StealTargetObject.All.Find(x => x.StealIndex == parameters.TargetId);
        if (stealTarget == null)
        {
            Debug.LogWarning($"[Steal] 找不到 StealIndex={parameters.TargetId} 的目標物件（可能已被偷走）");
            return;
        }

        stealTarget.BeStolen();
        CharacterSFXManager.Instance?.PlayUseCard();

        PlayerInventory user = PlayerInventoryManager.Instance.GetPlayer(parameters.UserId).GetComponent<PlayerInventory>();
        user.SetCooldownEnd(this.cardData);

        Debug.Log($"[Steal] 玩家 {parameters.UserId} 偷走了場景物件 StealIndex={parameters.TargetId}");

        if (CardHistoryManager.Instance != null)
        {
            CardHistoryManager.Instance.Record(new CardHistoryEntry(
                parameters.UserId,
                -1,
                "Steal",
                CardType.Mission
            ));
        }

        MissionWinSystem.Instance?.OnStealObjectCollected(parameters.UserId);
    }
}
