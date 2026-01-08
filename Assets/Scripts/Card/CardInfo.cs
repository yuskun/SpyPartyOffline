using Fusion;
using UnityEngine;

public class Card : ScriptableObject
{
    public CardData cardData;
    public string name;
    public string description;
    public Sprite image;
}
[System.Serializable]
public struct CardData : INetworkStruct
{
    public int cardId;
    public int id;
    public CardType type;
    public float cooldown;

    // 建構子
    public CardData(int cardId, int id, CardType type, float cooldown)
    {
        this.cardId = cardId;
        this.id = id;
        this.type = type;
        this.cooldown = cooldown;
    }

    // 取得一個「空卡」的靜態方法
    public static CardData Empty()
    {
        return new CardData(-1, -1, CardType.None, 0f);
    }

    // 判斷是否為空
    public bool IsEmpty()
    {
        return id == -1 && type == CardType.None;
    }
}
public enum CardType
{
    Mission,
    Function,
    Item,
    None
}
public struct CardUseParameters : INetworkStruct
{
    public int UserId;                    // 使用玩家 ID
    public int TargetId;                  // 目標玩家 ID
    public CardData Card;                 // 使用的卡片
    public int UseCardIndex;              // 使用卡片所在欄位
    public int SelectIndex;         // 使用者自己要操作的卡槽
    public int TargetSelectIndex;       // 使用者選定的目標玩家卡槽
}
[System.Serializable]
public class MissionData: INetworkStruct
{
    public int id;                // ✅ 任務唯一ID
    public string title;
    public string description;
    public int current;
    public int goal;

    public bool IsComplete => current >= goal;
    

    public MissionData(int id, string title, string desc, int goal)
    {
        this.id = id;
        this.title = title;
        this.description = desc;
        this.goal = goal;
        this.current = 0;
        
    }
}