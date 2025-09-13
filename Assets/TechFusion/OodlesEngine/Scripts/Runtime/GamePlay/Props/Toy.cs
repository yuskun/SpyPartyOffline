using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OodlesEngine
{ 
    public class Toy : Prop
    {
        [SerializeField]
        int score = 1;

        [SerializeField]
        [HideInInspector]
        Material usedMat;

        //[SyncVar(hook = nameof(SetUsed))]
        bool used = false;

        void SetUsed(bool oldB, bool newB)
        {
            if (newB)
            {
                GetComponent<Renderer>().material = usedMat;
            }
        }

        public bool IsUsed()
        {
            return used;
        }

        public void MarkUsed()
        {
            used = true;
        }

        public int GetScore()
        {
            return score;
        }
    }
}