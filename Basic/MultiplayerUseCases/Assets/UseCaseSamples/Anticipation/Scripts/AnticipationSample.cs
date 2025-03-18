using Unity.Collections;
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;
using Unity.Netcode.Samples.MultiplayerUseCases.Common;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace Unity.Netcode.Samples.MultiplayerUseCases.Anticipation
{
    public class AnticipationSample : NetworkBehaviour
    {
        const float k_ValueEChangePerSecond = 2.5f;

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

        NetworkManager NetworkManagerObject => NetworkManager.Singleton;
        PlayerMovableObject Player
        {
            get
            {
                if (m_Player == null)
                {
                    m_Player = FindFirstObjectByType<PlayerMovableObject>();
                }
                return m_Player;
            }
        }
        PlayerMovableObject m_Player;

        [SerializeField]
        UIDocument m_UIDocument;
        VisualElement m_UIRoot;
        Button m_ToggleServerVisualizationButton;
        Button m_ApplyLatencyAndJitterButton;
        Slider m_TransformSmoothDurationSlider;
        Slider m_TransformSmoothDistanceThresholdSlider;
        Slider m_VariableSmoothDurationSlider;
        SliderInt m_JitterSlider;
        SliderInt m_LatencySlider;
        Slider m_ValueASlider;
        Label m_ValueAValuesLabel;
        Slider m_ValueBSlider;
        Label m_ValueBValuesLabel;
        Slider m_ValueCSlider;
        Label m_ValueCValuesLabel;
        Slider m_ValueDSlider;
        Label m_ValueDValuesLabel;
        Label m_ValueEValuesLabel;

        [SerializeField]
        NetworkSimulator networkSimulator;
        int m_Latency = 200;
        int m_Jitter = 25;
        float m_SmoothTime = 0.25f;
        bool m_Restart = false;

        void Awake()
        {
            NetworkManagerObject.OnClientStarted += OnClientStarted;
            NetworkManagerObject.OnServerStarted += OnServerStarted;
            if (!NetworkManagerObject.IsListening && !m_Restart)
            {
                SetDebugSimulatorParameters(m_Latency, m_Jitter, 0);
            }
        }

        void InitializeUI()
        {
            m_UIDocument.gameObject.SetActive(true);
            m_UIRoot = m_UIDocument.rootVisualElement;
            m_ToggleServerVisualizationButton = UIElementsUtils.SetupButton("ToggleVisualizationButton", OnClickToggleVisualization, true, m_UIRoot, "Toggle Server Visualization (Follower)");
            m_ApplyLatencyAndJitterButton = UIElementsUtils.SetupButton("ApplyButton", OnClickApplyLatencyAndJitter, true, m_UIRoot, "Apply");
            m_JitterSlider = UIElementsUtils.SetupIntSlider("JitterSlider", 0, 50, m_Jitter, 1, OnJitterChanged, m_UIRoot);
            RefreshJitterLabel();
            m_LatencySlider = UIElementsUtils.SetupIntSlider("LatencySlider", 0, 300, m_Latency, 1, OnLatencyChanged, m_UIRoot);
            RefreshLatencyLabel();
            m_TransformSmoothDurationSlider = UIElementsUtils.SetupFloatSlider("DurationSlider", 0, 1, 0, 0.1f, OnPlayerSmoothDurationChanged, m_UIRoot);
            m_TransformSmoothDistanceThresholdSlider = UIElementsUtils.SetupFloatSlider("DistanceThresholdSlider", 0, 50, 0, 0.5f, OnPlayerSmoothDistanceThresholdChanged, m_UIRoot);
            m_VariableSmoothDurationSlider = UIElementsUtils.SetupFloatSlider("VariableSmoothDurationSlider", 0, 1, m_SmoothTime, 0.1f, OnVariableSmoothDurationChanged, m_UIRoot);
            RefreshVariableSmoothDurationLabel();
            m_ValueASlider = UIElementsUtils.SetupFloatSlider("ValueASlider", 0, 10, ValueA.Value, 0.1f, OnValueAChanged, m_UIRoot);
            m_ValueAValuesLabel = m_UIRoot.Query<Label>("ValueAValuesLabel");
            m_ValueBSlider = UIElementsUtils.SetupFloatSlider("ValueBSlider", 0, 10, ValueB.Value, 0.1f, OnValueBChanged, m_UIRoot);
            m_ValueBValuesLabel = m_UIRoot.Query<Label>("ValueBValuesLabel");
            m_ValueCSlider = UIElementsUtils.SetupFloatSlider("ValueCSlider", 0, 10, ValueC.Value, 0.1f, OnValueCChanged, m_UIRoot);
            m_ValueCValuesLabel = m_UIRoot.Query<Label>("ValueCValuesLabel");
            m_ValueDSlider = UIElementsUtils.SetupFloatSlider("ValueDSlider", 0, 10, ValueD.Value, 0.1f, OnValueDChanged, m_UIRoot);
            m_ValueDValuesLabel = m_UIRoot.Query<Label>("ValueDValuesLabel");
            m_ValueEValuesLabel = m_UIRoot.Query<Label>("ValueEValuesLabel");
        }

        void OnClientStarted()
        {
            if (IsServer) //Hosts already initialize the UI in OnServerStarted
            {
                return;
            }
            InitializeUI();
        }

        void OnServerStarted()
        {
            InitializeUI();
        }

        void OnClickToggleVisualization()
        {
            if (!IsClient)
            {
                return;
            }
            foreach (var childRenderer in Player.GhostTrasform.GetComponentsInChildren<MeshRenderer>())
            {
                childRenderer.enabled = !childRenderer.enabled;
            }
        }

        void OnClickApplyLatencyAndJitter()
        {
            if (!IsClient)
            {
                return;
            }
            m_Restart = true;
            NetworkManagerObject.Shutdown();
        }

        void OnJitterChanged(ChangeEvent<int> evt)
        {
            m_Jitter = evt.newValue;
            RefreshJitterLabel();
        }

        void RefreshJitterLabel()
        {
            m_JitterSlider.label = $"Jitter: {m_Jitter}ms";
        }

        void OnLatencyChanged(ChangeEvent<int> evt)
        {
            m_Latency = evt.newValue;
            RefreshLatencyLabel();
        }

        void RefreshLatencyLabel()
        {
            m_LatencySlider.label = $"Latency: {m_Latency}ms";
        }

        void OnPlayerSmoothDurationChanged(ChangeEvent<float> evt)
        {
            Player.SmoothTime = evt.newValue;
        }

        void OnPlayerSmoothDistanceThresholdChanged(ChangeEvent<float> evt)
        {
            Player.SmoothDistance = evt.newValue;
        }

        void OnVariableSmoothDurationChanged(ChangeEvent<float> evt)
        {
            m_SmoothTime = evt.newValue;
            RefreshVariableSmoothDurationLabel();
        }

        void RefreshVariableSmoothDurationLabel()
        {
            m_VariableSmoothDurationSlider.label = $"Variable smooth duration: {m_SmoothTime}s";
        }

        void OnValueAChanged(ChangeEvent<float> evt)
        {
            if (evt.newValue != ValueA.Value)
            {
                ValueA.Anticipate(evt.newValue);
                SetValueARpc(evt.newValue);
            }
        }

        void OnValueBChanged(ChangeEvent<float> evt)
        {
            if (evt.newValue != ValueB.Value)
            {
                ValueB.Anticipate(evt.newValue);
                SetValueBRpc(evt.newValue);
            }
        }

        void OnValueCChanged(ChangeEvent<float> evt)
        {
            if (evt.newValue != ValueC.Value)
            {
                ValueC.Anticipate(evt.newValue);
                SetValueCRpc(evt.newValue);
            }
        }

        void OnValueDChanged(ChangeEvent<float> evt)
        {
            if (evt.newValue != ValueD.Value)
            {
                ValueD.Anticipate(evt.newValue);
                SetValueDRpc(evt.newValue);
            }
        }

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
            ValueC.Smooth(previousValue, value, m_SmoothTime, Mathf.Lerp);
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
                
        public override void OnReanticipate(double lastRoundTripTime)
        {
            // Initialize the reanticipation for all of the values:
            // C and D react to a request to reanticipate by simply smoothing between the previous anticipated value
            // and the new authoritative value. They are not frequently updated and only need any reanticipation action
            // when the anticipation was wrong.

            if (ValueC.ShouldReanticipate)
            {
                ValueC.Smooth(ValueC.PreviousAnticipatedValue, ValueC.AuthoritativeValue, m_SmoothTime, Mathf.Lerp);
            }
            if (ValueD.ShouldReanticipate)
            {
                ValueD.Smooth(ValueD.PreviousAnticipatedValue, ValueD.AuthoritativeValue, m_SmoothTime, Mathf.Lerp);
            }

            // E is actually trying to anticipate the current value of a constantly changing object to hide latency.
            // It uses the amount of time that has passed since the authoritativetime to gauge the latency of this update
            // and anticipates a new value based on that delay. The server value is in the past, so the predicted value
            // attempts to guess what the value is in the present.
            if (ValueE.ShouldReanticipate)
            {
                // There is an important distinction between the smoothing this is doing and the smoothing the player object
                // is doing:
                // For the player object, it is replaying everything that has happened over a full round trip, so it has to
                // account for the entire difference between the current time and the authoritative time.
                // For this variable, we are only extrapolating over the time that has passed since the server sent us this
                // value - the difference between current time and authoritativeTime represents a full round trip, but the
                // actual time difference here is only a half round trip, so we multiply by 0.5.
                // Then, because smoothing adds its own latency, we add the smooth time into the mix.
                var secondsBehind = lastRoundTripTime * 0.5f + m_SmoothTime;

                var newAnticipatedValue = (float)(ValueE.AuthoritativeValue + k_ValueEChangePerSecond * secondsBehind) % 10;

                // This variable uses a custom interpolation callback that handles the drop from 10
                // down to 0. Without this, there is either weird smoothing behavior, or hitching.
                // This keeps the interpolation going, and handles the case where the interpolated value
                // goes over 10 and has to jump back to 0.
                ValueE.Smooth(ValueE.PreviousAnticipatedValue, newAnticipatedValue, m_SmoothTime, ((start, end, amount) =>
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

        void Update()
        {
            if (m_Restart && !NetworkManagerObject.IsListening && !NetworkManagerObject.ShutdownInProgress)
            {
                SetDebugSimulatorParameters(m_Latency, m_Jitter, 0);
                NetworkManagerObject.StartClient();
                m_Restart = false;
            }
            if (IsServer)
            {
                ValueE.AuthoritativeValue = (ValueE.AuthoritativeValue + k_ValueEChangePerSecond * Time.deltaTime) % 10;
            }
            if (NetworkManagerObject.IsListening)
            {
                m_ValueAValuesLabel.text = $"Client value: '{ValueA.Value}' | Server value: '{ValueA.AuthoritativeValue}'";
                m_ValueBValuesLabel.text = $"Client value: '{ValueB.Value}' | Server value: '{ValueB.AuthoritativeValue}'";
                m_ValueCValuesLabel.text = $"Client value: '{ValueC.Value}' | Server value: '{ValueC.AuthoritativeValue}'";
                m_ValueDValuesLabel.text = $"Client value: '{ValueD.Value}' | Server value: '{ValueD.AuthoritativeValue}'";
                m_ValueEValuesLabel.text = $"Client value: '{ValueE.Value}' | Server value: '{ValueE.AuthoritativeValue}'";
                if (IsClient)
                {
                    m_TransformSmoothDurationSlider.label = $"Transform smooth duration: {Player.SmoothTime}s";
                    m_TransformSmoothDistanceThresholdSlider.label = $"Transform smooth distance threshold: {Player.SmoothDistance}";
                }
            }
        }

        void SetDebugSimulatorParameters(int latency, int jitter, int dropRate)
        {
            INetworkSimulatorPreset preset = networkSimulator.CurrentPreset;
            preset.PacketDelayMs = m_Latency;
            preset.PacketJitterMs = m_Jitter;
            networkSimulator.ChangeConnectionPreset(preset);
        }
    }
}
