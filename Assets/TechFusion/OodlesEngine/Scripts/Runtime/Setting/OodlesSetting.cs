using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OodlesEngine
{
    public class OodlesSetting : Singleton<OodlesSetting>
    {
        public string PlayerLayerName = "Player";
        public string RagdollLayerName = "Ragdoll";
        public string RagdollHandsLayerName = "RagdollHands";

        [Range(0.0f, 9999.0f)]
        public float JointSpringsStrength = 420;
        [Range(0.0f, 1000.0f)]
        public float JointSpringDamper = 1000;
    }
}