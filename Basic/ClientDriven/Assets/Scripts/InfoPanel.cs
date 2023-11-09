using System.Text.RegularExpressions;
using TMPro;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class InfoPanel : MonoBehaviour
{
    public Toggle HalfFloatToggle;
    public Toggle InterpolateToggle;
    public Toggle QuatSynchToggle;
    public Toggle QuatCompToggle;
    public TMP_InputField PacketDelayMSInputField;
    public TMP_InputField PacketJitterMSInputField;
    public TMP_InputField PacketDropRateInputField;

    private UnityTransport.SimulatorParameters SimulatorParameters;
    private ClientNetworkTransform ClientNetworkTransform;

    void Start()
    {
        LocalPlayer.OnNetworkSpawnEvent += OnClientPlayerNetworkSpawn;
    }

    void Update()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        QuatCompToggle.interactable = QuatSynchToggle.isOn;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            HalfFloatToggle.isOn = !HalfFloatToggle.isOn;
        } 
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            InterpolateToggle.isOn = !InterpolateToggle.isOn;
        } 
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            QuatSynchToggle.isOn = !QuatSynchToggle.isOn;
        } 
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            QuatCompToggle.isOn = !QuatCompToggle.isOn;
        }
        
        PacketDelayMSInputField = GameObject.Find("PacketDelayInputField").GetComponent<TMP_InputField>();
    }
    private void OnClientPlayerNetworkSpawn(ClientNetworkTransform clientNetworkTransform)
    {
        Debug.Assert(clientNetworkTransform != null);
        ClientNetworkTransform = clientNetworkTransform;
        LocalPlayer.OnNetworkSpawnEvent -= OnClientPlayerNetworkSpawn;
        UpdateClientSideToggles();
    }
    
    public void OnUpdateHalfFloat(bool isOn)
    {
        if (ClientNetworkTransform != null)
        {
            ClientNetworkTransform.UseHalfFloatPrecision = isOn;
        }
    }

    public void OnUpdateQuatSync(bool isOn)
    {
        if (ClientNetworkTransform != null)
        {
            ClientNetworkTransform.UseQuaternionSynchronization = isOn;
        }
    }

    public void OnUpdateQuatComp(bool isOn)
    {
        if (ClientNetworkTransform != null)
        {
            ClientNetworkTransform.UseQuaternionCompression = isOn;
        }
    }

    public void OnInterpolate(bool isOn)
    {
        if (ClientNetworkTransform != null)
        {
            ClientNetworkTransform.Interpolate = isOn;
        }
    }

    private void UpdateClientSideToggles()
    {
        if (ClientNetworkTransform == null) return;
        HalfFloatToggle.isOn = ClientNetworkTransform.UseHalfFloatPrecision;
        InterpolateToggle.isOn = ClientNetworkTransform.Interpolate;
        QuatSynchToggle.isOn = ClientNetworkTransform.UseQuaternionSynchronization;
        QuatCompToggle.isOn = ClientNetworkTransform.UseQuaternionCompression;
    }
    
    private static string SanitizeDirtyString(string input)
    {
        var digitsOnly = new Regex(@"[^\d]"); 
        return digitsOnly.Replace(input, "");
    }

    public void OnUpdatePacketDelay()
    {
        PacketDelayMSInputField.characterValidation = TMP_InputField.CharacterValidation.Integer;
        if(int.TryParse(SanitizeDirtyString(PacketDelayMSInputField.text), out var packetDelay))
        {
            SimulatorParameters.PacketDelayMS = packetDelay;
        }
        NetworkManager.Singleton.GetComponent<UnityTransport>().DebugSimulator = SimulatorParameters;
    }

    public void OnUpdatePacketJitter()
    {
        PacketJitterMSInputField.characterValidation = TMP_InputField.CharacterValidation.Integer;
        if(int.TryParse(SanitizeDirtyString(PacketJitterMSInputField.text), out var packetJitter))
        {
            SimulatorParameters.PacketJitterMS = packetJitter;
        }
        NetworkManager.Singleton.GetComponent<UnityTransport>().DebugSimulator = SimulatorParameters;
    }

    public void OnUpdatePacketDropRate()
    {
        PacketDropRateInputField.characterValidation = TMP_InputField.CharacterValidation.Integer;
        if(int.TryParse(SanitizeDirtyString(PacketDropRateInputField.text), out var packetDropRate))
        {
            SimulatorParameters.PacketDropRate = packetDropRate;
        }
        NetworkManager.Singleton.GetComponent<UnityTransport>().DebugSimulator = SimulatorParameters;
    }
}