using System;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Netcode.Samples.MultiplayerUseCases.SelectionScreen
{
    [Serializable]
    internal struct SelectableScene
    {
        [SerializeField] internal string SceneName;
        [SerializeField] internal string DisplayName;
        [SerializeField] internal Texture2D Image;
    }

    /// <summary>
    /// An UI that allows players to pick a scene to load
    /// </summary>
    internal class SceneSelectionUI : MonoBehaviour
    {

        [SerializeField] SelectableScene[] m_Scenes;
        [SerializeField] GridLayoutGroup m_Container;
        [SerializeField] SceneSelectionElement m_SceneUIPrefab;

        void OnEnable()
        {
            Setup();
        }

        void Setup()
        {
            DestroyAllChildrenOf(m_Container.transform);
            foreach (var scene in m_Scenes)
            {
                SceneSelectionElement sceneUI = Instantiate(m_SceneUIPrefab, m_Container.transform);
                sceneUI.Setup(scene);
            }
        }

        static void DestroyAllChildrenOf(Transform t)
        {
            int childrenToRemove = t.childCount;
            for (int i = childrenToRemove - 1; i >= 0; i--)
            {
                GameObject.Destroy(t.GetChild(i).gameObject);
            }
        }
    }
}
