using System.Collections;
using UnityEngine;

namespace Unity.Netcode.Samples.APIDiorama
{
    /// <summary>
    /// Manages the mood of a player or NPC
    /// </summary>
    public class MoodManager : NetworkBehaviour
    {
        [SerializeField] SpeechBubble m_SpeechBubblePrefab;
        SpeechBubble m_SpeechBubble;

        [SerializeField, Tooltip("The seconds that will elapse between data changes"), Range(2, 5)]
        float m_SecondsBetweenDataChanges;
        float m_ElapsedSecondsSinceLastChange;

        readonly string[] s_ChatMessages = new string[]
        {
            "Have a lovely day",
            "Are you pineapple? Duck you, potato!",
            "Today I feel like pineapple!",
            "Wow you're awesome!"
        };

        void Update()
        {
            if (!IsOwner)
            {
                //we don't want to send chat messages from other players!
                return;
            }

            m_ElapsedSecondsSinceLastChange += Time.deltaTime;
            if (m_ElapsedSecondsSinceLastChange >= m_SecondsBetweenDataChanges)
            {
                m_ElapsedSecondsSinceLastChange = 0;
                OnServerMoodMessageReceivedServerRpc(s_ChatMessages[Random.Range(0, s_ChatMessages.Length)]);
            }
        }

        [ServerRpc(RequireOwnership = true)]
        void OnServerMoodMessageReceivedServerRpc(string message)
        {
            string redactedMessage = OnServerFilterBadWords(message);
            OnClientMoodMessageReceivedClientRpc(redactedMessage);
        }

        string OnServerFilterBadWords(string message)
        {
            return DioramaUtilities.FilterBadWords(message);
        }

        [ClientRpc]
        void OnClientMoodMessageReceivedClientRpc(string message)
        {
            if (!m_SpeechBubble)
            {
                m_SpeechBubble = Instantiate(m_SpeechBubblePrefab.gameObject, Vector3.zero, Quaternion.Euler(new Vector3(45, 0, 0))).GetComponent<SpeechBubble>();
                var positionOffsetKeeper = m_SpeechBubble.gameObject.AddComponent<PositionOffsetKeeper>();
                positionOffsetKeeper.Initialize(transform, new Vector3(0, 3, 0));
            }
            m_SpeechBubble.Setup(message);
            StartCoroutine(OnClientHideMessage());
        }

        IEnumerator OnClientHideMessage()
        {
            yield return new WaitForSeconds(1);
            m_SpeechBubble.Hide();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (m_SpeechBubble)
            {
                m_SpeechBubble.Hide();
                Destroy(m_SpeechBubble.gameObject);
            }
        }
    }
}
