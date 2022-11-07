using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace StarterAssets
{
    public partial class StarterAssetsDeployMenu : ScriptableObject
    {
        // prefab paths
        private const string PlayerArmaturePrefabName = "PlayerArmature";

#if STARTER_ASSETS_PACKAGES_CHECKED
        /// <summary>
        /// Check the Armature, main camera, cinemachine virtual camera, camera target and references
        /// </summary>
        [MenuItem(MenuRoot + "/Reset Third Person Controller Armature", false)]
        static void ResetThirdPersonControllerArmature()
        {
            var thirdPersonControllers = FindObjectsOfType<ThirdPersonController>();
            var player = thirdPersonControllers.FirstOrDefault(controller =>
                controller.GetComponent<Animator>() && controller.CompareTag(PlayerTag));

            GameObject playerGameObject = null;

            // player
            if (player == null)
            {
                if (TryLocatePrefab(PlayerArmaturePrefabName, null, new[] { typeof(ThirdPersonController), typeof(StarterAssetsInputs) }, out GameObject prefab, out string _))
                {
                    HandleInstantiatingPrefab(prefab, out playerGameObject);
                }
                else
                {
                    Debug.LogError("Couldn't find player armature prefab");
                }
            }
            else
            {
                playerGameObject = player.gameObject;
            }

            if (playerGameObject != null)
            {
                // cameras
                CheckCameras(playerGameObject.transform, GetThirdPersonPrefabPath());
            }
        }

        [MenuItem(MenuRoot + "/Reset Third Person Controller Capsule", false)]
        static void ResetThirdPersonControllerCapsule()
        {
            var thirdPersonControllers = FindObjectsOfType<ThirdPersonController>();
            var player = thirdPersonControllers.FirstOrDefault(controller =>
                !controller.GetComponent<Animator>() && controller.CompareTag(PlayerTag));

            GameObject playerGameObject = null;

            // player
            if (player == null)
            {
                if (TryLocatePrefab(PlayerCapsulePrefabName, null, new[] { typeof(ThirdPersonController), typeof(StarterAssetsInputs) }, out GameObject prefab, out string _))
                {
                    HandleInstantiatingPrefab(prefab, out playerGameObject);
                }
                else
                {
                    Debug.LogError("Couldn't find player capsule prefab");
                }
            }
            else
            {
                playerGameObject = player.gameObject;
            }

            if (playerGameObject != null)
            {
                // cameras
                CheckCameras(playerGameObject.transform, GetThirdPersonPrefabPath());
            }
        }

        static string GetThirdPersonPrefabPath()
        {
            if (TryLocatePrefab(PlayerArmaturePrefabName, null, new[] { typeof(ThirdPersonController), typeof(StarterAssetsInputs) }, out GameObject _, out string prefabPath))
            {
                var pathString = new StringBuilder();
                var currentDirectory = new FileInfo(prefabPath).Directory;
                while (currentDirectory.Name != "Assets")
                {
                    pathString.Insert(0, $"/{currentDirectory.Name}");
                    currentDirectory = currentDirectory.Parent;
                }

                pathString.Insert(0, currentDirectory.Name);
                return pathString.ToString();
            }

            return null;
        }
#endif
    }
}