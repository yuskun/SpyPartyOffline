using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;

namespace OodlesEngine
{
    public class EnemyAI : MonoBehaviour
    {
        private OodlesCharacter characterController;
        private OodlesCharacterInput aiInput;

        [SerializeField]
        private BehaviorTree enemyBrain;

        public bool alive = false;
        private EnemyAI target;

        private float lastJumpTime = 0f;
        private float jumpCooldown = 2f;

        private bool isRunningAway = false;
        private float runAwayTimer = 0f;
        private float runAwayDuration = 2.5f;

        private void Awake()
        {
            aiInput = new OodlesCharacterInput(0, 0, 0, 0, 0, 0, 0, Vector3.forward, 0, 0);

            enemyBrain = new BehaviorTreeBuilder(gameObject)
                .Selector("AI Brain")
                    .Sequence("Casual")
                        .Condition("No Target Found", () => { return FindNearestEnemy() == null; })
                        .Do("Casual Movement", () =>
                        {
                            Casual();
                            return TaskStatus.Success;
                        })
                        .WaitTime("Wait", 2)
                    .End()

                    .Selector("Combat")
                        .Sequence("Try Attack")
                            .Condition("Find Target", () => { return FindNearestEnemy() != null; })
                            .Condition("Check Attack Range", () => { return InAttackRange(); })
                            .Do("Attack", () =>
                            {
                                Attack();
                                StartRunAway(); // 打到就準備逃跑
                                return TaskStatus.Success;
                            })
                        .End()
                        .Sequence("Move To Target")
                            .Condition("Find Target", () => { return FindNearestEnemy() != null; })
                            .Do("Close To Target", () =>
                            {
                                if (InAttackRange())
                                    return TaskStatus.Success;

                                MoveToTarget();
                                return TaskStatus.Continue;
                            })
                        .End()
                    .End()
                .End()
                .Build();
        }

        private void Start()
        {
            characterController = GetComponent<OodlesCharacter>();
            characterController.AllowAttack = true;
        }

        private void Update()
        {
            if (!alive) return;

            aiInput.Reset();
            aiInput.deltaTime = Time.deltaTime;

            // --- 執行行為樹 ---
            enemyBrain.Tick();

            // --- 若在逃跑狀態，覆蓋當前輸入 ---
            if (isRunningAway)
            {
                RunAway();
            }
            else
            {
                MaybeJump();
            }

            // --- 最後才輸出輸入 ---
            characterController.ProcessInput(aiInput);
        }

        public void SetAlive(bool b) => alive = b;

        public Vector3 GetPosition() => characterController.GetCharacterPosition();

        private EnemyAI FindNearestEnemy()
        {
            EnemyAI[] all = FindObjectsOfType<EnemyAI>();
            EnemyAI closest = null;
            float minDist = Mathf.Infinity;

            foreach (var e in all)
            {
                if (e == this) continue;
                if (!e.alive) continue;

                float dist = Vector3.Distance(GetPosition(), e.GetPosition());
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = e;
                }
            }

            target = closest;
            return target;
        }

        private void Casual()
        {
            aiInput.forwardAxis = Mathf.Sin(Time.time * 0.5f);
            aiInput.leftAxis = Mathf.Cos(Time.time * 0.3f);
        }

        private bool InAttackRange()
        {
            if (target == null) return false;
            float dist = Vector3.Distance(GetPosition(), target.GetPosition());
            return dist < 1.2f;
        }

        private void Attack()
        {
            aiInput.fire1Axis = 1;
        }

        private void MoveToTarget()
        {
            if (target == null) return;

            Vector3 myPos = GetPosition();
            Vector3 targetPos = target.GetPosition();
            Vector3 moveDir = (targetPos - myPos).normalized;

            aiInput.cameraForward = moveDir;
            aiInput.forwardAxis = 1;
            aiInput.leftAxis = 0;
        }

        private void MaybeJump()
        {
            if (Time.time - lastJumpTime > jumpCooldown)
            {
                if (Random.value < 0.05f)
                {
                    aiInput.jumpAxis = 1;
                    lastJumpTime = Time.time;
                    jumpCooldown = Random.Range(2f, 5f);
                }
            }
        }

        // =========================
        // 🏃 逃跑邏輯
        // =========================
        private void StartRunAway()
        {
            if (isRunningAway) return;
            StartCoroutine(RunAwayDelay());
        }

        private IEnumerator RunAwayDelay()
        {
            yield return new WaitForSeconds(Random.Range(1f, 2f)); // 等1~2秒後觸發
            isRunningAway = true;
            runAwayTimer = 0f;
        }

        private void RunAway()
        {
            if (target == null)
            {
                isRunningAway = false;
                return;
            }

            runAwayTimer += Time.deltaTime;
            if (runAwayTimer >= runAwayDuration)
            {
                isRunningAway = false;
                return;
            }

            Vector3 myPos = GetPosition();
            Vector3 targetPos = target.GetPosition();
            Vector3 awayDir = (myPos - targetPos).normalized;

            aiInput.cameraForward = awayDir;
            aiInput.forwardAxis = 1;
            aiInput.leftAxis = 0;

            // 逃跑中偶爾跳一下
            if (Random.value < 0.02f)
            {
                aiInput.jumpAxis = 1;
            }
        }
    }
}
