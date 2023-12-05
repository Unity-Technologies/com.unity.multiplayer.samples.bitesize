using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using Unity.Multiplayer.Tools.NetStats;
using Unity.Multiplayer.Tools.NetStatsMonitor;
using Unity.Netcode;
using UnityEngine;

public enum TcpFrameState
{
    Unknown,
    ReadLength,
    ReadBuffer,
}

#if MULTIPLAYER_TOOLS
[MetricTypeEnum(DisplayName = "TcpTransportStats")]
public enum TcpTransportStats
{
    [MetricMetadata(Units = Units.Bytes, MetricKind = MetricKind.Counter)]
    OutboundBps,
    [MetricMetadata(Units = Units.Bytes, MetricKind = MetricKind.Counter)]
    InboundBps,
}
#endif

[AddComponentMenu("Netcode/TcpTransport")]
public partial class TcpTransport : NetworkTransport
{
    public override ulong ServerClientId { get; } = 0;

    public LatencyTypes OutboundLatencyType;

    [Tooltip("Expressed in MS")]
    [Range(0.0f, 500)]
    public float Latency = 100.0f;

#if MULTIPLAYER_TOOLS
    public RuntimeNetStatsMonitor RuntimeNetStatsMonitor;
#endif

    private float m_RandomLatency;
    private float m_NextRandomLatency;

    [HideInInspector]
    public bool UseLocalhost;
    [HideInInspector]
    public string Ip = "127.0.0.1";
    [HideInInspector]
    public int Port = 7777;

    NetworkManager m_NetworkManager;
    TcpClient m_Client;

    NetworkStream m_Stream;
    bool m_Connected = false;

    private TcpFrameState m_frameState;
    private int m_toRead;
    private byte[] m_readBuffer;

    public enum LatencyTypes
    {
        None,
        Constant,
        Random
    }

    private struct DelayedMessage
    {
        public float SendTime;
        public byte[] Message;
    }

    private List<DelayedMessage> m_OutboundMessages = new List<DelayedMessage>();

    private void Awake()
    {
#if MULTIPLAYER_TOOLS
        RuntimeNetStatsMonitor.gameObject.SetActive(false);
#endif
    }

    private ulong GetCurrentLatency()
    {
        if (OutboundLatencyType != LatencyTypes.None)
        {
            return (ulong)(OutboundLatencyType == LatencyTypes.Random ? m_RandomLatency : Latency);
        }
        return 0;
    }

    private void CalculatedRandomLatency()
    {
        m_RandomLatency = UnityEngine.Random.Range(Latency * 0.10f, Mathf.Clamp(Latency * 2.0f, Latency * 0.5f, Latency));
        m_NextRandomLatency = Time.realtimeSinceStartup + UnityEngine.Random.Range(0.10f, 2.0f);
    }

    public void SetLatencyType(LatencyTypes latencyTypes)
    {
        var previous = OutboundLatencyType;
        OutboundLatencyType = latencyTypes;
        if (OutboundLatencyType != previous && OutboundLatencyType == LatencyTypes.Random)
        {
            CalculatedRandomLatency();
        }
    }

    public override void Initialize(NetworkManager networkManager = null)
    {
        m_NetworkManager = networkManager;

        m_toRead = 4;
        m_readBuffer = new byte[m_toRead];
        m_frameState = TcpFrameState.ReadLength;
    }



    public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
    {
        if (OutboundLatencyType != LatencyTypes.None)
        {
            var delayedMessage = new DelayedMessage()
            {
                SendTime = Time.realtimeSinceStartup + GetCurrentLatency(),
                Message = new byte[sizeof(int) + payload.Count]
            };
            BitConverter.GetBytes(payload.Count).Reverse().ToArray().CopyTo(delayedMessage.Message, 0);
            payload.CopyTo(delayedMessage.Message, sizeof(int));
            m_OutboundMessages.Add(delayedMessage);
        }
        else
        {
            try
            {
                if (m_Client.Connected && m_Stream.CanWrite)
                {
                    // Network order (big endian)
                    m_Stream.Write(BitConverter.GetBytes(payload.Count).Reverse().ToArray());
                    m_Stream.Write(payload);
                }
            }
            catch(SocketException ex) 
            {
                Debug.LogException(ex);
            }
#if MULTIPLAYER_TOOLS
            RuntimeNetStatsMonitor.AddCustomValue(MetricId.Create(TcpTransportStats.OutboundBps), sizeof(int) + payload.Count);
#endif
        }
    }

