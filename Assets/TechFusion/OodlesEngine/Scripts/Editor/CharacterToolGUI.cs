using UnityEngine;
using UnityEditor;

namespace OodlesEngine.Editor
    { 
    public class CharacterToolGUI : EditorWindow
    {
        GameObject charModelObj;
        Animator charAnim;
        Avatar charRig;
        OodlesCharacter oodlesCharacter;

        float handRadius = 0.2f;
        float footRadius = 0.1f;
        float headRadius = 0.2f;
        float upperArmRadius = 0.2f;
        float lowerArmRadius = 0.2f;
        float upperLegRadius = 0.1f;
        float lowerLegRadius = 0.1f;
        Vector3 middleSpineSize = Vector3.one * 0.5f;
        Vector3 pelvisSize = Vector3.one * 0.3f;

        float characterCapsuleRadius = 0.5f;
        float characterCapsuleHeight = 1.5f;
        float characterCapsuleYOffset = 0.75f;

        [MenuItem("Window/OodlesCharacter")]
        public static void ShowWindow()
        {
            GetWindow<CharacterToolGUI>("OodlesEngine Character");
        }

        void OnGUI()
        {
            charModelObj = (GameObject)EditorGUILayout.ObjectField("Character Model:", Selection.activeGameObject, typeof(GameObject), true);

            if (charModelObj) { oodlesCharacter = charModelObj.GetComponent<OodlesCharacter>(); }
            else { oodlesCharacter = null; }
            if (charModelObj) { charAnim = charModelObj.GetComponent<Animator>(); }
            else { charAnim = null; }
            if (charModelObj) { charRig = GetRig(charModelObj); }
            else { charRig = null; }

            if (oodlesCharacter == null)
            {
                OnGUICreate();
            }
            else
            {
                OnGUIUpdateCollider();
            }
        }

        private void OnGUIInput()
        {
            GUILayout.Space(10);
            GUILayout.Label("Joint Collider", EditorStyles.boldLabel);
            GUILayout.Space(4);

            headRadius = EditorGUILayout.Slider("Head Radius", headRadius, 0.0001f, 3f);
            upperArmRadius = EditorGUILayout.Slider("Upper Arm Radius", upperArmRadius, 0.0001f, 3f);
            lowerArmRadius = EditorGUILayout.Slider("Lower Arm Radius", lowerArmRadius, 0.0001f, 3f);
            handRadius = EditorGUILayout.Slider("Hand Radius", handRadius, 0.0001f, 3f);
            upperLegRadius = EditorGUILayout.Slider("Upper Leg Radius", upperLegRadius, 0.0001f, 3f);
            lowerLegRadius = EditorGUILayout.Slider("Lower Leg Radius", lowerLegRadius, 0.0001f, 3f);
            footRadius = EditorGUILayout.Slider("Foot Radius", footRadius, 0.0001f, 3f);
            middleSpineSize = EditorGUILayout.Vector3Field("Middle Spine Size", middleSpineSize);
            pelvisSize = EditorGUILayout.Vector3Field("Pelvis Size", pelvisSize);

            GUILayout.Space(10);
            GUILayout.Label("Root Collider", EditorStyles.boldLabel);
            GUILayout.Space(4);
            characterCapsuleRadius = EditorGUILayout.Slider("Character Capsule Radius", characterCapsuleRadius, 0.0001f, 10f);
            characterCapsuleHeight = EditorGUILayout.Slider("Character Capsule Height", characterCapsuleHeight, 0.0001f, 10f);
            characterCapsuleYOffset = EditorGUILayout.Slider("Character Capsule YOffset", characterCapsuleYOffset, -10f, 10f);
            GUILayout.Space(4);
        }

        void OnGUICreate()
        {
            OnGUIInput();

            //Check to show GUI
            GUI.enabled = charRig != null;

            string errorString = "";
            if (GUILayout.Button("Create Character"))
            {
                CharacterBuilder rdBuilder = ScriptableObject.CreateInstance("CharacterBuilder") as CharacterBuilder;

                if (CheckValid(rdBuilder, ref errorString))
                {
                    oodlesCharacter = rdBuilder.OnCreateOodlesCharacter();

                    PostProcessPhysicsData();

                    Selection.activeGameObject = oodlesCharacter.gameObject;
                }
                else
                {
                    Debug.Log(errorString);
                }

            }
            GUI.enabled = true;

            string winmsg = charRig
                ? "Click the button to convert this character model into a Physics Character"
                : "Please select a character model with a humanoid rig from the hierarchy and click the button.";

            MessageType msgtype = charRig ? MessageType.Info : MessageType.Warning;

            GUILayout.FlexibleSpace();
            EditorGUILayout.HelpBox(winmsg, msgtype);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Character Builder For Oodles Engine", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        void OnGUIUpdateCollider()
        {
            if (oodlesCharacter == null) return;

            ReadPhysicsData();

            OnGUIInput();

            PostProcessPhysicsData();

            string msg = "Adjust the parameters of the collider size in Scene View.";

            MessageType msgtype = MessageType.Info;

            GUILayout.FlexibleSpace();
            EditorGUILayout.HelpBox(msg, msgtype);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Character Builder For Oodles Engine", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void ReadPhysicsData() 
        {
            if (oodlesCharacter == null) return;

            //pelvis, spine, hand, foot
            BoxCollider pelvisBox = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_Pelvis).GetComponent<BoxCollider>();
            if (pelvisBox != null)
            {
                pelvisSize = pelvisBox.size;
            }

            BoxCollider spineBox = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_Spine).GetComponent<BoxCollider>();
            if (spineBox != null)
            {
                middleSpineSize = spineBox.size;
            }

            SphereCollider handLeftSphere = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_HandLeft).GetComponent<SphereCollider>();
            if (handLeftSphere != null)
            {
                handRadius = handLeftSphere.radius;
            }

            SphereCollider footLeftSphere = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_FootLeft).GetComponent<SphereCollider>();
            if (footLeftSphere != null)
            {
                footRadius = footLeftSphere.radius;
            }

            //head
            SphereCollider headSphere = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_Head).GetComponent<SphereCollider>();
            if (headSphere != null)
            {
                headRadius = headSphere.radius;
            }

            //upper lower arms and legs
            CapsuleCollider upperArmLeftCapsule = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_UpArmLeft).GetComponent<CapsuleCollider>();
            if (upperArmLeftCapsule != null)
            {
                upperArmRadius = upperArmLeftCapsule.radius;
            }

            CapsuleCollider upperArmRightCapsule = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_UpArmRight).GetComponent<CapsuleCollider>();
            if (upperArmRightCapsule != null)
            {
                upperArmRadius = upperArmRightCapsule.radius;
            }

            CapsuleCollider lowerArmLeftCapsule = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_ArmLeft).GetComponent<CapsuleCollider>();
            if (lowerArmLeftCapsule != null)
            {
                lowerArmRadius = lowerArmLeftCapsule.radius;
            }

            CapsuleCollider lowerArmRightCapsule = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_ArmRight).GetComponent<CapsuleCollider>();
            if (lowerArmRightCapsule != null)
            {
                lowerArmRadius = lowerArmRightCapsule.radius;
            }

            CapsuleCollider upperLegLeftCapsule = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_UpLegLeft).GetComponent<CapsuleCollider>();
            if (upperLegLeftCapsule != null)
            {
                upperLegRadius = upperLegLeftCapsule.radius;
            }

            CapsuleCollider upperLegRightCapsule = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_UpLegRight).GetComponent<CapsuleCollider>();
            if (upperLegRightCapsule != null)
            {
                upperLegRadius = upperLegRightCapsule.radius;
            }

            CapsuleCollider lowerLegLeftCapsule = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_LegLeft).GetComponent<CapsuleCollider>();
            if (lowerLegLeftCapsule != null)
            {
                lowerLegRadius = lowerLegLeftCapsule.radius;
            }

            CapsuleCollider lowerLegRightCapsule = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_LegRight).GetComponent<CapsuleCollider>();
            if (lowerLegRightCapsule != null)
            {
                lowerLegRadius = lowerLegRightCapsule.radius;
            }

            CapsuleCollider capsuleCollider = oodlesCharacter.ragdollPlayer.GetComponent<CapsuleCollider>();
            if (capsuleCollider != null)
            {
                characterCapsuleHeight = capsuleCollider.height;
                characterCapsuleRadius = capsuleCollider.radius;
                characterCapsuleYOffset = capsuleCollider.center.y;
            }
        }

        private void PostProcessPhysicsData()
        {
            if (oodlesCharacter == null) return;

            //pelvis, spine, hand, foot
            BoxCollider pelvisBox = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_Pelvis).GetComponent<BoxCollider>();
            if (pelvisBox != null)
            {
                pelvisBox.size = pelvisSize;
            }

            BoxCollider spineBox = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_Spine).GetComponent<BoxCollider>();
            if (spineBox != null)
            {
                spineBox.size = middleSpineSize;
            }

            SphereCollider handLeftSphere = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_HandLeft).GetComponent<SphereCollider>();
            if (handLeftSphere != null)
            {
                handLeftSphere.radius = handRadius;
            }

            SphereCollider handRightSphere = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_HandRight).GetComponent<SphereCollider>();
            if (handRightSphere != null)
            {
                handRightSphere.radius = handRadius;
            }

            SphereCollider footLeftSphere = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_FootLeft).GetComponent<SphereCollider>();
            if (footLeftSphere != null)
            {
                footLeftSphere.radius = footRadius;
            }

            SphereCollider footRightSphere = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_FootRight).GetComponent<SphereCollider>();
            if (footRightSphere != null)
            {
                footRightSphere.radius = footRadius;
            }

            //head
            SphereCollider headSphere = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_Head).GetComponent<SphereCollider>();
            if (headSphere != null)
            {
                headSphere.radius = headRadius;
            }

            //upper lower arms and legs
            CapsuleCollider upperArmLeftCapsule = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_UpArmLeft).GetComponent<CapsuleCollider>();
            if (upperArmLeftCapsule != null)
            {
                upperArmLeftCapsule.radius = upperArmRadius;
            }

            CapsuleCollider upperArmRightCapsule = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_UpArmRight).GetComponent<CapsuleCollider>();
            if (upperArmRightCapsule != null)
            {
                upperArmRightCapsule.radius = upperArmRadius;
            }

            CapsuleCollider lowerArmLeftCapsule = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_ArmLeft).GetComponent<CapsuleCollider>();
            if (lowerArmLeftCapsule != null)
            {
                lowerArmLeftCapsule.radius = lowerArmRadius;
            }

            CapsuleCollider lowerArmRightCapsule = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_ArmRight).GetComponent<CapsuleCollider>();
            if (lowerArmRightCapsule != null)
            {
                lowerArmRightCapsule.radius = lowerArmRadius;
            }

            CapsuleCollider upperLegLeftCapsule = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_UpLegLeft).GetComponent<CapsuleCollider>();
            if (upperLegLeftCapsule != null)
            {
                upperLegLeftCapsule.radius = upperLegRadius;
            }

            CapsuleCollider upperLegRightCapsule = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_UpLegRight).GetComponent<CapsuleCollider>();
            if (upperLegRightCapsule != null)
            {
                upperLegRightCapsule.radius = upperLegRadius;
            }

            CapsuleCollider lowerLegLeftCapsule = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_LegLeft).GetComponent<CapsuleCollider>();
            if (lowerLegLeftCapsule != null)
            {
                lowerLegLeftCapsule.radius = lowerLegRadius;
            }

            CapsuleCollider lowerLegRightCapsule = oodlesCharacter.GetJoint(OodlesCharacter.BodyPart.BP_LegRight).GetComponent<CapsuleCollider>();
            if (lowerLegRightCapsule != null)
            {
                lowerLegRightCapsule.radius = lowerLegRadius;
            }

            CapsuleCollider capsuleCollider = oodlesCharacter.ragdollPlayer.GetComponent<CapsuleCollider>();
            if (capsuleCollider != null)
            {
                capsuleCollider.height = characterCapsuleHeight;
                capsuleCollider.radius = characterCapsuleRadius;
                Vector3 oldCenter = capsuleCollider.center;
                
                capsuleCollider.center = new Vector3(oldCenter.x, characterCapsuleYOffset, oldCenter.z);
            }
        }

        Avatar GetRig(GameObject characterModel)
        {
            Animator anim = characterModel.GetComponent<Animator>();
            return anim ? anim.avatar : characterModel.GetComponent<Avatar>();
        }

        bool CheckValid(CharacterBuilder rdBuilder, ref string errorString)
        {
            bool isValid = true;

            rdBuilder.pelvis = charAnim.GetBoneTransform(HumanBodyBones.Hips);
            rdBuilder.leftHips = charAnim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            rdBuilder.leftKnee = charAnim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            rdBuilder.leftFoot = charAnim.GetBoneTransform(HumanBodyBones.LeftFoot);
            rdBuilder.rightHips = charAnim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            rdBuilder.rightKnee = charAnim.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            rdBuilder.rightFoot = charAnim.GetBoneTransform(HumanBodyBones.RightFoot);
            rdBuilder.leftArm = charAnim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            rdBuilder.leftElbow = charAnim.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            rdBuilder.leftHand = charAnim.GetBoneTransform(HumanBodyBones.LeftHand);
            rdBuilder.rightArm = charAnim.GetBoneTransform(HumanBodyBones.RightUpperArm);
            rdBuilder.rightElbow = charAnim.GetBoneTransform(HumanBodyBones.RightLowerArm);
            rdBuilder.rightHand = charAnim.GetBoneTransform(HumanBodyBones.RightHand);
            rdBuilder.middleSpine = charAnim.GetBoneTransform(HumanBodyBones.Chest);
            if (rdBuilder.middleSpine == null)
                rdBuilder.middleSpine = charAnim.GetBoneTransform(HumanBodyBones.Spine);
            rdBuilder.head = charAnim.GetBoneTransform(HumanBodyBones.Head);

            errorString = "Null Bones:";
            if (rdBuilder.pelvis == null) { errorString += "[pelvis]"; isValid &= false; }
            if (rdBuilder.leftHips == null) { errorString += "[leftHips]"; isValid &= false; }
            if (rdBuilder.leftKnee == null) { errorString += "[leftKnee]"; isValid &= false; }
            if (rdBuilder.leftFoot == null) { errorString += "[leftFoot]"; isValid &= false; }
            if (rdBuilder.rightHips == null) { errorString += "[rightHips]"; isValid &= false; }
            if (rdBuilder.rightKnee == null) { errorString += "[rightKnee]"; isValid &= false; }
            if (rdBuilder.rightFoot == null) { errorString += "[rightFoot]"; isValid &= false; }
            if (rdBuilder.leftArm == null) { errorString += "[leftArm]"; isValid &= false; }
            if (rdBuilder.leftElbow == null) { errorString += "[leftElbow]"; isValid &= false; }
            if (rdBuilder.leftHand == null) { errorString += "[leftHand]"; isValid &= false; }
            if (rdBuilder.rightArm == null) { errorString += "[rightArm]"; isValid &= false; }
            if (rdBuilder.rightElbow == null) { errorString += "[rightElbow]"; isValid &= false; }
            if (rdBuilder.rightHand == null) { errorString += "[rightHand]"; isValid &= false; }
            if (rdBuilder.middleSpine == null) { errorString += "[middleSpine]"; isValid &= false; }
            if (rdBuilder.head == null) { errorString += "[head]"; isValid &= false; }

            return isValid;
        }
    }
}
