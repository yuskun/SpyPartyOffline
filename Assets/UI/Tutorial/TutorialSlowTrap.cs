using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OodlesEngine;

/// <summary>
/// 教學版減速陷阱 — 純本地 MonoBehaviour，不依賴 Photon Fusion。
/// 玩家踩到時降低 moveForce/jumpForce，slowDuration 後恢復；
/// 陷阱本體 trapLifetime 後自動消失。
/// </summary>
public class TutorialSlowTrap : MonoBehaviour
{
    [SerializeField] private float slowMultiplier = 0.2f;
    [SerializeField] private float trapLifetime   = 5f;
    [SerializeField] private float slowDuration   = 3f;

    private readonly Dictionary<OodlesCharacter, Coroutine> _slowed = new();
    private bool _expired = false;

    void Start()
    {
        StartCoroutine(LifetimeRoutine());
    }

    private void OnCollisionEnter(Collision other)
    {
        var rootGo = other.transform.root.gameObject;
        var character = rootGo.GetComponent<OodlesCharacter>();
        if (character == null) return;
        if (character.ragdollMode) return;

        if (_slowed.ContainsKey(character))
        {
            StopCoroutine(_slowed[character]);
            _slowed[character] = StartCoroutine(SlowEffect(character));
        }
        else
        {
            character.moveForce *= slowMultiplier;
            character.jumpForce *= slowMultiplier;
            _slowed.Add(character, StartCoroutine(SlowEffect(character)));
        }
    }

    private IEnumerator SlowEffect(OodlesCharacter character)
    {
        yield return new WaitForSeconds(slowDuration);
        if (character != null)
        {
            character.moveForce /= slowMultiplier;
            character.jumpForce /= slowMultiplier;
        }
        _slowed.Remove(character);

        if (_expired && _slowed.Count == 0) Destroy(gameObject);
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(trapLifetime);
        _expired = true;
        if (_slowed.Count == 0) Destroy(gameObject);
    }
}
