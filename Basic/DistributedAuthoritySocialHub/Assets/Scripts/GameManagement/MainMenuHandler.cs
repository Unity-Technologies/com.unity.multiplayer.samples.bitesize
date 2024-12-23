using System.Threading.Tasks;
using Unity.Multiplayer.Samples.SocialHub.Input;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.GameManagement
{
    class MainMenuHandler : MonoBehaviour
    {
        void Start()
        {
            InputSystemManager.Instance.EnableUIInputs();
            GameplayEventHandler.OnConnectToSessionCompleted += OnConnectToSessionCompleted;
        }

        void OnDestroy()
        {
            GameplayEventHandler.OnConnectToSessionCompleted -= OnConnectToSessionCompleted;
        }

        void OnConnectToSessionCompleted(Task task, string sessionName)
        {
            if (task.IsCompletedSuccessfully)
            {
                GameplayEventHandler.LoadInGameScene();
            }
        }
    }
}
