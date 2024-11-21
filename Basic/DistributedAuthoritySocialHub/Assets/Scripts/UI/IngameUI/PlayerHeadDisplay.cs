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
        Label m_PlayerNameLabel;

        internal VivoxParticipant VivoxParticipant => m_Participant;
        internal string PlayerId { get; set; }

        /// <summary>
        /// Display that is shown above a players head
        /// </summary>
        /// <param name="asset">Uxml to be used</param>
        internal PlayerHeadDisplay(VisualTreeAsset asset)
        {
            AddToClassList("player-top-ui");
            Add(asset.CloneTree());
            m_PlayerNameLabel = this.Q<Label>();
            m_MicIcon = this.Q<VisualElement>("mic-icon");
            ShowMicIcon(false);
        }

        internal void AttachVivoxParticipant(VivoxParticipant participant)
        {
            m_Participant = participant;
            m_Participant.ParticipantMuteStateChanged += OnParticipantMuteStateChanged;
            m_Participant.ParticipantSpeechDetected += OnParticipantSpeechDetected;
        }

        internal void RemoveVivoxParticipant(VivoxParticipant participant)
        {
            m_Participant.ParticipantMuteStateChanged -= OnParticipantMuteStateChanged;
            m_Participant.ParticipantSpeechDetected -= OnParticipantSpeechDetected;
            m_Participant = null;
        }

        void OnParticipantSpeechDetected()
        {
            ShowMicIcon(true);
            m_Scheduler ??= schedule.Execute(FadeOutMicIcon);
            m_Scheduler.ExecuteLater(3000);
        }

        void OnParticipantMuteStateChanged()
        {

        }

        void FadeOutMicIcon()
        {
            ShowMicIcon(false);
        }

        internal void SetPlayerName(string playerName)
        {
            m_PlayerNameLabel.text = playerName;
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
