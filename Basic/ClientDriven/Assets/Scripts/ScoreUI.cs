using UnityEngine;
using UnityEngine.UI;

public class ScoreUI : MonoBehaviour
{
    [SerializeField]
    private ServerScoreReplicator m_ScoreTracker;

    private Text m_Text;

    void Start()
    {
        m_Text = GetComponent<Text>();
    }

    void Update()
    {
        m_Text.text = $"{m_ScoreTracker.Score}"; // ouch my perf
    }
}
