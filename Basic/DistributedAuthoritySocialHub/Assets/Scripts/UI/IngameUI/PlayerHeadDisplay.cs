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

        const string k_PlayerMutedUSSClass = "player-mic-icon--muted";
        const string k_PlayerMicIconHidden = "player-mic-icon--disable";

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

            m_Participant.ParticipantMuteStateChanged -= EvaluateMuteState;
            m_Participant.ParticipantMuteStateChanged += EvaluateMuteState;

            m_Participant.ParticipantSpeechDetected -= OnParticipantSpeechDetected;
            m_Participant.ParticipantSpeechDetected += OnParticipantSpeechDetected;
            Debug.Log("Done Attaching + " + m_Participant.DisplayName);
            EvaluateMuteState();
        }

        internal void RemoveVivoxParticipant()
        {
            if(m_Participant == null)
                return;

            m_Participant.ParticipantMuteStateChanged -= EvaluateMuteState;
            m_Participant.ParticipantSpeechDetected -= OnParticipantSpeechDetected;
        }

        void OnParticipantSpeechDetected()
        {
            Debug.Log("Speaking + " + m_Participant.DisplayName + "is Muted" + m_Participant.IsMuted);
            if(m_Participant.IsMuted)
                return;

            ShowMicIcon(m_Participant.SpeechDetected);
        }

        void EvaluateMuteState()
        {
            Debug.Log("Checking Mic state for + " + m_Participant.DisplayName +"---"+ m_Participant.IsMuted);
            if (m_Participant.IsMuted)
            {
                m_MicIcon.AddToClassList(k_PlayerMutedUSSClass);
                ShowMicIcon(true);
                return;
            }
            m_MicIcon.RemoveFromClassList(k_PlayerMutedUSSClass);
        }

        internal void SetPlayerName(string playerName)
        {
            m_PlayerNameLabel.text = playerName;
        }

        void ShowMicIcon(bool show)
        {
            if (show)
                m_MicIcon.RemoveFromClassList(k_PlayerMicIconHidden);
            else
                m_MicIcon.AddToClassList(k_PlayerMicIconHidden);
        }
    }
}
