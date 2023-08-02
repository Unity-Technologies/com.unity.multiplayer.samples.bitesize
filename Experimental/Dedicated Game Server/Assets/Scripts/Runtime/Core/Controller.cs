using System;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// Base class for all Controllers in the application.
    /// A Controller's purpose is to act as bridge between its view and model, 
    /// reacting on events and performing operations on either side
    /// </summary>
    public class Controller : Element { }

    /// <summary>
    /// Base class for all Controller related classes.
    /// </summary>
    public abstract class Controller<T> : Controller where T : BaseApplication
    {
        /// <summary>
        /// Returns app as a custom 'T' type.
        /// </summary>
        new public T App => (T)base.App;

        /// <summary>
        /// Subscribes to an AppEvent
        /// </summary>
        /// <param name="evt">Callback for an AppEvent</param>
        internal void AddListener<E>(Action<E> evt) where E : AppEvent
        {
            App.EventManager.AddListener(evt);
        }

        internal void RemoveListener<E>(Action<E> evt) where E : AppEvent
        {
            App.EventManager.RemoveListener(evt);
        }

        internal abstract void RemoveListeners();
    }
}