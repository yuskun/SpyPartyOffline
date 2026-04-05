using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OodlesEngine
{ 
    public class Weapon : Prop
    {
        [HideInInspector]
        public OodlesCharacter owner;
        public int Time = 6;
        public bool CanBreak = true;

        [Header("耐久消耗")]
        public int swingCost = 2;     // 每次揮擊（空揮）消耗
        public int hitBonusCost = 1;  // 打到人額外消耗

        public float hitForce = 10000;

        /// <summary>打到人時額外扣耐久，如果歸零則立刻壞掉</summary>
        public void ApplyHitCost()
        {
            if (!CanBreak) return;
            Time -= hitBonusCost;
            if (Time <= 0 && owner != null)
            {
                owner.BreakWeapon(this);
            }
        }

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