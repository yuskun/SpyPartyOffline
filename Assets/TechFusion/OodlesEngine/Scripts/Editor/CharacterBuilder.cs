using UnityEngine;
using UnityEditor;
using System.Collections;
using OodlesEngine;

namespace OodlesEngine.Editor
{
    class CharacterBuilder : ScriptableWizard
    {
        public Transform pelvis;

        public Transform leftHips = null;
        public Transform leftKnee = null;
        public Transform leftFoot = null;

        public Transform rightHips = null;
        public Transform rightKnee = null;
        public Transform rightFoot = null;

        public Transform leftArm = null;
        public Transform leftElbow = null;
        public Transform leftHand = null;

        public Transform rightArm = null;
        public Transform rightElbow = null;
        public Transform rightHand = null;

        public Transform middleSpine = null;
        public Transform head = null;


        public float totalMass = 20;
        public float strength = 0.0F;


        Vector3 right = Vector3.right;
        Vector3 up = Vector3.up;
        Vector3 forward = Vector3.forward;

        RagdollProcessor ragdollPro;

        void OnDrawGizmos()
        {
            if (pelvis)
            {
                Gizmos.color = Color.red; Gizmos.DrawRay(pelvis.position, pelvis.TransformDirection(right));
                Gizmos.color = Color.green; Gizmos.DrawRay(pelvis.position, pelvis.TransformDirection(up));
                Gizmos.color = Color.blue; Gizmos.DrawRay(pelvis.position, pelvis.TransformDirection(forward));
            }
        }

        static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<CharacterBuilder>("Create Ragdoll");
        }

        public Vector3 GetFrontDirection()
        {
            Vector3 leftKneeDir = leftKnee.position - pelvis.position;
            Vector3 rightKneeDir = rightKnee.position - pelvis.position;

            return Vector3.Cross(leftKneeDir, rightKneeDir).normalized;
        }

        bool ApplyOodlesCharacterByBone(OodlesCharacter pc, int id, Animator ragdoll, Animator anim, HumanBodyBones bone)
        {
            if (ragdoll.GetBoneTransform(bone) == null)
                return false;

            GameObject ragdollBoneObj = ragdoll.GetBoneTransform(bone).gameObject;

            ConfigurableJoint cj = null;
            cj = ragdollBoneObj.GetComponent<ConfigurableJoint>();
            if (cj == null)
            {
                Debug.Log("No CJ in " + ragdollBoneObj.name);
            }
            pc.joints[id] = cj;

            JointMatch am = ragdollBoneObj.AddComponent<JointMatch>();
            am.thisJoint = cj;
            am.animationTarget = anim.GetBoneTransform(bone);

            if (bone == HumanBodyBones.LeftHand)
            {
                HandFunction hf = ragdollBoneObj.AddComponent<HandFunction>();
                pc.handFunctionLeft = hf;
                hf.oodlesCharacter = pc;
                hf.handSide = HandSide.HandLeft;
                EditorUtility.SetDirty(hf);
            }
            else if (bone == HumanBodyBones.RightHand)
            {
                HandFunction hf = ragdollBoneObj.AddComponent<HandFunction>();
                pc.handFunctionRight = hf;
                hf.oodlesCharacter = pc;
                hf.handSide = HandSide.HandRight;
                EditorUtility.SetDirty(hf);
            }

            return true;
        }

