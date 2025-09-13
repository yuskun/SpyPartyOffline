using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;
using UnityEngine.Windows;

namespace OodlesEngine
{
    public class EnemyAI : MonoBehaviour
    {
        OodlesCharacter characterController;
        OodlesCharacterInput aiInput;

        [SerializeField]
        private BehaviorTree enemyBrain;

        public bool alive = false;

        private void Awake()
        {
            aiInput = new OodlesCharacterInput(0, 0, 0, 0, 0, 0, 0, Vector3.forward, 0, 0);

            enemyBrain = new BehaviorTreeBuilder(gameObject)
                .Selector("AI Brain")
                    .Sequence("Casual")
                        .Condition("Player Lost Control", () => { return !IsPlayerStanding(); })
                        .Do("Casual Movement", () =>
                        {
                            Casual();

                            return TaskStatus.Success;
                        })
                        .WaitTime("Wait", 2)
                        .End()
                    .Selector("Combat")
                        .Sequence("Try Attack")
                            .Condition("Find Player", () => { return LocalPlayer.Instance != null; })
                            .Condition("Check Attack Range", () => { return InAttackRange(); })
                            .Do("Attack", () =>
                            {
                                Attack();

                                return TaskStatus.Success;
                            })
                            .End()
                        .Sequence("Move To Player")
                            .Condition("Find Player", () => { return LocalPlayer.Instance != null; })
                            .Do("Close To Player", () =>
                            {
                                if (InAttackRange())
                                {
                                    return TaskStatus.Success;
                                }

                                MoveToPlayer();

                                return TaskStatus.Failure;
                            })
                        .End()
                .Build();
        }

        // Start is called before the first frame update
        void Start()
        {
            characterController = GetComponent<OodlesCharacter>();
        }

        // Update is called once per frame
        void Update()
        {
            if (!alive) return;

            aiInput.Reset();
            aiInput.deltaTime = Time.deltaTime;

            enemyBrain.Tick();

            characterController.ProcessInput(aiInput);
        }

        public void SetAlive(bool b)
        {
            alive = b;
        }

        public Vector3 GetPosition()
        {
            return characterController.GetCharacterPosition();
        }

        bool IsPlayerStanding()
        {
            if (LocalPlayer.Instance.GetState() == OodlesCharacter.State.Control)
            {
                return true;
            }

            return false;
        }

        void Casual()
        {
            //move around
            aiInput.jumpAxis = 1;
        }

        bool InAttackRange()
        {
            Vector3 playerPosition = LocalPlayer.Instance.GetPosition();
            Vector3 myPosition = GetPosition();

            if (Vector3.Distance(playerPosition, myPosition) < 1.2f)
            {
                return true;
            }

            return false;
        }

        void Attack()
        {
            aiInput.fire1Axis = 1;
        }

        void MoveToPlayer()
        {
            Vector3 playerPosition = LocalPlayer.Instance.GetPosition();
            Vector3 myPosition = GetPosition();

            Vector3 moveDir = playerPosition - myPosition;
            moveDir.Normalize();

            Vector3 lookDir = aiInput.cameraForward;

            float cos = Vector3.Dot(lookDir, moveDir);
            if (cos > 0)
            {
                aiInput.forwardAxis = 1;
            }
            else if (cos < 0)
            {
                aiInput.forwardAxis = -1;
            }
            else
            {
                aiInput.forwardAxis = 0;
            }

            Vector3 cross = Vector3.Cross(lookDir, moveDir);
            if (cross.y > 0)
            {
                aiInput.leftAxis = 1;
            }
            else if(cross.y < 0)
            {
                aiInput.leftAxis = -1;
            }
            else
            {
                aiInput.leftAxis = 0;
            }
        }
    }
}
