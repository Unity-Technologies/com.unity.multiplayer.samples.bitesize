namespace Unity.DedicatedGameServerSample.Runtime
{
    /// <summary>
    /// Extension of the BaseApplication class to handle different types of Model View Controllers.
    /// </summary>
    /// <typeparam name="M"></typeparam>
    /// <typeparam name="V"></typeparam>
    /// <typeparam name="C"></typeparam>
    public class BaseApplication<M, V, C> : BaseApplication
        where M : Element
        where V : Element
        where C : Element
    {
        new internal BaseApplication<M, V, C> Instance => (BaseApplication<M, V, C>)(object)base.Instance;

        /// <summary>
        /// Model reference using the new type.
        /// </summary>
        new public M Model => (M)(object)base.Model;

        /// <summary>
        /// View reference using the new type.
        /// </summary>
        new public V View => (V)(object)base.View;

        /// <summary>
        /// Controller reference using the new type.
        /// </summary>
        new public C Controller => (C)(object)base.Controller;
    }

    /// <summary>
    /// Root class for the scene's scripts.
    /// </summary>
    public class BaseApplication : Element
    {
        internal BaseApplication Instance { get; private set; }

        internal EventManager EventManager;

        /// <summary>
        /// Fetches the root Model instance.
        /// </summary>
        internal Model Model => m_model = Find<Model>(m_model);
        Model m_model;

        /// <summary>
        /// Fetches the root View instance.
        /// </summary>
        internal View View => m_view = Find<View>(m_view);
        View m_view;

        /// <summary>
        /// Fetches the root Controller instance.
        /// </summary>
        internal Controller Controller => m_controller = Find<Controller>(m_controller);
        Controller m_controller;

        /// <summary>
        /// Initializes the BaseApplication
        /// </summary>
        public BaseApplication()
        {
            if (EventManager == null)
            {
                EventManager = new EventManager();
            }
        }

        protected virtual void Awake()
        {
            if (EventManager == null)
            {
                EventManager = new EventManager();
            }
        }

        /// <summary>
        /// Notifies an event to the component's of the app
        /// </summary>
        /// <param name="evt"></param>
        new internal void Broadcast(AppEvent evt)
        {
            EventManager.Broadcast(evt);
        }
    }
}
