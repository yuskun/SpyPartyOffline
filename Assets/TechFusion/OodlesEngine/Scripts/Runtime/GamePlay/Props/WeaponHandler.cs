using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OodlesEngine
{ 
    public class WeaponHandler : MonoBehaviour
    {
        public Weapon wepon;

        //which hand to hold it
        public HandSide handSide = HandSide.HandRight;

        public OodlesCharacter GetOwner()
        {
            if (wepon != null)
            {
                return wepon.owner;
            }

            return null;
        }
    
        public void SetOwner(OodlesCharacter pc)
        {
            if (wepon != null)
            {
                wepon.owner = pc;
            }
        }
    }
}