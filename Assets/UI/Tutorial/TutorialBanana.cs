using UnityEngine;
using OodlesEngine;

/// <summary>
/// 教學版香蕉皮 — 純本地 MonoBehaviour，不依賴 Photon Fusion。
/// 撞到 OodlesCharacter（且不在 ragdoll 模式）就把對方擊倒。
/// </summary>
public class TutorialBanana : MonoBehaviour
{
    [SerializeField] private float pushForce = 1000f;
    [SerializeField] private float autoDestroyAfter = 8f;
    [Tooltip("忽略丟擲者一段時間，避免剛 spawn 出去就打到自己")]
    [SerializeField] private float armDelay = 0.4f;

    private bool _hasHit = false;
    private float _spawnTime;
    private GameObject _thrower;

    public void SetThrower(GameObject thrower) { _thrower = thrower; }

    void Start()
    {
        _spawnTime = Time.time;
        if (autoDestroyAfter > 0f) Destroy(gameObject, autoDestroyAfter);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (_hasHit) return;
        if (Time.time - _spawnTime < armDelay) return; // 還沒 arm
        var rootGo = other.transform.root.gameObject;
        if (rootGo == _thrower) return; // 不打到自己

        var character = rootGo.GetComponent<OodlesCharacter>();
        if (character == null) return;
        if (character.ragdollMode) return;

        _hasHit = true;

        // 嘗試本地 KnockDown；失敗就 fallback 設 ragdoll
        try { character.KnockDown(); }
        catch
        {
            character.ragdollMode = true;
            try { character.ChangeState(OodlesCharacter.State.LostControl); } catch { }
        }

        // 沿移動方向加水平推力（複製 Banana.cs 的邏輯）
        var ragdoll = rootGo.transform.Find("Ragdoll");
        if (ragdoll != null)
        {
            var rb = ragdoll.GetComponent<Rigidbody>();
            Vector3 dir = Vector3.forward;
            if (rb != null && rb.linearVelocity.sqrMagnitude > 0.01f) dir = rb.linearVelocity.normalized;
            dir.y = 0f; if (dir.sqrMagnitude > 0.0001f) dir.Normalize();

            if (rb != null) rb.AddForce(dir * pushForce, ForceMode.Impulse);
        }

        Destroy(gameObject);
    }
}
