using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OodlesEngine
{
    [CreateAssetMenu(fileName = "New Global Settings", menuName = "OodlesParty/Global Settings")]
    public class GlobalSetting : ScriptableObject
    {
        public string PlayerLayerName;
        public string RagdollLayerName;
        public string RagdollHandsLayerName;

        [Range(0.0f, 9999.0f)]
        public float JointSpringsStrength = 420;
        [Range(0.0f, 1000.0f)]
        public float JointSpringDamper = 1;
    }
}
