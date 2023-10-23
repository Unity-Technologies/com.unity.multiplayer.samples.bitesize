using System.Threading.Tasks;
using Unity.DedicatedGameServerSample.Runtime;
using Unity.DedicatedGameServerSample.Runtime.SimpleJSON;
using Unity.DedicatedGameServerSample.Shared;
using Unity.Multiplayer;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.DedicatedGameServerSample.Editor
{
    ///<summary>
    /// Allows for fast switching between host/server only/client only modes in unity editor
    ///</summary>
    public class BootstrapperWindow : EditorWindow
    {
        ConfigurationManager Configuration
        {
            get
            {
                try
                {
                    if (m_Configuration == null)
                    {
                        m_Configuration = new ConfigurationManager(ConfigurationManager.k_DevConfigFile);
                    }
                    return m_Configuration;
                }
                catch (System.IO.FileNotFoundException)
                {
                    m_Configuration = new ConfigurationManager(ConfigurationManager.k_DevConfigFile, true);
                    ResetConfigurationToDefault();
                    return m_Configuration;
                }
            }
        }
        ConfigurationManager m_Configuration;

        /// <summary>
        /// How many players are needed to fill a game instance?
        /// </summary>
        public int MinPlayers
        {
            get => Configuration.GetInt(ConfigurationManager.k_MinPlayers);
            set => Configuration.Set(ConfigurationManager.k_MinPlayers, value);
        }

        /// <summary>
        /// How many players can a game instance accept?
        /// </summary>
        public int MaxPlayers
        {
            get => Configuration.GetInt(ConfigurationManager.k_MaxPlayers);
            set => Configuration.Set(ConfigurationManager.k_MaxPlayers, value);
        }

        /// <summary>
        /// Will the game run in a specific mode when started in the editor?
        /// </summary>
        public bool AutoConnectOnStartup
        {
            get => Configuration.GetBool(ConfigurationManager.k_Autoconnect);
            set => Configuration.Set(ConfigurationManager.k_Autoconnect, value);
        }

        VisualElement m_Root;
        Toggle m_AutoConnectOnStartupToggle;
        IntegerField m_MinPlayers;
        IntegerField m_MaxPlayers;

        /// <summary>
        /// Opens the bootstrapper window
        /// </summary>
        [MenuItem("Multiplayer/Bootstrapper")]
        public static void ShowWindow()
        {
            var window = GetWindow<BootstrapperWindow>("Bootstrapper");
            window.Show();
        }

        void SetupFrontend()
        {
            if (m_Root != null)
            {
                m_Root.Clear();
            }
            m_Root = rootVisualElement;

            VisualTreeAsset playerVisualTree = UIElementsUtils.LoadUXML("Bootstrapper");
            playerVisualTree.CloneTree(m_Root);

            UIElementsUtils.SetupButton("btnReset", OnClickReset, true, m_Root, "Reset to default", "Resets the state of the m_Configuration file to the one of the template provided in Resources/DefaultConfigurations/");
            m_AutoConnectOnStartupToggle = UIElementsUtils.SetupToggle("tglAutoConnectOnStartup", "Autoconnect on startup", string.Empty, AutoConnectOnStartup, OnAutoConnectChanged, m_Root);
            m_MinPlayers = UIElementsUtils.SetupIntegerField("intMinPlayers", MinPlayers, OnMinPlayersChanged, m_Root);
            m_MaxPlayers = UIElementsUtils.SetupIntegerField("intMaxPlayers", MaxPlayers, OnMaxPlayersChanged, m_Root);
            UpdateUIAccordingToNetworkMode();
        }

        void UpdateUIAccordingToNetworkMode()
        {
            UIElementsUtils.Show(m_AutoConnectOnStartupToggle);
            if (MultiplayerRolesManager.ActiveMultiplayerRoleMask == MultiplayerRoleFlags.Server)
            {
                UIElementsUtils.Show(m_MinPlayers);
                UIElementsUtils.Show(m_MaxPlayers);
            }
            else
            {
                UIElementsUtils.Hide(m_MinPlayers);
                UIElementsUtils.Hide(m_MaxPlayers);
            }
            
        }

        void OnAutoConnectChanged(ChangeEvent<bool> evt)
        {
            AutoConnectOnStartup = evt.newValue;
            ApplyChanges();
        }

        void OnMinPlayersChanged(ChangeEvent<int> evt)
        {
            MinPlayers = evt.newValue;
            ApplyChanges();
        }

        void OnMaxPlayersChanged(ChangeEvent<int> evt)
        {
            MaxPlayers = evt.newValue;
            ApplyChanges();
        }

        async void OnClickReset()
        {
            await ResetConfigurationToDefaultAsync();
        }

        void OnEnable()
        {
            SetupFrontend();
        }

        void ResetConfigurationToDefault()
        {
            OverwriteConfigurationAndReload(JSONUtilities.ReadJSONFromFile(ConfigurationManager.k_DevConfigFileDefault));
        }

        async Task ResetConfigurationToDefaultAsync()
        {
            JSONNode json = await JSONUtilities.ReadJSONFromFileAsync(ConfigurationManager.k_DevConfigFileDefault);
            OverwriteConfigurationAndReload(json);
        }

        void OverwriteConfigurationAndReload(JSONNode json)
        {
            m_Configuration.Overwrite(json);
            OnEnable();
            ApplyChanges();
        }

        void ApplyChanges()
        {
            Configuration.SaveAsJSON(false);
            UpdateUIAccordingToNetworkMode();
        }
    }
}
