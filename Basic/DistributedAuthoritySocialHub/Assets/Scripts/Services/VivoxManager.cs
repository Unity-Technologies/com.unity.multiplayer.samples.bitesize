using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;
using Unity.Multiplayer.Samples.SocialHub.Services;
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

        public async void LoginVivox(string playerName, string playerId)
        {
            await VivoxService.Instance.InitializeAsync();
            var loginOptions = new LoginOptions()
            {
                DisplayName = playerName,
                PlayerId = playerId,
            };
            await VivoxService.Instance.LoginAsync(loginOptions);
        }

        public async void JoinChannel(string channelName)
        {
            SessionName = channelName;
            var channelOptions = new ChannelOptions();
            await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.TextAndAudio, channelOptions);


            //Todo: this is a temporary solution to initialize the text chat manager
            //it is not guarenteed that TExtchatmanger is available already
            var textChatManager = FindFirstObjectByType<TextChatManager>();
            if(textChatManager != null)
            {
                textChatManager.Initialize();
                Debug.Log("Joined text and audio channel");
            }
        }

        // Todo: readd when adding Positional VoiceChat
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
