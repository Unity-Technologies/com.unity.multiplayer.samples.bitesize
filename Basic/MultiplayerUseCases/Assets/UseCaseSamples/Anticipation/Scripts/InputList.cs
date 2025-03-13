using System;

namespace DefaultNamespace
{
    /// <summary>
    /// Simple enum representing the player inputs that are used by this little demo.
    /// Quick and efficient to send over the network when no analog inputs are needed (all inputs are binary on or off)
    /// </summary>
    [Flags]
    public enum InputList
    {
        Up = 1 << 0,
        Left = 1 << 1,
        Down = 1 << 2,
        Right = 1 << 3,
        RandomTeleport = 1 << 4,
        SmallRandomTeleport = 1 << 5,
        PredictableTeleport = 1 << 6,
    }
}
