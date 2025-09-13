using UnityEngine;

namespace OodlesEngine
{ 
    public class LostControlState : State<OodlesCharacter>
    {
        public override void EnterState(OodlesCharacter _owner)
        {
            //Debug.Log("Entering ControlState State");
            _owner.JointLoseBalanceState();
        }

        public override void ExitState(OodlesCharacter _owner)
        {
            //Debug.Log("Exiting ControlState State");
        }

        public override void UpdateState(OodlesCharacter _owner)
        {
            _owner.UpdateStandUp();
        }
    }
}