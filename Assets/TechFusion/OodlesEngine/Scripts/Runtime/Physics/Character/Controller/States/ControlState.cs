using UnityEngine;

namespace OodlesEngine
{ 
    public class ControlState : State<OodlesCharacter>
    {
        public override void EnterState(OodlesCharacter _owner)
        {
            _owner.JointIdleState();
        }

        public override void ExitState(OodlesCharacter _owner)
        {
            //Debug.Log("Exiting ControlState State");
        }

        public override void UpdateState(OodlesCharacter _owner)
        {
            _owner.UpdatePickUp();
            _owner.UpdateThrow();
            _owner.UpdateEnergy();
            _owner.UpdateMovement();
            _owner.UpdateAnimations();
            _owner.SyncAnimator();
            _owner.UpdateIK();
            _owner.UpdateAttack();
            //_owner.UpdateAnimator();
            _owner.UpdateHandFunction();

            _owner.UpdateJointState();
        }
    }
}