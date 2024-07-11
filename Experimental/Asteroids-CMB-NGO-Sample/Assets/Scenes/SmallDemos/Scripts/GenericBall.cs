#if MULTIPLAYER_TOOLS
using Unity.Multiplayer.Tools.NetStats;
using Unity.Multiplayer.Tools.NetStatsMonitor;
#endif
using Unity.Netcode.Components;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using Unity.Netcode.Editor;
/// <summary>
/// The custom editor for the <see cref="GenericBall"/> component.
/// </summary>
[CustomEditor(typeof(GenericBall), true)]
public class GenericBalltEditor : NetworkTransformEditor
{
    private SerializedProperty m_SpawnRadius;
    private SerializedProperty m_MaxAngularVelocity;
    private SerializedProperty m_MaxLinearVelocity;

    public override void OnEnable()
    {
        m_SpawnRadius = serializedObject.FindProperty(nameof(GenericBall.SpawnRadius));
        m_MaxAngularVelocity = serializedObject.FindProperty(nameof(GenericBall.MaxAngularVelocity));
        m_MaxLinearVelocity = serializedObject.FindProperty(nameof(GenericBall.MaxLinearVelocity));
        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("GenericBall", EditorStyles.boldLabel);
        {
            EditorGUILayout.PropertyField(m_SpawnRadius);
            EditorGUILayout.PropertyField(m_MaxAngularVelocity);
            EditorGUILayout.PropertyField(m_MaxLinearVelocity);
        }
        EditorGUILayout.Space();
        base.OnInspectorGUI();
    }
}
#endif
public class GenericBall : NetworkTransform
{
    [Tooltip("The spawn radius of this object relative to its current position.")]
    [Range(1.0f, 40.0f)]
    public float SpawnRadius = 10.0f;

    [Tooltip("Maximum angular velocity the Rigibody can achieve.")]
    [Range(0.01f, 10.0f)]
    public float MaxAngularVelocity = 3.0f;

    [Tooltip("Maximum linear velocity the Rigibody can achieve.")]
    [Range(0.01f, 100.0f)]
    public float MaxLinearVelocity = 50.0f;

    public NetworkRigidbody NetworkRigidbody { get; private set; }

#if MULTIPLAYER_TOOLS
    // User-defined metrics can be defined using the MetricTypeEnum attribute
    [MetricTypeEnum(DisplayName = "MoverMetrics")]
    private enum MoverMetrics
    {
        // Metadata for each user-defined metric can be defined using the MetricMetadata Attribute

        [MetricMetadata(Units = Units.Bytes, MetricKind = MetricKind.Counter)]
        Outbound,

        [MetricMetadata(Units = Units.Bytes, MetricKind = MetricKind.Counter)]
        Inbound,
    }

    private RuntimeNetStatsMonitor m_NetStatsMonitor;

    private void UpdateStats(MoverMetrics metricType, float value)
    {
        if (m_NetStatsMonitor == null || !m_NetStatsMonitor.gameObject.activeInHierarchy)
        {
            return;
        }

        m_NetStatsMonitor.AddCustomValue(MetricId.Create(metricType), value);
    }

    protected override void OnAuthorityPushTransformState(ref NetworkTransformState networkTransformState)
    {
        // Collect stats of the outbound NetworkTransform data
        UpdateStats(MoverMetrics.Outbound, (float)networkTransformState.LastSerializedSize);
        base.OnAuthorityPushTransformState(ref networkTransformState);
    }

    protected override void OnNetworkTransformStateUpdated(ref NetworkTransformState oldState, ref NetworkTransformState newState)
    {
        // Collect stats of the inbound NetworkTransform data
        UpdateStats(MoverMetrics.Inbound, (float)newState.LastSerializedSize);
        base.OnNetworkTransformStateUpdated(ref oldState, ref newState);
    }

    protected override void OnNetworkPostSpawn()
    {
        m_NetStatsMonitor = NetworkManagerHelper.Instance.NetStatsMonitor;
        base.OnNetworkPostSpawn();
    }
#endif

    public override void OnGainedOwnership()
    {
        base.OnGainedOwnership();

        var ownedObjectCount = NetworkManager.LocalClient.OwnedObjects.Length;
        var playerSpawnHub = NetworkManager.LocalClient.PlayerObject.GetComponent<PlayerSpawnHub>();

        // Keep object count down to the maximum 
        if (ownedObjectCount > (playerSpawnHub.ObjectsPerPlayer + 1))
        {
            // If we are over the maximum number of objects (plus the player), then despawn
            // anything else distributed to this client
            NetworkObject.Despawn();
        }
    }


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        NetworkRigidbody = GetComponent<NetworkRigidbody>();
        var rigidBody = GetComponent<Rigidbody>();
        rigidBody.maxAngularVelocity = MaxAngularVelocity;
        rigidBody.maxLinearVelocity = MaxLinearVelocity;
        if (CanCommitToTransform)
        {
            Random.InitState((int)System.DateTime.Now.Ticks);
            transform.position += new Vector3(Random.Range(-SpawnRadius, SpawnRadius), 0.0f, Random.Range(0, SpawnRadius));
            SetState(transform.position, null, null, false);
        }
    }

}
