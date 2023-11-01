using Unity.Netcode;

namespace Unity.DedicatedGameServerSample.Runtime
{
    /// <summary>
    /// Manages the flow of the Game part of the application
    /// </summary>
    public class GameApplication : BaseApplication<GameModel, GameView, GameController>
    {
        internal new static GameApplication Instance { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
        }
    }
}