        public OodlesCharacter OnCreateOodlesCharacter()
        {
            GameObject staticAnim = InitialiseCharacterObject();

            GameObject activeRagdoll = staticAnim.transform.root.gameObject;
            GameObject ragdoll = (RecursiveFindChild(activeRagdoll.transform, "Ragdoll")).gameObject;

            Animator ragdollAnimator = ragdoll.GetComponent<Animator>();
            Animator staticAnimator = staticAnim.GetComponent<Animator>();

            ragdollPro = new RagdollProcessor(ragdoll, () => GetFrontDirection());
            ragdollPro.CreateRagdoll();

            OodlesCharacter pc = pelvis.transform.root.gameObject.AddComponent<OodlesCharacter>();  //Joint Match class added to root object

            //physics and animator root
            pc.ragdollPlayer = ragdoll;
            pc.animatorPlayer = staticAnimator;

            ConfigurableJoint rootCJ = pc.ragdollPlayer.AddComponent<ConfigurableJoint>();
            rootCJ.xMotion = ConfigurableJointMotion.Free;
            rootCJ.zMotion = ConfigurableJointMotion.Free;
            rootCJ.yMotion = ConfigurableJointMotion.Free;
            rootCJ.angularXMotion = ConfigurableJointMotion.Locked;
            rootCJ.angularYMotion = ConfigurableJointMotion.Free;
            rootCJ.angularZMotion = ConfigurableJointMotion.Locked;

            Rigidbody rootRB = pc.ragdollPlayer.GetComponent<Rigidbody>();
            if (rootRB == null) rootRB = pc.ragdollPlayer.AddComponent<Rigidbody>();
            rootRB.mass = 10;

            CapsuleCollider rootCC = pc.ragdollPlayer.AddComponent<CapsuleCollider>();
            rootCC.center = new Vector3(0, 0.55f, 0);
            rootCC.radius = 0.3f;
            rootCC.height = 1.2f;
            rootCC.direction = 1;

            pc.joints = new ConfigurableJoint[(int)OodlesCharacter.BodyPart.BP_MAX];

            //pelvis
            pc.joints[0] = pelvis.gameObject.AddComponent<ConfigurableJoint>();
            pc.joints[0].xMotion = ConfigurableJointMotion.Locked;
            pc.joints[0].zMotion = ConfigurableJointMotion.Locked;
            pc.joints[0].yMotion = ConfigurableJointMotion.Locked;
            pc.joints[0].angularXMotion = ConfigurableJointMotion.Limited;
            pc.joints[0].angularYMotion = ConfigurableJointMotion.Limited;
            pc.joints[0].angularZMotion = ConfigurableJointMotion.Limited;
            SoftJointLimit tmplmt = pc.joints[0].linearLimit;
            tmplmt.limit = 0;
            pc.joints[0].linearLimit = tmplmt;

            //set linear limit
            SoftJointLimit cjLinearLimit = pc.joints[0].linearLimit;
            cjLinearLimit.limit = 0.0001f;
            pc.joints[0].linearLimit = cjLinearLimit;
            SoftJointLimitSpring lmtSpring = pc.joints[0].linearLimitSpring;
            lmtSpring.spring = 4200;
            lmtSpring.damper = 28.0799f;
            pc.joints[0].linearLimitSpring = lmtSpring;
            pc.joints[0].connectedBody = rootRB;

            int offset = 0; //not use
            ApplyOodlesCharacterByBone
                (pc, offset + (int)OodlesCharacter.BodyPart.BP_Pelvis, ragdollAnimator, staticAnimator, HumanBodyBones.Hips);
            ApplyOodlesCharacterByBone
                (pc, offset + (int)OodlesCharacter.BodyPart.BP_UpLegLeft, ragdollAnimator, staticAnimator, HumanBodyBones.LeftUpperLeg);
            ApplyOodlesCharacterByBone
                (pc, offset + (int)OodlesCharacter.BodyPart.BP_LegLeft, ragdollAnimator, staticAnimator, HumanBodyBones.LeftLowerLeg);
            ApplyOodlesCharacterByBone
                (pc, offset + (int)OodlesCharacter.BodyPart.BP_FootLeft, ragdollAnimator, staticAnimator, HumanBodyBones.LeftFoot);
            ApplyOodlesCharacterByBone
                (pc, offset + (int)OodlesCharacter.BodyPart.BP_UpLegRight, ragdollAnimator, staticAnimator, HumanBodyBones.RightUpperLeg);
            ApplyOodlesCharacterByBone
                (pc, offset + (int)OodlesCharacter.BodyPart.BP_LegRight, ragdollAnimator, staticAnimator, HumanBodyBones.RightLowerLeg);
            ApplyOodlesCharacterByBone
                (pc, offset + (int)OodlesCharacter.BodyPart.BP_FootRight, ragdollAnimator, staticAnimator, HumanBodyBones.RightFoot);

            bool findChest = ApplyOodlesCharacterByBone
                (pc, offset + (int)OodlesCharacter.BodyPart.BP_Spine, ragdollAnimator, staticAnimator, HumanBodyBones.Chest);

            if (!findChest)
            {
                ApplyOodlesCharacterByBone
                (pc, offset + (int)OodlesCharacter.BodyPart.BP_Spine, ragdollAnimator, staticAnimator, HumanBodyBones.Spine);
            }

            ApplyOodlesCharacterByBone
                (pc, offset + (int)OodlesCharacter.BodyPart.BP_UpArmLeft, ragdollAnimator, staticAnimator, HumanBodyBones.LeftUpperArm);
            ApplyOodlesCharacterByBone
                (pc, offset + (int)OodlesCharacter.BodyPart.BP_ArmLeft, ragdollAnimator, staticAnimator, HumanBodyBones.LeftLowerArm);
            ApplyOodlesCharacterByBone
                (pc, offset + (int)OodlesCharacter.BodyPart.BP_HandLeft, ragdollAnimator, staticAnimator, HumanBodyBones.LeftHand);
            ApplyOodlesCharacterByBone
                (pc, offset + (int)OodlesCharacter.BodyPart.BP_Head, ragdollAnimator, staticAnimator, HumanBodyBones.Head);
            ApplyOodlesCharacterByBone
                (pc, offset + (int)OodlesCharacter.BodyPart.BP_UpArmRight, ragdollAnimator, staticAnimator, HumanBodyBones.RightUpperArm);
            ApplyOodlesCharacterByBone
                (pc, offset + (int)OodlesCharacter.BodyPart.BP_ArmRight, ragdollAnimator, staticAnimator, HumanBodyBones.RightLowerArm);
            ApplyOodlesCharacterByBone
                (pc, offset + (int)OodlesCharacter.BodyPart.BP_HandRight, ragdollAnimator, staticAnimator, HumanBodyBones.RightHand);

            if (ragdollAnimator != null) { DestroyImmediate(ragdollAnimator); }

            foreach (SkinnedMeshRenderer smr in staticAnim.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                DestroyImmediate(smr.gameObject);
            }

            foreach (Renderer r in staticAnim.GetComponentsInChildren<Renderer>())
            {
                r.enabled = false;
            }

            return pc;
        }