    public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
    {
        receiveTime = m_NetworkManager.LocalTime.TimeAsFloat;

        if (!m_Connected)
        {
            m_Connected = true;
            clientId = m_NetworkManager.LocalClientId;
            payload = default;
            return NetworkEvent.Connect;
        }

        if (!m_Client.Connected)
        {
            m_Connected = false;
            clientId = m_NetworkManager.LocalClientId;
            payload = default;
            return NetworkEvent.Disconnect;
        }

        // Only if we have latency enabled =or= we had it enabled, disabled it, and there are still pending outbound messages
        if (OutboundLatencyType != LatencyTypes.None || m_OutboundMessages.Count > 0)
        {
            var sentMessages = new List<int>();
            for (int i = 0; i < m_OutboundMessages.Count; i++)
            {
                if (!m_Stream.CanWrite)
                {
                    break;
                }
                // Check to see if the current queued message can be sent =or= if we disabled latency then send everything 
                if (m_OutboundMessages[i].SendTime <= Time.realtimeSinceStartup || OutboundLatencyType == LatencyTypes.None)
                {
                    sentMessages.Add(i);
                    m_Stream.Write(m_OutboundMessages[i].Message);
#if MULTIPLAYER_TOOLS
                    RuntimeNetStatsMonitor.AddCustomValue(MetricId.Create(TcpTransportStats.OutboundBps), m_OutboundMessages[i].Message.Length);
#endif
                }
                else // Send queued latent messages in their expected order
                {
                    break;
                }
            }

            // If we had messages queued but then disabled latency injection, then since we sent everything just clear the queue
            if (OutboundLatencyType == LatencyTypes.None)
            {
                m_OutboundMessages.Clear();
            }
            else // Otherwise remove the messages we sent
            {
                sentMessages.Reverse();
                foreach (var sentMessage in sentMessages)
                {
                    m_OutboundMessages.RemoveAt(sentMessage);
                }
            }
            sentMessages.Clear();
        }

        if (OutboundLatencyType == LatencyTypes.Random)
        {
            if (m_NextRandomLatency < Time.realtimeSinceStartup)
            {
                CalculatedRandomLatency();
            }
        }

        // Hack-Check: Sometimes this is not valid
        if (m_Stream == null || !m_Stream.CanRead)
        {
            m_Stream = m_Client.GetStream();
            if (m_Stream == null || !m_Stream.CanRead)
            {
                clientId = default;
                payload = default;
                return NetworkEvent.TransportFailure;
            }
        }

        while (m_Stream.DataAvailable)
        {
            var read = m_Stream.Read(m_readBuffer, m_readBuffer.Length - m_toRead, m_toRead);
            m_toRead -= read;

            if (m_toRead == 0)
            {
                switch (m_frameState)
                {
                    case TcpFrameState.ReadLength:
                        // Network order (big endian)
                        int bufferLength = BitConverter.ToInt32(m_readBuffer.Reverse().ToArray());
#if MULTIPLAYER_TOOLS
                        RuntimeNetStatsMonitor.AddCustomValue(MetricId.Create(TcpTransportStats.InboundBps), bufferLength + sizeof(int));
#endif
                        m_toRead = bufferLength;
                        m_readBuffer = new byte[m_toRead];
                        m_frameState = TcpFrameState.ReadBuffer;
                        break;

                    case TcpFrameState.ReadBuffer:
                        clientId = 0;
                        payload = m_readBuffer;

                        m_toRead = 4;
                        m_readBuffer = new byte[m_toRead];
                        m_frameState = TcpFrameState.ReadLength;
                        return NetworkEvent.Data;
                    case TcpFrameState.Unknown:
                        Debug.LogError("Unkown TCP frame state!");
                        break;
                }
            }
            else if (m_toRead < 0)
            {
                Debug.LogError($"m_toRead < 0! {m_toRead}");
            }
        }

        clientId = default;
        payload = default;
        return NetworkEvent.Nothing;
    }

    public void ToggleRNSMTool()
    {
#if MULTIPLAYER_TOOLS
        RuntimeNetStatsMonitor.gameObject.SetActive(!RuntimeNetStatsMonitor.gameObject.activeInHierarchy);
#endif
    }

    public override bool StartClient()
    {
        var ip = Ip;
        var port = Port;
        if (UseLocalhost)
        {
            ip = "127.0.0.1";
            port = 7777;
        }
        Debug.Log($"Connecting to {ip}:{port}");
        m_Client = new TcpClient(ip, port);
        while(!m_Client.Connected)
        {
            Debug.Log("Wait Client Connect");
        }
        m_Stream = m_Client.GetStream();

        return true;
    }

    public override bool StartServer()
    {
        throw new NotImplementedException();
    }

    public override void DisconnectRemoteClient(ulong clientId)
    {
        throw new NotImplementedException();
    }

    public override void DisconnectLocalClient()
    {
        m_Stream?.Close();
        m_Stream?.Dispose();
        m_Client?.Dispose();
        m_Connected = false;
        m_Client = null;
        m_Stream = null;
    }

    public override ulong GetCurrentRtt(ulong clientId)
    {
        return GetCurrentLatency();
    }

    public override void Shutdown()
    {
        m_Client?.Dispose();
    }
}
