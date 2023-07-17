using System;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class InfoPanel : NetworkBehaviour
{
    public ClientNetworkTransform ClientNetworkTransform;
    public List<ToggleEntry> ToggleEntries;
    private bool m_IsSpawned = false;

    void Start()
    {
        ClientPlayerMove.OnNetworkSpawnEvent += OnClientPlayerNetworkSpawn;
        foreach (ToggleEntry toggle in ToggleEntries)
        {
            Debug.Log("Toggle " + toggle.Toggle.name);
        }
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.M))
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
    private void OnClientPlayerNetworkSpawn()
    {
        m_IsSpawned = true;
        ClientPlayerMove.OnNetworkSpawnEvent -= OnClientPlayerNetworkSpawn;
        UpdateClientSideToggles();
    }
    
    public void OnUpdateHalfFloat(bool isOn)
    {
        // Question: Do I need to specify that this is a client-side function? (adding IsClient in the if statement)
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

    public void UpdateClientSideToggles()
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
    private void OnGUI()
    {
        if (IsSpawned)
        {
            if (!ClientNetworkTransform.CanCommitToTransform)
            {
                UpdateClientSideToggles();
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