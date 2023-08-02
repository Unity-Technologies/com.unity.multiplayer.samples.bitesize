namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// Base class for all View related classes.
    /// A View's purpose is to display data and objects (typically contained in the model)
    /// </summary>
    public class View : Element { }

    /// <summary>
    /// Base class for all View related classes.
    /// </summary>
    public class View<T> : View where T : BaseApplication
    {
        /// <summary>
        /// Returns app as a custom 'T' type.
        /// </summary>
        new public T App => (T)base.App;

        internal void Show()
        {
            gameObject.SetActive(true);
        }

        internal void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}