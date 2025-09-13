using System;
using UnityEditor;
using UnityEngine;

namespace OodlesEngine.Editor
{
    class RagdollProcessor
    {
        private GameObject characterGameObject;
        private Func<Vector3> getFrontDirection;
        private RagdollProperties defaultRagdollProperties = new RagdollProperties
        {
            AsTrigger = false,
            IsKinematic = false,
            RigidAngularDrag = 0.3f,
            RigidDrag = 0.3f
        };
        private int totalWeight = 60;

        public RagdollProcessor(GameObject characterGameObject, Func<Vector3> getFrontDirection)
        {
            this.characterGameObject = characterGameObject;
            this.getFrontDirection = getFrontDirection;
        }

        private void RemoveRagdoll()
        {
            RagdollCore core = new RagdollCore(characterGameObject.transform, getFrontDirection());
            core.ClearRagdoll();
        }

        public void CreateRagdoll()
        {
            RagdollCore core = new RagdollCore(characterGameObject.transform, getFrontDirection());
            core.ApplyRagdoll(totalWeight, defaultRagdollProperties);
        }
    }
}
