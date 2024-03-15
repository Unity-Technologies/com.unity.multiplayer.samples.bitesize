using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ScoreUI : MonoBehaviour
{
    [SerializeField]
    ServerScoreReplicator m_ScoreTracker;

    [SerializeField]
    UIDocument m_InGameUIDocument;

    VisualElement m_InGameRootVisualElement;

    TextElement m_ScoreText;

    void Awake()
    {
        m_InGameRootVisualElement = m_InGameUIDocument.rootVisualElement;

        m_ScoreText = m_InGameRootVisualElement.Query<TextElement>("ScoreText");
    }

    void Start()
    {
        OnScoreChanged(0, m_ScoreTracker.ReplicatedScore.Value);
        m_ScoreTracker.ReplicatedScore.OnValueChanged += OnScoreChanged;
    }

    void OnDestroy()
    {
        m_ScoreTracker.ReplicatedScore.OnValueChanged -= OnScoreChanged;
    }

    void OnScoreChanged(int previousValue, int newValue)
    {
        m_ScoreText.text = $"{m_ScoreTracker.Score}";
    }
}
