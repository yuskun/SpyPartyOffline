using UnityEngine;
[CreateAssetMenu(menuName = "Card/MissionCard/Steal")]
public class Steal : MissionCard
{
    
    public override void UseSkill(CardUseParameters parameters)
    {
        PlayerInventory Target = PlayerInventoryManager.Instance.GetPlayer(parameters.TargetId).GetComponent<PlayerInventory>();
        PlayerInventory User = PlayerInventoryManager.Instance.GetPlayer(parameters.UserId).GetComponent<PlayerInventory>();
        CardData card = Target.RandomGetCard();
        User.AddCard(card);
    }
}
