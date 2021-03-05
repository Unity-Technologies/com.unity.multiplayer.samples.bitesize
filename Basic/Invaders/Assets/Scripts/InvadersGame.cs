using System;
using System.Collections.Generic;
using MLAPI;
using MLAPI.Connection;
using MLAPI.NetworkVariable;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

public enum InvadersObjectType
{
    Alien = 1,
    Shield,
    Max
}

public class InvadersGame : NetworkBehaviour
{
    // The vertical offset we apply to each Alien transform once they touch an edge
    private const float k_AlienVerticalMovementOffset = -0.8f;
    [Header("Prefab settings")]
    public GameObject alien1Prefab;
    public GameObject alien2Prefab;
    public GameObject alien3Prefab;
    public GameObject saucerPrefab;
    public GameObject shieldPrefab;

    [Header("UI Settings")]
    public TMP_Text gameTimerText;
    public TMP_Text scoreText;
    public TMP_Text livesText;
    public TMP_Text gameOverText;

    [Header("GameMode Settings")]
    public Transform saucerSpawnPoint;

    [SerializeField]
    [Tooltip("Time Remaining until the game starts")]
    private float m_DelayedStartTime = 5.0f;

    [SerializeField]
    private NetworkVariableFloat m_TickPeriodic = new NetworkVariableFloat(0.2f);

    [SerializeField]
    private NetworkVariableFloat m_AlienDirection = new NetworkVariableFloat(0.3f);

    [SerializeField]
    private float m_RandomThresholdForSaucerCreation = 0.92f;

    private List<AlienInvader> m_Aliens = new List<AlienInvader>();

    //These help to simplify checking server vs client
    //[NSS]: This would also be a great place to add a state machine and use networked vars for this
    private bool m_ClientGameOver;
    private bool m_ClientGameStarted;
    private bool m_ClientStartCountdown;

    private NetworkVariableBool m_CountdownStarted = new NetworkVariableBool(false);

    private float m_NextTick;

    // the timer should only be synced at the beginning
    // and then let the client to update it in a predictive manner
    private NetworkVariableFloat m_ReplicatedTimeRemaining = new NetworkVariableFloat();
    private GameObject m_Saucer;
    private List<Shield> m_Shields = new List<Shield>();
    private float m_TimeRemaining;

    public static InvadersGame Singleton { get; private set; }

    public NetworkVariableBool hasGameStarted { get; } = new NetworkVariableBool(false);

    public NetworkVariableBool isGameOver { get; } = new NetworkVariableBool(false);

    /// <summary>
    ///     Awake
    ///     A good time to initialize server side values
    /// </summary>
    private void Awake()
    {
        // TODO: Improve this singleton pattern
        Singleton = this;
        OnSingletonReady?.Invoke();

        if (IsServer)
        {
            hasGameStarted.Value = false;

            //Set our time remaining locally
            m_TimeRemaining = m_DelayedStartTime;

            //Set for server side
            m_ReplicatedTimeRemaining.Value = m_DelayedStartTime;
        }
        else
        {
            //We do a check for the client side value upon instantiating the class (should be zero)
            Debug.LogFormat("Client side we started with a timer value of {0}", m_ReplicatedTimeRemaining.Value);
        }
    }

    /// <summary>
    ///     Update
    ///     MonoBehaviour Update method
    /// </summary>
    private void Update()
    {
        //Is the game over?
        if (IsCurrentGameOver()) return;

        //Update game timer (if the game hasn't started)
        UpdateGameTimer();

        //If we are a connected client, then don't update the enemies (server side only)
        if (!IsServer) return;

        //If we are the server and the game has started, then update the enemies
        if (HasGameStarted()) UpdateEnemies();
    }

    /// <summary>
    ///     OnDestroy
    ///     Clean up upon destruction of this class
    /// </summary>
    protected void OnDestroy()
    {
        if (IsServer)
        {
            m_Aliens.Clear();
            m_Shields.Clear();
        }
    }

    internal static event Action OnSingletonReady;

