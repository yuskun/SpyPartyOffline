using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OodlesEngine
{ 
    public class Weapon : Prop
    {
        [HideInInspector]
        public OodlesCharacter owner;

        public float hitForce = 15000;

        void OnCollisionEnter(Collision col)
        {
            if (owner == null) return;

            //not me
            if (col.gameObject.transform.IsChildOf(owner.transform))
            {
                return;
            }

            if (owner.isAttacking)
            {
                EventBetter.Raise(new WeaponAttackMessage()
                {
                    pc = owner,
                    wp = this,
                    dir = -col.GetContact(0).normal,
                    obj = col.gameObject,
                });
            }
        }
    }
}