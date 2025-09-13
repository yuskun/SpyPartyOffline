using UnityEngine;

namespace OodlesEngine.Editor
{
    public class RagdollProperties
    {
        public bool AsTrigger;
        public bool IsKinematic;
        public bool UseGravity = true;
        public bool CreateTips = true;
        public float RigidDrag;
        public float RigidAngularDrag;
        public CollisionDetectionMode CdMode;
    }

    abstract class RagdollJoint
    {
        public readonly Transform Transform;
        public Rigidbody Rigidbody;
        public ConfigurableJoint Joint;

        protected RagdollJoint(Transform transform)
        {
            Transform = transform;
        }
    }

    sealed class RagdollJointBox : RagdollJoint
    {
        public BoxCollider Collider;

        public RagdollJointBox(Transform transform) : base(transform) { }
    }

    sealed class RagdollJointCapsule : RagdollJoint
    {
        public CapsuleCollider Collider;

        public RagdollJointCapsule(Transform transform) : base(transform) { }
    }

    sealed class RagdollJointSphere : RagdollJoint
    {
        public SphereCollider Collider;

        public RagdollJointSphere(Transform transform) : base(transform) { }
    }

    struct WeightCalculator
    {
        public readonly float Pelvis;
        public readonly float Hip;
        public readonly float Knee;
        public readonly float Foot;
        public readonly float Arm;
        public readonly float Elbow;
        public readonly float Hand;
        public readonly float Chest;
        public readonly float Head;

        public WeightCalculator(float totalWeight, bool withTips)
        {
            Pelvis = totalWeight * 0.20f;
            Chest = totalWeight * 0.20f;
            Head = totalWeight * 0.05f;

            Hip = 0;
            Knee = 0;
            Foot = 0;
            Arm = 0;
            Elbow = 0;
            Hand = 0;
            CalculateWeightComponents(totalWeight, withTips, out Hip, out Knee, out Foot, out Arm, out Elbow, out Hand);

            float checkSum = CalculateChecksum();
            ValidateChecksum(totalWeight, checkSum);
        }

        private void CalculateWeightComponents(float totalWeight, bool withTips, out float hip, out float knee, out float foot, out float arm, out float elbow, out float hand)
        {
            hip = totalWeight * 0.20f / 2f;
            knee = withTips ? totalWeight * 0.15f / 2f : totalWeight * 0.20f / 2f;
            foot = withTips ? totalWeight * 0.05f / 2f : 0f;

            arm = totalWeight * 0.08f / 2f;
            elbow = withTips ? totalWeight * 0.05f / 2f : totalWeight * 0.07f / 2f;
            hand = withTips ? totalWeight * 0.02f / 2f : 0f;
        }

        private float CalculateChecksum()
        {
            return Pelvis +
                   Hip * 2f +
                   Knee * 2f +
                   Foot * 2f +
                   Arm * 2f +
                   Elbow * 2f +
                   Hand * 2f +
                   Chest +
                   Head;
        }

        private void ValidateChecksum(float totalWeight, float checkSum)
        {
            if (Mathf.Abs(totalWeight - checkSum) > Mathf.Epsilon)
                Debug.LogError("totalWeight != checkSum (" + totalWeight.ToString() + ", " + checkSum.ToString() + ")");
        }
    }
}
