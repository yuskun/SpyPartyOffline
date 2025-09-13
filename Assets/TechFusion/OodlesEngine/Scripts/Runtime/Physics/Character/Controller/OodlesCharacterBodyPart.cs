using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OodlesEngine
{ 
    public partial class OodlesCharacter : MonoBehaviour
    {
        public enum BodyPart
        {
            BP_Pelvis = 0,
            BP_UpLegLeft = 1,
            BP_LegLeft = 2,
            BP_FootLeft = 3,
            BP_UpLegRight = 4,
            BP_LegRight = 5,
            BP_FootRight = 6,
            BP_Spine = 7,
            BP_UpArmLeft = 8,
            BP_ArmLeft = 9,
            BP_HandLeft = 10,
            BP_Head = 11,
            BP_UpArmRight = 12,
            BP_ArmRight = 13,
            BP_HandRight = 14,
            BP_MAX = 15,
        }

        public enum BodyPartGroup
        {
            BPG_Head,
            BPG_Spine,
            BPG_ArmLeft,
            BPG_ArmRight,
            BPG_Pelvis,
            BPG_LegLeft,
            BPG_LegRight,
        }

        void SetJointDriveOnGroup(BodyPartGroup g, DriveState jd)
        {
            switch(g)
            {
                case BodyPartGroup.BPG_Head:
                    DriveState.ApplyDrive(joints[(int)BodyPart.BP_Head], jd);
                    break;
                case BodyPartGroup.BPG_Spine:
                    DriveState.ApplyDrive(joints[(int)BodyPart.BP_Spine], jd);
                    break;
                case BodyPartGroup.BPG_Pelvis:
                    DriveState.ApplyDrive(joints[(int)BodyPart.BP_Pelvis], jd);
                    break;
                case BodyPartGroup.BPG_ArmLeft:
                    DriveState.ApplyDrive(joints[(int)BodyPart.BP_UpArmLeft], jd);
                    DriveState.ApplyDrive(joints[(int)BodyPart.BP_ArmLeft], jd);
                    DriveState.ApplyDrive(joints[(int)BodyPart.BP_HandLeft], jd);
                    break;
                case BodyPartGroup.BPG_ArmRight:
                    DriveState.ApplyDrive(joints[(int)BodyPart.BP_UpArmRight], jd);
                    DriveState.ApplyDrive(joints[(int)BodyPart.BP_ArmRight], jd);
                    DriveState.ApplyDrive(joints[(int)BodyPart.BP_HandRight], jd);
                    break;
                case BodyPartGroup.BPG_LegLeft:
                    DriveState.ApplyDrive(joints[(int)BodyPart.BP_UpLegLeft], jd);
                    DriveState.ApplyDrive(joints[(int)BodyPart.BP_LegLeft], jd);
                    DriveState.ApplyDrive(joints[(int)BodyPart.BP_FootLeft], jd);
                    break;
                case BodyPartGroup.BPG_LegRight:
                    DriveState.ApplyDrive(joints[(int)BodyPart.BP_UpLegRight], jd);
                    DriveState.ApplyDrive(joints[(int)BodyPart.BP_LegRight], jd);
                    DriveState.ApplyDrive(joints[(int)BodyPart.BP_FootRight], jd);
                    break;
            }
        }

        bool CheckBodyCollide(BodyPart part, LayerMask layerMask)
        {
            ConfigurableJoint joint = joints[(int)part];

            Collider c = joint.GetComponent<Collider>();

            if (c)
            {
                if (c.GetType() == typeof(SphereCollider))
                {
                    SphereCollider sc = (SphereCollider)c;
                    return Physics.CheckSphere(joint.transform.position, sc.radius * joint.transform.lossyScale.x + 0.1f, layerMask);
                }
            }

            return false;
        }

        bool CheckBodyOnGround(BodyPart part, LayerMask layerMask)
        {
            ConfigurableJoint joint = joints[(int)part];

            Collider c = joint.GetComponent<Collider>();

            if (c)
            {
                if (c.GetType() == typeof(SphereCollider))
                {
                    SphereCollider sc = (SphereCollider)c;
                    //return Physics.CheckSphere(joint.transform.position, sc.radius * joint.transform.lossyScale.x + 0.1f, layerMask);
                    Ray ray = new Ray(joint.transform.position, -Vector3.up);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, sc.radius * joint.transform.lossyScale.x + 0.2f, layerMask))
                    {
                        if (hit.rigidbody != null && 
                            (hit.rigidbody == LeftHandGrabObject() || hit.rigidbody == RightHandGrabObject()))
                        {
                            return false;
                        }

                        return true;
                    }
                }
                else if (c.GetType() == typeof(BoxCollider))
                {
                    BoxCollider bc = (BoxCollider)c;
                    //return Physics.CheckSphere(joint.transform.position, sc.radius * joint.transform.lossyScale.x + 0.1f, layerMask);
                    Ray ray = new Ray(joint.transform.position, -Vector3.up);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, bc.size.magnitude * 0.5f * joint.transform.lossyScale.x + 0.2f, layerMask))
                    {
                        if (hit.rigidbody != null &&
                            (hit.rigidbody == LeftHandGrabObject() || hit.rigidbody == RightHandGrabObject()))
                        {
                            return false;
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        Transform GetAnimatorBodyPart(BodyPart part)
        {
            ConfigurableJoint joint = joints[(int)part];

            JointMatch am = joint.GetComponent<JointMatch>();
            if (!am) return null;

            return am.animationTarget;
        }
    }
}