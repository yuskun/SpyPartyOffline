using Fusion;
using UnityEngine;

namespace OodlesEngine
{
    public struct OodlesCharacterInput : INetworkInput
    {
        public float DeltaTime
        {
            get { return deltaTime; }
            set { deltaTime = value; }
        }
            
        public uint Tick => tick;
        
        public float deltaTime;
        public uint tick;

        public float forwardAxis;
        public float leftAxis;
        public float jumpAxis;
        public float mouseYAxis;
        public float fire1Axis;
        public float fire2Axis;
        public float doAction1;
        public Vector3 cameraForward;

        public OodlesCharacterInput(float forwardAxis,
        float leftAxis,
        float jumpAxis,
        float mouseYAxis,
        float fire1Axis,
        float fire2Axis,
        float doAction1,
        Vector3 cameraForward,
        float deltaTime, uint tick)
        {
            this.forwardAxis = forwardAxis;
            this.leftAxis = leftAxis;
            this.jumpAxis = jumpAxis;
            this.mouseYAxis = mouseYAxis;
            this.fire1Axis = fire1Axis;
            this.fire2Axis = fire2Axis;
            this.doAction1 = doAction1;
            this.cameraForward = cameraForward;

            this.deltaTime = deltaTime;
            this.tick = tick;
        }

        public void Reset()
        {
            this.forwardAxis = 0;
            this.leftAxis = 0;
            this.jumpAxis = 0;
            this.mouseYAxis = 0;
            this.fire1Axis = 0;
            this.fire2Axis = 0;
            this.doAction1 = 0;
            this.cameraForward = Vector3.forward;

            this.deltaTime = 0;
            this.tick = 0;
        }
    }
}