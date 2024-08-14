using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Unity.Multiplayer.Samples.SocialHub.Input
{
    class AvatarInputs : MonoBehaviour
    {
        [Header("Character Input Values")]
        [SerializeField]
        internal Vector2 Move;
        [SerializeField]
        internal Vector2 Look;
        [SerializeField]
        internal bool Jump;
        [SerializeField]
        internal bool Sprint;

        [Header("Movement Settings")]
        [SerializeField]
        internal bool AnalogMovement;

        [Header("Mouse Cursor Settings")]
        [SerializeField]
        internal bool CursorLocked = true;
        [SerializeField]
        internal bool CursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
        void OnMove(InputValue value)
        {
            MoveInput(value.Get<Vector2>());
        }

        void OnLook(InputValue value)
        {
            if (CursorInputForLook)
            {
                LookInput(value.Get<Vector2>());
            }
        }

        void OnJump(InputValue value)
        {
            JumpInput(value.isPressed);
        }

        void OnSprint(InputValue value)
        {
            SprintInput(value.isPressed);
        }
#endif

        void MoveInput(Vector2 newMoveDirection)
        {
            Move = newMoveDirection;
        }

        void LookInput(Vector2 newLookDirection)
        {
            Look = newLookDirection;
        }

        void JumpInput(bool newJumpState)
        {
            Jump = newJumpState;
        }

        void SprintInput(bool newSprintState)
        {
            Sprint = newSprintState;
        }

        void OnApplicationFocus(bool hasFocus)
        {
            SetCursorState(CursorLocked);
        }

        void SetCursorState(bool newState)
        {
            Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
        }
    }
}
