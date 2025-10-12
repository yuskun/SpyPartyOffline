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
        //Debug.Log("Peek Execute"); 
        PlayerInventory User = PlayerInventoryManager.Instance.GetPlayer(parameters.UserId).GetComponent<PlayerInventory>();
        PlayerInventory Target = PlayerInventoryManager.Instance.GetPlayer(parameters.TargetId).GetComponent<PlayerInventory>();

        //UIManager.Instance.SetPeekUISprites(CardManager.Instance.GetCardInfo(Target.GetAllCards()));
        //UIManager.Instance.PeekUI.SetActive(true);
        Debug.Log("準備呼叫 RemoveCard, User=" + (User != null ? User.GetInstanceID().ToString() : "null") + ", useCardIndex=" + parameters.UseCardIndex);
        Debug.Log($"查找結果: playerId={User.playerId}, InstanceID={User.GetInstanceID()}");
        User.RemoveCard(parameters.UseCardIndex);
        
    }
}
