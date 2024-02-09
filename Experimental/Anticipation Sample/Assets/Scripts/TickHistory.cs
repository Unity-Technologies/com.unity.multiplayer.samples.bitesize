using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    /// <summary>
    /// Simple container to store historical data associated with a network tick for the sake of replaying that data.
    /// Main use case in this demo is storing historical input for the player character so we can replay those inputs
    /// when we need to re-anticipate a new location based on updated server data.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TickHistory<T>
    {
        public struct TickWithItem
        {
            public double Tick;
            public T Item;
            public float DeltaTime;
        }
        private List<TickWithItem> m_History = new List<TickWithItem>();

        /// <summary>
        /// Add a value to the history for the given tick.
        /// This generally expects that items will be added to the history in the order that they occur
        /// (i.e., each call to this has a tick with a greater value than the previous). Nothing enforces this
        /// expectation, but failure to follow it could result in things being replayed out of order later, as
        /// there is no sorting done within this class.
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="value"></param>
        public void Add(double tick, T value)
        {
            m_History.Add(new TickWithItem{Tick = tick, Item = value, DeltaTime = Time.deltaTime});
        }

        /// <summary>
        /// Remove all items before a given tick. Useful to keep memory usage from growing by discarding old data
        /// that you know you won't need anymore.
        /// </summary>
        /// <param name="tick"></param>
        public void RemoveBefore(double tick)
        {
            m_History.RemoveAll(item => item.Tick < tick);
        }

        /// <summary>
        /// Remove all items after a given tick. Useful if the data you are storing here needs to be replaced
        /// as part of a re-anticipation action (i.e., if you are storing some historical position data and want
        /// to recalculate everything after a given tick)
        /// </summary>
        /// <param name="tick"></param>
        public void RemoveAfter(double tick)
        {
            m_History.RemoveAll(item => item.Tick > tick);
        }

        /// <summary>
        /// Get the full history, useful for iterating through all the values to reapply them when reanticipating.
        /// </summary>
        /// <returns></returns>
        public List<TickWithItem> GetHistory()
        {
            return m_History;
        }
    }
}
