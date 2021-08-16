﻿using System;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using UnityEngine;
using UnityEngine.Assertions;

public class Buff
{
    public enum BuffType
    {
        Speed,
        Rotate,
        Triple,
        Double,
        Health,
        Energy,
        QuadDamage,
        Bounce,
        Last
    };

    public static Color[] bufColors = { Color.red, Color.blue, Color.cyan, Color.yellow, Color.green, Color.magenta, new Color(1, 0.5f, 0), new Color(0, 1, 0.5f) };

    public static Color GetColor(BuffType bt)
    {
        return bufColors[(int)bt];
    }
};

public class ShipControl : NetworkBehaviour
{
    static string s_ObjectPoolTag = "ObjectPool";

    NetworkObjectPool m_ObjectPool;
    public GameObject BulletPrefab;
    public AudioSource fireSound;

    float rotateSpeed = 200f;
    float acceleration = 12f;
    float bulletLifetime = 2;
    float topSpeed = 7.0f;

    public NetworkVariableInt Health = new NetworkVariableInt(100);
    public NetworkVariableInt Energy = new NetworkVariableInt(100);

    int m_Deaths = 0;

    public NetworkVariableFloat SpeedBuffTimer = new NetworkVariableFloat(0);
    public NetworkVariableFloat RotateBuffTimer = new NetworkVariableFloat(0);
    public NetworkVariableFloat TripleShotTimer = new NetworkVariableFloat(0);
    public NetworkVariableFloat DoubleShotTimer = new NetworkVariableFloat(0);
    public NetworkVariableFloat QuadDamageTimer = new NetworkVariableFloat(0);
    public NetworkVariableFloat BounceTimer = new NetworkVariableFloat(0);

    float m_EnergyTimer = 0;

    public NetworkVariableString PlayerName = new NetworkVariableString("");

    [SerializeField]
    Texture m_Box;
    public ParticleSystem friction;
    public ParticleSystem thrust;

    private NetworkVariableFloat m_FrictionEffectStartTimer = new NetworkVariableFloat(-10);

    // for client movement command throttling
    float m_OldMoveForce = 0;
    float m_OldSpin = 0;

    // server movement
    private NetworkVariableFloat m_Thrusting = new NetworkVariableFloat();

    float m_Spin;

    Rigidbody2D m_Rigidbody2D;

    void Awake()
    {
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        m_ObjectPool = GameObject.FindWithTag(s_ObjectPoolTag).GetComponent<NetworkObjectPool>();
        Assert.IsNotNull(m_ObjectPool, $"{nameof(NetworkObjectPool)} not found in scene. Did you apply the {s_ObjectPoolTag} to the GameObject?");
    }

    void OnHealth(int oldValue, int newValue)
    {
        Health.Value = newValue;
    }

    void OnEnable()
    {
        Health.OnValueChanged += OnHealth;
    }

    void OnDisable()
    {
        Health.OnValueChanged -= OnHealth;
    }

    void Start()
    {
        thrust.Stop();

        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        GetComponent<AudioListener>().enabled = IsOwner;
        PlayerName.Value = $"Player {OwnerClientId}";
    }

    public void TakeDamage(int amount)
    {
        Health.Value = Health.Value - amount;
        m_FrictionEffectStartTimer.Value = NetworkManager.LocalTime.TimeAsFloat;

        if (Health.Value <= 0)
        {
            Health.Value = 0;

            //todo: reset all buffs

            m_Deaths += 1;
            Health.Value = 100;
            transform.position = NetworkManager.GetComponent<RandomPositionPlayerSpawner>().GetNextSpawnPosition();
            GetComponent<Rigidbody2D>().velocity = Vector3.zero;
            GetComponent<Rigidbody2D>().angularVelocity = 0;
        }
    }

    void Fire(Vector3 direction)
    {
        fireSound.Play();

        int damage = 5;
        if (QuadDamageTimer.Value > Time.time)
        {
            damage = 20;
        }

        bool bounce = BounceTimer.Value > Time.time;

        GameObject bullet = m_ObjectPool.GetNetworkObject(BulletPrefab).gameObject;
        bullet.transform.position = transform.position + direction;

        var bulletRb = bullet.GetComponent<Rigidbody2D>();

        var velocity = m_Rigidbody2D.velocity;
        velocity += (Vector2)(direction) * 10;
        bulletRb.velocity = velocity;
        bullet.GetComponent<Bullet>().Config(this, damage, bounce, bulletLifetime);

        bullet.GetComponent<NetworkObject>().Spawn(null, true);
    }

