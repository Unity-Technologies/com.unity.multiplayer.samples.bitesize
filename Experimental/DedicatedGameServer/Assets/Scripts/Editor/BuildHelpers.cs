using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Editor
{
    ///<summary>
    /// Utility menus to easily create client and server builds to accelerate testing iterations.
    ///</summary>
    public static class BuildHelpers
    {
        const string k_MenuRoot = "DedicatedGameServerSample/Builds/";
        const string k_BuildClient = k_MenuRoot + "Build Client(s)";
        const string k_BuildServer = k_MenuRoot + "Build Server(s)";
        const string k_BuildClientAndServer = k_MenuRoot + "Build Client(s) and Server(s)";

        const string k_ToggleAllName = k_MenuRoot + "Toggle All clients and servers";
        
        const string k_ClientToggleName = k_MenuRoot + "Toggle All clients";
        const string k_ClientMacOSToggleName = k_MenuRoot + "Toggle MacOS client";
        const string k_ClientWindowsToggleName = k_MenuRoot + "Toggle Windows client";
        const string k_ClientLinuxToggleName = k_MenuRoot + "Toggle Linux client";
        
        const string k_ServerToggleName = k_MenuRoot + "Toggle All servers";
        const string k_ServerMacOSToggleName = k_MenuRoot + "Toggle MacOS server";
        const string k_ServerWindowsToggleName = k_MenuRoot + "Toggle Windows server";
        const string k_ServerLinuxToggleName = k_MenuRoot + "Toggle Linux server";
        
        const int k_MenuGroupingBuild = 0; // to add separator in menus
        const int k_MenuGroupingToggles = 11;
        const int k_MenuGroupingClientPlatforms = 22;
        const int k_MenuGroupingServerPlatforms = 33;

        static bool s_ExitApplicationOnFailure = false;
        
        [MenuItem(k_ToggleAllName, false, k_MenuGroupingToggles)]
        static void ToggleAllClientsAndServers()
        {
            var newValue = ToggleMenu(k_ToggleAllName);
            ToggleMenu(k_ClientToggleName, newValue);
            ToggleMenu(k_ClientMacOSToggleName, newValue);
            ToggleMenu(k_ClientWindowsToggleName, newValue);
            ToggleMenu(k_ClientLinuxToggleName, newValue);
            ToggleMenu(k_ServerToggleName, newValue);
            ToggleMenu(k_ServerMacOSToggleName, newValue);
            ToggleMenu(k_ServerWindowsToggleName, newValue);
            ToggleMenu(k_ServerLinuxToggleName, newValue);
        }
        
        [MenuItem(k_ClientToggleName, false, k_MenuGroupingToggles)]
        static void ToggleAllClients()
        {
            var newValue = ToggleMenu(k_ClientToggleName);
            ToggleMenu(k_ClientMacOSToggleName, newValue);
            ToggleMenu(k_ClientWindowsToggleName, newValue);
            ToggleMenu(k_ClientLinuxToggleName, newValue);
        }
        
        [MenuItem(k_ClientMacOSToggleName, false, k_MenuGroupingClientPlatforms)]
        static void ToggleClientMacOS()
        {
            ToggleMenu(k_ClientMacOSToggleName);
        }
        
        [MenuItem(k_ClientWindowsToggleName, false, k_MenuGroupingClientPlatforms)]
        static void ToggleClientWindows()
        {
            ToggleMenu(k_ClientWindowsToggleName);
        }
        
        [MenuItem(k_ClientLinuxToggleName, false, k_MenuGroupingClientPlatforms)]
        static void ToggleClientLinux()
        {
            ToggleMenu(k_ClientLinuxToggleName);
        }
        
        [MenuItem(k_ServerToggleName, false, k_MenuGroupingToggles)]
        static void ToggleAllServers()
        {
            var newValue = ToggleMenu(k_ServerToggleName);
            ToggleMenu(k_ServerMacOSToggleName, newValue);
            ToggleMenu(k_ServerWindowsToggleName, newValue);
            ToggleMenu(k_ServerLinuxToggleName, newValue);
        }
        
        [MenuItem(k_ServerMacOSToggleName, false, k_MenuGroupingServerPlatforms)]
        static void ToggleServerMacOS()
        {
            ToggleMenu(k_ServerMacOSToggleName);
        }
        
        [MenuItem(k_ServerWindowsToggleName, false, k_MenuGroupingServerPlatforms)]
        static void ToggleServerWindows()
        {
            ToggleMenu(k_ServerWindowsToggleName);
        }
        
        [MenuItem(k_ServerLinuxToggleName, false, k_MenuGroupingServerPlatforms)]
        static void ToggleServerLinux()
        {
            ToggleMenu(k_ServerLinuxToggleName);
        }
        
        [MenuItem(k_BuildClientAndServer, true)]
        static bool CanBuildServerAndClient()
        {
            return CanBuildClient() && CanBuildServer();
        }

        [MenuItem(k_BuildClientAndServer, false, k_MenuGroupingBuild)]
        static void BuildEnabledServersAndClients()
        {
            BuildAllEnabledServers();
            BuildAllEnabledClients();
        }
        
        [MenuItem(k_BuildServer, true)]
        static bool CanBuildServer()
        {
            return Menu.GetChecked(k_ServerMacOSToggleName) ||
                Menu.GetChecked(k_ServerWindowsToggleName) ||
                Menu.GetChecked(k_ServerLinuxToggleName);
        }

        [MenuItem(k_BuildServer, false, k_MenuGroupingBuild)]
        static void BuildAllEnabledServers()
        {
            bool buildMacOS = Menu.GetChecked(k_ServerMacOSToggleName);
            bool buildWindows = Menu.GetChecked(k_ServerWindowsToggleName);
            bool buildLinux = Menu.GetChecked(k_ServerLinuxToggleName);

            var buildPathRoot = Path.Combine("Builds", "Server");

            DeleteOutputFolder("Server/");

            if (buildMacOS)
            {
                BuildProcessor.BuildServer(BuildTarget.StandaloneOSX, Path.Combine(buildPathRoot, "MacOSX", "Game.app"), s_ExitApplicationOnFailure);
            }

            if (buildWindows)
            {
                BuildProcessor.BuildServer(BuildTarget.StandaloneWindows, Path.Combine(buildPathRoot, "Windows10", "Game.exe"), s_ExitApplicationOnFailure);
            }

            if (buildLinux)
            {
                BuildProcessor.BuildServer(BuildTarget.StandaloneLinux64, Path.Combine(buildPathRoot, "Linux64", "Game.x86_64"), s_ExitApplicationOnFailure);
            }
        }
        
        [MenuItem(k_BuildClient, true)]
        static bool CanBuildClient()
        {
            return Menu.GetChecked(k_ClientMacOSToggleName) ||
                Menu.GetChecked(k_ClientWindowsToggleName) ||
                Menu.GetChecked(k_ClientLinuxToggleName);
        }

        [MenuItem(k_BuildClient, false, k_MenuGroupingBuild)]
        static void BuildAllEnabledClients()
        {
            bool buildMacOS = Menu.GetChecked(k_ClientMacOSToggleName);
            bool buildWindows = Menu.GetChecked(k_ClientWindowsToggleName);
            bool buildLinux = Menu.GetChecked(k_ClientLinuxToggleName);

            var buildPathRoot = Path.Combine("Builds", "Client");

            DeleteOutputFolder("Client/");

            if (buildMacOS)
            {
                BuildProcessor.BuildClient(BuildTarget.StandaloneOSX, Path.Combine(buildPathRoot, "MacOSX", "Game.app"), s_ExitApplicationOnFailure);
            }

            if (buildWindows)
            {
                BuildProcessor.BuildClient(BuildTarget.StandaloneWindows, Path.Combine(buildPathRoot, "Windows10", "Game.exe"), s_ExitApplicationOnFailure);
            }

            if (buildLinux)
            {
                BuildProcessor.BuildClient(BuildTarget.StandaloneLinux64, Path.Combine(buildPathRoot, "Linux64", "Game.x86_64"), s_ExitApplicationOnFailure);
            }
        }
        
        /// <summary>
        /// Toggles everything on and builds a client and a server for each platform. This is used in the continuous integration flow.
        /// </summary>
        public static void BuildEverything()
        {
            // setting menus unchecked so toggling afterwards will check everything
            ToggleMenu(k_ClientToggleName, false);
            ToggleMenu(k_ServerToggleName, false);
            
            // toggling on every platform
            ToggleAllClients();
            ToggleAllServers();
            s_ExitApplicationOnFailure = true;
            BuildEnabledServersAndClients();
            s_ExitApplicationOnFailure = false;
        }

        static bool ToggleMenu(string menuName, bool? valueToSet = null)
        {
            var toSet = valueToSet != null ? valueToSet.Value : !Menu.GetChecked(menuName);

            Menu.SetChecked(menuName, toSet);
            return toSet;
        }

        static void DeleteOutputFolder(string pathFromBuildsFolder)
        {
            string projectPath = Path.Combine(Application.dataPath, "..", "Builds", pathFromBuildsFolder);
            var directoryInfo = new FileInfo(projectPath).Directory;
            if (directoryInfo != null && directoryInfo.Exists)
            {
                directoryInfo.Delete(true);
            }
        }
    }
}
