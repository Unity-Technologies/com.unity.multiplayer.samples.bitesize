using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.GameManagement
{
    class MainMenuHandler : MonoBehaviour
    {
        void Start()
        {
            GameplayEventHandler.OnConnectToSessionCompleted += OnConnectToSessionCompleted;
        }

        void OnDestroy()
        {
            GameplayEventHandler.OnConnectToSessionCompleted -= OnConnectToSessionCompleted;
        }

        void OnConnectToSessionCompleted(Task task)
        {
            if (task.IsCompletedSuccessfully)
            {
                GameplayEventHandler.LoadInGameScene();
            }
        }
    }
}
