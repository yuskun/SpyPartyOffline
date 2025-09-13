using UnityEngine;
[CreateAssetMenu(menuName = "Card/FunctionCard/Give")]
public class Give : FunctionCard
{
    public override void Execute(CardUseParameters parameters)
    {
        PlayerInventory User = PlayerInventoryManager.Instance.GetPlayer(parameters.UserId).GetComponent<PlayerInventory>();
        PlayerInventory Target = PlayerInventoryManager.Instance.GetPlayer(parameters.TargetId).GetComponent<PlayerInventory>();
        Target.AddCard(User.GetCard(parameters.SelectIndex));
        User.RemoveCard(parameters.SelectIndex);
        User.RemoveCard(parameters.UseCardIndex); 
    }
}
