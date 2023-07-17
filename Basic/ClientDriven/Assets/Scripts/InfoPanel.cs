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
    private bool isSpawned = false;

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

        if (Input.GetKeyDown(KeyCode.N))
        {
            var foundItem = ToggleEntries.Find(item => item.Toggle.name == "HalfFloat");
            foundItem.Toggle.isOn = !foundItem.Toggle.isOn;
        }
    }
    private void OnClientPlayerNetworkSpawn()
    {
        isSpawned = true;
        ClientPlayerMove.OnNetworkSpawnEvent -= OnClientPlayerNetworkSpawn;
        UpdateClientSideToggles();
    }
    
    public void OnUpdateHalfFloat(bool isOn)
    {
        if (ClientNetworkTransform != null && IsServer)
        {
            ClientNetworkTransform.UseHalfFloatPrecision = isOn;
        }   
    }

    public void OnUpdateQuatSync(bool isOn)
    {
        if (ClientNetworkTransform != null && IsServer)
        {
            ClientNetworkTransform.UseQuaternionSynchronization = isOn;
        }
    }

    public void OnUpdateQuatComp(bool isOn)
    {
        if (ClientNetworkTransform != null && IsServer)
        {
            ClientNetworkTransform.UseQuaternionCompression = isOn;
        }
    }

    public void OnInterpolate(bool isOn)
    {
        if (ClientNetworkTransform != null && IsServer)
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