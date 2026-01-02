using Fusion;
using UnityEngine;

public class CardCooldownManager : NetworkBehaviour
{

    [Networked]
    public NetworkDictionary<int, int> CardCooldownEndTick => default;
    void SetCooldownEnd(CardData card)
    {
        if (!Object.HasStateAuthority)
            return;

        int cooldownTicks =
            Mathf.CeilToInt(card.cooldown / Runner.DeltaTime);

        int endTick = Runner.Tick + cooldownTicks;

        CardCooldownEndTick.Set(card.cardId, endTick);
    }
    bool CanUse(CardData card)
{
    if (!CardCooldownEndTick.TryGet(card.cardId, out int endTick))
        return true; // 從未進 CD

    return Runner.Tick >= endTick;
}
}
