using UnityEngine;

namespace OodlesEngine
{ 
    public class JointMatch : MonoBehaviour
    {
        public ConfigurableJoint thisJoint;
        public Transform animationTarget;
        [HideInInspector]
        public OodlesCharacter oodlesCharacter;
        private Quaternion InvRotation;
        private Quaternion Rotation;
        private Quaternion startRotation;
        private Quaternion currentRotation;

        void Start()
        {
            ComputeStartRotation();
        }

        void ComputeStartRotation()
        {
            Quaternion tempRot = animationTarget.localRotation;
            InvRotation = Quaternion.Inverse(tempRot);
            startRotation = tempRot;
        }

        void ComputeCurrentRotation()
        {
            Quaternion tempRot = animationTarget.localRotation;
            Rotation = tempRot;
            currentRotation = tempRot;
        }

        void FixedUpdate()
        {
            ComputeCurrentRotation();
            ConfigurableJointExtensions.SetTargetRotationLocal(thisJoint, currentRotation, startRotation);
        }
    }
}