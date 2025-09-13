using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OodlesEngine
{ 
    public partial class OodlesCharacter : MonoBehaviour
    {
        public struct DriveState
        {
            public float positionSpring;
            public float positionDamper;

            static public void ApplyDrive(ConfigurableJoint joint, DriveState state)
            {
                JointDrive jDrivex = joint.angularXDrive;
                JointDrive jDriveyz = joint.angularYZDrive;
                jDrivex.positionSpring = state.positionSpring;
                jDriveyz.positionSpring = state.positionSpring;
                jDrivex.positionDamper = state.positionDamper;
                jDriveyz.positionDamper = state.positionDamper;
                joint.angularXDrive = jDrivex;
                joint.angularYZDrive = jDriveyz;
            }
        }

        DriveState DriveOff, DriveLow, DriveMedium, DriveHigh, DriveControl, DriveFix;

        void InitJointDrives()
        {
            DriveOff = new DriveState();
            DriveOff.positionSpring = 40;
            DriveOff.positionDamper = 10;

            DriveLow = new DriveState();
            DriveLow.positionSpring = 80;//200
            DriveLow.positionDamper = 1;

            DriveMedium = new DriveState();
            DriveMedium.positionSpring = 400;//400
            DriveMedium.positionDamper = 1;

            DriveHigh = new DriveState();
            DriveHigh.positionSpring = 800;//80
            DriveHigh.positionDamper = 1;

            DriveControl = new DriveState();
            DriveControl.positionSpring = 1000;//2000
            DriveControl.positionDamper = 1;

            DriveFix = new DriveState();
            DriveFix.positionSpring = 200;//10000
            DriveFix.positionDamper = 5;
        }
    }
}