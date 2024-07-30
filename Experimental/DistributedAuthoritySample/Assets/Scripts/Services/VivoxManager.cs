using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Vivox;

namespace Services
{
    public class VivoxManager : MonoBehaviour
    {
        public static VivoxManager Instance { get; private set; }

        private List<RosterItem> m_RosterItems = new List<RosterItem>();
        public GameObject m_ParticipantPrefab; // Assign this in the Inspector
        public Transform m_ParticipantListParent; // Assign this in the Inspector

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

        private void Start()
        {
            InitializeVivoxAsync();
        }

        public async Task InitializeVivoxAsync()
        {
            await VivoxService.Instance.InitializeAsync();
            await LoginToVivoxAsync();
        }

        private async Task LoginToVivoxAsync()
        {
            LoginOptions options = new LoginOptions();
            options.DisplayName = "Player_" + AuthenticationService.Instance.PlayerId;
            options.EnableTTS = false;
            VivoxService.Instance.LoggedIn += LoggedInToVivox;
            await VivoxService.Instance.LoginAsync(options);

            // TODO change the channel name
            JoinChannel("GeneralChat");
        }

        async void JoinChannel(string channelName)
        {
            var channelOptions = new ChannelOptions();
            await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.TextOnly, channelOptions);
            Debug.Log("Joined Text Channel");
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