    public override void NetworkStart()
    {
        if (IsClient && !IsServer)
        {
            m_ClientGameOver = false;
            m_ClientStartCountdown = false;
            m_ClientGameStarted = false;

            m_ReplicatedTimeRemaining.OnValueChanged += (oldAmount, newAmount) =>
            {
                // See the ShouldStartCountDown method for when the server updates the value
                if (m_TimeRemaining == 0)
                {
                    Debug.LogFormat("Client side our first timer update value is {0}", newAmount);
                    m_TimeRemaining = newAmount;
                }
                else
                {
                    Debug.LogFormat("Client side we got an update for a timer value of {0} when we shouldn't", m_ReplicatedTimeRemaining.Value);
                }
            };

            m_CountdownStarted.OnValueChanged += (oldValue, newValue) =>
            {
                m_ClientStartCountdown = newValue;
                Debug.LogFormat("Client side we were notified the start count down state was {0}", newValue);
            };

            hasGameStarted.OnValueChanged += (oldValue, newValue) =>
            {
                m_ClientGameStarted = newValue;
                gameTimerText.gameObject.SetActive(!m_ClientGameStarted);
                Debug.LogFormat("Client side we were notified the game started state was {0}", newValue);
            };

            isGameOver.OnValueChanged += (oldValue, newValue) =>
            {
                m_ClientGameOver = newValue;
                Debug.LogFormat("Client side we were notified the game over state was {0}", newValue);
            };
        }

        //Both client and host/server will set the scene state to "ingame" which places the PlayerControl into the SceneTransitionHandler.SceneStates.INGAME
        //and in turn makes the players visible and allows for the players to be controlled.
        SceneTransitionHandler.sceneTransitionHandler.SetSceneState(SceneTransitionHandler.SceneStates.Ingame);

        base.NetworkStart();
    }

    /// <summary>
    ///     ShouldStartCountDown
    ///     Determines when the countdown should start
    /// </summary>
    /// <returns>true or false</returns>
    private bool ShouldStartCountDown()
    {
        //If the game has started, then don't both with the rest of the count down checks.
        if (HasGameStarted()) return false;
        if (IsServer)
        {
            m_CountdownStarted.Value = SceneTransitionHandler.sceneTransitionHandler.AllClientsAreLoaded();

            //While we are counting down, continually set the m_ReplicatedTimeRemaining.Value (client should only receive the update once)
            if (m_CountdownStarted.Value && m_ReplicatedTimeRemaining.Settings.SendTickrate != -1)
            {
                //Now we can specify that we only want this to be sent once
                m_ReplicatedTimeRemaining.Settings.SendTickrate = -1;

                //Now set the value for our one time m_ReplicatedTimeRemaining networked var for clients to get updated once
                m_ReplicatedTimeRemaining.Value = m_DelayedStartTime;
            }

            return m_CountdownStarted.Value;
        }

        return m_ClientStartCountdown;
    }

    /// <summary>
    ///     IsCurrentGameOver
    ///     Returns whether the game is over or not
    /// </summary>
    /// <returns>true or false</returns>
    private bool IsCurrentGameOver()
    {
        if (IsServer)
            return isGameOver.Value;
        return m_ClientGameOver;
    }

    /// <summary>
    ///     HasGameStarted
    ///     Determine whether the game has started or not
    /// </summary>
    /// <returns>true or false</returns>
    private bool HasGameStarted()
    {
        if (IsServer)
            return hasGameStarted.Value;
        return m_ClientGameStarted;
    }

    /// <summary>
    ///     Client side we try to predictively update the gameTimer
    ///     as there shouldn't be a need to receive another update from the server
    ///     We only got the right m_TimeRemaining value when we started so it will be enough
    /// </summary>
    /// <returns> True when m_HasGameStared is set </returns>
    private void UpdateGameTimer()
    {
        if (!ShouldStartCountDown()) return;
        if (!HasGameStarted() && m_TimeRemaining > 0.0f)
        {
            m_TimeRemaining -= Time.deltaTime;

            if (IsServer) // Only the server should be updating this
            {
                if (m_TimeRemaining <= 0.0f)
                {
                    m_TimeRemaining = 0.0f;
                    hasGameStarted.Value = true;
                    OnGameStarted();
                }

                m_ReplicatedTimeRemaining.Value = m_TimeRemaining;
            }

            if (m_TimeRemaining > 0.1f)
                gameTimerText.SetText("{0}", Mathf.FloorToInt(m_TimeRemaining));
        }
    }

