using System;
using System.Collections;
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
#if NGO_DAMODE
        StartCoroutine(WaitForScoreTracker());
#else
        OnScoreChanged(0, m_ScoreTracker.ReplicatedScore.Value);
        m_ScoreTracker.ReplicatedScore.OnValueChanged += OnScoreChanged;
#endif
    }

    private IEnumerator WaitForScoreTracker()
    {
        var waitTime = new WaitForSeconds(0.1f);

        while(!ServerScoreReplicator.Instance)
        {
            yield return waitTime;
        }

        m_ScoreTracker = ServerScoreReplicator.Instance;
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
