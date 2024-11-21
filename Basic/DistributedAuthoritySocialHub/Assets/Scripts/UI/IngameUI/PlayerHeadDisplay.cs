using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    class PlayerHeadDisplay : VisualElement
    {
        VivoxParticipant m_Participant;
        IVisualElementScheduledItem m_Scheduler;
        VisualElement m_MicIcon;

        /// <summary>
        /// Display that is shown above a players head
        /// </summary>
        /// <param name="asset">Uxml to be used</param>
        internal PlayerHeadDisplay(VisualTreeAsset asset)
        {
            AddToClassList("player-top-ui");
            Add(asset.CloneTree());
            m_MicIcon = this.Q<VisualElement>("mic-icon");
            ShowMicIcon(false);
        }

        public VivoxParticipant VivoxParticipant => m_Participant;
        public string GetPlayerName => this.Q<Label>().text;

        internal void InitVivox(VivoxParticipant participant)
        {
            m_Participant = participant;
            m_Participant.ParticipantMuteStateChanged += OnParticipantMuteStateChanged;
            m_Participant.ParticipantSpeechDetected += OnParticipantSpeechDetected;
        }

        internal void RemoveVivox(VivoxParticipant participant)
        {
            m_Participant.ParticipantMuteStateChanged -= OnParticipantMuteStateChanged;
            m_Participant.ParticipantSpeechDetected -= OnParticipantSpeechDetected;
        }

        void OnParticipantSpeechDetected()
        {
            ShowMicIcon(true);
            m_Scheduler ??= schedule.Execute(FadeOutMicIcon);
            m_Scheduler.ExecuteLater(3000);
        }

        void FadeOutMicIcon()
        {
            ShowMicIcon(false);
        }

        void OnParticipantMuteStateChanged()
        {

        }

        internal void SetPlayerName(string playerName)
        {
            this.Q<Label>().text = playerName;
        }

        void ShowMicIcon(bool show)
        {
            if (show)
            {
                m_MicIcon.RemoveFromClassList("player-mic-icon--disable");
            }
            else
            {
                m_MicIcon.AddToClassList("player-mic-icon--disable");
            }
        }
    }
}
