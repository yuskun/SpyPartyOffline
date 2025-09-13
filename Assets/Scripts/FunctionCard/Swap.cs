using UnityEngine;
[CreateAssetMenu(menuName = "Card/FunctionCard/Swap")]
public class Swap : FunctionCard
{
    public override void Execute(CardUseParameters parameters)
    {
        PlayerInventory User = PlayerInventoryManager.Instance.GetPlayer(parameters.UserId).GetComponent<PlayerInventory>();
        PlayerInventory Target = PlayerInventoryManager.Instance.GetPlayer(parameters.TargetId).GetComponent<PlayerInventory>();
        var tmp = Target.GetCard(parameters.TargetSelectIndex);
        Target.ReplaceCard(parameters.TargetSelectIndex, User.GetCard(parameters.SelectIndex));
        User.ReplaceCard(parameters.SelectIndex, tmp);
        
    }
}
