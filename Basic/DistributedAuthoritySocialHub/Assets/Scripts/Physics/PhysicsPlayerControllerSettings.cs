using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Physics
{
    [CreateAssetMenu(fileName = "PhysicsPlayerControllerSettings", menuName = "ScriptableObjects/PhysicsPlayerControllerSettings", order = 1)]
    class PhysicsPlayerControllerSettings : ScriptableObject
    {
        [SerializeField]
        internal float WalkSpeed;
        [SerializeField]
        internal float SprintSpeed;
        [SerializeField]
        internal float Acceleration;
        [SerializeField]
        internal float DragCoefficient;
        [SerializeField]
        internal float AirControlFactor;
        [SerializeField]
        internal float JumpImpusle;
        [SerializeField]
        internal float CustomGravityMultiplier;
        [SerializeField]
        internal float RotationSpeed;
        [SerializeField]
        internal float GroundCheckDistance;
    }
}
