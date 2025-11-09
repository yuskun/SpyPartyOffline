using Fusion;
using OodlesEngine;
using UnityEngine;

public class Banana : NetworkBehaviour
{
    public float pushForce = 1000f; // 額外的力
    

    private void OnCollisionEnter(Collision other)
    {
        GameObject player = other.transform.root.gameObject;
        var character = player.GetComponent<OodlesCharacter>();

        if (character == null) return;

        character.KnockDown();
        var ragdoll = player.transform.Find("Ragdoll");

        // 嘗試取得移動方向（方式一：透過 Rigidbody）
        Rigidbody body = ragdoll.GetComponent<Rigidbody>();
        Vector3 moveDir = Vector3.forward; // 預設值
        if (body != null && body.linearVelocity.sqrMagnitude > 0.01f)
        {
            moveDir = body.linearVelocity.normalized;
        }

        // 只保留水平分量（移除垂直方向）
        moveDir.y = 0f;
        moveDir.Normalize();

        // 使用可調變數 pushForce
        Vector3 force = moveDir * pushForce;

        if (ragdoll != null)
        {
            Rigidbody ragdollBody = ragdoll.GetComponent<Rigidbody>();
            if (ragdollBody != null)
            {
                ragdollBody.AddForce(force, ForceMode.Impulse);
            }
        }
        Runner.Despawn(this.GetComponent<NetworkObject>());
    }
}
