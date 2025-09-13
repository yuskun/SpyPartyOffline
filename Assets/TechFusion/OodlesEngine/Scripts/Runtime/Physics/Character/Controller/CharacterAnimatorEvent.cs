using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OodlesEngine
{ 
    public class CharacterAnimatorEvent : MonoBehaviour
    {
        public OodlesCharacter controller;

        void HitB()
        {
            controller.isAttacking = true;
        }

        void HitE()
        {
            controller.isAttacking = false;
        }
    }
}