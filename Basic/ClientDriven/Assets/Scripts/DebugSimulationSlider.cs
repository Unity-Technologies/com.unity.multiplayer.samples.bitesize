using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public class DebugSimulationSlider : MonoBehaviour
{
    public SimulationParameters DebugSimType;
    private Slider m_Slider;
    private Text m_Amount;
    private string m_AmountPrefixText;
    private NetworkManager m_NetworkManager;
    private UnityTransport m_UnityTransport;

    public enum SimulationParameters
    {
        DropRate,
        Latency,
        Jitter
    }

    private void Awake()
    {
        m_Amount = GetComponentInChildren<Text>();
        m_AmountPrefixText = m_Amount.text;
        m_Slider = GetComponentInChildren<Slider>();
        var valueChanged = new Slider.SliderEvent();
        valueChanged.AddListener(new UnityAction<float>(ValueChanged));
        m_Slider.onValueChanged = valueChanged;

        var bootStrap = GameObject.Find("BootstrapManager");
        m_NetworkManager = bootStrap.GetComponent<NetworkManager>();
        m_NetworkManager.OnServerStarted -= OnStarted;
        m_NetworkManager.OnServerStarted += OnStarted;
        m_NetworkManager.OnClientStopped -= OnStopped;
        m_NetworkManager.OnServerStopped -= OnStopped;
        m_NetworkManager.OnClientStopped += OnStopped;
        m_NetworkManager.OnServerStopped += OnStopped;
        m_UnityTransport = m_NetworkManager.NetworkConfig.NetworkTransport as UnityTransport;

        SetParameter((int)m_Slider.value);
    }

    private void ValueChanged(float value)
    {
        SetParameter((int)value);
    }

    private void OnStarted()
    {
        this.gameObject.SetActive(false);
    }

    private void OnStopped(bool value)
    {
        this.gameObject.SetActive(true);
    }

    private void SetParameter(int value)
    {
        var debugSim = m_UnityTransport.DebugSimulator;
        switch(DebugSimType)
        {
            case SimulationParameters.DropRate:
                {
                    debugSim.PacketDropRate = value;
                    m_Amount.text = $"{m_AmountPrefixText}: {value}%";
                    break;
                }
            case SimulationParameters.Latency:
                {
                    debugSim.PacketDelayMS = value;
                    m_Amount.text = $"{m_AmountPrefixText}: {value}ms";
                    break;
                }
            case SimulationParameters.Jitter:
                {
                    debugSim.PacketJitterMS = value;
                    m_Amount.text = $"{m_AmountPrefixText}: +/-{value}ms";
                    break;
                }
        }
        m_UnityTransport.SetDebugSimulatorParameters(debugSim.PacketDelayMS, debugSim.PacketJitterMS, debugSim.PacketDropRate);
    }
}
