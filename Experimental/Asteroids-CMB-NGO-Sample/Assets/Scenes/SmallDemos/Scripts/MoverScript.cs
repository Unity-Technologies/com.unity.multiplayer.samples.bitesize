using UnityEngine;
using Unity.Netcode.Components;
using System.Collections.Generic;
using Unity.Netcode;
#if UNITY_EDITOR
using Unity.Netcode.Editor;
using UnityEditor;

/// <summary>
/// The custom editor for the <see cref="MoverScript"/> component.
/// </summary>
[CustomEditor(typeof(MoverScript), true)]
public class MoverScriptEditor : NetworkTransformEditor
{
    private SerializedProperty m_Radius;
    private SerializedProperty m_Increment;
    private SerializedProperty m_InputMotion;
    private SerializedProperty m_RotateVelocity;
    private SerializedProperty m_LinearVelocity;
    private SerializedProperty m_MaxAngularVelocity;
    private SerializedProperty m_MaxLinearVelocity;    
    private SerializedProperty m_PickupOnCollison;
    private SerializedProperty m_CanBePickedUp;
    private SerializedProperty m_TransferOwnershipOnCollision;
    private SerializedProperty m_CanBeSelected;
    private SerializedProperty m_ThrowForce;
    
    public override void OnEnable()
    {
        m_Radius = serializedObject.FindProperty(nameof(MoverScript.Radius));
        m_Increment = serializedObject.FindProperty(nameof(MoverScript.Increment));
        m_InputMotion = serializedObject.FindProperty(nameof(MoverScript.InputMotion));
        m_RotateVelocity = serializedObject.FindProperty(nameof(MoverScript.RotateVelocity));
        m_LinearVelocity = serializedObject.FindProperty(nameof(MoverScript.LinearVelocity));
        m_MaxAngularVelocity = serializedObject.FindProperty(nameof(MoverScript.MaxAngularVelocity));
        m_MaxLinearVelocity = serializedObject.FindProperty(nameof(MoverScript.MaxLinearVelocity));
        m_PickupOnCollison = serializedObject.FindProperty(nameof(MoverScript.PickupOnCollison));
        m_CanBePickedUp = serializedObject.FindProperty(nameof(MoverScript.CanBePickedUp));
        m_TransferOwnershipOnCollision = serializedObject.FindProperty(nameof(MoverScript.TransferOwnershipOnCollision));
        m_CanBeSelected = serializedObject.FindProperty(nameof(MoverScript.CanBeSelected));
        m_ThrowForce = serializedObject.FindProperty(nameof(MoverScript.ThrowForce));

        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("Mover Properties", EditorStyles.boldLabel);
        {
            EditorGUILayout.PropertyField(m_Radius);
            EditorGUILayout.PropertyField(m_Increment);
            EditorGUILayout.PropertyField(m_InputMotion);
            EditorGUILayout.PropertyField(m_RotateVelocity);
            EditorGUILayout.PropertyField(m_LinearVelocity);
            EditorGUILayout.PropertyField(m_MaxAngularVelocity);
            EditorGUILayout.PropertyField(m_MaxLinearVelocity);
            EditorGUILayout.PropertyField(m_PickupOnCollison);
            EditorGUILayout.PropertyField(m_CanBePickedUp);
            EditorGUILayout.PropertyField(m_TransferOwnershipOnCollision);
            EditorGUILayout.PropertyField(m_CanBeSelected);
            EditorGUILayout.PropertyField(m_ThrowForce);
        }
        EditorGUILayout.Space();
        base.OnInspectorGUI();
    }
}
#endif
public class MoverScript : NetworkTransform
{
    [Range(1.0f, 40.0f)]
    public float Radius = 10.0f;

    [Range(0.001f, 10.0f)]
    public float Increment = 1.0f;

    [Tooltip("When enabled, user input will control the Rigidbody's linear and angular velocity.")]
    public bool InputMotion = false;

