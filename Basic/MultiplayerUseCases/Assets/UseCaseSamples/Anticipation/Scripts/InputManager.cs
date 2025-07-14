using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Unity.Netcode.Samples.MultiplayerUseCases.Anticipation
{
    /// <summary>
    /// Stores current and historical player input and allows that input to be queried.
    /// Could be optimized to cache the input and re-read at the start of each frame instead of reading
    /// when GetInput() is called. If this were used in more than one place, that would be necessary,
    /// as it would otherwise result in multiple inputs being pushed into the history each frame.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        NetworkManager NetworkManager => NetworkManager.Singleton;
        FrameHistory<InputList> m_HistoricalInput = new FrameHistory<InputList>();
        InputList m_LastInput;
        InputAction m_RandomTeleportAction;
        InputAction m_SmallRandomTeleportAction;
        InputAction m_PredictableTeleportAction;

        Vector2 m_Move;
        public void OnMove(InputValue value)
        {
            m_Move = value.Get<Vector2>();
        }

        void Start()
        {
            m_RandomTeleportAction = InputSystem.actions.FindAction("RandomTeleport");
            m_SmallRandomTeleportAction = InputSystem.actions.FindAction("SmallRandomTeleport");
            m_PredictableTeleportAction = InputSystem.actions.FindAction("PredictableTeleport");
        }

        /// <summary>
        /// Retrieve input for the current frame.
        /// </summary>
        /// <returns></returns>
        public InputList GetInput()
        {
            if (!NetworkManager.IsListening)
            {
                return 0;
            }

            InputList input = 0;
            if (m_Move.y > 0)
            {
                input |= InputList.Up;
            }
            if (m_Move.x < 0)
            {
                input |= InputList.Left;
            }
            if (m_Move.y < 0)
            {
                input |= InputList.Down;
            }
            if (m_Move.x > 0)
            {
                input |= InputList.Right;
            }
            if (m_RandomTeleportAction.IsPressed())
            {
                input |= InputList.RandomTeleport;
            }
            if (m_SmallRandomTeleportAction.IsPressed())
            {
                input |= InputList.SmallRandomTeleport;
            }
            if (m_PredictableTeleportAction.IsPressed())
            {
                input |= InputList.PredictableTeleport;
            }

            // To simulate checks for GetKeyDown while in FixedUpdate:
            // We store the unaltered input each frame. Then we alter the current frame's input
            // so that if these buttons were pressed, we remove them from the input we are going to
            // return and add to the history. That ensure no two input frames in a row contain these inputs,
            // while still letting us do the input polling within FixedUpdate.
            var lastInput = m_LastInput;
            m_LastInput = input;

            if ((lastInput & InputList.RandomTeleport) != 0)
            {
                input &= ~InputList.RandomTeleport;
            }
            if ((lastInput & InputList.SmallRandomTeleport) != 0)
            {
                input &= ~InputList.SmallRandomTeleport;
            }
            if ((lastInput & InputList.PredictableTeleport) != 0)
            {
                input &= ~InputList.PredictableTeleport;
            }

            m_HistoricalInput.Add(NetworkManager.LocalTime.Time, input);

            return input;
        }

        /// <summary>
        /// Remove historical input before the given time
        /// </summary>
        /// <param name="time"></param>
        public void RemoveBefore(double time)
        {
            m_HistoricalInput.RemoveBefore(time);
        }

        /// <summary>
        /// Retrieves historical inputs
        /// </summary>
        /// <returns></returns>
        public List<FrameHistory<InputList>.ItemFrameData> GetHistory()
        {
            return m_HistoricalInput.GetHistory();
        }

        /// <summary>
        /// Remove all items from the history
        /// </summary>
        public void Clear()
        {
            m_HistoricalInput.Clear();
        }
    }
}
