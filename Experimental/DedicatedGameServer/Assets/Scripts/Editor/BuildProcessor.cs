using System.Collections.Generic;
using System.Linq;
using Unity.DedicatedGameServerSample.Runtime;
using Unity.Multiplayer;
using Unity.Multiplayer.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Editor
{
    ///<summary>
    ///Performs additional operations before/after the build is done
    ///</summary>
    public class BuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        static readonly string[] k_BuildOnlySymbols = new string[]
        {
            //"LIVE", //this is an example, add your own symbols instead
        };

        static readonly string[] k_EditorOnlySymbols = new string[]
        {
            //"DEV", //this is an example, add your own symbols instead
        };

        /// <summary>
        /// CallbackOrder of the preprocessing and postprocessing calls.
        /// </summary>
        public int callbackOrder => 0;

        /// <summary>
        /// Called at the beginning of the build process
        /// </summary>
        /// <param name="report">The generated build report.</param>
        public void OnPreprocessBuild(BuildReport report)
        {
            DisableBurstCompiler();
            ApplyChangesToMetagameApplication();

            string definesString = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
            List<string> allDefines = definesString.Split(';').ToList();
            if (k_BuildOnlySymbols.Length > 0)
            {
                allDefines.AddRange(k_BuildOnlySymbols.Except(allDefines));
            }

            if (k_EditorOnlySymbols.Length > 0)
            {
                allDefines.RemoveAll(def => k_EditorOnlySymbols.Contains(def));
            }

            Debug.Log($"Symbols used for build: {string.Join(";", allDefines.ToArray())}");
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup), string.Join(";", allDefines.ToArray()));
        }

        void DisableBurstCompiler()
        {
            //unfortunately we can't use burst compilation due to a 
            //bug in its latest version, so we need to disable it.
            //It annoyingly re-enables every time you switch platform...
            Burst.BurstCompiler.Options.EnableBurstCompilation = false;
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Called at the end of the build process
        /// </summary>
        /// <param name="report">The generated build report.</param>
        public void OnPostprocessBuild(BuildReport report)
        {
            DisableBurstCompiler();
            RevertChangesToMetagameApplication();
            string definesString = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
            List<string> allDefines = definesString.Split(';').ToList();

            if (k_BuildOnlySymbols.Length > 0)
            {
                allDefines.RemoveAll(def => k_BuildOnlySymbols.Contains(def));
            }

            if (k_EditorOnlySymbols.Length > 0)
            {
                allDefines.AddRange(k_EditorOnlySymbols.Except(allDefines));
            }

            Debug.Log($"Symbols restored after build: {string.Join(";", allDefines.ToArray())}");
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup), string.Join(";", allDefines.ToArray()));
            AssetDatabase.SaveAssets();
#if !CLOUD_BUILD_WINDOWS && !CLOUD_BUILD_LINUX && !CLOUD_BUILD_MAX
            Debug.Log($"Manually Doing PostExport: {report.summary.outputPath}");
            bool isServerBuild = report.summary.outputPath.Contains(".x86_64", System.StringComparison.OrdinalIgnoreCase); //.x86_64 is the extension of the Linux build
            CloudBuildHelpers.PostExport(report.summary.outputPath, isServerBuild);
#endif
        }

        void ApplyChangesToMetagameApplication()
        {
            MetagameApplication app = FindMetagameAppInProject();

            //add your code to apply changes to the MetagameApplication here, I.E: to reference different testing environments
            PrefabUtility.SavePrefabAsset(app.gameObject, out bool savedSuccessfully);
            if (!savedSuccessfully)
            {
                throw new BuildPlayerWindow.BuildMethodException("Failed to alter MetagameApplication before building");
            }

            Debug.Log("Updated MetagameApp before build");
        }

        void RevertChangesToMetagameApplication()
        {
            MetagameApplication app = FindMetagameAppInProject();

            //add your code to revert changes to the MetagameApplication here, I.E: to reference different testing environments
            PrefabUtility.SavePrefabAsset(app.gameObject, out bool savedSuccessfully);
            if (!savedSuccessfully)
            {
                throw new BuildPlayerWindow.BuildMethodException("Failed to restore MetagameApplication after building");
            }

            Debug.Log("Updated MetagameApp after build");
        }

        MetagameApplication FindMetagameAppInProject()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new string[] {"Assets/Prefabs/Metagame"}))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = (GameObject) AssetDatabase.LoadMainAssetAtPath(path);
                if (root.GetComponent<MetagameApplication>())
                {
                    return root.GetComponent<MetagameApplication>();
                }
            }

            return null;
        }

        internal static void BuildServer(BuildTarget target, string locationPathName, bool exitApplicationOnFailure = false)
        {
            Debug.Log($"Building {target} server in {locationPathName}");
            EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Server, BuildTarget.StandaloneLinux64);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = GetScenePaths(),
                locationPathName = locationPathName,
                target = target,
                subtarget = (int)StandaloneBuildSubtarget.Server,
            });
            if (exitApplicationOnFailure && report.summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }

        internal static void BuildClient(BuildTarget target, string locationPathName, bool exitApplicationOnFailure = false)
        {
            Debug.Log($"Building {target} client in {locationPathName}");
            
            EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Standalone, target);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = GetScenePaths(),
                locationPathName = locationPathName,
                target = target,
                subtarget = (int)StandaloneBuildSubtarget.Player,
            });
            if (exitApplicationOnFailure && report.summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }

        static string[] GetScenePaths()
        {
            var scenes = new string[EditorBuildSettings.scenes.Length];
            for (int i = 0; i < scenes.Length; i++)
            {
                scenes[i] = EditorBuildSettings.scenes[i].path;
            }

            return scenes;
        }
    }
}