        Transform RecursiveFindChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                {
                    return child;
                }
                else
                {
                    Transform found = RecursiveFindChild(child, childName);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            return null;
        }

        GameObject InitialiseCharacterObject()
        {
            //Store PhysicsBody to variable.
            Transform PhysicsBody = pelvis.root;
            PhysicsBody.name = "Ragdoll";

            //Create empty GameObject to serve as root, containing the PhysicsBody and StaticAnimator
            GameObject ActiveRagdollRoot = new GameObject();
            ActiveRagdollRoot.transform.position = PhysicsBody.position;
            ActiveRagdollRoot.transform.rotation = PhysicsBody.rotation;
            ActiveRagdollRoot.name = "Oodles Physics Character";
            //Create copy of character model root obj, to serve as StaticAnim.
            GameObject staticAnimator = Instantiate(PhysicsBody.gameObject, pelvis.root.position, Quaternion.identity);
            staticAnimator.name = "Animator";
            //TO DO: Give custom transparent material to static anim meshes.

            Animator anim;
            //If StaticAnimator doesn't have an animator component, add one.
            anim = staticAnimator.GetComponent<Animator>();
            if (anim == null)
            {
                anim = staticAnimator.GetComponentInChildren<Animator>();
            }
            if (anim == null) { anim = staticAnimator.AddComponent<Animator>(); }

            //Set animator variables
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            anim.applyRootMotion = true;
            anim.updateMode = AnimatorUpdateMode.Normal;

            //Set ActiveRagdoll parent/child heirachry.
            PhysicsBody.parent = ActiveRagdollRoot.transform;
            staticAnimator.transform.parent = ActiveRagdollRoot.transform;

            return staticAnimator;
        }
    }
}

