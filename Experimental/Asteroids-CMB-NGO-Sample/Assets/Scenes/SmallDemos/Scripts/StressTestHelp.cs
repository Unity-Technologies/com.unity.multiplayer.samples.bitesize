using UnityEngine;

public class StressTestHelp : MonoBehaviour
{
    public GameObject HelpInfo;
    public GameObject HelpInfoNotification;
    private bool m_DisplayHelp;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H)) 
        { 
            if (m_DisplayHelp)
            {
                HelpInfo.SetActive(false);
                HelpInfoNotification.SetActive(true);
                m_DisplayHelp = false;
            }
            else
            {
                HelpInfo.SetActive(true);
                HelpInfoNotification.SetActive(false);
                m_DisplayHelp = true;
            }
        }
    }
}
