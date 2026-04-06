using Unity.VisualScripting;
using UnityEngine;
[CreateAssetMenu(menuName = "Card/MissionCard/Catch")]
public class Catch : MissionCard
{
    public Card targetCard;

    [Header("任務步驟文字")]
    public string step0Title = "尋找小偷";
    public string step0Desc = "抓到持有 Steal 卡的玩家";
    public string step1Title = "押送小偷";
    public string step1Desc = "在小偷附近待20秒";
      public override bool CanUse(PlayerInventory user, PlayerInventory target, CardData card)
    {
        if (target == null || user.CanUse(card) == false)
            return false;
        return true;
    }


    public override void UseSkill(CardUseParameters parameters)
    {
        CheckMission(parameters);
    }
    public override void CheckMission(CardUseParameters parameters)
    {
        CharacterSFXManager.Instance?.PlayUseCard();

        bool hasSteal = PlayerInventoryManager.Instance.GetPlayer(parameters.TargetId).GetComponent<PlayerInventory>().HasCard(targetCard.cardData);
        Debug.Log($"[Catch] UserId={parameters.UserId} TargetId={parameters.TargetId} targetHasSteal={hasSteal} targetCardData=id:{targetCard?.cardData.id} type:{targetCard?.cardData.type}");
        if (hasSteal)
        {
            // 目標持有 Steal 卡 → 進入押送流程（MissionStates 由 StartEscort 重置，TickEscort 每秒更新）
            MissionWinSystem.Instance.StartEscort(parameters.UserId, parameters.TargetId);
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
                    CardType.Mission
                    //result
                )
            );
        }
        
        
    }
}