    [Tooltip("Amount of angular velocity to apply when controlling the object")]
    [Range(0.01f, 10.0f)]
    public float RotateVelocity = 1.0f;

    [Tooltip("Amount of linear velocity to apply when controlling the object")]
    [Range(0.01f, 100.0f)]
    public float LinearVelocity = 50.0f;

    [Tooltip("Maximum angular velocity the Rigibody can achieve.")]
    [Range(0.01f, 10.0f)]
    public float MaxAngularVelocity = 3.0f;
    private float m_MaxAngularVelocity;

    [Tooltip("Maximum linear velocity the Rigibody can achieve.")]
    [Range(0.01f, 100.0f)]
    public float MaxLinearVelocity = 50.0f;
    private float m_MaxLinearVelocity;

    [Tooltip("When set to true on a player, the player will attempt to pickup the object it is colliding with.")]
    public bool PickupOnCollison = false;

    [Tooltip("When set to true, a player can pickup the object.")]
    public bool CanBePickedUp = false;

    [Tooltip("When set to true, if a player collides with a transferrable object it will acquire ownership.")]
    public bool TransferOwnershipOnCollision = false;

    [Tooltip("When set to true, if a player triggers the transferrable object it will acquire ownership.")]
    public bool TransferOwnershipOnTrigger = false;

    [Tooltip("When set to true, the object can be selected by moving the mouse cursor over it and left clicking the button.")]
    public bool CanBeSelected;

    [Tooltip("The amount of force used to throw the object. At 100% it will be 5x this amount.")]
    [Range(1.0f, 500.0f)]
    public float ThrowForce = 150.0f;



    [HideInInspector]
    public GameObject SelectedObject;
    [HideInInspector]
    public GameObject SelectedNonOwnerObject;

    public GameObject HoldPosition;
    public GameObject HoldingText;
    public TextMesh m_TextMesh;


    public bool OnCollideNotifyRigidBody;

    private MoverScript m_ObjectBeingHeld;
    private Rigidbody m_Rigidbody;
    public NetworkRigidbody NetworkRigidbody { get; private set; }
    private Vector3 m_CameraOriginalPosition;
    private Quaternion m_CameraOriginalRotation;

    private float m_PauseCollisionDetection = 0.0f;

    /// <summary>
    /// Handles initializing the object when spawned
    /// </summary>
    public override void OnNetworkSpawn()
    {
        // Always invoked base when deriving from NetworkTransform
        base.OnNetworkSpawn();

        m_Rigidbody = GetComponent<Rigidbody>();
        NetworkRigidbody = GetComponent<NetworkRigidbody>();
        if (HoldingText)
        {
            m_TextMesh = HoldingText.GetComponent<TextMesh>();
        }

        if (CanCommitToTransform)
        {
            Random.InitState((int)System.DateTime.Now.Ticks);
            transform.position += new Vector3(Random.Range(-Radius, Radius), 0.0f, Random.Range(0, Radius));
            SetState(transform.position, null, null, false);
            if (IsLocalPlayer)
            {
                NetworkObject.DontDestroyWithOwner = false;
                m_CameraOriginalPosition = Camera.main.transform.position;
                m_CameraOriginalRotation = Camera.main.transform.rotation;
                Camera.main.transform.SetParent(transform, false);
                m_Rigidbody.maxAngularVelocity = 2.5f;
                m_Rigidbody.maxLinearVelocity = 15.0f;
            }
        }

        if (NetworkObject.IsPlayerObject)
        {
            gameObject.name = $"Player-{OwnerClientId}";
        }

        if (NetworkObject.IsOwnershipRequestRequired)
        {
            NetworkObject.OnOwnershipRequested = OnOwnershipRequested;
            NetworkObject.OnOwnershipRequestResponse = OwnershipRequestResponse;
        }

        NetworkObject.OnOwnershipPermissionsFailure = OnOwnershipPermissionsFailure;

        if (CanBeSelected)
        {
            SelectedObject?.SetActive(false);

            SelectedNonOwnerObject?.SetActive(false);
        }
        if (ObjectSpawner.Instance != null)
        {
            ObjectSpawner.Instance.RegisterMoverScript(this);
        }
    }