    void Update()
    {
        if (IsServer)
        {
            UpdateServer();
        }

        if (IsClient)
        {
            UpdateClient();
        }
    }

    void UpdateServer()
    {
        // energy regen
        if (m_EnergyTimer < Time.time)
        {
            if (Energy.Value < 100)
            {
                if (Energy.Value + 20 > 100)
                {
                    Energy.Value = 100;
                }
                else
                {
                    Energy.Value += 20;
                }
            }

            m_EnergyTimer = Time.time + 1;
        }

        // update rotation 
        float rotate = m_Spin * rotateSpeed;
        if (RotateBuffTimer.Value > Time.time)
        {
            rotate *= 2;
        }

        m_Rigidbody2D.angularVelocity = rotate;

        // update thrust
        if (m_Thrusting.Value != 0)
        {
            float accel = acceleration;
            if (SpeedBuffTimer.Value > Time.time)
            {
                accel *= 2;
            }

            Vector3 thrustVec = transform.right * (m_Thrusting.Value * accel);
            m_Rigidbody2D.AddForce(thrustVec);

            // restrict max speed
            float top = topSpeed;
            if (SpeedBuffTimer.Value > Time.time)
            {
                top *= 1.5f;
            }

            if (m_Rigidbody2D.velocity.magnitude > top)
            {
                m_Rigidbody2D.velocity = m_Rigidbody2D.velocity.normalized * top;
            }
        }
    }

    private void HandleFrictionGraphics()
    {
        var time = NetworkManager.ServerTime.Time;
        var start = m_FrictionEffectStartTimer.Value;
        
        bool frictionShouldBeActive = time >= start && time < start + 1f; // 1f is the duration of the effect

        if (frictionShouldBeActive)
        {
            if (friction.isPlaying == false)
            {
                friction.Play();
            }
        }
        else
        {
            if (friction.isPlaying)
            {
                friction.Stop();
            }
        }
    }

    void UpdateClient()
    {
        HandleFrictionGraphics();

        if (!IsLocalPlayer)
        {
            return;
        }

        // movement
        int spin = 0;
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            spin += 1;
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            spin -= 1;
        }

        int moveForce = 0;
        if (Input.GetKey(KeyCode.UpArrow))
        {
            moveForce += 1;
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            moveForce -= 1;
        }

        if (m_OldMoveForce != moveForce || m_OldSpin != spin)
        {
            ThrustServerRpc(moveForce, spin);
            m_OldMoveForce = moveForce;
            m_OldSpin = spin;
        }

        // control thrust particles
        if (moveForce == 0.0f)
        {
            thrust.Stop();
            GetComponent<AudioSource>().Pause();
        }
        else
        {
            thrust.Play();
            GetComponent<AudioSource>().Play();
        }

        // fire
        if (Input.GetKeyDown(KeyCode.Space))
        {
            FireServerRpc();
        }

