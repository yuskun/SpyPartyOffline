using UnityEngine;

public class MissionCard : Card
{
    public MissionType missionType;
    public int Count;
    public int totalCount;

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

public enum MissionType
{
    Trigger,
    Collect,
}
