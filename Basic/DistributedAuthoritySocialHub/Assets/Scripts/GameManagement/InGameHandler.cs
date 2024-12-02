using System;
using Unity.Multiplayer.Samples.SocialHub.Input;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.GameManagement
{
    class InGameHandler : MonoBehaviour
    {
        void Start()
        {
            InputSystemManager.Instance.EnableGameplayInputs();
            GameplayEventHandler.OnReturnToMainMenuButtonPressed += GameplayEventHandler.LoadMainMenuScene;
            GameplayEventHandler.OnExitedSession += GameplayEventHandler.LoadMainMenuScene;
        }

        void OnDestroy()
        {
            GameplayEventHandler.OnReturnToMainMenuButtonPressed -= GameplayEventHandler.LoadMainMenuScene;
            GameplayEventHandler.OnExitedSession -= GameplayEventHandler.LoadMainMenuScene;
        }
    }
}
