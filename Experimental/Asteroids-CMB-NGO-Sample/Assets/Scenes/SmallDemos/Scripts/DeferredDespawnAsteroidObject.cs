using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.VisualScripting;

#if UNITY_EDITOR
using UnityEditor;
/// <summary>
/// The custom editor for the <see cref="AsteroidObject"/> component.
/// </summary>
[CustomEditor(typeof(DeferredDespawnAsteroidObject), true)]
public class DeferredDespawnAsteroidObjectEditor : AsteroidObjectEditor
{
    private SerializedProperty m_Laser;
    private SerializedProperty m_TurretOffset;
    private SerializedProperty m_PerspectiveCamera;
    private SerializedProperty m_CameraLerpFactor;
    private SerializedProperty m_LaserOffset;
    private SerializedProperty m_LaserVelocity;
    private SerializedProperty m_ScrollDeltaMult;

    public override void OnEnable()
    {
        m_Laser = serializedObject.FindProperty(nameof(DeferredDespawnAsteroidObject.Laser));
        m_TurretOffset = serializedObject.FindProperty(nameof(DeferredDespawnAsteroidObject.TurretOffset));
        m_PerspectiveCamera = serializedObject.FindProperty(nameof(DeferredDespawnAsteroidObject.PerspectiveCamera));
        m_CameraLerpFactor = serializedObject.FindProperty(nameof(DeferredDespawnAsteroidObject.CameraLerpFactor));
        m_LaserOffset = serializedObject.FindProperty(nameof(DeferredDespawnAsteroidObject.LaserOffset));
        m_LaserVelocity = serializedObject.FindProperty(nameof(DeferredDespawnAsteroidObject.LaserVelocity));
        m_ScrollDeltaMult = serializedObject.FindProperty(nameof(DeferredDespawnAsteroidObject.ScrollDeltaMult));
        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        var deferredDespawnAsteroidObject = target as DeferredDespawnAsteroidObject;
        deferredDespawnAsteroidObject.DeferredDespawnAsteroidObjectProperitiesVisible = EditorGUILayout.BeginFoldoutHeaderGroup(deferredDespawnAsteroidObject.DeferredDespawnAsteroidObjectProperitiesVisible, $"{nameof(DeferredDespawnAsteroidObject)} Properties");
        if (deferredDespawnAsteroidObject.DeferredDespawnAsteroidObjectProperitiesVisible)
        {
            EditorGUILayout.PropertyField(m_Laser);
            EditorGUILayout.PropertyField(m_TurretOffset);
            EditorGUILayout.PropertyField(m_PerspectiveCamera);
            EditorGUILayout.PropertyField(m_CameraLerpFactor);
            EditorGUILayout.PropertyField(m_LaserOffset);
            EditorGUILayout.PropertyField(m_LaserVelocity);
            EditorGUILayout.PropertyField(m_ScrollDeltaMult);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();
        base.OnInspectorGUI();
    }
}
#endif

public class DeferredDespawnAsteroidObject : AsteroidObject
{
#if UNITY_EDITOR
    public bool DeferredDespawnAsteroidObjectProperitiesVisible;
#endif

    private static Vector3 CameraOffset;
    private static Vector3 CameraStart;
    private static bool CameraOffsetInitialized;

    public GameObject Laser;
    public float TurretOffset = 2.0f;

    private ObjectPoolSystem LaserPoolSystem;

    [HideInInspector]
    public Camera MainCamera;
    public Camera PerspectiveCamera;
    public float CameraLerpFactor = 2.0f;
    public float LaserOffset = 3.0f;
    public float LaserVelocity = 20.0f;

    public float ScrollDeltaMult = 5.0f;

    private enum PerspectiveNames
    {
        Top,
        Bottom,
        Left,
        Right,
    }

    private struct Perspective
    {
        public PerspectiveNames PerspectiveName;
        public Vector3 Postion;
        public Vector3 Angles;
        public float MaxZoomOffset;
    }

    private bool LeftOrRightOfCamera;
    private GameObject m_SpawnInfoUI;
    private Text m_PerspectiveZoom;
    private Text m_PerspectiveName;

    private Text m_DeferredTicksValue;
    private Slider m_DeferredTicksSlider;


