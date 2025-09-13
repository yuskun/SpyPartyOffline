using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace OodlesEngine.Editor
{
	/// <summary>
	/// Class responsible for regdoll and unregdoll character
	/// </summary>
	sealed class RagdollCore
	{
        const string ColliderNodeSuffix = "_ColliderRotator";
        readonly bool _readyToGenerate;
        readonly Vector3 _playerDirection;
        readonly Transform _rootNode;

        // Ragdoll joints for various body parts
        private RagdollJointBox _pelvis;
        private RagdollJointCapsule _leftHip, _leftKnee, _rightHip, _rightKnee;
        private RagdollJointCapsule _leftArm, _leftElbow, _rightArm, _rightElbow;
        private RagdollJointBox _chest;
        private RagdollJointSphere _head;
        private RagdollJointSphere _leftFoot, _rightFoot, _leftHand, _rightHand;

        public RagdollCore(Transform player, Vector3 playerDirection)
        {
            _playerDirection = playerDirection;
            _readyToGenerate = false;

            // Find Animator
            Animator animator = FindAnimator(player);
            if (animator == null)
                return;

            _rootNode = animator.transform;

            // Initialize ragdoll joints
            InitializeRagdollJoints(animator);

            if (!CheckFields())
            {
                Debug.LogError("Not all nodes were found!");
                return;
            }

            _readyToGenerate = true;
        }

        private void InitializeRagdollJoints(Animator animator)
        {
            _pelvis = CreateRagdollJointBox(animator.GetBoneTransform(HumanBodyBones.Hips));
            _leftHip = CreateRagdollJointCapsule(animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg));
            _leftKnee = CreateRagdollJointCapsule(animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg));
            _rightHip = CreateRagdollJointCapsule(animator.GetBoneTransform(HumanBodyBones.RightUpperLeg));
            _rightKnee = CreateRagdollJointCapsule(animator.GetBoneTransform(HumanBodyBones.RightLowerLeg));
            _leftArm = CreateRagdollJointCapsule(animator.GetBoneTransform(HumanBodyBones.LeftUpperArm));
            _leftElbow = CreateRagdollJointCapsule(animator.GetBoneTransform(HumanBodyBones.LeftLowerArm));
            _rightArm = CreateRagdollJointCapsule(animator.GetBoneTransform(HumanBodyBones.RightUpperArm));
            _rightElbow = CreateRagdollJointCapsule(animator.GetBoneTransform(HumanBodyBones.RightLowerArm));
            _chest = CreateRagdollJointBox(animator.GetBoneTransform(HumanBodyBones.Chest));
            _head = CreateRagdollJointSphere(animator.GetBoneTransform(HumanBodyBones.Head));

            _leftFoot = CreateRagdollJointSphere(animator.GetBoneTransform(HumanBodyBones.LeftFoot));
            _rightFoot = CreateRagdollJointSphere(animator.GetBoneTransform(HumanBodyBones.RightFoot));
            _leftHand = CreateRagdollJointSphere(animator.GetBoneTransform(HumanBodyBones.LeftHand));
            _rightHand = CreateRagdollJointSphere(animator.GetBoneTransform(HumanBodyBones.RightHand));

            if (_chest.Transform == null)
                _chest = CreateRagdollJointBox(animator.GetBoneTransform(HumanBodyBones.Spine));
        }

        private RagdollJointBox CreateRagdollJointBox(Transform transform)
        {
            return new RagdollJointBox(transform);
        }

        private RagdollJointCapsule CreateRagdollJointCapsule(Transform transform)
        {
            return new RagdollJointCapsule(transform);
        }

        private RagdollJointSphere CreateRagdollJointSphere(Transform transform)
        {
            return new RagdollJointSphere(transform);
        }

        /// <summary>
        /// Finds animator component in "player" and in parents till it find Animator component. Otherwise returns null
        /// </summary>
        static Animator FindAnimator(Transform player)
        {
            Animator animator;
            do
            {
                animator = player.GetComponent<Animator>();
                if (animator != null && animator.enabled)
                    break;

                player = player.parent;
            }
            while (player != null);

            if (animator == null | player == null)
            {
                Debug.LogError("An Animator must be attached to find bones!");
                return null;
            }
            if (!animator.isHuman)
            {
                Debug.LogError("To auto detect bones, there has to be a humanoid Animator!");
                return null;
            }
            return animator;
        }

        /// <summary>
        /// Some checks before Applying ragdoll
        /// </summary>
        bool CheckFields()
        {
            if (_rootNode == null |
                _pelvis == null |
                _leftHip == null |
                _leftKnee == null |
                _rightHip == null |
                _rightKnee == null |
                _leftArm == null |
                _leftElbow == null |
                _rightArm == null |
                _rightElbow == null |
                _chest == null |
                _head == null)
                return false;

            return true;
        }

        /// <summary>
        /// Create all ragdoll's components and set their properties
        /// </summary>
        public void ApplyRagdoll(float totalMass, RagdollProperties ragdollProperties)
        {
            if (!_readyToGenerate)
            {
                Debug.LogError("Initialization failed. Reinstance object!");
                return;
            }

            var weight = new WeightCalculator(totalMass, ragdollProperties.CreateTips);

            bool alreadyRagdolled = _pelvis.Transform.gameObject.GetComponent<Rigidbody>() != null;

            AddComponentsTo(_pelvis, ragdollProperties, weight.Pelvis, false);
            AddComponentsTo(_leftHip, ragdollProperties, weight.Hip, true);
            AddComponentsTo(_leftKnee, ragdollProperties, weight.Knee, true);
            AddComponentsTo(_rightHip, ragdollProperties, weight.Hip, true);
            AddComponentsTo(_rightKnee, ragdollProperties, weight.Knee, true);
            AddComponentsTo(_leftArm, ragdollProperties, weight.Arm, true);
            AddComponentsTo(_leftElbow, ragdollProperties, weight.Elbow, true);
            AddComponentsTo(_rightArm, ragdollProperties, weight.Arm, true);
            AddComponentsTo(_rightElbow, ragdollProperties, weight.Elbow, true);
            AddComponentsTo(_chest, ragdollProperties, weight.Chest, true);
            AddComponentsTo(_head, ragdollProperties, weight.Head, true);

            if (ragdollProperties.CreateTips)
            {
                AddComponentsTo(_leftFoot, ragdollProperties, weight.Foot, true);
                AddComponentsTo(_rightFoot, ragdollProperties, weight.Foot, true);
                AddComponentsTo(_leftHand, ragdollProperties, weight.Hand, true);
                AddComponentsTo(_rightHand, ragdollProperties, weight.Hand, true);
            }

            if (alreadyRagdolled)
                return;

            // Pelvis
            Vector3 pelvisSize = new Vector3(0.32f, 0.31f, 0.3f);
            Vector3 pelvisCenter = new Vector3(00f, 0.06f, -0.01f);
            _pelvis.Collider.size = Abs(_pelvis.Transform.InverseTransformVector(pelvisSize));
            _pelvis.Collider.center = _pelvis.Transform.InverseTransformVector(pelvisCenter);

            ApplySide(true, ragdollProperties.CreateTips);
            ApplySide(false, ragdollProperties.CreateTips);

            // Chest collider
            Vector3 chestSize = new Vector3(0.34f, 0.34f, 0.28f);

            float y = (pelvisSize.y + chestSize.y) / 2f + pelvisCenter.y;
            y -= _chest.Transform.position.y - _pelvis.Transform.position.y;
            _chest.Collider.size = Abs(_chest.Transform.InverseTransformVector(chestSize));
            //_chest.Collider.center = _chest.Transform.InverseTransformVector(new Vector3(0f, y, -0.03f));
            //todo, fix
            _chest.Collider.center = _chest.Transform.InverseTransformVector(new Vector3(0f, 0, -0.03f));

            // Chest joint
            var chestJoint = _chest.Joint;
            ConfigureJointParams(_chest, _pelvis.Rigidbody, _rootNode.right, _rootNode.forward);

            // head
            float headScale = 3f / (_head.Transform.lossyScale.x + _head.Transform.lossyScale.y + _head.Transform.lossyScale.z);
            _head.Collider.radius = 0.1f * headScale;
            _head.Collider.center = _head.Transform.InverseTransformVector(new Vector3(0f, 0.09f, 0.03f));
            var headJoint = _head.Joint;
            ConfigureJointParams(_head, _chest.Rigidbody, _rootNode.right, _rootNode.forward);
        }

        private Vector3 Abs(Vector3 v)
        {
            return new Vector3(
                Mathf.Abs(v.x),
                Mathf.Abs(v.y),
                Mathf.Abs(v.z)
            );
        }

        static void ConfigureJointParams(RagdollJoint part, Rigidbody anchor, Vector3 axis, Vector3 secondaryAxis)
        {
            part.Joint.connectedBody = anchor;
            part.Joint.axis = part.Transform.InverseTransformDirection(axis);
            part.Joint.secondaryAxis = part.Transform.InverseTransformDirection(secondaryAxis);
        }

        /// Configure one hand and one leg
        /// <param name="leftSide">If true, configuration applies to the left hand and left leg, otherwise right hand and right leg</param>
        void ApplySide(bool leftSide, bool createTips)
        {
            RagdollJointCapsule hip = (leftSide ? _leftHip : _rightHip);
            RagdollJointCapsule knee = (leftSide ? _leftKnee : _rightKnee);
            RagdollJointSphere foot = (leftSide ? _leftFoot : _rightFoot);

            RagdollJointCapsule arm = (leftSide ? _leftArm : _rightArm);
            RagdollJointCapsule elbow = (leftSide ? _leftElbow : _rightElbow);
            RagdollJointSphere hand = (leftSide ? _leftHand : _rightHand);

            ConfigureRagdollForLimb(hip, knee, foot, createTips);
            ConfigureLegsJoints(hip, knee, foot, createTips);

            ConfigureRagdollForLimb(arm, elbow, hand, createTips);
            ConfigureHandJoints(arm, elbow, hand, leftSide, createTips);
        }

        /// <summary>
        /// Configures one of 4 body parts: right leg, left leg, right hand, or left hand
        /// </summary>
        static void ConfigureRagdollForLimb(RagdollJointCapsule limbUpper, RagdollJointCapsule limbLower, RagdollJointSphere tip, bool createTips)
        {
            float totalLength = limbUpper.Transform.InverseTransformPoint(tip.Transform.position).magnitude;

            // limbUpper
            CapsuleCollider upperCapsule = limbUpper.Collider;
            var boneEndPos = limbUpper.Transform.InverseTransformPoint(limbLower.Transform.position);
            upperCapsule.direction = GetXyzDirection(limbLower.Transform.localPosition);
            upperCapsule.radius = totalLength * 0.12f;
            upperCapsule.height = boneEndPos.magnitude;
            upperCapsule.center = Vector3.Scale(boneEndPos, Vector3.one * 0.5f);

            // limbLower
            CapsuleCollider endCapsule = limbLower.Collider;
            boneEndPos = limbLower.Transform.InverseTransformPoint(tip.Transform.position);
            endCapsule.direction = GetXyzDirection(boneEndPos);
            endCapsule.radius = totalLength * 0.12f;
            endCapsule.height = boneEndPos.magnitude;
            endCapsule.center = Vector3.Scale(boneEndPos, Vector3.one * 0.5f);

            // tip
            if (createTips)
            {
                boneEndPos = GetLongestTransform(tip.Transform).position;
                boneEndPos = tip.Transform.InverseTransformPoint(boneEndPos);

                Vector3 tipDir = GetXyzDirectionV(boneEndPos);
                Vector3 tipSides = (tipDir - Vector3.one) * -1;
                Vector3 boxSize = tipDir * boneEndPos.magnitude * 1.3f + tipSides * totalLength * 0.2f;

                SphereCollider tipSphere = tip.Collider;
                tipSphere.radius = boxSize.x;

                float halfTipLength = boneEndPos.magnitude / 2f;
                tipSphere.center = Vector3.Scale(boneEndPos.normalized, Vector3.one * halfTipLength);
            }
        }

        private static Transform GetLongestTransform(Transform limb)
        {
            float longestF = -1;
            Transform longestT = null;

            // find the farthest object that attached to 'limb'
            foreach (Transform t in limb.GetComponentsInChildren<Transform>())
            {
                float length = (limb.position - t.position).sqrMagnitude;
                if (length > longestF)
                {
                    longestF = length;
                    longestT = t;
                }
            }

            return longestT;
        }

        static Vector3 GetXyzDirectionV(Vector3 node)
        {
            var d = GetXyzDirection(node);

            switch (d)
            {
                case 0: return Vector3.right;
                case 1: return Vector3.up;
                case 2: return Vector3.forward;
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Get the most appropriate direction in terms of PhysX (0,1,2 directions)
        /// </summary>
        static int GetXyzDirection(Vector3 node)
        {
            float x = Mathf.Abs(node.x);
            float y = Mathf.Abs(node.y);
            float z = Mathf.Abs(node.z);

            if (x > y & x > z)        // x is the biggest
                return 0;
            if (y > x & y > z)        // y is the biggest
                return 1;

            // z is the biggest
            return 2;
        }

        void ConfigureHandJoints(RagdollJointCapsule arm, RagdollJointCapsule elbow, RagdollJointSphere hand, bool leftHand, bool createTips)
		{
			var dirUpper = elbow.Transform.position - arm.Transform.position;
			var dirLower = hand.Transform.position - elbow.Transform.position;
			var dirHand = hand.Transform.forward;

            //todo
            if (GetLongestTransform(hand.Transform) != null)
            {
                dirHand = GetLongestTransform(hand.Transform).position - hand.Transform.position;
                if (dirHand.magnitude == 0)
                {
                    dirHand = hand.Transform.forward;
                }
            }

			if (leftHand)
			{
				dirUpper = -dirUpper;
				dirLower = -dirLower;
				dirHand = -dirHand;
			}

			var upU = Vector3.Cross(_playerDirection, dirUpper);
			var upL = Vector3.Cross(_playerDirection, dirLower);
			var upH = Vector3.Cross(_playerDirection, dirHand);
			ConfigureJointParams(arm, _chest.Rigidbody, upU, _playerDirection);
			ConfigureJointParams(elbow, arm.Rigidbody, upL, _playerDirection);
			if (createTips)
			{
				ConfigureJointParams(hand, elbow.Rigidbody, upH, _playerDirection);
			}
		}

		void ConfigureLegsJoints(RagdollJointCapsule hip, RagdollJointCapsule knee, RagdollJointSphere foot, bool createTips)
		{
			var hipJoint = hip.Joint;
			var kneeJoint = knee.Joint;
			var footJoint = foot.Joint;

			ConfigureJointParams(hip, _pelvis.Rigidbody, _rootNode.right, _rootNode.forward);
			ConfigureJointParams(knee, hip.Rigidbody, _rootNode.right, _rootNode.forward);

			if (createTips)
			{
				ConfigureJointParams(foot, knee.Rigidbody, _rootNode.right, _rootNode.forward);
			}
		}

		static void AddComponentsTo(RagdollJointBox part, RagdollProperties ragdollProperties, float mass, bool addJoint)
		{
			AddComponentsToBase(part, ragdollProperties, mass, addJoint);
			GameObject go = part.Transform.gameObject;

			part.Collider = GetCollider<BoxCollider>(go.transform);
			if (part.Collider == null)
				part.Collider = go.AddComponent<BoxCollider>();
			part.Collider.isTrigger = ragdollProperties.AsTrigger;
		}

		static void AddComponentsTo(RagdollJointCapsule part, RagdollProperties ragdollProperties, float mass, bool addJoint)
		{
			AddComponentsToBase(part, ragdollProperties, mass, addJoint);
			GameObject go = part.Transform.gameObject;

			part.Collider = GetCollider<CapsuleCollider>(go.transform);
			if (part.Collider == null)
				part.Collider = go.AddComponent<CapsuleCollider>();
			part.Collider.isTrigger = ragdollProperties.AsTrigger;
		}

		static void AddComponentsTo(RagdollJointSphere part, RagdollProperties ragdollProperties, float mass, bool addJoint)
		{
			AddComponentsToBase(part, ragdollProperties, mass, addJoint);
			GameObject go = part.Transform.gameObject;

			part.Collider = GetCollider<SphereCollider>(go.transform);
			if (part.Collider == null)
				part.Collider = go.AddComponent<SphereCollider>();
			part.Collider.isTrigger = ragdollProperties.AsTrigger;
		}

		static void AddComponentsToBase(RagdollJoint part, RagdollProperties ragdollProperties, float mass, bool addJoint)
		{
			GameObject go = part.Transform.gameObject;

			part.Rigidbody = go.GetComponent<Rigidbody>();
			if (part.Rigidbody == null)
				part.Rigidbody = go.AddComponent<Rigidbody>();
			part.Rigidbody.mass = mass;
			part.Rigidbody.linearDamping = ragdollProperties.RigidDrag;
			part.Rigidbody.angularDamping = ragdollProperties.RigidAngularDrag;
			part.Rigidbody.collisionDetectionMode = ragdollProperties.CdMode;
			part.Rigidbody.isKinematic = ragdollProperties.IsKinematic;
			part.Rigidbody.useGravity = ragdollProperties.UseGravity;

			if (addJoint)
			{
				part.Joint = go.GetComponent<ConfigurableJoint>();
				if (part.Joint == null)
					part.Joint = go.AddComponent<ConfigurableJoint>();

				part.Joint.enablePreprocessing = false;

                //Lock joint motion in all axes.
                part.Joint.xMotion = ConfigurableJointMotion.Locked;
                part.Joint.yMotion = ConfigurableJointMotion.Locked;
                part.Joint.zMotion = ConfigurableJointMotion.Locked;
                
            }
		}

		static T GetCollider<T>(Transform transform)
			where T : Collider
		{
			for (int i = 0; i < transform.childCount; ++i)
			{
				Transform child = transform.GetChild(i);

				if (child.name.EndsWith(ColliderNodeSuffix))
				{
					transform = child;
					break;
				}
			}

			return transform.GetComponent<T>();
		}

		/// <summary>
		/// Remove all colliders, joints, and rigids
		/// </summary>
		public void ClearRagdoll()
		{
			foreach (var component in _pelvis.Transform.GetComponentsInChildren<Collider>())
				GameObject.DestroyImmediate(component);
			foreach (var component in _pelvis.Transform.GetComponentsInChildren<ConfigurableJoint>())
				GameObject.DestroyImmediate(component);
			foreach (var component in _pelvis.Transform.GetComponentsInChildren<Rigidbody>())
				GameObject.DestroyImmediate(component);

			DeleteColliderNodes(_pelvis.Transform);
		}
		/// <summary>
		/// Correct deleting collider with collider's separate nodes
		/// </summary>
		/// <param name="node"></param>
		private static void DeleteColliderNodes(Transform node)
		{
			for (int i = 0; i < node.childCount; ++i)
			{
				Transform child = node.GetChild(i);

				if (child.name.EndsWith(ColliderNodeSuffix))
					GameObject.DestroyImmediate(child.gameObject);
				else
					DeleteColliderNodes(child);
			}
		}
	}
}