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

    void Update()
    {
        m_ScoreText.text = $"{m_ScoreTracker.Score}"; // ouch my perf
    }
}
