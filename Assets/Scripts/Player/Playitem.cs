using Fusion;
using UnityEngine;

public class PlayerItem : NetworkBehaviour
{
   
    public CardData cardData;
 

    void OnTriggerEnter(Collider other)
    {
        if (other.name == "Ragdoll")
        {
            if (other.transform.parent.gameObject.GetComponent<PlayerInventory>().AddCard(cardData))
            {
                if (cardData.type == CardType.Mission)
                {
                    TraceMission.Instance.ProcessPlayerCards();
                }
                Runner.Despawn(this.GetComponent<NetworkObject>());
            }

        }
    }
}
