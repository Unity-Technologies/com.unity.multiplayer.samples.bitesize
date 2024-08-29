using TMPro;
using Unity.Collections;
using Unity.Netcode.Samples.MultiplayerUseCases.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Netcode.Samples.MultiplayerUseCases.NetworkVariables
{
    /// <summary>
    /// A complex data structure. Can only contain the types listed here: https://docs-multiplayer.unity3d.com/netcode/current/basics/networkvariable/index.html#supported-types
    /// </summary>
    struct SyncableCustomData : INetworkSerializable
    {
        public int Health;
        public FixedString128Bytes Username; //value-type version of string with fixed allocation. Strings should be avoided in general when dealing with netcode. Fixed strings are a "less bad" option.

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Health);
            serializer.SerializeValue(ref Username);
        }
    }

    /// <summary>
    /// Manages the data of a character
    /// </summary>
    public class CharacterManager : NetworkBehaviour
    {
        /// <summary>
        /// The NetworkVariable holding the custom data to synchronize.
        /// </summary>
        NetworkVariable<SyncableCustomData> m_SyncedCustomData = new NetworkVariable<SyncableCustomData>(writePerm: NetworkVariableWritePermission.Owner); //you can adjust who can write to it with parameters

        [SerializeField] Image m_HealthBarImage;
        [SerializeField] TMP_Text m_UsernameLabel;

        [SerializeField, Tooltip("The seconds that will elapse between data changes")]
        float m_SecondsBetweenDataChanges;
        float m_ElapsedSecondsSinceLastChange;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsClient)
            {
                /*
                 * We call the color change method manually when we connect to ensure that our color is correctly initialized.
                 * This is helpful for when a client joins mid-game and needs to catch up with the current state of the game.
                 */
                OnClientCustomDataChanged(m_SyncedCustomData.Value, m_SyncedCustomData.Value);
                m_SyncedCustomData.OnValueChanged += OnClientCustomDataChanged; //this will be called on the client whenever the value is changed by the server
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (IsClient)
            {
                m_SyncedCustomData.OnValueChanged -= OnClientCustomDataChanged;
            }
        }

        void Update()
        {
            if (!IsSpawned)
            {
                //the player disconnected
                return;
            }

            if (!IsServer)
            {
                /*
                 * By default, only the server is allowed to change the value of NetworkVariables.
                 * This can be changed through the NetworkVariable's constructor.
                 */
                return;
            }

            m_ElapsedSecondsSinceLastChange += Time.deltaTime;

            if (m_ElapsedSecondsSinceLastChange >= m_SecondsBetweenDataChanges)
            {
                m_ElapsedSecondsSinceLastChange = 0;
                OnServerChangeData();
            }
        }


        void OnServerChangeData()
        {
            m_SyncedCustomData.Value = new SyncableCustomData
            {
                Health = Random.Range(10, 101),
                Username = MultiplayerUseCasesUtilities.GetRandomUsername()
            };
        }

        void OnClientHealthChanged(int previousHealth, int newHealth)
        {
            m_HealthBarImage.rectTransform.localScale = new Vector3((float)newHealth / 100.0f, 1);//(float)newHealth / 100.0f;
            OnClientUpdateHealthBarColor(newHealth);
            //note: you could use the previousHealth to play a healing/damage animation
        }

        void OnClientUpdateHealthBarColor(int newHealth)
        {
            const int k_MaxHealth = 100;
            float healthPercent = (float)newHealth / k_MaxHealth;
            Color healthBarColor = new Color(1 - healthPercent, healthPercent, 0);
            m_HealthBarImage.color = healthBarColor;
        }

        void OnClientUsernameChanged(string newUsername)
        {
            m_UsernameLabel.text = newUsername;
        }

        void OnClientCustomDataChanged(SyncableCustomData previousValue, SyncableCustomData newValue)
        {
            OnClientHealthChanged(previousValue.Health, newValue.Health);
            OnClientUsernameChanged(newValue.Username.ToString());
        }
    }
}
