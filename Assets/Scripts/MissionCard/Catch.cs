using UnityEngine;
[CreateAssetMenu(menuName = "Card/MissionCard/Catch")]
public class Catch : MissionCard
{
    public Card targetCard;


    public override void UseSkill(CardUseParameters parameters)
    {   
        CheckMission(parameters);
    }
    public override void CheckMission(CardUseParameters parameters)
    {

        if (PlayerInventoryManager.Instance.GetPlayer(parameters.TargetId).GetComponent<PlayerInventory>().HasCard(targetCard.cardData))
        {
            MissionWinSystem.Instance.GameOver();
        }
    }
}
