using Unity.Multiplayer.Tools.NetStatsMonitor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.Misc
{
    [RequireComponent(typeof(RuntimeNetStatsMonitor))]
    class RuntimeStatsMonitorController : MonoBehaviour
    {
        [SerializeField]
        InputActionReference m_InteractActionReference;

        RuntimeNetStatsMonitor m_RuntimeNetStatsMonitor;

        void Start()
        {
            m_RuntimeNetStatsMonitor = GetComponent<RuntimeNetStatsMonitor>();
            var uiDocuments = FindObjectsOfType<UIDocument>(true);

            if(m_RuntimeNetStatsMonitor.PanelSettingsOverride == null)
            {
                Debug.LogWarning("Assign PanelSettingsOverride to this MonoBehaviour!", this);
                return;
            }

            if(m_InteractActionReference == null)
            {
                Debug.LogWarning("Assign InputActionReference to this MonoBehaviour!", this);
                return;
            }

            foreach (var uiDoc in uiDocuments)
            {
                if (uiDoc.panelSettings == m_RuntimeNetStatsMonitor.PanelSettingsOverride)
                {
                    var rsnm = uiDoc.runtimePanel.visualTree.Q<VisualElement>(className: "rnsm-monitor");
                    if (rsnm == null)
                    {
                        Debug.LogWarning("Could not find RuntimeNetworkStatsMonitor VisualElement, cannot attach UI.", this);
                        return;
                    }

                    var label = new Label("Toggle visibility with M")
                    {
                        style =
                        {
                            backgroundColor = new StyleColor(Color.black),
                            unityTextAlign = TextAnchor.MiddleCenter
                        }
                    };
                    rsnm.Add(label);
                }
            }

            m_InteractActionReference.action.performed += OnToggleVisibility;
            m_InteractActionReference.action.Enable();
        }

        void OnToggleVisibility(InputAction.CallbackContext obj)
        {
            m_RuntimeNetStatsMonitor.Visible = !m_RuntimeNetStatsMonitor.Visible;
        }
    }
}
