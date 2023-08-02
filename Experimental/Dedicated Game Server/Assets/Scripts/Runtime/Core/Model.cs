namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// Base class for all Model related classes.
    /// A Model's purpose is to contain data about something (tipically its view)
    /// </summary>
    public class Model : Element { }
    
    /// <summary>
    /// Base class for all Model related classes.
    /// </summary>
    public class Model<T> : Model where T : BaseApplication
    {
        /// <summary>
        /// Returns app as a custom 'T' type.
        /// </summary>
        new public T App => (T)base.App;
    }
}