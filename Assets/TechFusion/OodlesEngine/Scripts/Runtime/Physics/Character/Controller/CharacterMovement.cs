using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

namespace OodlesEngine
{
    public class CharacterMovement : MonoBehaviour
    {
        //Assignables

        private float deltaTime;
        public Transform playerCam;
        public Transform orientation;
        public OodlesCharacter controller;

        //Other
        private Rigidbody rb;

        //Rotation and Look
        private float xRotation;

        //Movement
        public float moveSpeed = 80000;
        public float sprintScale = 1;
        public float maxSpeed = 4;
        public bool grounded;
        public LayerMask whatIsGround;
        private float footstepTimer;

        public float counterMovement = 2;//0.175f;
        private float threshold = 0.01f;
        public float maxSlopeAngle = 45f;

        [Header("空中控制 (Air Control)")]
        [Range(0f, 1f)]
        public float airControlStrength = 0.3f;  // 空中移動力度 (0 = 無法操控, 1 = 與地面相同)
        public float airMaxSpeed = 4f;            // 空中最大水平速度上限


        //Crouch and Slide
        private Vector3 crouchScale = new Vector3(1, 0.5f, 1);
        private Vector3 playerScale;
        public float slideForce = 500;
        public float slideCounterMovement = 0.2f;


        //Jumping
        private bool readyToJump = true;
        private float jumpCooldown = 1.25f;
        public float jumpForce = 800000f;

        // 卡住偵測（高度不變 + 不在地面 → 允許跳躍脫困）
        private float stuckCheckTimer = 0f;
        private float lastHeightY;
        private const float StuckHeightThreshold = 0.05f; // 高度變化小於此視為沒動
        private const float StuckTimeRequired = 1.5f;     // 持續多久判定卡住（秒）
        private bool isStuck = false;

        //Input
        private float x, y;
        private bool jumping, sprinting, crouching;

        //Sliding
        private Vector3 normalVector = Vector3.up;
        private Vector3 wallNormalVector;


        //Grab rigid body on awake
        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        //After awake(grabbing rigidbody)
        private void Start()
        {
            //Grab player's current scale
            playerScale = transform.localScale;

            GameObject OriObj = new GameObject("Orientation");
            OriObj.transform.parent = transform;
            OriObj.transform.localPosition = Vector3.zero;
            OriObj.transform.localRotation = Quaternion.identity;
            orientation = OriObj.transform;
            if (NetworkManager.instance != null)
            {
                deltaTime = NetworkManager.instance._runner.DeltaTime;
            }
            else
            {
                deltaTime = Time.fixedDeltaTime;
            }
        }

        public void ProcessInput()
        {
            MoveInput();
            MoveRotate();
            Movement();
        }

        private void MoveInput()
        {
            //Get X movement. 0 = not moving 1 = moving
            x = controller.inputState.leftAxis;
            //Get Y movement. 0 = not moving 1 = moving
            y = controller.inputState.forwardAxis;

            //Get Jump Button;
            jumping = controller.inputState.jumpAxis == 1;
            
        }



        private void Jump()
        {
            //If player is ground (or stuck) and ready to jump
            if ((grounded || isStuck) && readyToJump)
            {
                readyToJump = false;

                // 卡住脫困跳：重置狀態
                if (isStuck)
                {
                    isStuck = false;
                    stuckCheckTimer = 0f;
                }

                //Add the jump forces
                rb.AddForce(Vector2.up * jumpForce * 1.5f);
                rb.AddForce(normalVector * jumpForce * 0.5f);

                //If Jumping while falling, reset y velocity
                Vector3 vel = rb.linearVelocity;
                if (rb.linearVelocity.y < 0.5f)
                {
                    rb.linearVelocity = new Vector3(vel.x, 0, vel.z);
                }
                else if (rb.linearVelocity.y > 0)
                    rb.linearVelocity = new Vector3(vel.x, vel.y / 2, vel.z);

                //Invoke ResetJump with the jumpCooldown
                Invoke(nameof(ResetJump), jumpCooldown);
            }
        }

        private void ResetJump()
        {
            readyToJump = true;
        }

        private float desiredX;

        private void MoveRotate()
        {
            var lookPos = controller.inputState.cameraForward;
            lookPos.y = 0;

            if (x != 0 || y != 0)
            {
                Vector3 localDir = Vector3.right * x + Vector3.forward * y;
                localDir.y = 0;
                Vector3 worldDir = Quaternion.LookRotation(lookPos) * localDir;
                GetComponent<ConfigurableJoint>().targetRotation = Quaternion.Inverse(Quaternion.LookRotation(worldDir));
                //transform.LookAt(transform.position + worldDir);
            }

            orientation.transform.rotation = Quaternion.LookRotation(lookPos);
        }

