using System;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    /// <summary>
    /// Networked script to handle player character logic that needs to be networked.
    /// Inherits from NetcodeHooks class to provide hooks for spawn and despawn events.
    /// </summary>
    public class NetworkedAICharacter : NetcodeHooks, ICharacter
    {
        NetworkVariable<float> m_Speed = new NetworkVariable<float>();

        public float Speed
        {
            get => m_Speed.Value;
            set => m_Speed.Value = value;
        }
    }
}
