// Assets/Scripts/Inventory/CardCatalog.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardCatalog", menuName = "SpyParty/Card Catalog")]
public class CardCatalog : ScriptableObject
{
    // 你的 CardData 必須是可序列化的 class/struct
    // 若 CardData 是 ScriptableObject，也可改為 List<CardDataSO>。
    public List<Card> cards = new List<Card>();
}
