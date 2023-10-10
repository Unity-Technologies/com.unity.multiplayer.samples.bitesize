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
    public GameObject PacketDelayMSInputField2;

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

        /*var simulatorParameters = new UnityTransport.SimulatorParameters
        {
            PacketDelayMS = 50,
        };
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            // Apply the simulator parameters to the Transport layer
            NetworkManager.Singleton.GetComponent<UnityTransport>().DebugSimulator = simulatorParameters;
        }*/
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
    
    private string SanitizeDirtyString(string input)
    {
        return input.Replace("\n", "").Replace("\r", "").Replace(" ", "");
    }

    public void OnDebugSimulatorChanges()
    {
        var simulatorParameters = new UnityTransport.SimulatorParameters
        {
            // Set the parameters for simulating network conditions here
            PacketDelayMS = PacketDelayMSInputField.text == "" ? 0 : int.Parse(SanitizeDirtyString(PacketDelayMSInputField.text)),
            //PacketJitterMS = packetJitterMs,
            //PacketDropRate = packetDropRate
        };

        // Apply the simulator parameters to the Transport layer
        NetworkManager.Singleton.GetComponent<UnityTransport>().DebugSimulator = simulatorParameters;
    }
}