using System;
using DefaultNamespace;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using Random = UnityEngine.Random;

public class AnticipationSample : NetworkBehaviour
{
    /// <summary>
    /// This value is a snap value with correct anticipation. When the player changes the value, an RPC will be
    /// sent to the server, and the server will change to the same value.
    /// </summary>
    public AnticipatedNetworkVariable<float> ValueA = new AnticipatedNetworkVariable<float>(0);
    /// <summary>
    /// This value is a snap value with incorrect anticipation. When the player changes the value, an RPC will be
    /// sent to the server, and the server will change to a random value. The anticipation will then snap to the
    /// new value.
    /// </summary>
    public AnticipatedNetworkVariable<float> ValueB = new AnticipatedNetworkVariable<float>(0);

    /// <summary>
    /// This value is a smooth value with correct anticipation. When the player changes the value, an RPC will be
    /// sent to the server, and the server will change to the same value. The result will be the same as a snap value
    /// on the local client, but will smooth when viewed on a remote client.
    /// </summary>
    public AnticipatedNetworkVariable<float> ValueC = new AnticipatedNetworkVariable<float>(0);
    /// <summary>
    /// This value is a smooth value with incorrect anticipation. When the player changes the value, an RPC will be
    /// sent to the server, and the server will change to a random value. The anticipation will then interpolate to the
    /// new value.
    /// </summary>
    public AnticipatedNetworkVariable<float> ValueD = new AnticipatedNetworkVariable<float>(0);

    /// <summary>
    /// This is a server-controlled value that gets updated by the server, and the client anticipates what it should be
    /// "now" based on the latency to the server (knowing that the value it sees from the server is actually in the past)
    /// </summary>
    public AnticipatedNetworkVariable<float> ValueE = new AnticipatedNetworkVariable<float>(0, StaleDataHandling.Reanticipate);

    public NetworkManager NetworkManagerObject;
    public PlayerMovableObject Player;

    [Rpc(SendTo.Server)]
    void SetValueARpc(float value)
    {
        ValueA.AuthoritativeValue = value;
        LogEverywhereRpc($"Set value A to {ValueA.AuthoritativeValue}");
    }

    [Rpc(SendTo.Server)]
    void SetValueBRpc(float value)
    {
        ValueB.AuthoritativeValue = Random.Range(0f, 10f);
        LogEverywhereRpc($"Set value B to {ValueB.AuthoritativeValue}");
    }

    [Rpc(SendTo.Server)]
    void SetValueCRpc(float value)
    {
        ValueC.AuthoritativeValue = value;
        LogEverywhereRpc($"Set value C to {ValueC.AuthoritativeValue}");
    }

    [Rpc(SendTo.Server)]
    void SetValueDRpc(float value)
    {
        ValueD.AuthoritativeValue = Random.Range(0f, 10f);
        LogEverywhereRpc($"Set value D to {ValueD.AuthoritativeValue}");
    }

    [Rpc(SendTo.Everyone)]
    void LogEverywhereRpc(FixedString128Bytes message)
    {
        Debug.Log(message.ToString());
    }


    private const float k_ValueEChangePerSecond = 2.5f;

    public override void OnNetworkSpawn()
    {
        // Initialize the reanticipation for all of the values:
        // C and D react to a request to reanticipate by simply smoothing between the previous anticipated value
        // and the new authoritative value. They are not frequently updated and only need any reanticipation action
        // when the anticipation was wrong.
        AnticipatedNetworkVariable<float>.OnReanticipateDelegate smooth = (AnticipatedNetworkVariable<float> variable, in float anticipatedValue, double anticipationTime, in float authoritativeValue, double authoritativeTime) =>
        {
            variable.Smooth(anticipatedValue, authoritativeValue, 0.25f, Mathf.Lerp);
        };
        ValueC.OnReanticipate = smooth;
        ValueD.OnReanticipate = smooth;

        // E is actually trying to anticipate the current value of a constantly changing object to hide latency.
        // It uses the amount of time that has passed since the authoritativetime to gauge the latency of this update
        // and anticipates a new value based on that delay. The server value is in the past, so the predicted value
        // attempts to guess what the value is in the present.
        ValueE.OnReanticipate = (AnticipatedNetworkVariable<float> variable, in float anticipatedValue, double anticipationTime, in float authoritativeValue, double authoritativeTime) =>
        {
            var secondsBehind = (NetworkManager.LocalTime.Time - authoritativeTime);

            var newAnticipatedValue = (float)(authoritativeValue + k_ValueEChangePerSecond * secondsBehind) % 10;
            // If we always smooth here, we will have differences in behavior between the client and server simulations
            // The server simulation jumps backward when it reaches the end, but smoothing would result in a smooth
            // movement back to the beginning.
            // Checking if the new value is less than the previous anticipated value does not work here because
            // network jitter can make that happen any time. Checking if the new value is less than the authority value
            // also does not work because that will disable smoothing for the entire duration that the anticipated value
            // is near the beginning of the scale. So what we do instead is check the size of the change: if the distance
            // between the previous value and the new one is less than 1, we smooth it, otherwise, we snap to the new
            // value.
            if (Math.Abs(newAnticipatedValue - anticipatedValue) < 1f)
            {
                variable.Smooth(anticipatedValue, newAnticipatedValue, 0.1f, Mathf.Lerp);
            }
            else
            {
                variable.Anticipate(newAnticipatedValue);
            }
        };

        AnticipatedNetworkVariable<float>.OnAuthoritativeValueChangedDelegate onUpdate = (AnticipatedNetworkVariable<float> variable, in float value, in float newValue) =>
        {
            Debug.Log($"{variable.Name} value updated to {newValue}");
        };
        ValueA.OnAuthoritativeValueChanged = onUpdate;
        ValueB.OnAuthoritativeValueChanged = onUpdate;
        ValueC.OnAuthoritativeValueChanged = onUpdate;
        ValueD.OnAuthoritativeValueChanged = onUpdate;
    }

