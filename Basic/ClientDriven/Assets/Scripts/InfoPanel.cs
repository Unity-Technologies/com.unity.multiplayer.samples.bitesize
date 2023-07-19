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
            var foundItem = ToggleEntries.Find(item => item.Toggle.name == "HalfFloat");
            foundItem.Toggle.isOn = !foundItem.Toggle.isOn;
        } 
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            var foundItem = ToggleEntries.Find(item => item.Toggle.name == "QuatSynch");
            foundItem.Toggle.isOn = !foundItem.Toggle.isOn;
        } 
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            // Only activate Quaternion Compression if Quaternion Synchronization is enabled
            if (ClientNetworkTransform.UseQuaternionSynchronization)
            {
                var foundItem = ToggleEntries.Find(item => item.Toggle.name == "QuatComp");
                foundItem.Toggle.isOn = !foundItem.Toggle.isOn;
            }
        } 
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            var foundItem = ToggleEntries.Find(item => item.Toggle.name == "Interpolate");
            foundItem.Toggle.isOn = !foundItem.Toggle.isOn;
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
            entry.Toggle.enabled = true;
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
            entry.Toggle.enabled = false;
        }
    }
    /*private void OnGUI()
    {
        if (IsSpawned)
        {
            if (!ClientNetworkTransform.CanCommitToTransform)
            {
                UpdateClientSideToggles();
            }
        }
    }*/
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