    /// <summary>
    ///     OnGameStarted
    ///     Only invoked by the server, this hides the timer text and initializes the aliens and level
    /// </summary>
    private void OnGameStarted()
    {
        gameTimerText.gameObject.SetActive(false);
        CreateAliens();
        CreateShields();
        CreateSaucer();
    }

    private void UpdateEnemies()
    {
        // update aliens
        if (Time.time >= m_NextTick)
        {
            m_NextTick = Time.time + m_TickPeriodic.Value;

            var foundEdge = false;
            var foundEligibleAlienEnemy = false;
            UpdateShootingEnemies(ref foundEligibleAlienEnemy, ref foundEdge);

            if (!foundEligibleAlienEnemy)
            {
                CreateAliens();
                m_TickPeriodic.Value = 0.2f;
            }

            if (foundEdge)
            {
                m_AlienDirection.Value = -m_AlienDirection.Value;
                m_TickPeriodic.Value *= 0.95f; // get faster

                var aliensCount = m_Aliens.Count;
                for (var index = 0; index < aliensCount; index++)
                {
                    var alien = m_Aliens[index];
                    alien.transform.Translate(0, k_AlienVerticalMovementOffset, 0);
                }
            }

            if (m_Saucer == null)
                if (Random.Range(0, 1.0f) > m_RandomThresholdForSaucerCreation)
                    CreateSaucer();
        }
    }

    private bool UpdateShootingEnemies(ref bool foundAlien, ref bool foundEdge)
    {
        var aliensCount = m_Aliens.Count;
        for (var index = 0; index < aliensCount; index++)
        {
            var alien = m_Aliens[index];
            Assert.IsTrue(alien);

            if (alien.score > 100)
                continue;

            foundAlien = true;
            alien.transform.position += new Vector3(m_AlienDirection.Value, 0, 0);

            if (alien.transform.position.x > 10 || alien.transform.position.x < -10)
                foundEdge = true;

            // can shoot if the lowest in my column
            var canShoot = true;
            var column = alien.column;
            var row = alien.row;
            for (var otherIndex = 0; otherIndex < aliensCount; otherIndex++)
            {
                var otherAlien = m_Aliens[otherIndex];
                Assert.IsTrue(otherAlien != null);

                if (Math.Abs(otherAlien.column - column) < 0.001f)
                    if (otherAlien.row < row)
                    {
                        canShoot = false;
                        break;
                    }
            }

            alien.canShoot = canShoot;
        }

        return foundAlien;
    }

    public void SetScore(int score)
    {
        scoreText.SetText("0{0}", score);
    }

    public void SetLives(int lives)
    {
        livesText.SetText("0{0}", lives);
    }

    public void DisplayGameOverText(string message)
    {
        if (gameOverText) gameOverText.gameObject.SetActive(true);
    }

    public void SetGameEnd(bool isGameOver)
    {
        Assert.IsTrue(IsServer, "SetGameEnd should only be called server side!");

        // We should only end the game if all the player's are dead
        if (isGameOver)
        {
            foreach (NetworkClient networkedClient in NetworkManager.Singleton.ConnectedClientsList)
            {
                var playerObject = networkedClient.PlayerObject;
                if(playerObject == null) continue;

                // We should just early out if any of the player's are still alive
                if (playerObject.GetComponent<PlayerControl>().IsAlive)
                    return;
            }
        }
        this.isGameOver.Value = isGameOver;
    }

    public void RegisterSpawnableObject(InvadersObjectType invadersObjectType, GameObject gameObject)
    {
        Assert.IsTrue(IsClient);

        switch (invadersObjectType)
        {
            case InvadersObjectType.Alien:
            {
                // Don't register if this is a saucer
                if (gameObject.TryGetComponent<Saucer>(out var saucer))
                    return;

                gameObject.TryGetComponent<AlienInvader>(out var alienInvader);
                Assert.IsTrue(alienInvader != null);
                if (!m_Aliens.Contains(alienInvader))
                    m_Aliens.Add(alienInvader);
                break;
            }
            case InvadersObjectType.Shield:
            {
                gameObject.TryGetComponent<Shield>(out var shield);
                Assert.IsTrue(shield != null);
                m_Shields.Add(shield);
                break;
            }
            default:
                Assert.IsTrue(false);
                break;
        }
    }

