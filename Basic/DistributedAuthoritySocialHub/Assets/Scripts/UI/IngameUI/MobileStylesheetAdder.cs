using Unity.Multiplayer.Samples.SocialHub.Input;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    [RequireComponent(typeof(UIDocument))]
    class MobileStylesheetAdder : MonoBehaviour
    {
        [SerializeField]
        StyleSheet[] m_Stylesheet;

        async void Awake()
        {
            var isMobile = await InputSystemManager.IsMobile;
            if (!isMobile)
            {
                return;
            }

            var uiDocument = GetComponent<UIDocument>();
            foreach (var styleSheet in m_Stylesheet)
            {
                uiDocument.rootVisualElement.styleSheets.Add(styleSheet);
            }
        }
    }
}
