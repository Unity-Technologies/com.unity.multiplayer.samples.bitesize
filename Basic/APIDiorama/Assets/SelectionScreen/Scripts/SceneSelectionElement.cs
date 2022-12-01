using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Unity.Netcode.Samples.APIDiorama
{
    /// <summary>
    /// Allows to select a scene in the SelectionScene
    /// </summary>
    internal class SceneSelectionElement : MonoBehaviour
    {
        [SerializeField] Button m_SceneButton;
        [SerializeField] TMP_Text m_TitleLabel;

        internal void Setup(SelectableScene selectableScene)
        {
            m_SceneButton.onClick.RemoveAllListeners();
            m_SceneButton.onClick.AddListener(() => OnClick(selectableScene.Scene.SceneName));
            m_TitleLabel.text = selectableScene.DisplayName;
        }

        void OnClick(string sceneName)
        {
            LoadScene(sceneName);
        }

        void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
    }
}