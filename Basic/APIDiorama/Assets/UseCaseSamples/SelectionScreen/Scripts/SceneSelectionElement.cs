using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Unity.Netcode.Samples.APIDiorama.SelectionScreen
{
    /// <summary>
    /// Allows to select a scene in the SelectionScene
    /// </summary>
    internal class SceneSelectionElement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] Button m_SceneButton;
        [SerializeField] TMP_Text m_TitleLabel;
        [SerializeField] Image m_ContrastImage;

        internal void Setup(SelectableScene selectableScene)
        {
            m_SceneButton.onClick.RemoveAllListeners();
            m_SceneButton.onClick.AddListener(() => OnClick(selectableScene.SceneName));
            m_TitleLabel.text = selectableScene.DisplayName;
            if (selectableScene.Image)
            {
                m_SceneButton.image.sprite = Sprite.Create(selectableScene.Image, new Rect(0, 0, selectableScene.Image.width, selectableScene.Image.height), new Vector2(0.5f, 0.5f));
            }
            EnableOverlayElements(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            EnableOverlayElements(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            EnableOverlayElements(false);
        }

        void EnableOverlayElements(bool enable)
        {
            m_ContrastImage.gameObject.SetActive(enable);
            m_TitleLabel.gameObject.SetActive(enable);
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