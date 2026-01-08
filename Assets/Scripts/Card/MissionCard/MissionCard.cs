using UnityEngine;

public class MissionCard : Card
{


    public MissionData data;

    public virtual bool CanUse(PlayerInventory user, PlayerInventory target, CardData card)
    {
        return true;
    }

    public virtual void UseSkill(CardUseParameters parameters)
    {
        Debug.Log("使用任務卡: " + name);
    }

    public virtual void CheckMission(CardUseParameters parameters)
    {
        Debug.Log("檢查任務: " + name);
    }

    public virtual void ResetMission(CardUseParameters parameters) { }
}


