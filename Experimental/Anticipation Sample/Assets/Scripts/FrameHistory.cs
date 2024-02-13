using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    /// <summary>
    /// Simple container to store historical data associated with a frame for the sake of replaying that data.
    /// Main use case in this demo is storing historical input for the player character so we can replay those inputs
    /// when we need to re-anticipate a new location based on updated server data.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FrameHistory<T>
    {
        public struct ItemFrameData
        {
            public double Time;
            public T Item;
            public float DeltaTime;
        }
        private List<ItemFrameData> m_History = new List<ItemFrameData>();

        /// <summary>
        /// Add a value to the history for the current frame.
        /// This generally expects that items will be added to the history in the order that they occur
        /// (i.e., each call to this has a time with a greater value than the previous). Nothing enforces this
        /// expectation, but failure to follow it could result in things being replayed out of order later, as
        /// there is no sorting done within this class.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="value"></param>
        public void Add(double time, T value)
        {
            m_History.Add(new ItemFrameData{Time = time, Item = value, DeltaTime = Time.deltaTime});
        }

        /// <summary>
        /// Remove all items before a given time. Useful to keep memory usage from growing by discarding old data
        /// that you know you won't need anymore.
        /// </summary>
        /// <param name="time"></param>
        public void RemoveBefore(double time)
        {
            m_History.RemoveAll(item => item.Time < time);
        }

        /// <summary>
        /// Remove all items after a given time. Useful if the data you are storing here needs to be replaced
        /// as part of a re-anticipation action (i.e., if you are storing some historical position data and want
        /// to recalculate everything after a given time)
        /// </summary>
        /// <param name="time"></param>
        public void RemoveAfter(double time)
        {
            m_History.RemoveAll(item => item.Time > time);
        }

        /// <summary>
        /// Get the full history, useful for iterating through all the values to reapply them when reanticipating.
        /// </summary>
        /// <returns></returns>
        public List<ItemFrameData> GetHistory()
        {
            return m_History;
        }
    }
}
