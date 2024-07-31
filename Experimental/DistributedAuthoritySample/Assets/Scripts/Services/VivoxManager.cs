using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Vivox;

namespace Services
{
    public class VivoxManager : MonoBehaviour
    {
        private List<RosterItem> m_RosterItems = new List<RosterItem>();
        public GameObject m_ParticipantPrefab; // Assign this in the Inspector
        public Transform m_ParticipantListParent; // Assign this in the Inspector

        public static VivoxManager Instance { get; private set; }

        public static string PlayerProfileName { get; private set; }
        public string SessionName { get; set; }

        [SerializeField] private int ProxyAudibleDistance = 10, ConversationalDistance = 10;
        [SerializeField] private float AudioFadeIntensityByDistanceAudio = 10.0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public async Task InitializeVivoxAsync(string playerName)
        {
            PlayerProfileName = playerName;
            await VivoxService.Instance.InitializeAsync();
            await LoginToVivoxAsync();
        }

        private async Task LoginToVivoxAsync()
        {
            LoginOptions options = new LoginOptions();
            options.DisplayName = "Player_" + PlayerProfileName;
            options.EnableTTS = false;
            VivoxService.Instance.LoggedIn += LoggedInToVivox;
            await VivoxService.Instance.LoginAsync(options);
        }

        public async void JoinChannel(string channelName)
        {
            SessionName = channelName;
            var channelOptions = new ChannelOptions();
            //await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.TextAndAudio, channelOptions);

            // TODO: proxy chat
            await VivoxService.Instance.JoinPositionalChannelAsync(channelName, ChatCapability.TextAndAudio,
                new Channel3DProperties(
                    ProxyAudibleDistance,
                    ConversationalDistance,
                    AudioFadeIntensityByDistanceAudio,
                    AudioFadeModel.ExponentialByDistance
                ),
                channelOptions);

            Debug.Log("Joined text and audio channel");
        }

        void LoggedInToVivox()
        {
            Debug.Log(nameof(LoggedInToVivox));
        }

        private void BindSessionEvents(bool doBind)
        {
            if(doBind)
            {
                VivoxService.Instance.ParticipantAddedToChannel += onParticipantAddedToChannel;
                VivoxService.Instance.ParticipantRemovedFromChannel += onParticipantRemovedFromChannel;
            }
            else
            {
                VivoxService.Instance.ParticipantAddedToChannel -= onParticipantAddedToChannel;
                VivoxService.Instance.ParticipantRemovedFromChannel -= onParticipantRemovedFromChannel;
            }
        }

        private void onParticipantAddedToChannel(VivoxParticipant participant)
        {
            var participantGO = Instantiate(m_ParticipantPrefab, m_ParticipantListParent);
            var participantComponent = participantGO.GetComponent<RosterItem>();
            participantComponent.SetupRosterItem(participant);
            //RosterItem newRosterItem = new RosterItem();
            //newRosterItem.SetupRosterItem(participant);
            m_RosterItems.Add(participantComponent);
        }

        private void onParticipantRemovedFromChannel(VivoxParticipant participant)
        {
            var participantToRemove = m_RosterItems.Find(p => p.Participant.PlayerId == participant.PlayerId);
            if (participantToRemove != null)
            {
                m_RosterItems.Remove(participantToRemove);
                Destroy(participantToRemove.gameObject);
            }
            /*RosterItem rosterItemToRemove = m_RosterItems.FirstOrDefault(p => p.Participant.PlayerId == participant.PlayerId);
            m_RosterItems.Remove(rosterItemToRemove);*/
        }
    }
}