    /// <summary>
    /// Notify if changing ownership failed due to permissions
    /// </summary>
    /// <param name="status"></param>
    private void OnOwnershipPermissionsFailure(NetworkObject.OwnershipPermissionsFailureStatus status)
    {
        NetworkManagerHelper.Instance.LogMessage($"[{name}][Ownership Permissions] Failed to change ownership because: {status}");
        m_PauseCollisionDetection = Time.realtimeSinceStartup + 2.0f;
    }

    /// <summary>
    /// Handle processing an ownership request
    /// </summary>
    /// <param name="requestingClient">the client requesting ownership</param>
    /// <returns></returns>
    private bool OnOwnershipRequested(ulong requestingClient)
    {
        return ObjectSpawner.Instance.ApproveRequest;
    }

    /// <summary>
    /// Notify if the ownership request was approved or denied or there was an in-flight permissions update 
    /// that prevented the request.
    /// </summary>
    /// <param name="ownershipRequestResponse">the request response status</param>
    private void OwnershipRequestResponse(NetworkObject.OwnershipRequestResponseStatus ownershipRequestResponse)
    {
        NetworkManagerHelper.Instance.LogMessage($"{name} ownership request response: {ownershipRequestResponse}");
    }

    public override void OnNetworkDespawn()
    {
        if (IsLocalPlayer)
        {
            Camera.main.transform.SetParent(null, false);
            Camera.main.transform.position = m_CameraOriginalPosition;
            Camera.main.transform.rotation = m_CameraOriginalRotation;
        }
        base.OnNetworkDespawn();
    }

    /// <summary>
    /// Using a trigger that is slightly bigger than the collider
    /// can help prevent the initial collision into a kinematic body.
    /// </summary>
    /// <remarks>
    /// Non-player object detects the trigger and if the collider
    /// causing the trigger is a local player and the non-player 
    /// object is not already owned, then have the local player
    /// object gain ownership.
    /// </remarks>
    private void OnTriggerEnter(Collider other)
    {
        if (!IsSpawned || IsLocalPlayer || TransferOwnershipOnCollision)
        {
            return;
        }

        var moverScript = other.transform.root.gameObject.GetComponent<MoverScript>();
        if (moverScript == null)
        {
            return;
        }

        if (OwnerClientId != moverScript.OwnerClientId && moverScript.IsLocalPlayer)
        {
            moverScript.ChangeOwnershipOnTrigger(this);
        }
    }

    internal void ChangeOwnershipOnTrigger(MoverScript objectTriggered)
    {
        if (!IsLocalPlayer)
        {
            return;
        }
        objectTriggered.NetworkObject.ChangeOwnership(OwnerClientId);
    }

