using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OodlesEngine
{
    public class EngineSetup : SingletonMono<EngineSetup>
    {
        public bool Networked { get { return networked; } }

        protected bool networked = false;

        override protected void Awake()
        {
            base.Awake();

            string player = OodlesSetting.Instance.PlayerLayerName;
            string ragdoll = OodlesSetting.Instance.RagdollLayerName;
            string ragdollHands = OodlesSetting.Instance.RagdollHandsLayerName;

            int layerPlayer = LayerMask.NameToLayer(player);
            int layerRagdoll = LayerMask.NameToLayer(ragdoll);
            int layerRagdollHands = LayerMask.NameToLayer(ragdollHands);

            Physics.IgnoreLayerCollision(layerPlayer, layerRagdoll);
            Physics.IgnoreLayerCollision(layerPlayer, layerRagdollHands);

            Physics.defaultSolverIterations = 32;
            Physics.defaultSolverVelocityIterations = 16;

            Physics.gravity = new Vector3(0, -10, 0);

            Time.fixedDeltaTime = 0.01f;
        }
    }
}