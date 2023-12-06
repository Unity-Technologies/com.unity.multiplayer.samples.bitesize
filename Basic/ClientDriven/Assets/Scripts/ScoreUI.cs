using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class ScoreUI : MonoBehaviour
{
    [SerializeField]
    UIDocument m_InGameUIDocument;

    VisualElement m_InGameRootVisualElement;

    TextElement m_ScoreText;

    void Awake()
    {
        m_InGameRootVisualElement = m_InGameUIDocument.rootVisualElement;

        m_ScoreText = m_InGameRootVisualElement.Query<TextElement>("ScoreText");
    }


    private void OnGUI()
    {
        if (!ServerScoreReplicator.Instance)
        {
            return;
        }
        m_ScoreText.text = $"{ServerScoreReplicator.Instance.Score}";
    }
}
