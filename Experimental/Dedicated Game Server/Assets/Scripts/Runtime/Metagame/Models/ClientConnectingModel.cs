using System;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class ClientConnectingModel : Model<MetagameApplication>
    {
        public float ElapsedTime { get; private set; }

        void Update()
        {
            ElapsedTime += Time.deltaTime;
        }

        public void InitializeTimer()
        {
            ElapsedTime = 0;
        }
    }
}
