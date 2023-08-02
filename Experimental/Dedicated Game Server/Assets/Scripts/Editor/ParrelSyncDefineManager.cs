using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEditor.PackageManager;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// Adds or removes a define depending on whether the ParrelSync package is in the project or not
    /// </summary>
    class ParrelSyncDefineManager
    {
        const string k_HasParrelSyncDefine = "HAS_PARRELSYNC";

        [InitializeOnLoadMethod]
        static void EditProjectBasedOnParrelSyncPresence()
        {
            Events.registeredPackages += OnRegisteredPackages; //when packages are updated, useful for detecting removal of ParrelSync
            EditorCoroutineUtility.StartCoroutineOwnerless(AlterDefinesBasedOnParrelSync());
        }

        public static void OnRegisteredPackages(PackageRegistrationEventArgs diff)
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(AlterDefinesBasedOnParrelSync());
        }

        static IEnumerator AlterDefinesBasedOnParrelSync()
        {
            var pack = Client.List();
            while (!pack.IsCompleted)
            {
                yield return null;
            }
            var hasParrelSync = pack.Result.Any(q => q.name == "com.veriorpies.parrelsync");

            string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            List<string> allDefines = definesString.Split(';').ToList();
            if (allDefines.Contains(k_HasParrelSyncDefine))
            {
                if (hasParrelSync)
                {
                    yield break;
                }
                allDefines.Remove(k_HasParrelSyncDefine);
            }
            else
            {
                if (!hasParrelSync)
                {
                    yield break;
                }
                allDefines.Add(k_HasParrelSyncDefine);
            }
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", allDefines.ToArray()));
            AssetDatabase.SaveAssets();
        }
    }
}