    private List<Perspective> Perspectives = new List<Perspective>();
    private int m_CurrentPerspective = 0;

    private const float k_MaxZoomOut = 50.0f;
    private float ZoomFactor = 0.0f;

    // These are the base position and Euler angle values used to cycle through the different perspectives
    private Vector3 PerpsectivePositionBase = new Vector3(20, 20, 45);
    private Vector3 PerspectiveAnglesBase = new Vector3(90, 90, 0);
    private TagHandle m_LaserCanHit;

    private NetworkVariable<float> m_DeferredTick = new NetworkVariable<float>();

    /// <summary>
    /// This defines the various perspectives of the asteroid perspective view based on
    /// fixed position and rotation values coupled with the companion axial defines for
    /// each respective perspective.
    /// </summary>
    /// <remarks>
    /// The deferred despawn demo demonstrates:
    /// - How deferring despawn changes when a NetworkObject is despawned and how to use it to synchronize FX for things like impacts.
    /// - How to handle receiving mouse input from non-authority clients and keeping the mouse position scaled relative to the authority's render view.
    /// </remarks>
    private void InitializePerspecties()
    {
        // Top
        Perspectives.Add(new Perspective()
        {
            PerspectiveName = PerspectiveNames.Top,
            Postion = new Vector3(0, 1, 1),
            Angles = new Vector3(1, 0, 0),
            MaxZoomOffset = 11.0f,
        });

        // Bottom
        Perspectives.Add(new Perspective()
        {
            PerspectiveName = PerspectiveNames.Bottom,
            Postion = new Vector3(0, -1, 1),
            Angles = new Vector3(-1, 0, 0),
            MaxZoomOffset = 11.0f,
        });

        // Left
        Perspectives.Add(new Perspective()
        {
            PerspectiveName = PerspectiveNames.Left,
            Postion = new Vector3(1, 0, 1),
            Angles = new Vector3(0, -1, 0),
            MaxZoomOffset = 22.0f,
        });

        // Right
        Perspectives.Add(new Perspective()
        {
            PerspectiveName = PerspectiveNames.Right,
            Postion = new Vector3(-1, 0, 1),
            Angles = new Vector3(0, 1, 0),
            MaxZoomOffset = 22.0f,
        });
    }


    protected override void Awake()
    {
        base.Awake();
        m_LaserCanHit = TagHandle.GetExistingTag("DeferredDespawnDemo");
        MainCamera = Camera.main;
        var cameras = MainCamera.transform.GetComponentsInChildren<Camera>(true);
        foreach (var camera in cameras)
        {
            if (camera == MainCamera)
            {
                continue;
            }
            PerspectiveCamera = camera;
            break;
        }

        InitializePerspecties();
        InitializeObjects();
    }

    private void OnDeferredDespawnTicksChanged()
    {
        if (!HasAuthority)
        {
            return;
        }
        if (m_DeferredTicksValue != null)
        {
            m_DeferredTicksValue.text = $"Deferred Ticks: {m_DeferredTicksSlider.value}";
        }
        if (IsSpawned)
        {
            m_DeferredTick.Value = m_DeferredTicksSlider.value;
        }
    }

    private void OnDeferredTickUpdated(float previous, float current)
    {
        if (m_DeferredTicksValue != null)
        {
            m_DeferredTicksValue.text = $"Deferred Ticks: {current}";
        }

        if (m_DeferredTicksSlider)
        {
            m_DeferredTicksSlider.value = current;
        }
    }

