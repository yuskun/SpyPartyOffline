using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OodlesEngine
{ 
    public partial class OodlesCharacter : MonoBehaviour
    {
        DriveState PrevArmDrive;
        bool LeftArmOnUse = false, RightArmOnUse = false;

        public void JointMoveState()
        {
            SetJointDriveOnGroup(BodyPartGroup.BPG_Head, DriveMedium);
            SetJointDriveOnGroup(BodyPartGroup.BPG_ArmLeft, DriveControl);//low
            SetJointDriveOnGroup(BodyPartGroup.BPG_ArmRight, DriveControl);//low

            SetJointDriveOnGroup(BodyPartGroup.BPG_Pelvis, DriveHigh);
            SetJointDriveOnGroup(BodyPartGroup.BPG_Spine, DriveHigh);//fix

            SetJointDriveOnGroup(BodyPartGroup.BPG_LegLeft, DriveMedium);//fix
            SetJointDriveOnGroup(BodyPartGroup.BPG_LegRight, DriveMedium);//fix

            ControlRootJoint(true);

            PrevArmDrive = DriveLow;
        }

        public void JointInAirState()
        {
            //SetJointDriveOnGroup(BodyPartGroup.BPG_Head, DriveLow);
            //SetJointDriveOnGroup(BodyPartGroup.BPG_Spine, DriveLow);
            SetJointDriveOnGroup(BodyPartGroup.BPG_ArmLeft, DriveLow);
            SetJointDriveOnGroup(BodyPartGroup.BPG_ArmRight, DriveLow);

            SetJointDriveOnGroup(BodyPartGroup.BPG_LegLeft, DriveMedium);
            SetJointDriveOnGroup(BodyPartGroup.BPG_LegRight, DriveMedium);

            SetJointDriveOnGroup(BodyPartGroup.BPG_Head, DriveHigh);
            SetJointDriveOnGroup(BodyPartGroup.BPG_Spine, DriveHigh);
            SetJointDriveOnGroup(BodyPartGroup.BPG_Pelvis, DriveHigh);

            //physicsBodyJoint.slerpDrive = DriveControl;
            //DriveState.ApplyDrive(physicsBodyJoint, DriveControl);
            ControlRootJoint(true);

            PrevArmDrive = DriveLow;
        }

        public void JointDizzyState()
        {
            //SetJointDriveOnGroup(BodyPartGroup.BPG_Head, DriveLow);
            //SetJointDriveOnGroup(BodyPartGroup.BPG_Spine, DriveLow);
            SetJointDriveOnGroup(BodyPartGroup.BPG_ArmLeft, DriveLow);
            SetJointDriveOnGroup(BodyPartGroup.BPG_ArmRight, DriveLow);
        
            SetJointDriveOnGroup(BodyPartGroup.BPG_Head, DriveLow);
            SetJointDriveOnGroup(BodyPartGroup.BPG_Spine, DriveLow);

            SetJointDriveOnGroup(BodyPartGroup.BPG_LegLeft, DriveLow);
            SetJointDriveOnGroup(BodyPartGroup.BPG_LegRight, DriveLow);
            SetJointDriveOnGroup(BodyPartGroup.BPG_Pelvis, DriveLow);

            //physicsBodyJoint.slerpDrive = DriveControl;
            //DriveState.ApplyDrive(physicsBodyJoint, DriveOff);
            ControlRootJoint(false);

            PrevArmDrive = DriveLow;
        }

        public void JointIdleState()
        {
            SetJointDriveOnGroup(BodyPartGroup.BPG_Head, DriveLow);
            SetJointDriveOnGroup(BodyPartGroup.BPG_Spine, DriveHigh);
            SetJointDriveOnGroup(BodyPartGroup.BPG_LegLeft, DriveHigh);//low
            SetJointDriveOnGroup(BodyPartGroup.BPG_LegRight, DriveHigh);//low
            SetJointDriveOnGroup(BodyPartGroup.BPG_Pelvis, DriveHigh);

            SetJointDriveOnGroup(BodyPartGroup.BPG_ArmLeft, DriveLow);
            SetJointDriveOnGroup(BodyPartGroup.BPG_ArmRight, DriveLow);

            //DriveState.ApplyDrive(physicsBodyJoint, DriveControl);
            ControlRootJoint(true);

            PrevArmDrive = DriveLow;
        }

        public void JointPickState()
        {
            SetJointDriveOnGroup(BodyPartGroup.BPG_Head, DriveLow);
            SetJointDriveOnGroup(BodyPartGroup.BPG_Spine, DriveLow);
            SetJointDriveOnGroup(BodyPartGroup.BPG_LegLeft, DriveHigh);
            SetJointDriveOnGroup(BodyPartGroup.BPG_LegRight, DriveHigh);
            SetJointDriveOnGroup(BodyPartGroup.BPG_Pelvis, DriveLow);

            SetJointDriveOnGroup(BodyPartGroup.BPG_ArmLeft, DriveLow);
            SetJointDriveOnGroup(BodyPartGroup.BPG_ArmRight, DriveLow);

            //DriveState.ApplyDrive(physicsBodyJoint, DriveHigh);
            ControlRootJoint(true);

            PrevArmDrive = DriveLow;
        }

        public void JointLoseBalanceState()
        {
            SetJointDriveOnGroup(BodyPartGroup.BPG_Head, DriveLow);
            SetJointDriveOnGroup(BodyPartGroup.BPG_Spine, DriveLow);
            SetJointDriveOnGroup(BodyPartGroup.BPG_ArmLeft, DriveLow);
            SetJointDriveOnGroup(BodyPartGroup.BPG_ArmRight, DriveLow);
            SetJointDriveOnGroup(BodyPartGroup.BPG_LegLeft, DriveLow);
            SetJointDriveOnGroup(BodyPartGroup.BPG_LegRight, DriveLow);
            SetJointDriveOnGroup(BodyPartGroup.BPG_Pelvis, DriveLow);

            //DriveState.ApplyDrive(physicsBodyJoint, DriveOff);
            ControlRootJoint(false);

            //not reasonable
            PrevArmDrive = DriveOff;
        }

        public void JointActionState()
        {
            SetJointDriveOnGroup(BodyPartGroup.BPG_Head, DriveMedium);
            SetJointDriveOnGroup(BodyPartGroup.BPG_ArmLeft, DriveControl);
            SetJointDriveOnGroup(BodyPartGroup.BPG_ArmRight, DriveControl);

            SetJointDriveOnGroup(BodyPartGroup.BPG_Spine, DriveHigh);
            SetJointDriveOnGroup(BodyPartGroup.BPG_Pelvis, DriveHigh);

            SetJointDriveOnGroup(BodyPartGroup.BPG_LegLeft, DriveMedium);
            SetJointDriveOnGroup(BodyPartGroup.BPG_LegRight, DriveMedium);

            //DriveState.ApplyDrive(physicsBodyJoint, DriveControl);
            ControlRootJoint(true);
        }

        public void JointUseLeftArm()
        {
            if (LeftArmOnUse) return;

            //SetJointDriveOnGroup(BodyPartGroup.BPG_ArmLeft, DriveControl);
        }

        public void JointUnUseLeftArm()
        {
            if (!LeftArmOnUse) return;

            //SetJointDriveOnGroup(BodyPartGroup.BPG_ArmLeft, PrevArmDrive);
        }

        public void JointUseRightArm()
        {
            if (RightArmOnUse) return;

            //SetJointDriveOnGroup(BodyPartGroup.BPG_ArmRight, DriveControl);
        }

        public void JointUnUseRightArm()
        {
            if (!RightArmOnUse) return;

            //SetJointDriveOnGroup(BodyPartGroup.BPG_ArmRight, PrevArmDrive);
        }

        private int rootJointControlState = 0; //0:control 1:pending control 2:uncontrol

        private void ControlRootJoint(bool control)
        {
            if (control)
            {
                if (rootJointControlState == 0)
                {
                    DriveState.ApplyDrive(physicsBodyJoint, DriveHigh);
                }
                else if (rootJointControlState == 2)
                {
                    DriveState.ApplyDrive(physicsBodyJoint, DriveOff);
                }

                rootJointControlState = 0;

                physicsBodyJoint.angularXMotion = ConfigurableJointMotion.Limited;
                physicsBodyJoint.angularZMotion = ConfigurableJointMotion.Limited;
            }
            else
            {
                rootJointControlState = 2;

                DriveState.ApplyDrive(physicsBodyJoint, DriveOff);
                physicsBodyJoint.angularXMotion = ConfigurableJointMotion.Free;
                physicsBodyJoint.angularZMotion = ConfigurableJointMotion.Free;
            }

            //if (control)
            //{
            //    //DriveState.ApplyDrive(physicsBodyJoint, DriveHigh);
            //    //physicsBodyJoint.angularXMotion = ConfigurableJointMotion.Locked;
            //    //physicsBodyJoint.angularZMotion = ConfigurableJointMotion.Locked;

            //    if (rootJointControlState == 0) return;
            //    else if (rootJointControlState == 1) return;
            //    else
            //    {
            //        //StopCoroutine(ProgressControlRootJoint());
            //        StartCoroutine(ProgressControlRootJoint());
            //    }
            //}
            //else
            //{
            //    //StopCoroutine(ProgressControlRootJoint());
            //    if (rootJointControlState == 0)
            //    {
            //    }
            //    else if (rootJointControlState == 1)
            //    {
            //        StopCoroutine(ProgressControlRootJoint());
            //    }
            //    else return;

            //    rootJointControlState = 2;
            //    DriveState.ApplyDrive(physicsBodyJoint, DriveOff);
            //    physicsBodyJoint.angularXMotion = ConfigurableJointMotion.Free;
            //    physicsBodyJoint.angularZMotion = ConfigurableJointMotion.Free;
            //}
        }

        IEnumerator ProgressControlRootJoint()
        {
            physicsBodyJoint.angularXMotion = ConfigurableJointMotion.Locked;
            physicsBodyJoint.angularZMotion = ConfigurableJointMotion.Locked;
            rootJointControlState = 1;

            yield return new WaitForSeconds(1.0f);
            DriveState.ApplyDrive(physicsBodyJoint, DriveLow);

            yield return new WaitForSeconds(1.0f);
            DriveState.ApplyDrive(physicsBodyJoint, DriveMedium);

            yield return new WaitForSeconds(1.0f);
            DriveState.ApplyDrive(physicsBodyJoint, DriveHigh);
            rootJointControlState = 0;
        }
    }
}