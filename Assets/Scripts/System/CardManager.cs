using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    [Header("卡片資料庫（用 ID 對應）")]
    public static CardManager Instance;
    public CardCatalog Catalog => catalog;

    [SerializeField]
    private CardCatalog catalog; // ScriptableObject 總表

    private Dictionary<int, FunctionCard> funcDictionary = new();
    private Dictionary<int, MissionCard> missionDictionary = new();
    private Dictionary<int, ItemCard> itemDictionary = new();

    public Dictionary<int, MissionCard> GetAllMissions() => missionDictionary;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildDictionary();
    }

    private void BuildDictionary()
    {
        funcDictionary.Clear();
        missionDictionary.Clear();
        itemDictionary.Clear();

        foreach (var card in catalog.cards)
        {
            if (card == null)
                continue;

            switch (card.cardData.type)
            {
                case CardType.Function:
                    if (!funcDictionary.ContainsKey(card.cardData.id))
                        funcDictionary[card.cardData.id] = card as FunctionCard;
                    break;

                case CardType.Mission:
                    if (!missionDictionary.ContainsKey(card.cardData.id))
                        missionDictionary[card.cardData.id] = card as MissionCard;
                    break;

                case CardType.Item:
                    if (!itemDictionary.ContainsKey(card.cardData.id))
                        itemDictionary[card.cardData.id] = card as ItemCard;
                    break;
            }
        }
    }

    public Sprite[] GetCardInfo(CardData[] data)
    {
        if (data == null || data.Length == 0)
            return new Sprite[0];

        Sprite[] result = new Sprite[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            CardData d = data[i];
            Sprite sprite = null;

            // 根據 type 分類查找
            switch (d.type)
            {
                case CardType.Function:
                    var func = CardManager.Instance.GetFunctionCard(d.id);
                    if (func != null)
                        sprite = func.image;
                    break;

                case CardType.Mission:
                    var mission = CardManager.Instance.GetMissionCard(d.id);
                    if (mission != null)
                        sprite = mission.image;
                    break;

                case CardType.Item:
                    var item = CardManager.Instance.GetItemCard(d.id);
                    if (item != null)
                        sprite = item.image;
                    break;

                default:
                    sprite = null; // 沒有的話就保持 null
                    break;
            }

            result[i] = sprite;
        }

        return result;
    }

    // 依 ID 取卡片
    public FunctionCard GetFunctionCard(int id) =>
        funcDictionary.TryGetValue(id, out var card) ? card : null;

    public MissionCard GetMissionCard(int id) =>
        missionDictionary.TryGetValue(id, out var card) ? card : null;

    public ItemCard GetItemCard(int id) =>
        itemDictionary.TryGetValue(id, out var card) ? card : null;
}
