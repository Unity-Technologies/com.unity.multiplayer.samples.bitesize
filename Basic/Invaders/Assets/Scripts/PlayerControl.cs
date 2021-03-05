using System;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using UnityEngine;
using UnityEngine.Assertions;

public class PlayerControl : NetworkBehaviour
{
    [Header("Weapon Settings")]
    public GameObject bulletPrefab;

    [Header("Movement Settings")]
    [SerializeField]
    private float m_MoveSpeed = 3.5f;

    [Header("Player Settings")]
    [SerializeField]
    private NetworkVariableInt m_Lives = new NetworkVariableInt(3);

    private SceneTransitionHandler.SceneStates m_CurrentSceneState;
    private bool m_HasGameStarted;

    private bool m_IsAlive = true;

    private NetworkVariableInt m_MoveX = new NetworkVariableInt(0);

    private GameObject m_MyBullet;
    private ClientRpcParams m_OwnerRPCParams;

    private SpriteRenderer m_PlayerVisual;
    private NetworkVariableInt m_Score = new NetworkVariableInt(0);

    public bool IsAlive => m_Lives.Value > 0;

    private void Awake()
    {
        m_HasGameStarted = false;
    }

    private void Start()
    {
        m_PlayerVisual = GetComponent<SpriteRenderer>();
        if (m_PlayerVisual != null) m_PlayerVisual.material.color = Color.black;
    }

    private void Update()
    {
        switch (m_CurrentSceneState)
        {
            case SceneTransitionHandler.SceneStates.Ingame:
            {
                InGameUpdate();
                break;
            }
        }
    }

    protected void OnDestroy()
    {
        if (IsClient)
        {
            m_Lives.OnValueChanged -= OnLivesChanged;
            m_Lives.OnValueChanged -= OnScoreChanged;
        }

        if (InvadersGame.Singleton)
        {
            InvadersGame.Singleton.isGameOver.OnValueChanged -= OnGameStartedChanged;
            InvadersGame.Singleton.hasGameStarted.OnValueChanged -= OnGameStartedChanged;
        }
    }

    private void SceneTransitionHandler_clientLoadedScene(ulong clientId)
    {
        SceneStateChangedClientRpc(m_CurrentSceneState);
    }

    [ClientRpc]
    private void SceneStateChangedClientRpc(SceneTransitionHandler.SceneStates state)
    {
        if (!IsServer) SceneTransitionHandler.sceneTransitionHandler.SetSceneState(state);
    }

    private void SceneTransitionHandler_sceneStateChanged(SceneTransitionHandler.SceneStates newState)
    {
        m_CurrentSceneState = newState;
        if (m_CurrentSceneState == SceneTransitionHandler.SceneStates.Ingame)
        {
            if (m_PlayerVisual != null) m_PlayerVisual.material.color = Color.green;
        }
        else
        {
            if (m_PlayerVisual != null) m_PlayerVisual.material.color = Color.black;
        }
    }

    public override void NetworkStart()
    {
        base.NetworkStart();

        // Bind to OnValueChanged to display in log the remaining lives of this player
        // And to update InvadersGame singleton client-side
        m_Lives.OnValueChanged += OnLivesChanged;
        m_Score.OnValueChanged += OnScoreChanged;

        if (IsServer) m_OwnerRPCParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } } };

        if (!InvadersGame.Singleton)
            InvadersGame.OnSingletonReady += SubscribeToDelegatesAndUpdateValues;
        else
            SubscribeToDelegatesAndUpdateValues();

        if (IsServer) SceneTransitionHandler.sceneTransitionHandler.OnClientLoadedScene += SceneTransitionHandler_clientLoadedScene;

        SceneTransitionHandler.sceneTransitionHandler.OnSceneStateChanged += SceneTransitionHandler_sceneStateChanged;
    }

    private void SubscribeToDelegatesAndUpdateValues()
    {
        InvadersGame.Singleton.hasGameStarted.OnValueChanged += OnGameStartedChanged;
        InvadersGame.Singleton.isGameOver.OnValueChanged += OnGameStartedChanged;

        if (IsClient && IsOwner)
        {
            InvadersGame.Singleton.SetScore(m_Score.Value);
            InvadersGame.Singleton.SetLives(m_Lives.Value);
        }
    }

    public void IncreasePlayerScore(int amount)
    {
        Assert.IsTrue(IsServer, "IncreasePlayerScore should be called server-side only");
        m_Score.Value += amount;
    }

    private void OnGameStartedChanged(bool previousValue, bool newValue)
    {
        m_HasGameStarted = newValue;
    }

    private void OnLivesChanged(int previousAmount, int currentAmount)
    {
        if (!IsOwner) return;
        Debug.LogFormat("Lives {0} ", currentAmount);
        if (InvadersGame.Singleton != null) InvadersGame.Singleton.SetLives(m_Lives.Value);

        if (m_Lives.Value <= 0) m_IsAlive = false;
    }

    private void OnScoreChanged(int previousAmount, int currentAmount)
    {
        if (!IsOwner) return;
        Debug.LogFormat("Score {0} ", currentAmount);
        if (InvadersGame.Singleton != null) InvadersGame.Singleton.SetScore(m_Score.Value);
    } // ReSharper disable Unity.PerformanceAnalysis
    private void InGameUpdate()
    {
        if (!IsLocalPlayer || !IsOwner || !m_HasGameStarted) return;
        if (!m_IsAlive) return;

        var deltaX = 0;
        if (Input.GetKey(KeyCode.LeftArrow)) deltaX -= 1;
        if (Input.GetKey(KeyCode.RightArrow)) deltaX += 1;

        if (deltaX != 0)
        {
            var newMovement = new Vector3(deltaX, 0, 0);
            transform.position = Vector3.MoveTowards(transform.position, transform.position + newMovement, m_MoveSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.Space)) ShootServerRPC();
    }

    [ServerRpc]
    private void ShootServerRPC()
    {
        if (!m_IsAlive)
            return;

        if (m_MyBullet == null)
        {
            m_MyBullet = Instantiate(bulletPrefab, transform.position + Vector3.up, Quaternion.identity);
            m_MyBullet.GetComponent<PlayerBullet>().owner = this;
            m_MyBullet.GetComponent<NetworkObject>().Spawn();
        }
    }

    public void HitByBullet()
    {
        Assert.IsTrue(IsServer, "HitByBullet must be called server-side only!");
        if (!m_IsAlive) return;

        m_Lives.Value -= 1;

        if (m_Lives.Value <= 0)
        {
            // gameover!
            m_IsAlive = false;
            m_MoveX.Value = 0;
            m_Lives.Value = 0;
            InvadersGame.Singleton.SetGameEnd(true);
            NotifyDeathClientRpc(m_OwnerRPCParams);
        }
    }

    [ClientRpc]
    public void NotifyDeathClientRpc(ClientRpcParams clientParams)
    {
        m_HasGameStarted = false;
        InvadersGame.Singleton.DisplayGameOverText("You Are Dead!");
    }
}
