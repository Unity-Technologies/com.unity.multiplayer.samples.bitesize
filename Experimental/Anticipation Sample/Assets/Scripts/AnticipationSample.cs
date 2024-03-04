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
    ///
    /// Smoothing is applied every frame, while the Reanticipate callback is only called when data changes, so it is
    /// best to handle these kinds of situations via some logic in your game code rather than via jumps in the actual
    /// value in order to maintain the most consistent player experience.
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
        var previousValue = ValueC.AuthoritativeValue;
        ValueC.AuthoritativeValue = value;
        ValueC.Smooth(previousValue, value, SmoothTime, Mathf.Lerp);
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
            variable.Smooth(anticipatedValue, authoritativeValue, SmoothTime, Mathf.Lerp);
        };
        ValueC.OnReanticipate = smooth;
        ValueD.OnReanticipate = smooth;

        // E is actually trying to anticipate the current value of a constantly changing object to hide latency.
        // It uses the amount of time that has passed since the authoritativetime to gauge the latency of this update
        // and anticipates a new value based on that delay. The server value is in the past, so the predicted value
        // attempts to guess what the value is in the present.
        ValueE.OnReanticipate = (AnticipatedNetworkVariable<float> variable, in float anticipatedValue, double anticipationTime, in float authoritativeValue, double authoritativeTime) =>
        {
            // There is an important distinction between the smoothing this is doing and the smoothing the player object
            // is doing:
            // For the player object, it is replaying everything that has happened over a full round trip, so it has to
            // account for the entire difference between the current time and the authoritative time.
            // For this variable, we are only extrapolating over the time that has passed since the server sent us this
            // value - the difference between current time and authoritativeTime represents a full round trip, but the
            // actual time difference here is only a half round trip, so we multiply by 0.5.
            // Then, because smoothing adds its own latency, we add the smooth time into the mix.
            var secondsBehind = (NetworkManager.LocalTime.Time - authoritativeTime) * 0.5f + SmoothTime;

            var newAnticipatedValue = (float)(authoritativeValue + k_ValueEChangePerSecond * secondsBehind) % 10;

            // This variable uses a custom interpolation callback that handles the drop from 10
            // down to 0. Without this, there is either weird smoothing behavior, or hitching.
            // This keeps the interpolation going, and handles the case where the interpolated value
            // goes over 10 and has to jump back to 0.
            variable.Smooth(anticipatedValue, newAnticipatedValue, SmoothTime, ((start, end, amount) =>
            {
                if (end < 3 && start > 7)
                {
                    end += 10;
                }

                return Mathf.Lerp(start, end, amount) % 10;
            }));
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
        if (Restart && !NetworkManagerObject.IsListening && !NetworkManagerObject.ShutdownInProgress)
        {
            var unityTransport = NetworkManagerObject.NetworkConfig.NetworkTransport as UnityTransport;
            unityTransport.SetDebugSimulatorParameters(Latency, Jitter, 0);
            NetworkManagerObject.StartClient();
            Restart = false;
        }
        if (IsServer)
        {
            ValueE.AuthoritativeValue = (ValueE.AuthoritativeValue + k_ValueEChangePerSecond * Time.deltaTime) % 10;
        }
    }

    private int Latency = 200;
    private int Jitter = 25;
    private float SmoothTime = 0.25f;
    private bool Restart = false;

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

            if (IsClient)
            {
                GUILayout.Label("");
                GUILayout.Label($"Variable smooth duration: {SmoothTime}s");
                SmoothTime = GUILayout.HorizontalSlider(SmoothTime, 0, 1);
            }

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
                GUILayout.BeginArea(new Rect(610, 456, 300, 150));
                GUILayout.Label($"Latency: {Latency}ms");
                Latency = (int)GUILayout.HorizontalSlider(Latency, 0, 300);
                GUILayout.Label($"Jitter: {Jitter}ms");
                Jitter = (int)GUILayout.HorizontalSlider(Jitter, 0, 50);
                if (GUILayout.Button("Apply"))
                {
                    Restart = true;
                    NetworkManagerObject.Shutdown();
                }
                GUILayout.EndArea();
            }
        }
        else
        {
            GUILayout.BeginArea(new Rect(0, 0, 300, 600));

            if (!NetworkManagerObject.IsListening && !Restart){
                if (GUILayout.Button("Start Server"))
                {
                    var unityTransport = NetworkManagerObject.NetworkConfig.NetworkTransport as UnityTransport;
                    unityTransport.SetDebugSimulatorParameters(Latency, Jitter, 0);
                    NetworkManagerObject.StartServer();
                }
                if (GUILayout.Button("Start Client"))
                {
                    var unityTransport = NetworkManagerObject.NetworkConfig.NetworkTransport as UnityTransport;
                    unityTransport.SetDebugSimulatorParameters(Latency, Jitter, 0);
                    NetworkManagerObject.StartClient();
                }
            }
            GUILayout.EndArea();
        }
    }
}
