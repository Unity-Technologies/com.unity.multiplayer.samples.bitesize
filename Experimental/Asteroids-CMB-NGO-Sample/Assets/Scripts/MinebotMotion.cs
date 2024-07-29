using Unity.Netcode;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
/// <summary>
/// The custom editor for the <see cref="MinebotMotion"/> component.
/// </summary>
[CustomEditor(typeof(MinebotMotion), true)]
public class MinebotMotionEditor : PhysicsObjectMotionEditor
{
    private SerializedProperty m_MaxDistanceFromCenter;
    private SerializedProperty m_KeepWithinDistanceFromCenter;
    private SerializedProperty m_ReleaseVelocity;
    private SerializedProperty m_MinMaxReleaseTorque;

    public override void OnEnable()
    {
        m_MaxDistanceFromCenter = serializedObject.FindProperty(nameof(MinebotMotion.MaxDistanceFromCenter));
        m_KeepWithinDistanceFromCenter = serializedObject.FindProperty(nameof(MinebotMotion.KeepWithinDistanceFromCenter));
        m_ReleaseVelocity = serializedObject.FindProperty(nameof(MinebotMotion.ReleaseVelocity));
        m_MinMaxReleaseTorque = serializedObject.FindProperty(nameof(MinebotMotion.MinMaxReleaseTorque));
        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        var minebotMotion = target as MinebotMotion;
        minebotMotion.MinebotMotionrPropertiesVisible = EditorGUILayout.BeginFoldoutHeaderGroup(minebotMotion.MinebotMotionrPropertiesVisible, $"{nameof(MinebotMotion)} Properties");
        if (minebotMotion.MinebotMotionrPropertiesVisible)
        {
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.PropertyField(m_MaxDistanceFromCenter);
            EditorGUILayout.PropertyField(m_KeepWithinDistanceFromCenter);
            EditorGUILayout.PropertyField(m_ReleaseVelocity);
            EditorGUILayout.PropertyField(m_MinMaxReleaseTorque);
        }
        else
        {
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        
        EditorGUILayout.Space();
        base.OnInspectorGUI();
    }
}
#endif


public class MinebotMotion : PhysicsObjectMotion
{
#if UNITY_EDITOR
    public bool MinebotMotionrPropertiesVisible = false;
#endif

    public float MaxDistanceFromCenter = 1024.0f;
    public bool KeepWithinDistanceFromCenter;

    [Tooltip("The velocity applied, plus the parent's velocity, when a mine is released.")]
    public float ReleaseVelocity = 30.0f;

    [Tooltip("The impulse force applied when a mine is released.")]
    public MinMaxVector2Physics MinMaxReleaseTorque = new MinMaxVector2Physics(5.0f, 10.0f);

    public GameObject MineHeldVisual;
    private Color m_OriginalMeshColor;
    private float m_LastDistanceCheck = 0.0f;
    private MeshRenderer m_Renderer;

    protected override void Awake()
    {
        base.Awake();
        m_Renderer = GetComponent<MeshRenderer>();
        m_OriginalMeshColor = m_Renderer.material.color;
        MineHeldVisual.SetActive(false);
    }

    public void ReleaseMine(Vector3 direction, Vector3 velocity)
    {
        if (!HasAuthority)
        {
            return;
        }

        NetworkObject.SetOwnershipLock(false);

        var finalVelocity = (direction * ReleaseVelocity * 2) + (velocity.magnitude * direction);
        UpdateVelocity(finalVelocity);
        UpdateTorque(GetRandomVector3(MinMaxReleaseTorque, Vector3.one, true));
    }

    private BaseObjectMotionHandler m_PreviousParent;
    public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
    {
        base.OnNetworkObjectParentChanged(parentNetworkObject);
        if (!IsSpawned)
        {
            return;
        }

        if (parentNetworkObject != null)
        {
            m_PreviousParent = parentNetworkObject.GetComponent<BaseObjectMotionHandler>();
            MineHeldVisual.SetActive(true);
            m_Renderer.material.color = PlayerColor.GetPlayerColor(OwnerClientId);
        }
        else
        {
            MineHeldVisual.SetActive(false);
            m_Renderer.material.color = m_OriginalMeshColor;
            if (HasAuthority)
            {
                NetworkRigidbody.transform.forward = m_PreviousParent.transform.forward;
                NetworkRigidbody.SetRotation(m_PreviousParent.NetworkRigidbody.GetRotation());
                ReleaseMine(m_PreviousParent.transform.forward, m_PreviousParent.GetObjectVelocity());
                // Unlock the ownership of the mine when releasing it.
                NetworkObject.SetOwnershipLock(false);
                IgnoreCollision(gameObject, m_PreviousParent.gameObject, false);
            }
        }
    }

    public override void OnUpdate()
    {
        base.OnUpdate();

        if (!IsSpawned || !CanCommitToTransform || !KeepWithinDistanceFromCenter)
        {
            return;
        }

        if (transform.position.magnitude >= MaxDistanceFromCenter && m_LastDistanceCheck < Time.realtimeSinceStartup)
        {
            m_LastDistanceCheck = Time.realtimeSinceStartup + 1.0f;
            var velocity = GetObjectVelocity();
            velocity.y = 0;
            SetObjectVelocity(-1 * velocity);
        }
    }

    // TODO: We might just remove this since the boundary pushes things away from it now
    protected override bool OnBoundaryReached()
    {
        // Allow the boundary warp to opposite side only if we are not
        // being carried around by a ship. Otherwise, the ship will 
        // warp and bring this mine with it.
       
        return transform.parent == null;
    }
}
