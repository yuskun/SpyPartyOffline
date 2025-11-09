using Fusion;
using UnityEngine;

public class RandomCard : NetworkBehaviour
{
    public CardCatalog cardCatalog;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Test");
        if (other.name == "Ragdoll")
        {
            if (cardCatalog == null || cardCatalog.cards.Count == 0)
            {
                Debug.LogWarning("[RandomCard] CardCatalog 未設置或卡池為空！");
                return;
            }
            // 隨機抽卡
            CardData randomCard = GetRandomCard();
            if (other.transform.parent.gameObject.GetComponent<PlayerInventory>().AddCard(randomCard))
            {
                Runner.Despawn(this.GetComponent<NetworkObject>());
            }
        }
    }

    private CardData GetRandomCard()
    {
        int index = Random.Range(0, cardCatalog.CanSpwanCard.Count);
        return cardCatalog.cards[index].cardData;
    }
}
