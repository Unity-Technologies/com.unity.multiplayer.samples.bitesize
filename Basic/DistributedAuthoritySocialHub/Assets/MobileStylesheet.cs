using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MobileStylesheet : MonoBehaviour
{
    [SerializeField]
    StyleSheet[] m_Stylesheet;
    void Start()
    {
        var uiDocument = GetComponent<UIDocument>();
        foreach (var styleSheet in m_Stylesheet)
        {
            uiDocument.rootVisualElement.styleSheets.Add(styleSheet);
        }
    }
}
