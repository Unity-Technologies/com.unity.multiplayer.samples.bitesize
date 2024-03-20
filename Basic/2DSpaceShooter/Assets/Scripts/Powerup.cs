using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

public class Powerup : NetworkBehaviour
{
    static string s_ObjectPoolTag = "ObjectPool";

    public static int numPowerUps = 0;
    
    NetworkObjectPool m_ObjectPool;

    public NetworkVariable<Buff.BuffType> buffType = new NetworkVariable<Buff.BuffType>();

    [SerializeField] 
    Renderer m_PowerUpGlow;
    
    [SerializeField]
    Renderer m_PowerUpGlow2;

    [SerializeField]
    UIDocument m_PowerUpUIDocument;

    VisualElement m_PowerUpRootVisualElement;
    
    VisualElement m_PowerUpUIWrapper;
    
    TextElement m_PowerUpLabel;

    Camera m_MainCamera;
    
    public Vector2 m_ScreenPosition;

    IPanel m_Panel;
    
    void Awake()
    {
        m_ObjectPool = GameObject.FindWithTag(s_ObjectPoolTag).GetComponent<NetworkObjectPool>();
        Assert.IsNotNull(m_ObjectPool, $"{nameof(NetworkObjectPool)} not found in scene. Did you apply the {s_ObjectPoolTag} to the GameObject?");
        m_MainCamera = Camera.main;
    }

    void OnEnable()
    {
        m_PowerUpRootVisualElement = m_PowerUpUIDocument.rootVisualElement;
        m_PowerUpUIWrapper = m_PowerUpRootVisualElement.Q<VisualElement>("PowerUpUIBox");
        m_PowerUpLabel = m_PowerUpRootVisualElement.Q<TextElement>("PowerUpLabel");
        m_Panel = m_PowerUpUIWrapper.panel;
    }

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            OnStartClient();
        }

        if (IsServer)
        {
            OnStartServer();
        }
        
        UpdateVisuals(buffType.Value);
        buffType.OnValueChanged += OnBuffTypeChanged;
    }

    public override void OnNetworkDespawn()
    {
        buffType.OnValueChanged -= OnBuffTypeChanged;
    }

    void OnStartClient()
    {
        float dir = -70.0f;
        transform.rotation = Quaternion.Euler(0, 180, dir);
        GetComponent<Rigidbody2D>().angularVelocity = dir;

        if (!IsServer)
        {
            numPowerUps += 1;
        }
    }

    void OnStartServer()
    {
        numPowerUps += 1;
    }
    
    void OnBuffTypeChanged(Buff.BuffType previousValue, Buff.BuffType newValue)
    {
        UpdateVisuals(newValue);
    }

    void UpdateVisuals(Buff.BuffType buffType)
    {
        var buffColor = Buff.buffColors[(int)buffType];
        GetComponent<Renderer>().material.color = buffColor;
        m_PowerUpGlow.material.SetColor("_Color", buffColor);
        m_PowerUpGlow.material.SetColor("_EmissiveColor", buffColor);
        m_PowerUpGlow2.material.SetColor("_Color", buffColor);
        m_PowerUpGlow2.material.SetColor("_EmissiveColor", buffColor);

        m_PowerUpLabel.text = buffType.ToString().ToUpper();
        
        if (buffType == Buff.BuffType.QuadDamage)
        {
            m_PowerUpLabel.text = "Quad Damage";
        }
        
        m_PowerUpLabel.style.color = buffColor;
    }

    void LateUpdate()
    {
        SetLabelPosition();
    }

    void SetLabelPosition()
    {
        if (m_Panel != null)
        {
            m_ScreenPosition = RuntimePanelUtils.CameraTransformWorldToPanel(m_Panel, transform.position, m_MainCamera);
            m_PowerUpUIWrapper.transform.position = m_ScreenPosition;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer)
        {
            return;
        }

        var otherShipControl = other.gameObject.GetComponent<ShipControl>();
        if (otherShipControl != null)
        {
            otherShipControl.AddBuff(buffType.Value);
            DestroyPowerUp();
        }
    }

    void DestroyPowerUp()
    {
        AudioSource.PlayClipAtPoint(GetComponent<AudioSource>().clip, transform.position);
        numPowerUps -= 1;
       
        NetworkObject.Despawn(true);
    }
}
