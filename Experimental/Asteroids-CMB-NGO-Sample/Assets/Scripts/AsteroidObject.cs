using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
/// <summary>
/// The custom editor for the <see cref="AsteroidObject"/> component.
/// </summary>
[CustomEditor(typeof(AsteroidObject), true)]
public class AsteroidObjectEditor : PhysicsObjectMotionEditor
{
    private SerializedProperty m_StartingHealth;
    private SerializedProperty m_NumberOfStages;
    private SerializedProperty m_MassStageDecrement;
    private SerializedProperty m_DebugFragmentation;
    
    public override void OnEnable()
    {
        m_StartingHealth = serializedObject.FindProperty(nameof(AsteroidObject.StartingHealth));
        m_NumberOfStages = serializedObject.FindProperty(nameof(AsteroidObject.NumberOfStages));
        m_MassStageDecrement = serializedObject.FindProperty(nameof(AsteroidObject.MassStageDecrement));
        m_DebugFragmentation = serializedObject.FindProperty(nameof(AsteroidObject.DebugFragmentation));
        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        var asteroidObject = target as AsteroidObject;
        asteroidObject.AsteroidObjectPropertiesVisible = EditorGUILayout.BeginFoldoutHeaderGroup(asteroidObject.AsteroidObjectPropertiesVisible, $"{nameof(AsteroidObject)} Properties");
        if (asteroidObject.AsteroidObjectPropertiesVisible)
        {
            EditorGUILayout.PropertyField(m_StartingHealth);
            EditorGUILayout.PropertyField(m_NumberOfStages);
            EditorGUILayout.PropertyField(m_MassStageDecrement);
            EditorGUILayout.PropertyField(m_DebugFragmentation);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();
        base.OnInspectorGUI();
    }
}
#endif
public class AsteroidObject : PhysicsObjectMotion
{
#if UNITY_EDITOR
    public bool AsteroidObjectPropertiesVisible = false;
#endif

    [Range(50.0f, 1000.0f)]
    public float StartingHealth = 100.0f;

    [Range(1, 5)]
    public int NumberOfStages = 4;

    [Range(0.10f, 0.75f)]
    public float MassStageDecrement = 0.15f;

    public bool DebugFragmentation;


    public static List<ObjectPoolSystem> AsteroidPoolSystems = new List<ObjectPoolSystem>();

    public static void SetAsteroidPoolSystem(ObjectPoolSystem poolSystem)
    {
        if (!AsteroidPoolSystems.Contains(poolSystem))
        {
            AsteroidPoolSystems.Add(poolSystem);
        }
    }

    public static ObjectPoolSystem GetRandomPoolSystem()
    {
        return AsteroidPoolSystems[Random.Range(0, AsteroidPoolSystems.Count - 1)];
    }

    private NetworkVariable<float> Health = new NetworkVariable<float>(0.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> FragmentStage = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [HideInInspector]

    public bool IgnoreZeroHealth;

    public int GetFragmentStage()
    {
        if (!IsSpawned)
        {
            return -1;
        }
        return FragmentStage.Value == 0 ? NumberOfStages : FragmentStage.Value;
    }

    public bool IsFragmenting()
    {
        return Health.Value == 0.0f && !IgnoreZeroHealth;
    }

    /// <inheritdoc/>
    private void OnDisable()
    {
        var ownerObjectColor = GetComponentInChildren<ObjectOwnerColor>();
        if (ownerObjectColor != null)
        {
            ownerObjectColor.gameObject.SetActive(true);
        }
    }

    /// <inheritdoc/>
    protected override void OnHandleCollision(CollisionMessageInfo damageMessage, bool isLocal = false, bool applyImmediately = false)
    {
        if (Health.Value == 0.0f || damageMessage.GetCollisionType() == CollisionTypes.Ship)
        {
            base.OnHandleCollision(damageMessage, isLocal, applyImmediately);
            return;
        }
        var currentHealth = Mathf.Max(0.0f, Health.Value - damageMessage.Damage);
        var currentStage = FragmentStage.Value;

        if (currentHealth == 0.0f)
        {
            Rigidbody.isKinematic = true;
            EnableColliders(false);
            currentStage--;
            if (currentStage > 0 && AsteroidPoolSystems.Count > 0)
            {
                for (int i = 0; i < (currentStage + 1); i++)
                {
                    SpawnAsteroidFragment(currentStage);
                }
            }
            Health.Value = currentHealth;
            NetworkObject.Despawn();
            return;
        }
        else
        {
            Health.Value = currentHealth;
        }

        base.OnHandleCollision(damageMessage, isLocal, applyImmediately);
    }

    private void SpawnAsteroidFragment(int currentStage)
    {
        var instance = GetRandomPoolSystem().GetInstance((IsServer && !NetworkManager.DistributedAuthorityMode) || (NetworkManager.DistributedAuthorityMode && NetworkManager.LocalClientId == OwnerClientId));
        // Adjust for modifiers
        var modifierStage = currentStage + 1;
        var fragmentScaleModifierMax = 1.0f / modifierStage;
        var fragmentScaleModifierMin = 1.0f / (modifierStage + 1);
        var spawnPosition = GetRandomVector3(2 + (8.0f * fragmentScaleModifierMin), 4 + (12.0f * fragmentScaleModifierMax), Vector3.one, true);
        spawnPosition.y = 0.0f;
        instance.transform.position = spawnPosition + transform.position;
        instance.transform.localScale = GetRandomVector3(0.4f, 0.7f, transform.localScale);
        instance.DontDestroyWithOwner = true;
        if (DebugFragmentation)
        {
            NetworkManagerHelper.Instance.LogMessage($"[Fragment] Pos: {instance.transform.position} Scale: {instance.transform.localScale}");
        }
        instance.Spawn();
        var instanceObject = instance.GetComponent<AsteroidObject>();
        if (instanceObject != null)
        {
            instanceObject.InitializeAsteroid(StartingHealth * fragmentScaleModifierMax, currentStage);

        }
    }

    public void InitializeAsteroid()
    {
        InitializeAsteroid(StartingHealth, NumberOfStages);
    }

    public void InitializeAsteroid(float health, int stage)
    {
        if (IsSpawned)
        {
            Health.Value = health;
            FragmentStage.Value = stage;
            var overOne = 1.0f / NumberOfStages;
            Rigidbody.mass = Rigidbody.mass * stage * overOne;
        }
    }
}
