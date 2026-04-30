using Fusion;
using UnityEngine;

public class PlayerItem : NetworkBehaviour
{
    public CardData cardData;

    // 被擊倒掉落時的散射初速（由 ObjectSpawner 設置，0 代表一般生成不走物理）
    [Networked] public Vector3 InitialVelocity { get; set; }

    private Rigidbody _rb;
    [SerializeField]private SphereCollider _physicsCollider;

    public override void Spawned()
    {
        if (InitialVelocity.sqrMagnitude > 0.0001f)
        {
            SetupPhysicsBody();
            _rb.linearVelocity = InitialVelocity;
            _rb.AddTorque(Random.insideUnitSphere * 3f, ForceMode.Impulse);
        }
    }

    private void SetupPhysicsBody()
    {
        _physicsCollider.radius = 0.15f;
        _physicsCollider.isTrigger = false;

        _rb = gameObject.AddComponent<Rigidbody>();
        _rb.mass = 2f;
        _rb.linearDamping = 0.6f;
        _rb.angularDamping = 1.5f;
        _rb.useGravity = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "Ragdoll")
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