    private void InitializeObjects()
    {
        if (MainCamera != null && !CameraOffsetInitialized)
        {
            CameraStart = MainCamera.transform.position;
            CameraOffset = MainCamera.transform.position - transform.position;
            CameraOffsetInitialized = true;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (m_SpawnInfoUI == null)
        {
            m_SpawnInfoUI = DeferredUIElements.Instance.SpawnedInfo;
        }

        if (m_SpawnInfoUI != null)
        {
            m_SpawnInfoUI.SetActive(true);
        }

        if (PerspectiveCamera != null)
        {
            SetPerspective();
        }

        NetworkManager.OnSessionOwnerPromoted += OnSessionOwnerPromoted;
    }

    private void OnSessionOwnerPromoted(ulong sessionOwnerPromoted)
    {
        if (sessionOwnerPromoted == NetworkManager.LocalClientId)
        {
            if (!IsOwner)
            {
                NetworkObject.ChangeOwnership(sessionOwnerPromoted);
            }

            if (m_DeferredTicksSlider)
            {
                m_DeferredTicksSlider.interactable = true;
            }

            m_DeferredTick.OnValueChanged -= OnDeferredTickUpdated;
        }
    }

    public override void OnNetworkDespawn()
    {
        NetworkManager.OnSessionOwnerPromoted -= OnSessionOwnerPromoted;
        base.OnNetworkDespawn();
    }

    /// <summary>
    /// All things that need to be initialized oncw we know everything is loaded, spawned, and synchronized
    /// </summary>
    protected override void OnNetworkSessionSynchronized()
    {
        var uiObject = GameObject.Find("PerspectiveName");
        if (uiObject != null)
        {
            m_PerspectiveName = uiObject.GetComponent<Text>();
        }
        else
        {
            m_PerspectiveName = DeferredUIElements.Instance.m_PerspectiveName;
        }

        uiObject = GameObject.Find("Zoom");
        if (uiObject != null)
        {
            m_PerspectiveZoom = uiObject.GetComponent<Text>();
        }
        else
        {
            m_PerspectiveZoom = DeferredUIElements.Instance.m_PerspectiveZoom;
        }

        uiObject = GameObject.Find("DeferredTickValue");
        if (uiObject != null)
        {
            m_DeferredTicksValue = uiObject.GetComponent<Text>();
        }
        else
        {
            m_DeferredTicksValue = DeferredUIElements.Instance.m_DeferredTicksValue;
        }

        uiObject = GameObject.Find("DeferredDespawnTicks");
        if (uiObject == null)
        {
            m_DeferredTicksSlider = DeferredUIElements.Instance.m_DeferredTicksSlider;
        }
        else
        {
            m_DeferredTicksSlider = uiObject.GetComponent<Slider>();
        }
        m_DeferredTicksSlider.onValueChanged.AddListener(delegate { OnDeferredDespawnTicksChanged(); });
        m_DeferredTicksSlider.interactable = NetworkManager.LocalClient.IsSessionOwner;
        
        m_SpawnInfoUI = GameObject.Find("SpawnedInfo");
        if (m_SpawnInfoUI != null)
        {
            m_SpawnInfoUI.SetActive(IsSpawned);
        }

        if (ObjectPoolSystem.ExistingPoolSystems.ContainsKey(Laser))
        {
            LaserPoolSystem = ObjectPoolSystem.ExistingPoolSystems[Laser];
        }
        if (NetworkManager.LocalClient.IsSessionOwner)
        {
            if (MainCamera)
            {
                MainCamera.transform.position = CameraStart + CameraOffset;
            }
        }
        else
        {
            m_DeferredTick.OnValueChanged += OnDeferredTickUpdated;
        }

        base.OnNetworkSessionSynchronized();
    }

    /// <summary>
    /// This is just a convenient way to handle positioning, offseting, and zooming a camera perspective all based on the zoom amount
    /// and the preset base position values.
    /// </summary>
    private Vector3 MulPosVector3(Vector3 a, Vector3 b, float zoomViewOffset)
    {
        return new Vector3((a.x * b.x) + (a.x * ZoomFactor), (a.y * b.y) + (a.y * ZoomFactor), (a.z * b.z) - zoomViewOffset);
    }

    private Vector3 MulAngleVector3(Vector3 a, Vector3 b)
    {
        return new Vector3((a.x * b.x), (a.y * b.y), a.z * b.z);
    }

    private void SetPerspective()
    {
        // Get the fraction percentage value of the current zoom
        var zoomPercent = 1.0f - Mathf.Clamp(ZoomFactor, 0.01f, k_MaxZoomOut) / k_MaxZoomOut;
        // Calculate the amount to offset the camera based on the perspective
        var zoomViewOffset = (1.0f - zoomPercent) * Perspectives[m_CurrentPerspective].MaxZoomOffset;
        // Apply updated position and angles of the camera's perspective
        PerspectiveCamera.transform.localPosition = MulPosVector3(Perspectives[m_CurrentPerspective].Postion, PerpsectivePositionBase, zoomViewOffset);
        PerspectiveCamera.transform.localEulerAngles = MulAngleVector3(Perspectives[m_CurrentPerspective].Angles, PerspectiveAnglesBase);

        if (m_PerspectiveZoom != null)
        {
            m_PerspectiveZoom.text = $"Zoom: {Mathf.Round(zoomPercent * 100.0f):F0}%";
        }
        if (m_PerspectiveName != null)
        {
            m_PerspectiveName.text = $"{Perspectives[m_CurrentPerspective].PerspectiveName}";
        }
    }

    /// <summary>
    /// Used to move the camera with the asteroid
    /// </summary>
    private void LateUpdate()
    {
        if (!IsSpawned)
        {
            return;
        }

        if (MainCamera != null)
        {
            MainCamera.transform.position = transform.position + CameraOffset;

            if (Input.GetMouseButtonDown(0))
            {
                if (HasAuthority)
                {
                    ShootLaser(Input.mousePosition);
                }
                else
                {
                    // This demo also demonstrates how to keep a remote client mouse input relative to the authority's render view.
                    ShootLaserRpc(Input.mousePosition, Display.main.renderingWidth, Display.main.renderingHeight);
                }
            }
            UpdatedInput();
        }
    }

    [Rpc(SendTo.Authority)]
    private void ShootLaserRpc(Vector3 mousePosition, int width, int height)
    {
        // On the authority side, get the rendering width and height ratio between the non-authority and authority
        var widthRatio = Display.main.renderingWidth / (float)width;
        var heightRatio = Display.main.renderingHeight / (float)height;
        // Multiply the width and height ratio to the respective non-authority's mouse position
        mousePosition.x *= widthRatio;
        mousePosition.y *= heightRatio;
        // Shoot the laser 
        ShootLaser(mousePosition);
    }

    private void ShootLaser(Vector3 mousePosition)
    {
        if (Camera.current == null)
        {
            Camera.SetupCurrent(Camera.allCameras[0]);
        }

        var selectedPoint = Vector3.zero;
        if (!MouseSelectObject.SelectPoint<Collider>(out selectedPoint, m_LaserCanHit, mousePosition))
        {
            selectedPoint = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 10.0f));
        }

