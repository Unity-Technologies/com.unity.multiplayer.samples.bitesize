using System;
using Unity.Multiplayer.Tools.NetStatsMonitor;
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;
using UnityEngine;

namespace Misc
{
    public class MttToolsToggleController : MonoBehaviour
    {
        public RuntimeNetStatsMonitor m_Rnsm;
        public NetworkSimulator m_NetworkSimulator;

        private bool isRnsmOverlayActive = false;

        void Awake()
        {
            DontDestroyOnLoad(this);
        }

        void Start()
        {
            m_Rnsm.Visible = isRnsmOverlayActive;
        }

        void Update()
        {
            // RNSM
            if (Input.GetKeyDown(KeyCode.S))
            {
                if (Input.GetKeyDown(KeyCode.LeftShift))
                {
                    isRnsmOverlayActive = !isRnsmOverlayActive;
                    m_Rnsm.Visible = isRnsmOverlayActive;
                }
            }

            // Network Simulator
            if (Input.GetKeyDown(KeyCode.N))
            {
                if (Input.GetKeyDown(KeyCode.LeftShift))
                {
                    ToggleNetworkSimulator();
                }
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                if (Input.GetKeyDown(KeyCode.LeftShift))
                {
                    TriggerLagSpike(500);
                }
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                if (Input.GetKeyDown(KeyCode.LeftShift))
                {
                    SetNetworkDisconnect(true);
                }
            }

        }

        private void ToggleNetworkSimulator()
        {
            m_NetworkSimulator.enabled = !m_NetworkSimulator.enabled;
            Debug.Log("Network Simulator " + (m_NetworkSimulator.enabled ? "Enabled": "Disabled"));
        }

        private void TriggerLagSpike(int durationMs)
        {
            TimeSpan duration = TimeSpan.FromMilliseconds(durationMs);
            m_NetworkSimulator.TriggerLagSpike(duration);
            Debug.Log("Lag spike triggered for " + durationMs + " ms");
        }

        private void SetNetworkDisconnect(bool disconnect)
        {
            if (disconnect)
            {
                m_NetworkSimulator.Disconnect();
                Debug.Log("Network " + (disconnect ? "Disconnected": "Connected"));
            }
        }

        public void SetNetworkPresetHomeFiber()
        {
            var preset = m_NetworkSimulator.ConnectionPreset;
            preset.PacketDelayMs = 10;
            preset.PacketJitterMs = 1;
            preset.PacketLossInterval = 0;
            preset.PacketLossPercent = 0;
            Debug.Log("Network preset set to Home Fiber");
        }
    }
}