    public void UnregisterSpawnableObject(InvadersObjectType invadersObjectType, GameObject gameObject)
    {
        Assert.IsTrue(IsServer);

        switch (invadersObjectType)
        {
            case InvadersObjectType.Alien:
            {
                // Don't unregister if this is a saucer
                if (gameObject.TryGetComponent<Saucer>(out var saucer))
                    return;

                gameObject.TryGetComponent<AlienInvader>(out var alienInvader);
                Assert.IsTrue(alienInvader != null);
                if (m_Aliens.Contains(alienInvader))
                    Assert.IsTrue(m_Aliens.Remove(alienInvader));
                break;
            }
            case InvadersObjectType.Shield:
            {
                gameObject.TryGetComponent<Shield>(out var shield);
                Assert.IsTrue(shield != null);
                if (m_Shields.Contains(shield))
                    Assert.IsTrue(m_Shields.Remove(shield));
                break;
            }
            default:
                Assert.IsTrue(false);
                break;
        }
    }

    public void ExitGame()
    {
        if (IsServer) NetworkManager.Singleton.StopServer();
        if (IsClient) NetworkManager.Singleton.StopClient();
        SceneTransitionHandler.sceneTransitionHandler.ExitAndLoadStartMenu();
    }

    private void CreateShield(GameObject prefab, int posX, int posY)
    {
        Assert.IsTrue(IsServer, "Create Shield should be called server-side only!");

        const float dy = 0.41f;
        const float dx = 0.41f;

        var ycount = 0;
        for (float y = posY; y < posY + 2; y += dy)
        {
            var xcount = 0;
            for (float x = posX; x < posX + 2; x += dx)
            {
                if (ycount == 4 && (xcount == 0 || xcount == 4))
                {
                    xcount += 1;
                    continue;
                }

                var shield = Instantiate(prefab, new Vector3(x - 1, y, 0), Quaternion.identity);

                // Spawn the Networked Object, this should notify the clients
                shield.GetComponent<NetworkObject>().Spawn();
                xcount += 1;
            }

            ycount += 1;
        }
    }

    private void CreateShields()
    {
        // Create Shields
        CreateShield(shieldPrefab, -7, -1);
        CreateShield(shieldPrefab, 0, -1);
        CreateShield(shieldPrefab, 7, -1);
    }

    private void CreateSaucer()
    {
        Assert.IsTrue(IsServer, "Create Saucer should be called server-side only!");

        m_Saucer = Instantiate(saucerPrefab, saucerSpawnPoint.position, Quaternion.identity);

        // Spawn the Networked Object, this should notify the clients
        m_Saucer.GetComponent<NetworkObject>().Spawn();
    }

    private void CreateAlien(GameObject prefab, float posX, float posY)
    {
        Assert.IsTrue(IsServer, "Create Alien should be called server-side only!");

        var newAlien = Instantiate(prefab);
        newAlien.transform.position = new Vector3(posX, posY, 0.0f);
        newAlien.GetComponent<AlienInvader>().Setup(Mathf.RoundToInt(posX), Mathf.RoundToInt(posY));

        // Spawn the Networked Object, this should notify the clients
        newAlien.GetComponent<NetworkObject>().Spawn();
    }

    public void CreateAliens()
    {
        float startx = -8;
        for (var i = 0; i < 10; i++)
        {
            CreateAlien(alien1Prefab, startx, 12);
            startx += 1.6f;
        }

        startx = -8;
        for (var i = 0; i < 10; i++)
        {
            CreateAlien(alien2Prefab, startx, 10);
            startx += 1.6f;
        }

        startx = -8;
        for (var i = 0; i < 10; i++)
        {
            CreateAlien(alien3Prefab, startx, 8);
            startx += 1.6f;
        }
    }
}
