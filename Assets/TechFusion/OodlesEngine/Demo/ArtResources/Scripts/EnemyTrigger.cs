using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OodlesEngine;

public class EnemyTrigger : MonoBehaviour
{
    public EnemyAI enemy;

    private bool used = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (used) return;

        LocalPlayer lp = other.gameObject.GetComponentInParent<LocalPlayer>();
        if (lp != null)
        {
            Debug.Log("Player Enter!");
            used = true;
            enemy.SetAlive(true);
        }
    }
}
