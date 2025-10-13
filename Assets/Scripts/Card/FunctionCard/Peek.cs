using UnityEngine;

[CreateAssetMenu(menuName = "Card/FunctionCard/Peek")]
public class Peek : FunctionCard
{
    public override bool CanUse(PlayerInventory user, PlayerInventory target)
    {
        if (target == null)
            return false;
        else
            return true;
    }

    public override void Execute(CardUseParameters parameters)
    {
        PlayerInventory Target = PlayerInventoryManager.Instance.GetPlayer(parameters.TargetId).GetComponent<PlayerInventory>();
        UIManager.Instance.SetPeekUISprites(CardManager.Instance.GetCardInfo(Target.GetAllCards()));
        UIManager.Instance.PeekUI.SetActive(true);
    }
}
