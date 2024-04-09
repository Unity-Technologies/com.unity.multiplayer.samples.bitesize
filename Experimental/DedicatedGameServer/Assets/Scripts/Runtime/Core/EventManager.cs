using System;
using System.Collections.Generic;

namespace Unity.DedicatedGameServerSample.Runtime
{
    public class AppEvent { }

    /// <summary>
    /// A simple Event System that can be used for remote systems communication
    /// </summary>
    public class EventManager
    {
        readonly Dictionary<Type, Action<AppEvent>> m_Events = new Dictionary<Type, Action<AppEvent>>();
        readonly Dictionary<Delegate, Action<AppEvent>> m_EventLookups = new Dictionary<Delegate, Action<AppEvent>>();

        internal void AddListener<T>(Action<T> evt) where T : AppEvent
        {
            if (m_EventLookups.ContainsKey(evt))
            { 
                return; 
            }

            Action<AppEvent> newAction = (e) => evt((T)e);
            m_EventLookups[evt] = newAction;

            if (m_Events.TryGetValue(typeof(T), out Action<AppEvent> internalAction))
            {
                m_Events[typeof(T)] = internalAction += newAction;
            }
            else
            {
                m_Events[typeof(T)] = newAction;
            }
        }

        internal void RemoveListener<T>(Action<T> evt) where T : AppEvent
        {
            if (!m_EventLookups.TryGetValue(evt, out var action))
            {
                return;
            }

            if (m_Events.TryGetValue(typeof(T), out var tempAction))
            {
                tempAction -= action;
                if (tempAction == null)
                {
                    m_Events.Remove(typeof(T));
                }
                else
                {
                    m_Events[typeof(T)] = tempAction;
                }
            }

            m_EventLookups.Remove(evt);
        }

        internal void Broadcast(AppEvent evt)
        {
            if (m_Events.TryGetValue(evt.GetType(), out var action))
            {
                action.Invoke(evt);
            }
        }

        internal void Clear()
        {
            m_Events.Clear();
            m_EventLookups.Clear();
        }
    }
}
