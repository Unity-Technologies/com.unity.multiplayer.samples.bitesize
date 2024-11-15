using System;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.GameManagement
{
    class InGameHandler : MonoBehaviour
    {
        void Start()
        {
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