        private void CounterMovement(float x, float y, Vector2 mag)
        {
            if (!grounded || jumping) return;

            //Slow down sliding
            if (crouching)
            {
                rb.AddForce(moveSpeed * deltaTime * -rb.linearVelocity.normalized * slideCounterMovement);
                return;
            }

            //Counter movement
            if (Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) ||
                (mag.x > threshold && x < 0))
            {
                rb.AddForce(moveSpeed * orientation.transform.right * deltaTime * -mag.x * counterMovement);
            }
            if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
            {
                rb.AddForce(moveSpeed * orientation.transform.forward * deltaTime * -mag.y * counterMovement);
            }

            //Limit diagonal running
            if (Mathf.Sqrt((Mathf.Pow(rb.linearVelocity.x, 2) + Mathf.Pow(rb.linearVelocity.z, 2))) > maxSpeed)
            {
                float fallspeed = rb.linearVelocity.y;
                Vector3 n = rb.linearVelocity.normalized * maxSpeed;
                rb.linearVelocity = new Vector3(n.x, fallspeed, n.z);
            }
        }

        /// Find the velocity relative to where the player is looking
        /// Useful for vectors calculations regarding movement an dlmiting movement
        public Vector2 FindVelRelativeToLook()
        {
            float lookAngle = orientation.transform.eulerAngles.y;
            float moveAngle = Mathf.Atan2(rb.linearVelocity.x, rb.linearVelocity.z) * Mathf.Rad2Deg;


            float u = Mathf.DeltaAngle(lookAngle, moveAngle);
            float v = 98 - u;

            float magnitude = rb.linearVelocity.magnitude;
            float yMag = magnitude * Mathf.Cos(u * Mathf.Deg2Rad);
            float xMag = magnitude * Mathf.Cos(v * Mathf.Deg2Rad);

            return new Vector2(xMag, yMag);
        }


        private bool IsFloor(Vector3 v)
        {
            float angle = Vector3.Angle(Vector3.up, v);
            return angle < maxSlopeAngle;
        }

        private void Movement()
        {
            if (!grounded)
                rb.AddForce(Vector3.down * (deltaTime * 10));

            Vector2 mag = FindVelRelativeToLook();
            CounterMovement(x, y, mag);

            if (readyToJump && jumping) Jump();

            // 沿地面方向計算推力
            Vector3 moveDir = (orientation.forward * y + orientation.right * x).normalized;
            moveDir = Vector3.ProjectOnPlane(moveDir, normalVector);

            if (grounded)
            {
                // ✅ 地面：全推力
                rb.AddForce(moveDir * (moveSpeed * sprintScale) * deltaTime);
            }
            else
            {
                // ✅ 空中 Air Control：允許用較小推力影響方向
                Vector3 desiredDir = (orientation.forward * y + orientation.right * x).normalized;

                if (desiredDir != Vector3.zero)
                {
                    // 以 airControlStrength 比例施力（0~1）
                    rb.AddForce(desiredDir * (moveSpeed * sprintScale * airControlStrength) * deltaTime);

                    // 空中水平速度不能超過 airMaxSpeed（避免越跳越快）
                    Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                    if (horizontalVel.magnitude > airMaxSpeed)
                    {
                        Vector3 clamped = horizontalVel.normalized * airMaxSpeed;
                        rb.linearVelocity = new Vector3(clamped.x, rb.linearVelocity.y, clamped.z);
                    }
                }
            }
        }


        private void Update()
        {
            CapsuleCollider cc = GetComponent<CapsuleCollider>();
            if (cc == null) return;

            float bias = 0.2f;
            Vector3 capsuleCenter = transform.position + Vector3.up * cc.center.y;
            float rayLength = cc.height * 0.5f + bias;
            float radius = cc.radius * 0.9f;
            Vector3 origin = capsuleCenter;
            if (Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, rayLength, whatIsGround))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);

                if (slopeAngle <= maxSlopeAngle)
                {
                    grounded = true;
                    normalVector = hit.normal;
                }
                else
                {
                    grounded = false;
                    normalVector = Vector3.up;
                }
            }
            else
            {
                grounded = false;
                normalVector = Vector3.up;
            }

            // ── 卡住偵測：不在地面 + 高度幾乎不變 → 允許跳躍脫困 ──
            if (!grounded)
            {
                float heightDiff = Mathf.Abs(transform.position.y - lastHeightY);
                if (heightDiff < StuckHeightThreshold)
                {
                    stuckCheckTimer += Time.deltaTime;
                    if (stuckCheckTimer >= StuckTimeRequired)
                    {
                        isStuck = true;
                    }
                }
                else
                {
                    stuckCheckTimer = 0f;
                    isStuck = false;
                }
            }
            else
            {
                stuckCheckTimer = 0f;
                isStuck = false;
            }
            lastHeightY = transform.position.y;
        }
    }
}