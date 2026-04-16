using Fusion;
using UnityEngine;

public class PlayerItem : NetworkBehaviour
{

    public CardData cardData;


    void OnTriggerEnter(Collider other)
    {
        if (other.name == "Ragdoll")
        {
            // 倒地中的玩家不能撿取
            var character = other.transform.parent.gameObject.GetComponent<OodlesEngine.OodlesCharacter>();
            if (character != null && character.ragdollMode) return;

            if (other.transform.parent.gameObject.GetComponent<PlayerInventory>().AddCard(cardData))
            {
                // 通知 ObjectSpawner：這張卡是被玩家撿走的，不需要救援重生
                if (ObjectSpawner.Instance != null)
                    ObjectSpawner.Instance.UnregisterLostCard(this);

                this.gameObject.SetActive(false);
                if (cardData.type == CardType.Mission)
                {
                    Debug.LogWarning("ProcessPlayerCards()");
                    TraceMission.Instance.ProcessPlayerCards();
                }
                Runner.Despawn(this.GetComponent<NetworkObject>());
            }

        }
    }
}
