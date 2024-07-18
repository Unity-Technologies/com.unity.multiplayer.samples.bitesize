#if TCPTransport
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Unity.Netcode;

public class LatencyPanel : MonoBehaviour
{
    public NetworkManager NetworkManager;
    public Slider LatencySlider;
    public Text LatencyAmountText;
    public Dropdown LatencyDropdown;
    public GameObject Panel;

    // Start is called before the first frame update
    void Start()
    {
        m_TcpTransport = NetworkManager.NetworkConfig.NetworkTransport as TcpTransport;

        if (m_TcpTransport == null )
        {
            enabled = false;
            NetworkManagerHelper.Instance.LogMessage("[!!!! No TcpTransport Found !!!!] Disabling latency panel.");
            return;
        }
        else
        {
            Panel.SetActive(false);
        }

        var sliderEvent = new Slider.SliderEvent();
        sliderEvent.AddListener(new UnityAction<float>(OnSliderChanged));
        LatencySlider.value = m_TcpTransport.Latency;
        LatencySlider.onValueChanged = sliderEvent;

        var dropdownEvent = new Dropdown.DropdownEvent();
        dropdownEvent.AddListener(new UnityAction<int>(OnDropdownChanged));

        LatencyDropdown.onValueChanged = dropdownEvent;

        LatencyAmountText.text = m_TcpTransport.Latency.ToString();
    }

    private void OnDropdownChanged(int value)
    {
        m_TcpTransport.SetLatencyType((TcpTransport.LatencyTypes)value);
    }

    private void OnSliderChanged(float value)
    {
        m_TcpTransport.Latency = value;
        LatencyAmountText.text = value.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        if (!NetworkManager.IsConnectedClient) 
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            if (m_TcpTransport.enabled)
            {
                Panel.SetActive(!Panel.activeInHierarchy);
            }
            else
            {
                NetworkManagerHelper.Instance.LogMessage($"[TcpTransport Disabled] Latency panel is only for the temporary TcpTransport!");
            }
        }
    }
}
#endif