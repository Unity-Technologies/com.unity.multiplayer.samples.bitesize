using System;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using UnityEngine;
using UnityEngine.UI;

public class InfoPanel : MonoBehaviour
{
    public List<ToggleEntry> ToggleEntries;
    private ClientNetworkTransform ClientNetworkTransform;

    void Start()
    {
        LocalPlayer.OnNetworkSpawnEvent += OnClientPlayerNetworkSpawn;
    }

    void Awake()
    {
        Debug.Assert(ToggleEntries != null && ToggleEntries.Count > 0);
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            var toggleEntry = ToggleEntries.Find(entry => entry.Toggle.name == "HalfFloat");
            toggleEntry.Toggle.isOn = !toggleEntry.Toggle.isOn;
        } 
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            var toggleEntry = ToggleEntries.Find(entry => entry.Toggle.name == "QuatSynch");
            toggleEntry.Toggle.isOn = !toggleEntry.Toggle.isOn;
        } 
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            // Only activate Quaternion Compression if Quaternion Synchronization is enabled
            if (ClientNetworkTransform.UseQuaternionSynchronization)
            {
                var toggleEntry = ToggleEntries.Find(entry => entry.Toggle.name == "QuatComp");
                toggleEntry.Toggle.isOn = !toggleEntry.Toggle.isOn;
            }
        } 
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            var toggleEntry = ToggleEntries.Find(entry => entry.Toggle.name == "Interpolate");
            toggleEntry.Toggle.isOn = !toggleEntry.Toggle.isOn;
        }
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
        // Question: Do I need to specify that this is a client-side function? (adding IsClient in the if statement)
        ClientNetworkTransform.UseHalfFloatPrecision = isOn;
    }

    public void OnUpdateQuatSync(bool isOn)
    {
        ClientNetworkTransform.UseQuaternionSynchronization = isOn;
    }

    public void OnUpdateQuatComp(bool isOn)
    {
        ClientNetworkTransform.UseQuaternionCompression = isOn;
    }

    public void OnInterpolate(bool isOn)
    {
        ClientNetworkTransform.Interpolate = isOn;
    }

    private void UpdateClientSideToggles()
    {
        foreach (var entry in ToggleEntries)
        {
            switch (entry.ToggleEntryType)
            {
                case ToggleEntry.ToggleEntryTypes.HalfFloat:
                    {
                        entry.Toggle.isOn = ClientNetworkTransform.UseHalfFloatPrecision;
                        break;
                    }
                case ToggleEntry.ToggleEntryTypes.QuatSynch:
                    {
                        entry.Toggle.isOn = ClientNetworkTransform.UseQuaternionSynchronization;
                        break;
                    }
                case ToggleEntry.ToggleEntryTypes.QuatComp:
                    {
                        entry.Toggle.isOn = ClientNetworkTransform.UseQuaternionCompression;
                        break;
                    }
                case ToggleEntry.ToggleEntryTypes.Interpolate:
                    {
                        entry.Toggle.isOn = ClientNetworkTransform.Interpolate;
                        break;
                    }
            }
        }
    }
}

[Serializable]
public class ToggleEntry
{
    public enum ToggleEntryTypes
    {
        HalfFloat,
        QuatSynch,
        QuatComp,
        Interpolate,
    }

    public Toggle Toggle;
    public ToggleEntryTypes ToggleEntryType;
}