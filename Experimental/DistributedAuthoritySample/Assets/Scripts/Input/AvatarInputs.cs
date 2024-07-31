using UnityEngine;
using UnityEngine.InputSystem;

namespace com.unity.multiplayer.samples.distributed_authority.input
{
    class AvatarInputs : MonoBehaviour
    {
        [Header("Character Input Values")]
        [SerializeField]
        internal Vector2 move;
        [SerializeField]
        internal Vector2 look;
        [SerializeField]
        internal bool jump;
        [SerializeField]
        internal bool sprint;

        [Header("Movement Settings")]
        [SerializeField]
        internal bool analogMovement;

        [Header("Mouse Cursor Settings")]
        [SerializeField]
        internal bool cursorLocked = true;
        [SerializeField]
        internal bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
        void OnMove(InputValue value)
        {
            MoveInput(value.Get<Vector2>());
        }

        void OnLook(InputValue value)
        {
            if (cursorInputForLook)
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
            move = newMoveDirection;
        }

        void LookInput(Vector2 newLookDirection)
        {
            look = newLookDirection;
        }

        void JumpInput(bool newJumpState)
        {
            jump = newJumpState;
        }

        void SprintInput(bool newSprintState)
        {
            sprint = newSprintState;
        }

        void OnApplicationFocus(bool hasFocus)
        {
            SetCursorState(cursorLocked);
        }

        void SetCursorState(bool newState)
        {
            Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
        }
    }
}
