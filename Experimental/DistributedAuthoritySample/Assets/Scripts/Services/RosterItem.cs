using System;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Vivox;

public class RosterItem : MonoBehaviour
{
    public VivoxParticipant Participant { get; private set; }
    public Text PlayerNameText;
    public Image ChatStateImage;
    public Sprite MutedImage;
    public Sprite SpeakingImage;
    public Sprite NotSpeakingImage;

    public void SetupRosterItem(VivoxParticipant participant)
    {
        Participant = participant;
        PlayerNameText.text = Participant.DisplayName;
        UpdateChatStateImage();
        Participant.ParticipantMuteStateChanged += OnParticipantMuteStateChanged;
        Participant.ParticipantSpeechDetected += OnParticipantSpeechDetected;
    }

    private void OnDestroy()
    {
        if (Participant != null)
        {
            Participant.ParticipantMuteStateChanged -= OnParticipantMuteStateChanged;
            Participant.ParticipantSpeechDetected -= OnParticipantSpeechDetected;
        }
    }

    private void OnParticipantMuteStateChanged()
    {
        UpdateChatStateImage();
    }

    private void OnParticipantSpeechDetected()
    {
        UpdateChatStateImage();
    }

    private void UpdateChatStateImage()
    {
        if (Participant.IsMuted)
        {
            ChatStateImage.sprite = MutedImage;
            ChatStateImage.gameObject.transform.localScale = Vector3.one;
        }
        else if (Participant.SpeechDetected)
        {
            ChatStateImage.sprite = SpeakingImage;
            ChatStateImage.gameObject.transform.localScale = Vector3.one;
        }
        else
        {
            ChatStateImage.sprite = NotSpeakingImage;
        }
    }
}
