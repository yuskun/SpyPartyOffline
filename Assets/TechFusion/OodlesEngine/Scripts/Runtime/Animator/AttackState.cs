using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OodlesEngine
{
    public class AttackState : StateMachineBehaviour
    {

        //OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.SetLayerWeight(layerIndex, 1);

            OodlesCharacter character = animator.GetComponentInParent<OodlesCharacter>();
            if (character != null)
            {
                character.isAttacking = true;
                if(character.HoldWeapon() == true)
                {// 在本地玩家身上發送同步音效
                NetworkPlayer.Local.RPC_PlayGlobalSFX(CharacterSFXManager.SFXType.Attack,NetworkPlayer.Local.PlayerId);
                //CharacterSFXManager.Instance?.PlayAttack();
                Debug.LogWarning("播放攻擊音效");
                }
                else
                {
                NetworkPlayer.Local.RPC_PlayGlobalSFX(CharacterSFXManager.SFXType.Punch,NetworkPlayer.Local.PlayerId);
                //CharacterSFXManager.Instance?.PlayPunch();
                Debug.LogWarning("播放拳頭音效");
                }
            }
        }

        // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
        //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        //{
        //    
        //}

        // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.ResetTrigger("Attack");
            animator.SetLayerWeight(layerIndex, 0);

            OodlesCharacter character = animator.GetComponentInParent<OodlesCharacter>();
            if (character != null)
            {
                character.OnAttackFinish();
                character.isAttacking = false;
            }
        }

        // OnStateMove is called right after Animator.OnAnimatorMove()
        //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        //{
        //    // Implement code that processes and affects root motion
        //}

        // OnStateIK is called right after Animator.OnAnimatorIK()
        //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        //{
        //    // Implement code that sets up animation IK (inverse kinematics)
        //}
    }
}