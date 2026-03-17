using UnityEngine;

[CreateAssetMenu(menuName = "Card/FunctionCard/Wiretap")]
public class Wiretap : FunctionCard
{
    public float duration = 60f;

    public override bool CanUse(PlayerInventory user, PlayerInventory target)
    {
        if (target == null) return false;
        if (!user.CanUse(this.cardData)) return false;
        return true;
    }

    public override void Execute(CardUseParameters parameters)
    {
        PlayerInventory user = PlayerInventoryManager.Instance
            .GetPlayer(parameters.UserId).GetComponent<PlayerInventory>();

        user.RemoveCard(parameters.UseCardIndex);
        user.SetCooldownEnd(this.cardData);

        // 取得使用方的 PlayerRef，RPC 只發給他
        NetworkPlayer tapperNet = PlayerInventoryManager.Instance
            .GetPlayer(parameters.UserId).GetComponent<NetworkPlayer>();

        GameManager.instance.Rpc_SetWiretap(tapperNet.PlayerId, parameters.TargetId, duration);

        if (CardHistoryManager.Instance != null)
            CardHistoryManager.Instance.Record(new CardHistoryEntry(
                parameters.UserId, parameters.TargetId, "Wiretap", CardType.Function));
    }
}
