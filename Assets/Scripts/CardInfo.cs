using UnityEngine;

public class Card : ScriptableObject
{
    public CardData cardData;
    public string name;
    public string description;
    public Sprite image;
}
[System.Serializable]
public struct CardData
{
    public int id;
    public CardType type;
    public float cooldown;

    // 建構子
    public CardData(int id, CardType type, float cooldown)
    {
        this.id = id;
        this.type = type;
        this.cooldown = cooldown;
    }

    // 取得一個「空卡」的靜態方法
    public static CardData Empty()
    {
        return new CardData(-1, CardType.None, 0f);
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
public struct CardUseParameters
{
    public int UserId;                    // 使用玩家 ID
    public int TargetId;                  // 目標玩家 ID
    public CardData Card;                 // 使用的卡片
    public int UseCardIndex;              // 使用卡片所在欄位
    public int SelectIndex;         // 使用者自己要操作的卡槽
    public int TargetSelectIndex;       // 使用者選定的目標玩家卡槽
}