using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OodlesEngine;
using Fusion;

public class SlowTrap : NetworkBehaviour
{
    [SerializeField] private float slowMultiplier = 0.2f; // 減速倍率
    [SerializeField] private float trapLifetime = 5f;     // 陷阱存活時間
    [SerializeField] private float slowDuration = 3f;     // 減速持續時間

    // 記錄玩家與其減速剩餘時間
    private readonly Dictionary<OodlesCharacter, Coroutine> slowedPlayers = new();

    private bool trapExpired = false; // 陷阱是否到期（時間到）

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            var Player = other.transform.root;
            if (Player != null)
            {
                var character = Player.GetComponent<OodlesCharacter>();
                if (character != null)
                {
                    Debug.Log("玩家踩到陷阱");

                    // 如果已經在減速，先重置計時
                    if (slowedPlayers.ContainsKey(character))
                    {
                        StopCoroutine(slowedPlayers[character]);
                        slowedPlayers[character] = StartCoroutine(SlowEffect(character));
                    }
                    else
                    {
                        // 套用減速
                        character.moveForce *= slowMultiplier;
                        character.jumpForce *= slowMultiplier;

                        var co = StartCoroutine(SlowEffect(character));
                        slowedPlayers.Add(character, co);
                    }
                }
            }
        }

        // 啟動陷阱存活計時（只會執行一次）
        if (!trapExpired)
        {
            StartCoroutine(TrapTimer());
        }
    }

    private IEnumerator SlowEffect(OodlesCharacter character)
    {
        yield return new WaitForSeconds(slowDuration);

        // 結束效果，恢復數值
        if (character != null)
        {
            character.moveForce /= slowMultiplier;
            character.jumpForce /= slowMultiplier;
        }

        slowedPlayers.Remove(character);

        // 如果陷阱已過期 & 沒有玩家再受影響 → 銷毀陷阱
        if (trapExpired && slowedPlayers.Count == 0)
        {
            Runner.Despawn(this.GetComponent<NetworkObject>());
        }
    }

    private IEnumerator TrapTimer()
    {
        yield return new WaitForSeconds(trapLifetime);

        trapExpired = true;

        // 如果沒有玩家還在受影響，直接銷毀
        if (slowedPlayers.Count == 0)
        {
            Runner.Despawn(this.GetComponent<NetworkObject>());
        }
    }
}
