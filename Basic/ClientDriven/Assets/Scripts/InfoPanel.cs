using System;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using UnityEngine;
using UnityEngine.UI;

public class InfoPanel : MonoBehaviour
{
    public Toggle HalfFloatToggle;
    public Toggle InterpolateToggle;
    public Toggle QuatSynchToggle;
    public Toggle QuatCompToggle;
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
}