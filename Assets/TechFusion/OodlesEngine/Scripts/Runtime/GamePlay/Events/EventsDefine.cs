using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OodlesEngine
{
    public class SearchGrabTargetMessage
    {
        public delegate void SearchTargetCallback(Transform res);

        public HandFunction hc;
        public float radius;
        public SearchTargetCallback callback;
    }

    public class CheckGrabableMessage
    {
        public delegate void CheckGrabableCallback(bool res);

        public OodlesCharacter pc;
        public GameObject obj;
        public CheckGrabableCallback callback;
    }

    public class GrabObjectMessage
    {
        public OodlesCharacter pc;
        public HandFunction hf;
        public GameObject obj;
    }

    public class ReleaseObjectMessage
    {
        public OodlesCharacter pc;
        public GameObject obj;
    }

    public class ThrowObjectMessage
    {
        public OodlesCharacter pc;
        public GameObject obj;
        public Vector3 dir; 
        public bool twoHands;
    }

    public class TouchObjectMessage
    {
        public OodlesCharacter pc;
        public HandFunction hf;
        public GameObject obj;
    }

    public class HandAttackMessage
    {
        public OodlesCharacter pc;
        public Collision col;
    }

    public class WeaponAttackMessage
    {
        public OodlesCharacter pc;
        public Weapon wp; 
        public Vector3 dir;
        public GameObject obj;
    }
}