using Fusion;
using UnityEngine;

public class RandomCard : NetworkBehaviour
{
    public CardCatalog cardCatalog;

    private void OnTriggerEnter(Collider other)
    {
        
        if (other.name == "Ragdoll")
        {
            // 倒地中的玩家不能撿取
            var character = other.transform.parent.gameObject.GetComponent<OodlesEngine.OodlesCharacter>();
            if (character != null && character.ragdollMode) return;

            if (cardCatalog == null || cardCatalog.cards.Count == 0)
            {
                Debug.LogWarning("[RandomCard] CardCatalog 未設置或卡池為空！");
                return;
            }
            // 隨機抽卡
            CardData randomCard = GetRandomCard();
            Debug.Log(randomCard.type+" "+randomCard.cardId);
            if (other.transform.parent.gameObject.GetComponent<PlayerInventory>().AddCard(randomCard))
            {
                CharacterSFXManager.Instance?.PlayPickUp();
                Runner.Despawn(this.GetComponent<NetworkObject>());
            }
        }
    }

    private CardData GetRandomCard()
    {
        int index = Random.Range(0, cardCatalog.CanSpwanCard.Count);
        return cardCatalog.CanSpwanCard[index].cardData;
    }
}
