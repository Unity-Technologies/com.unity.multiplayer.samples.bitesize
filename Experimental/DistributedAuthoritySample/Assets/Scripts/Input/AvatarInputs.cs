using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

namespace Unity.Multiplayer.Samples.SocialHub.Input
{
    class AvatarInputs : MonoBehaviour
    {
        [SerializeField]
        InputActionReference m_InteractActionReference;

        [Header("Character Input Values")]
        [SerializeField]
        internal Vector2 Move;
        [SerializeField]
        internal Vector2 Look;
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

        internal event Action Jumped;
        internal event Action InteractTapped;
        internal event Action<double> InteractHeld;

        // tracking when a Hold interaction has started/ended
        bool m_HoldingInteractionPerformed;

        void Start()
        {
            if (m_InteractActionReference == null)
            {
                Debug.LogWarning("Assign Interact InputActionReference to this MonoBehaviour!", this);
                return;
            }

            m_InteractActionReference.action.performed += OnInteractPerformed;
            m_InteractActionReference.action.canceled += OnInteractCanceled;
            m_InteractActionReference.action.Enable();
        }

        void OnDestroy()
        {
            if (m_InteractActionReference != null)
            {
                m_InteractActionReference.action.performed -= OnInteractPerformed;
                m_InteractActionReference.action.canceled -= OnInteractCanceled;
                m_InteractActionReference.action.Disable();
            }
        }

        void OnInteractPerformed(InputAction.CallbackContext context)
        {
            switch (context.interaction)
            {
                case HoldInteraction:
                    m_HoldingInteractionPerformed = true;
                    break;
                case TapInteraction:
                    InteractTapped?.Invoke();
                    break;
            }
        }

        void OnInteractCanceled(InputAction.CallbackContext context)
        {
            if (context.interaction is HoldInteraction)
            {
                if (m_HoldingInteractionPerformed)
                {
                    InteractHeld?.Invoke(context.duration);
                }
                m_HoldingInteractionPerformed = false;
            }
        }

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
            Jumped?.Invoke();
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
