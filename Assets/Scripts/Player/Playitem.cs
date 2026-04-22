using Fusion;
using UnityEngine;
using System.Collections;

public class PlayerItem : NetworkBehaviour
{

    public CardData cardData;

    // 被擊倒掉落時的散射初速（由 ObjectSpawner 設置，0 代表一般生成不走物理）
    [Networked] public Vector3 InitialVelocity { get; set; }

    private Rigidbody _rb;
    private SphereCollider _physicsCollider;

    public override void Spawned()
    {
        if (InitialVelocity.sqrMagnitude > 0.0001f)
        {
            SetupPhysicsBody();
            _rb.linearVelocity = InitialVelocity;
            _rb.AddTorque(Random.insideUnitSphere * 3f, ForceMode.Impulse);
            StartCoroutine(FreezeAfterSettle());
        }
    }

    private void SetupPhysicsBody()
    {
        _physicsCollider = gameObject.AddComponent<SphereCollider>();
        _physicsCollider.radius = 0.15f;
        _physicsCollider.isTrigger = false;

        _rb = gameObject.AddComponent<Rigidbody>();
        _rb.mass = 0.5f;
        _rb.linearDamping = 0.6f;
        _rb.angularDamping = 1.5f;
        _rb.useGravity = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private IEnumerator FreezeAfterSettle()
    {
        yield return new WaitForSeconds(2.5f);
        float t = 0f;
        while (_rb != null && _rb.linearVelocity.sqrMagnitude > 0.04f && t < 1.5f)
        {
            t += Time.deltaTime;
            yield return null;
        }
        if (_rb != null)
        {
            _rb.isKinematic = true;
            if (Object != null && Object.HasStateAuthority)
            {
                var sp = GetComponent<SetPosition>();
                if (sp != null) sp.Setpos(transform.position);
            }
        }
    }
    void OollisionEnter(Collision collision)
    {
        var other=collision;
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
