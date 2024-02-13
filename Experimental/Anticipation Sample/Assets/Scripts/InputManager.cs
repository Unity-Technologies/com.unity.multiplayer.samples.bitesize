using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace DefaultNamespace
{
    /// <summary>
    /// Stores current and historical player input and allows that input to be queried.
    /// Could be optimized to cache the input and re-read at the start of each frame instead of reading
    /// when GetInput() is called. If this were used in more than one place, that would be necessary,
    /// as it would otherwise result in multiple inputs being pushed into the history each frame.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        public NetworkManager NetworkManager;
        private FrameHistory<InputList> m_HistoricalInput = new FrameHistory<InputList>();

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
            if (Input.GetKey(KeyCode.W))
            {
                input |= InputList.Up;
            }
            if (Input.GetKey(KeyCode.A))
            {
                input |= InputList.Left;
            }
            if (Input.GetKey(KeyCode.S))
            {
                input |= InputList.Down;
            }
            if (Input.GetKey(KeyCode.D))
            {
                input |= InputList.Right;
            }
            if (Input.GetKeyDown(KeyCode.Q))
            {
                input |= InputList.RandomTeleport;
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                input |= InputList.SmallRandomTeleport;
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                input |= InputList.PredictableTeleport;
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
    }
}
