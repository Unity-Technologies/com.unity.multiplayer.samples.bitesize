using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerSpawnHub : NetworkBehaviour
{
    [Tooltip("The prefab to spawn. This should be the same prefab that is applied on the BallPoolSystem.")]
    public GameObject PrefabSpawnObject;

    [Tooltip("The maximum number of spawned objects that a client can own. As more clients join, the objects should be distributed and total number of spawned objects increases.")]
    [Range(1, 500)]
    public int ObjectsPerPlayer = 250;

    [Tooltip("The tick frequency of continually generating an implosion/attractor force using the [ALT]+[Right-MouseButton] combo.")]
    [Range(1, 15)]
    public int ImplosionTickSpacing = 2;

    [Tooltip("How fast the invisible player spawner can rotate (clockwise or counter clockwise) around the center of the world space.")]
    [Range(0.1f, 10.0f)]
    public float MoveSpeed = 1.0f;

    [Tooltip("The force applied to a newly spawned ball.")]
    [Range(0.1f, 100.0f)]
    public float LaunchForce = 20.0f;

    [Tooltip("The force applied to all spawned objects within the ExplodeRadius.")]
    [Range(0.1f, 300.0f)]
    public float ExplodeForce = 100.0f;

    [Tooltip("The percentage of the inverse explosion force to apply for implosions.")]
    [Range(0.01f, 1.0f)]
    public float ImplosionForceScale = 0.75f;

    [Tooltip("The radius that any spawned objects must be within to have any implosion/explosion force applied.")]
    [Range(0.1f, 1000.0f)]
    public float ExplodeRadius = 100.0f;


    private ObjectPoolSystem m_ObjectPool;
    private TagHandle m_FloorTag;

    private void Awake()
    {
        m_FloorTag = TagHandle.GetExistingTag("Floor");
    }

    public override void OnNetworkSpawn()
    {
        if (HasAuthority)
        {
            Camera.main.transform.SetParent(transform, false);
        }

        if (NetworkObject.IsPlayerObject)
        {
            gameObject.name = $"Player-{OwnerClientId}";
        }

        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        if (HasAuthority)
        {
            Camera.main.transform.SetParent(null, false);
        }

        base.OnNetworkDespawn();
    }

    protected override void OnNetworkPostSpawn()
    {
        m_ObjectPool = ObjectPoolSystem.GetPoolSystem(PrefabSpawnObject);
        base.OnNetworkPostSpawn();
    }

    private float m_Accelerate = 0.0f;
    private float m_AppliedAcceleration = 0.0f;

    private int m_LastTick;
    private int m_NextImplosionTick;

    private enum MotionModes
    {
        Manual,
        AutoLeft,
        AutoRight,
    }

    private MotionModes m_MotionMode;

    private bool m_NoFocusLastFrame;

    private void Update()
    {
        if (!IsSpawned || !HasAuthority)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            NetworkManagerHelper.Instance.ToggleNetStatsMonitor();
        }

        var hitShift = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        if (Input.GetMouseButton(1) && m_LastTick != NetworkManager.ServerTime.Tick)
        {
            var ownedObjectCount = NetworkManager.LocalClient.OwnedObjects.Length;
            if (ownedObjectCount <= ObjectsPerPlayer)
            {
                var instance = m_ObjectPool.GetInstance();
                instance.transform.position = transform.position;
                instance.transform.forward = transform.forward;
                var rigidBody = instance.GetComponent<Rigidbody>();
                instance.Spawn();
                if (rigidBody != null)
                {
                    rigidBody.AddForce((transform.forward * LaunchForce) + (Vector3.up * LaunchForce * 0.75f), ForceMode.Impulse);
                }
            }
        }
        else
        {
            var explode = Input.GetMouseButtonDown(0);
            var implode = Input.GetKey(KeyCode.Space) && explode;
            var followImplode = hitShift && Input.GetMouseButton(0) && (m_NextImplosionTick <= NetworkManager.ServerTime.Tick);

            if (!m_NoFocusLastFrame && (explode || implode || followImplode))
            {
                if (followImplode)
                {
                    m_NextImplosionTick = NetworkManager.ServerTime.Tick + ImplosionTickSpacing;
                    implode = true;
                }

                var selectedPoint = Vector3.zero;

                if (MouseSelectObject.SelectPoint<Collider>(out selectedPoint, m_FloorTag))
                {
                    var screenPoint = Camera.main.WorldToScreenPoint(selectedPoint);
                    var forceGroundZero = (implode || followImplode) ? Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z)) : selectedPoint;
                    if (!StressTestState.Instance.ForceModeOwnersOnly())
                    {
                        var baseTarget = NetworkManager.CMBServiceConnection ? RpcTarget.NotServer : RpcTarget.Everyone;
                        ApplyExplodeImplodeForceRpc(new HalfVector4(forceGroundZero.x, forceGroundZero.y, forceGroundZero.z, implode ? -ImplosionForceScale : 1.0f), baseTarget);
                    }
                    else
                    {
                        ApplyForce(new HalfVector4(forceGroundZero.x, forceGroundZero.y, forceGroundZero.z, implode ? -ImplosionForceScale : 1.0f));
                    }
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Backspace) && m_MotionMode != MotionModes.Manual)
        {
            m_MotionMode = MotionModes.Manual;
        }

        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) || m_MotionMode == MotionModes.AutoLeft)
        {
            m_MotionMode = hitShift ? MotionModes.AutoLeft : m_MotionMode;
            Accelerate(1.0f);
        }
        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) || m_MotionMode == MotionModes.AutoRight)
        {
            m_MotionMode = hitShift ? MotionModes.AutoRight : m_MotionMode;
            Accelerate(-1.0f);
        }
        else
        {
            Decelerate();
        }
        transform.LookAt(Vector3.zero);
        m_LastTick = NetworkManager.ServerTime.Tick;
        m_NoFocusLastFrame = !Application.isFocused;
    }

    private void ApplyForce(HalfVector4 explosionPositionForce)
    {
        var positionForce = explosionPositionForce.ToVector4();
        var ownedObjects = NetworkManager.LocalClient.OwnedObjects;
        foreach (var networkObject in ownedObjects)
        {
            var targetBody = networkObject.GetComponent<Rigidbody>();
            if (targetBody)
            {
                var force = Random.Range(ExplodeForce * 0.80f, ExplodeForce * 1.10f) * positionForce.w;
                var upward = Random.Range(ExplodeForce * 0.5f, ExplodeForce * 0.75f);
                targetBody.AddExplosionForce(force, positionForce, ExplodeRadius, upward, ForceMode.Impulse);
            }
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void ApplyExplodeImplodeForceRpc(HalfVector4 explosionPositionForce, RpcParams rpcParams)
    {
        ApplyForce(explosionPositionForce);
    }

    private void Accelerate(float direction)
    {
        m_Accelerate = Mathf.Lerp(m_Accelerate, MoveSpeed, 0.001f);
        m_AppliedAcceleration = direction * m_Accelerate;
        transform.RotateAround(Vector3.zero, transform.up, m_AppliedAcceleration);
    }

    private void Decelerate()
    {
        m_Accelerate = Mathf.Lerp(m_Accelerate, 0.0f, 0.01f);
        m_AppliedAcceleration = Mathf.Sign(m_AppliedAcceleration) * m_Accelerate;
        transform.RotateAround(Vector3.zero, transform.up, m_AppliedAcceleration);
    }
}
