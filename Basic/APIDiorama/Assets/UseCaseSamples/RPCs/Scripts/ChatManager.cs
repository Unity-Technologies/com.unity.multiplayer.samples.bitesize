using System.Collections;
using UnityEngine;

namespace Unity.Netcode.Samples.APIDiorama
{
    /// <summary>
    /// Manages the chat behaviour of a specific player
    /// </summary>
    public class ChatManager : NetworkBehaviour
    {
        [SerializeField] SpeechBubble m_SpeechBubblePrefab;
        SpeechBubble m_SpeechBubble;

        [SerializeField, Tooltip("The seconds that will elapse between data changes"), Range(2, 5)]
        float m_SecondsBetweenDataChanges;
        float m_ElapsedSecondsSinceLastChange;

        readonly string[] s_ChatMessages = new string[]
        {
            "Have a lovely day",
            "Are you stupid? Fuck you, idiot!",
            "This is the 3rd time I call you, stupid!",
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
                OnServerChatMessageReceivedServerRpc(s_ChatMessages[Random.Range(0, s_ChatMessages.Length)]);
            }
        }

        [ServerRpc(RequireOwnership = true)]
        void OnServerChatMessageReceivedServerRpc(string message)
        {
            string redactedMessage = OnServerFilterBadWords(message);
            OnClientChatMessageReceivedClientRpc(redactedMessage);
        }

        string OnServerFilterBadWords(string message)
        {
            return DioramaUtilities.FilterBadWords(message);
        }

        [ClientRpc]
        void OnClientChatMessageReceivedClientRpc(string message)
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
            m_SpeechBubble.Hide();
            Destroy(m_SpeechBubble.gameObject);
        }
    }
}
