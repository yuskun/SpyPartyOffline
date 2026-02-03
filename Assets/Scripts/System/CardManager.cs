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
    public Card GetCardScriptObject(CardData data)
    {


        Card cardSO = null;

        switch (data.type)
        {
            case CardType.Function:
                cardSO = GetFunctionCard(data.id);
                break;

            case CardType.Mission:
                cardSO = GetMissionCard(data.id);
                break;

            case CardType.Item:
                cardSO = GetItemCard(data.id);
                break;
        }

        return cardSO;
    }



    public List<CardData> GetAllMissionCardData()
    {
        List<CardData> missionCardDataList = new List<CardData>();

        foreach (var missionCard in missionDictionary.Values)
        {
            if (missionCard != null)
            {
                missionCardDataList.Add(missionCard.cardData);
            }
        }

        return missionCardDataList;
    }
    // 依 ID 取卡片
    public FunctionCard GetFunctionCard(int id) =>
        funcDictionary.TryGetValue(id, out var card) ? card : null;

    public MissionCard GetMissionCard(int id) =>
        missionDictionary.TryGetValue(id, out var card) ? card : null;

    public ItemCard GetItemCard(int id) =>
        itemDictionary.TryGetValue(id, out var card) ? card : null;

    public void UseCard(CardUseParameters cardUse)
    {

        Card UseCard = GetCardScriptObject(cardUse.Card);
        switch (UseCard.cardData.type)
        {
            case CardType.Function:
                FunctionCard Card1 = GetFunctionCard(UseCard.cardData.id);
                Card1.Execute(cardUse);
                break;

            case CardType.Mission:
                MissionCard Card2 = GetMissionCard(UseCard.cardData.id);
                Card2.UseSkill(cardUse);
                break;

            case CardType.Item:
                ItemCard Card3 = GetItemCard(UseCard.cardData.id);
                Card3.Execute(cardUse);
                break;
        }
    }
    public void UpdateMissionData(int missionID, int newGoal)
    {
        MissionCard missionCard = GetMissionCard(missionID);
        if (missionCard != null)
        {
            missionCard.UpdateGoal(newGoal);
            Debug.Log($"[CardManager] 已更新任務卡(ID={missionID})的目標值為 {newGoal}");
        }
        else
        {
            Debug.LogWarning($"[CardManager] 找不到任務卡 ID={missionID} 以更新目標值");
        }
    }
}
