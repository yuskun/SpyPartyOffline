namespace OodlesEngine
{
    public interface IInput
    {
        /// <summary>
        /// The amount of time the input was recorded for 
        /// </summary>
        float DeltaTime { get; set; }

        /// <summary>
        /// The tick for 
        /// </summary>network
        uint Tick { get; }
    }
}