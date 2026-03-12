using UnityEngine;
using System.Collections.Generic;
using Fusion;

public class Door :  NetworkBehaviour
{
    private Animator animator;  // 儲存 Animator
    private HashSet<GameObject> playersInRange = new HashSet<GameObject>(); // 範圍內玩家列表

    void Awake()
    {
        // 抓取自己底下的 Animator
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("Door: No Animator found in children!");
        }
    }

    // 當有物體進入 Collider (需設為 Trigger)
    private void OnTriggerEnter(Collider other)
    {
          if (!Runner.IsServer) return;
        if (other.CompareTag("Player"))
        {
            playersInRange.Add(other.gameObject);
            if (animator != null)
            {
                animator.SetBool("IsOpen", true);
            }
        }
    }

    // 當有物體離開 Collider
    private void OnTriggerExit(Collider other)
    {
          if (!Runner.IsServer) return;
        if (other.CompareTag("Player"))
        {
            playersInRange.Remove(other.gameObject);

            // 如果範圍內已經沒有玩家了
            if (playersInRange.Count == 0 && animator != null)
            {
                animator.SetBool("IsOpen", false);
            }
        }
    }
}