        var direction = (selectedPoint - MainCamera.transform.position).normalized;
        var laser = LaserPoolSystem.GetInstance(true);
        var laserMotion = laser.GetComponent<LaserMotion>();
        var leftRightOffset = LeftOrRightOfCamera ? MainCamera.transform.right : -1.0f * MainCamera.transform.right;
        laserMotion.IgnoreYAxisClamp = true;
        laserMotion.IgnoreImpactForce = true;

        laserMotion.transform.position = MainCamera.transform.position + (direction * LaserOffset) + leftRightOffset * TurretOffset;
        laserMotion.transform.forward = (selectedPoint - (MainCamera.transform.position + (leftRightOffset * TurretOffset))).normalized;

        if (laserMotion.NetworkRigidbody.UseRigidBodyForMotion)
        {
            laserMotion.NetworkRigidbody.ApplyCurrentTransform();
        }

        laserMotion.ShootLaser(laserMotion.transform.position, laserMotion.transform.rotation, laserMotion.transform.forward * LaserVelocity);
        laserMotion.DeferredDespawnTicks = (int)m_DeferredTicksSlider.value;

        laserMotion.NetworkObject.Spawn();
        LeftOrRightOfCamera = !LeftOrRightOfCamera;
    }

    private void UpdatedInput()
    {
        // Zoom in and out
        var updatePerspective = false;
        if (Mathf.Abs(Input.mouseScrollDelta.y) > 0.0f)
        {
            ZoomFactor = Mathf.Clamp((Input.mouseScrollDelta.y * ScrollDeltaMult) + ZoomFactor, 0.0f, k_MaxZoomOut);
            updatePerspective = true;
        }

        // Change perspective
        if (Input.GetMouseButtonDown(1))
        {
            m_CurrentPerspective++;
            m_CurrentPerspective = m_CurrentPerspective % Perspectives.Count;
            updatePerspective = true;
        }
        if (updatePerspective)
        {
            SetPerspective();
        }
    }
}
