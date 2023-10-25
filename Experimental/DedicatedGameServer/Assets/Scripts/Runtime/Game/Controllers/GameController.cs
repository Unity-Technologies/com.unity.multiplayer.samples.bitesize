using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    /// <summary>
    /// Main controller of the <see cref="GameApplication"></see>
    /// </summary>
    public class GameController : Controller<GameApplication>
    {
        GameModel Model => App.Model;

        void OnDestroy()
        {
            RemoveListeners();
        }

        internal override void RemoveListeners()
        {
        }
    }
}