    private void Update()
    {
        if (IsServer)
        {
            ValueE.AuthoritativeValue = (ValueE.AuthoritativeValue + k_ValueEChangePerSecond * Time.deltaTime) % 10;
        }
    }

    void OnGUI()
    {
        Vector3 scale = new Vector3 (Screen.width / 910f, Screen.height / 600f, 1.0f);
        GUI.matrix = Matrix4x4.TRS (new Vector3(0, 0, 0), Quaternion.identity, scale);
        if (NetworkManagerObject.IsListening)
        {
            GUILayout.BeginArea(new Rect(0, 0, 900, 72));
            GUILayout.Label("Anticipated Network Variable:");
            GUILayout.Label("Each pair of sliders represents a network variable's authoritative and anticipate values. Changing the top slider sends an RPC to the server, which updates the bottom slider. The top slider shows the current 'anticipated' value, including any smoothing, while the bottom represents the authoritative value.");
            GUILayout.EndArea();
            GUILayout.BeginArea(new Rect(0, 72, 300, 300));

            GUILayout.BeginVertical("Box");
            GUILayout.Label("Value A (snap, correct anticipation):");
            var updatedValue = GUILayout.HorizontalSlider(ValueA.Value, 0, 10);
            if (updatedValue != ValueA.Value)
            {
                ValueA.Anticipate(updatedValue);
                SetValueARpc(updatedValue);
            }
            GUILayout.Label("Value A Current Server Value:");
            GUILayout.HorizontalSlider(ValueA.AuthoritativeValue, 0, 10);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("Box");
            GUILayout.Label("Value B (snap, incorrect anticipation):");
            updatedValue = GUILayout.HorizontalSlider(ValueB.Value, 0, 10);
            if (updatedValue != ValueB.Value)
            {
                ValueB.Anticipate(updatedValue);
                SetValueBRpc(updatedValue);
            }
            GUILayout.Label("Value B Current Server Value:");
            GUILayout.HorizontalSlider(ValueB.AuthoritativeValue, 0, 10);
            GUILayout.EndVertical();

            GUILayout.EndArea();
            GUILayout.BeginArea(new Rect(305, 72, 300, 300));

            GUILayout.BeginVertical("Box");
            GUILayout.Label("Value C (smooth, correct anticipation):");
            updatedValue = GUILayout.HorizontalSlider(ValueC.Value, 0, 10);
            if (updatedValue != ValueC.Value)
            {
                ValueC.Anticipate(updatedValue);
                SetValueCRpc(updatedValue);
            }
            GUILayout.Label("Value C Current Server Value:");
            GUILayout.HorizontalSlider(ValueC.AuthoritativeValue, 0, 10);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("Box");
            GUILayout.Label("Value D (smooth, incorrect anticipation):");
            updatedValue = GUILayout.HorizontalSlider(ValueD.Value, 0, 10);
            if (updatedValue != ValueD.Value)
            {
                ValueD.Anticipate(updatedValue);
                SetValueDRpc(updatedValue);
            }
            GUILayout.Label("Value D Current Server Value:");
            GUILayout.HorizontalSlider(ValueD.AuthoritativeValue, 0, 10);
            GUILayout.EndVertical();

            GUILayout.EndArea();
            GUILayout.BeginArea(new Rect(610, 72, 300, 300));

            GUILayout.BeginVertical("Box");
            GUILayout.Label("Value E (Server-controlled, continuous anticipation):");
            GUILayout.HorizontalSlider(ValueE.Value, 0, 10);
            GUILayout.Label("Value E Current Server Value:");
            GUILayout.HorizontalSlider(ValueE.AuthoritativeValue, 0, 10);
            GUILayout.EndVertical();

            GUILayout.EndArea();
            if(IsClient)
            {
                GUILayout.BeginArea(new Rect(0, 310, 600, 300));
                GUILayout.Label("Anticipated Network Transform controls:");
                GUILayout.Label("W: Move Forward | S: Move Backward | A: Turn Left | D: Turn Right");
                GUILayout.Label("Q: Large random teleport (very different server result)");
                GUILayout.Label("E: Small random teleport (slightly different server result)");
                GUILayout.Label("R: Return to center (same server result)");
                GUILayout.Label("");

                GUILayout.Label($"Transform smooth duration: {Player.SmoothTime}s");
                Player.SmoothTime = GUILayout.HorizontalSlider(Player.SmoothTime, 0, 1);
                GUILayout.Label($"Transform smooth distance threshold: {Player.SmoothDistance}");
                Player.SmoothDistance = GUILayout.HorizontalSlider(Player.SmoothDistance, 0, 50);
                if (GUILayout.Button("Toggle Server Visualization (Follower)"))
                {
                    foreach (var childRenderer in Player.GhostTrasform.GetComponentsInChildren<MeshRenderer>())
                    {
                        childRenderer.enabled = !childRenderer.enabled;
                    }
                }
                GUILayout.EndArea();
            }
        }
        else
        {
            GUILayout.BeginArea(new Rect(0, 0, 300, 600));

            if (!NetworkManagerObject.IsListening){
                if (GUILayout.Button("Start Server"))
                {
                    NetworkManagerObject.StartServer();
                }
                if (GUILayout.Button("Start Client"))
                {
                    var unityTransport = NetworkManagerObject.NetworkConfig.NetworkTransport as UnityTransport;
                    unityTransport.SetDebugSimulatorParameters(100, 0, 0);
                    NetworkManagerObject.StartClient();
                }
            }
            GUILayout.EndArea();
        }
    }
}
