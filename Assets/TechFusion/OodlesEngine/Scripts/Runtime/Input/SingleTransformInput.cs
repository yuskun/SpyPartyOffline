using UnityEngine;

namespace OodlesEngine
{
    public struct SingleTransformInput : IInput
    {
        public float DeltaTime
        {
            get { return deltaTime; }
            set { deltaTime = value; }
        }

        public uint Tick => tick;
        
        public float deltaTime;
        public uint tick;
        
        public SingleTransformInput(float deltaTime, uint tick)
        {
            this.deltaTime = deltaTime;
            this.tick = tick;
        }

        public override string ToString()
        {
            return $"DT: {deltaTime.ToString()} | Tick: {tick.ToString()}";
        }
    }
}