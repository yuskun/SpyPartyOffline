using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OodlesEngine
{ 
    public class CharacterMovement : MonoBehaviour
    {
        //Assignables
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

        public float counterMovement = 2;//0.175f;
        private float threshold = 0.01f;
        public float maxSlopeAngle = 45f;


        //Crouch and Slide
        private Vector3 crouchScale = new Vector3(1, 0.5f, 1);
        private Vector3 playerScale;
        public float slideForce = 500;
        public float slideCounterMovement = 0.2f;


        //Jumping
        private bool readyToJump = true;
        private float jumpCooldown = 1.25f;
        public float jumpForce = 800000f;


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

        private void Movement()
        {
            //Debug.Log("Time delta " + Time.deltaTime.ToString());
            //Extra gravity to make sure player isn't flying
            rb.AddForce(Vector3.down * (Time.deltaTime * 10));

            //Find the actual velocity relative to where player is lookinhg
            Vector2 mag = FindVelRelativeToLook();

            float xMag = mag.x, yMag = mag.y;


            //Counteract sliding and sloppy movement
            CounterMovement(x, y, mag);


            //if Holding jump && ready to jump, then jump
            if (readyToJump && jumping) Jump();

            //Set the max speed a player can go
            float maxSpeed = this.maxSpeed;

            //if sliding down a ramp, add force down so the player stays on the ground but can also build up speed;
            if (crouching && grounded && readyToJump)
            {
                //ADd a bunch of force downwards
                rb.AddForce(Vector3.down * Time.deltaTime * 3000);
                return;
            }


            //if speed is larger then maxspeed, cancel out the input so you don't go over max speed
            //If x > 0, and the magnitude of x is greater then the max speed, then set x to 0 so player can't surpass maxSpeed
            if (x > 0 && xMag > maxSpeed) x = 0;
            //If x < 0, which is negative speed and is LESS then the max speed, then set x to 0 so player can't go backward in time; 
            if (x < 0 && xMag < -maxSpeed) x = 0;

            //Same as x but, replace x with Y. This is based on jump velocity.
            if (y > 0 && yMag > maxSpeed) y = 0;
            if (y < 0 && yMag < -maxSpeed) y = 0;


            //Multipliers to slow down movement/increase movement control
            float multiplier = 1f, multiplierV = 1f;

            //Movement in Air
            if (!grounded)
            {
                multiplier = 0.5f;
                multiplierV = 0.5f;
            }

            //Movement while sliding
            if (grounded && crouching) multiplierV = 0f;


            //Apply all the forces generated to move player
            rb.AddForce(orientation.transform.forward * y * (moveSpeed * sprintScale) * Time.deltaTime * multiplier * multiplierV);
            rb.AddForce(orientation.transform.right * x * (moveSpeed * sprintScale) * Time.deltaTime * multiplier);
        }


        private void Jump()
        {
            //If player is ground and ready to jump
            if (grounded && readyToJump)
            {
                readyToJump = false;

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
                rb.AddForce(moveSpeed * Time.deltaTime * -rb.linearVelocity.normalized * slideCounterMovement);
                return;
            }

            //Counter movement
            if (Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) ||
                (mag.x > threshold && x < 0))
            {
                rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * -mag.x * counterMovement);
            }
            if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
            {
                rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * -mag.y * counterMovement);
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

        private void Update()
        {
            CapsuleCollider cc = GetComponent<CapsuleCollider>();
            float bias = 0.2f;
            if (cc != null)
            {
                Vector3 capsuleCenter = transform.position + Vector3.up * cc.center.y;

                Ray ray = new Ray(capsuleCenter, Vector3.down);

                float rayLength = cc.height * 0.5f + bias;

                if (Physics.Raycast(ray, out RaycastHit hit, rayLength, whatIsGround))
                {
                    grounded = true;
                }
                else
                {
                    grounded = false;
                }
            }
        }
    }
}