        // center camera.. only if this is MY player!
        Vector3 pos = transform.position;
        pos.z = -50;
        Camera.main.transform.position = pos;
    }

    public void AddBuff(Buff.BuffType buff)
    {
        if (buff == Buff.BuffType.Speed)
        {
            SpeedBuffTimer.Value = Time.time + 10;
        }

        if (buff == Buff.BuffType.Rotate)
        {
            RotateBuffTimer.Value = Time.time + 10;
        }

        if (buff == Buff.BuffType.Triple)
        {
            TripleShotTimer.Value = Time.time + 10;
        }

        if (buff == Buff.BuffType.Double)
        {
            DoubleShotTimer.Value = Time.time + 10;
        }

        if (buff == Buff.BuffType.Health)
        {
            Health.Value += 20;
            if (Health.Value >= 100)
            {
                Health.Value = 100;
            }
        }

        if (buff == Buff.BuffType.Energy)
        {
            Energy.Value += 50;
            if (Energy.Value >= 100)
            {
                Energy.Value = 100;
            }
        }

        if (buff == Buff.BuffType.QuadDamage)
        {
            QuadDamageTimer.Value = Time.time + 10;
        }

        if (buff == Buff.BuffType.Bounce)
        {
            QuadDamageTimer.Value = Time.time + 10;
        }
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (NetworkManager.Singleton.IsServer == false)
        {
            return;
        }

        var asteroid = other.gameObject.GetComponent<Asteroid>();
        if (asteroid != null)
        {
            TakeDamage(5);
        }
    }

    // --- ServerRPCs ---

    [ServerRpc]
    public void ThrustServerRpc(float thrusting, int spin)
    {
        m_Thrusting.Value = thrusting;
        m_Spin = spin;
    }

    [ServerRpc]
    public void FireServerRpc()
    {
        if (Energy.Value >= 10)
        {
            var right = transform.right;
            if (TripleShotTimer.Value > Time.time)
            {
                Fire(Quaternion.Euler(0, 0, 20) * right);
                Fire(Quaternion.Euler(0, 0, -20) * right);
                Fire(right);
            }
            else if (DoubleShotTimer.Value > Time.time)
            {
                Fire(Quaternion.Euler(0, 0, -10) * right);
                Fire(Quaternion.Euler(0, 0, 10) * right);
            }
            else
            {
                Fire(right);
            }

            Energy.Value -= 10;
            if (Energy.Value <= 0)
            {
                Energy.Value = 0;
            }
        }
    }

    [ServerRpc]
    public void SetNameServerRpc(string name)
    {
        PlayerName.Value = name;
    }

    void OnGUI()
    {
        Vector3 pos = Camera.main.WorldToScreenPoint(transform.position);

        // draw the name with a shadow (colored for buf)	
        GUI.color = Color.black;
        GUI.Label(new Rect(pos.x - 20, Screen.height - pos.y - 30, 100, 30), PlayerName.Value);

        GUI.color = Color.white;
        if (SpeedBuffTimer.Value > Time.time) { GUI.color = Buff.GetColor(Buff.BuffType.Speed); }

        if (RotateBuffTimer.Value > Time.time) { GUI.color = Buff.GetColor(Buff.BuffType.Rotate); }

        if (TripleShotTimer.Value > Time.time) { GUI.color = Buff.GetColor(Buff.BuffType.Triple); }

        if (DoubleShotTimer.Value > Time.time) { GUI.color = Buff.GetColor(Buff.BuffType.Double); }

        if (QuadDamageTimer.Value > Time.time) { GUI.color = Buff.GetColor(Buff.BuffType.QuadDamage); }

        if (BounceTimer.Value > Time.time) { GUI.color = Buff.GetColor(Buff.BuffType.Bounce); }

        GUI.Label(new Rect(pos.x - 21, Screen.height - pos.y - 31, 100, 30), PlayerName.Value);

        // draw health bar background
        GUI.color = Color.grey;
        GUI.DrawTexture(new Rect(pos.x - 26, Screen.height - pos.y + 20, 52, 7), m_Box);

        // draw health bar amount
        GUI.color = Color.green;
        GUI.DrawTexture(new Rect(pos.x - 25, Screen.height - pos.y + 21, Health.Value / 2, 5), m_Box);

        // draw energy bar background
        GUI.color = Color.grey;
        GUI.DrawTexture(new Rect(pos.x - 26, Screen.height - pos.y + 27, 52, 7), m_Box);

        // draw energy bar amount
        GUI.color = Color.magenta;
        GUI.DrawTexture(new Rect(pos.x - 25, Screen.height - pos.y + 28, Energy.Value / 2, 5), m_Box);
    }

    // ShipControl
    // public void FakeOnUnserializeVars(NetworkReader reader, bool initialState)
    // {
    //     int num = (int)reader.ReadPackedUInt32();
    //     if ((num & 1) != 0)
    //     {
    //         if (initialState)
    //         {
    //             Health.Value = (int)reader.ReadPackedUInt32();
    //         }
    //         else
    //         { 
    //             OnHealth((int)reader.ReadPackedUInt32());
    //         }
    //     }
    //
    //     if ((num & 2) != 0)
    //     {
    //         this.energy = (int)reader.ReadPackedUInt32();
    //     }
    //
    //     if ((num & 4) != 0)
    //     {
    //         this.speedBufTimer = reader.ReadSingle();
    //     }
    //
    //     if ((num & 8) != 0)
    //     {
    //         this.rotateBufTimer = reader.ReadSingle();
    //     }
    //
    //     if ((num & 16) != 0)
    //     {
    //         this.tripleshotTimer = reader.ReadSingle();
    //     }
    //
    //     if ((num & 32) != 0)
    //     {
    //         this.doubleshotTimer = reader.ReadSingle();
    //     }
    //
    //     if ((num & 64) != 0)
    //     {
    //         this.quadDamageTimer = reader.ReadSingle();
    //     }
    //
    //     if ((num & 128) != 0)
    //     {
    //         this.bounceTimer = reader.ReadSingle();
    //     }
    //
    //     if ((num & 256) != 0)
    //     {
    //         this.playerName = reader.ReadString();
    //     }
    //
    //     if ((num & 512) != 0)
    //     {
    //         this.m_Thrusting = reader.ReadSingle();
    //     }
    // }
}
