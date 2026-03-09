using UnityEngine;

public class FunctionCard : Card
{
     public bool needTarget = false;

    public virtual bool CanUse(PlayerInventory user, PlayerInventory target)
    {
        return true;
    }

    public virtual void Execute(CardUseParameters parameters) { }


}