    /// <summary>
    /// Transfer ownership on collision:
    /// - This demo uses the <see cref="OnOwnershipPermissionsFailure"/> callback to notify if you can transfer ownership or not
    /// - Since this is a multi-use script, additioanl properties are used to determine if the object can be picked up
    /// - If the object can be picked up, then change ownership locally
    /// 
    /// Picking up a Rigidbody when colliding/touching it:
    /// - This demo focuses on transfer of ownership (i.e. the object becomes non-kinematic)
    /// - If the check to acquire ownership and this instance has authority, then:
    ///   - Try setting the parent
    ///   - If the parent was set, then lock ownership while the object is held
    /// </summary>    
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsSpawned || !IsLocalPlayer || TransferOwnershipOnTrigger)
        {
            return;
        }

        // Spam prevention
        if (m_PauseCollisionDetection > Time.realtimeSinceStartup)
        {
            return;
        }

        var moverScript = collision.gameObject.GetComponent<MoverScript>();
        if (moverScript == null)
        {
            return;
        }

        if (OwnerClientId != moverScript.OwnerClientId && moverScript.TransferOwnershipOnCollision)
        {
            moverScript.NetworkObject.ChangeOwnership(OwnerClientId);
        }

        if (PickupOnCollison && !m_ObjectBeingHeld && moverScript.HasAuthority && moverScript.CanBePickedUp)
        {
            if (Input.GetKey(KeyCode.Space))
            {
                m_ObjectBeingHeld = moverScript;
                HoldingText?.SetActive(true);
                moverScript.NetworkObject.SetOwnershipLock(true);
                var meshRenderer = GetComponent<MeshRenderer>();
                var color = meshRenderer.material.color;
                color.a = 0.35f;
                meshRenderer.material.color = color;

                if (HoldPosition != null)
                {
                    if (moverScript.NetworkRigidbody != null && NetworkRigidbody != null)
                    {
                        // Attach the sphere to the player
                        if (!moverScript.NetworkRigidbody.AttachToFixedJoint(NetworkRigidbody, HoldPosition.transform.position))
                        {
                            Debug.LogError($"Failed to created {nameof(FixedJoint)}!");
                            HoldingText?.SetActive(false);
                            moverScript.NetworkObject.SetOwnershipLock(false);
                            m_ObjectBeingHeld = null;
                            color.a = 1.0f;
                            meshRenderer.material.color = meshRenderer.material.color = color;
                        }
                    }
                    else
                    {
                        Debug.LogError($"Could not find {nameof(NetworkRigidbody)}!");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Handles moving the Rigidbody if <see cref="InputMotion"/> is set or if 
    /// there is a drop force (throw) that needs to be applied. 
    /// </summary>
    private void FixedUpdate()
    {
        if (!IsSpawned || !CanCommitToTransform)
        {
            return;
        }

        if (InputMotion)
        {
            var hasMotion = false;
            var hasRotation = false;
            var motion = transform.forward;
            float rotate = 0.0f;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                hasMotion = true;
            }
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                hasMotion = true;
                motion *= -1.0f;
            }
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                hasRotation = true;
                rotate = -1.0f;
            }
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                hasRotation = true;
                rotate = 1.0f;
            }

            if (hasRotation)
            {
                m_Rigidbody.angularVelocity += Vector3.up * rotate * RotateVelocity;
            }
            if (hasMotion)
            {
                motion *= LinearVelocity;
                motion.y = 0.0f;
#if UNITY_2023_3_OR_NEWER
                m_Rigidbody.linearVelocity += motion;
#else
                m_Rigidbody.velocity += motion;
#endif
            }

        }
        if (m_AddThrowForce)
        {
            m_Rigidbody.AddForce(m_DropForce, ForceMode.Impulse);
            m_DropForce = Vector3.Lerp(m_DropForce, Vector3.zero, 0.10f);

            if (m_DropForce.magnitude < 0.001f)
            {
                m_DropForce = Vector3.zero;
                m_AddThrowForce = false;
            }
        }
    }

    private bool m_ThrowingBall;
    private float m_ThrowingBallStart;

    /// <summary>
    /// Handles throwing the ball if the ball is held
    /// Handles adjusting the max angular and linear velocity (if changed in the inspector view for tweaking simulation)
    /// </summary>

    private void Update()
    {
        if (!IsSpawned)
        {
            return;
        }

        // When adjusting maximum angular velocity, update the Rigidbody
        if (m_MaxAngularVelocity != MaxAngularVelocity)
        {
            m_MaxAngularVelocity = MaxAngularVelocity;
            if (m_Rigidbody)
            {
                m_Rigidbody.maxAngularVelocity = m_MaxAngularVelocity;
            }
        }

        // When adjusting maximum linear velocity, update the Rigidbody
        if (m_MaxLinearVelocity != MaxLinearVelocity)
        {
            m_MaxLinearVelocity = MaxLinearVelocity;
            if (m_Rigidbody)
            {
                m_Rigidbody.maxLinearVelocity = m_MaxLinearVelocity;
            }
        }

        if (!CanCommitToTransform)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab) && IsLocalPlayer)
        {
            NetworkManagerHelper.Instance.ToggleNetStatsMonitor();
        }

        if (PickupOnCollison && !m_ThrowingBall && Input.GetKeyDown(KeyCode.T) && m_ObjectBeingHeld)
        {
            m_ThrowingBall = true;
            m_ThrowingBallStart = Time.realtimeSinceStartup;
        }

        if (m_ThrowingBall && m_ObjectBeingHeld)
        {
            var throwForceAmount = Mathf.Clamp(Time.realtimeSinceStartup - m_ThrowingBallStart, 0.01f, 5.0f);
            if (m_TextMesh)
            {
                if (throwForceAmount < 5.0f)
                {
                    m_TextMesh.text = $"Throw Force: {(int)(100 * (throwForceAmount / 5.0f))}";
                }
                else
                {
                    m_TextMesh.text = $"**Max Throw Force**";
                }
            }

            if (Input.GetKeyUp(KeyCode.T))
            {
                HoldingText?.SetActive(false);
                if (m_TextMesh)
                {
                    m_TextMesh.text = $"Holding Ball";
                }
                m_ThrowingBall = false;

                m_ObjectBeingHeld.AddForce(transform.forward + (transform.up * 0.75f), ThrowForce * throwForceAmount);
                m_ObjectBeingHeld.NetworkRigidbody.DetachFromFixedJoint();
                m_ObjectBeingHeld.NetworkObject.SetOwnershipLock(false);
                m_ObjectBeingHeld = null;
                var meshRenderer = GetComponent<MeshRenderer>();
                var color = meshRenderer.material.color;
                color.a = 1.0f;
                meshRenderer.material.color = color;
            }
        }
    }

    private bool m_AddThrowForce = false;
    private Vector3 m_DropForce = Vector3.zero;
    public void AddForce(Vector3 dir, float magnitude)
    {
        if (m_Rigidbody != null && HasAuthority)
        {
            dir.y = Mathf.Max(dir.y, 0.7f);
            m_DropForce = dir.normalized * magnitude;
            m_AddThrowForce = true;
        }
    }

    public static List<NetworkObject> SelectedObjects = new List<NetworkObject>();

    public delegate void OwnershipChangedNotificationHandler(MoverScript moverScript);
    public event OwnershipChangedNotificationHandler OwnershipChangedNotification;

    protected override void OnOwnershipChanged(ulong previous, ulong current)
    {
        if (!IsOwner && SelectedObjects.Contains(NetworkObject))
        {
            NetworkLog.LogWarningServer($"Selected object {name} changed ownership when it should be locked!");
            SelectedObjects.Remove(NetworkObject);
            SelectedObject.SetActive(false);
        }
        OwnershipChangedNotification?.Invoke(this);
        base.OnOwnershipChanged(previous, current);
    }

    public void SetObjectSelected()
    {
        if (!CanBeSelected || SelectedObject == null)
        {
            return;
        }
        if (!SelectedObjects.Contains(NetworkObject))
        {
            NetworkObject.SetOwnershipLock(true);
            SelectedObjects.Add(NetworkObject);
            SelectedObject.SetActive(true);
        }
        else if (SelectedObjects.Contains(NetworkObject))
        {
            NetworkObject.SetOwnershipLock(false);
            SelectedObjects.Remove(NetworkObject);
            SelectedObject.SetActive(false);
        }
    }

    public void SetNonOwnerObjectSelected(bool forceDisable = false)
    {
        if (!CanBeSelected || SelectedNonOwnerObject == null)
        {
            return;
        }

        if (forceDisable)
        {
            SelectedNonOwnerObject.SetActive(false);
        }
        else
        {
            SelectedNonOwnerObject.SetActive(!SelectedNonOwnerObject.activeInHierarchy);
        }
    }
}