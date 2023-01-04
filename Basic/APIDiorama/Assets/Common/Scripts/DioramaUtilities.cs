using UnityEngine;

namespace Unity.Netcode.Samples.APIDiorama
{
    /// <summary>
    /// A class of utilities used across Dioramas
    /// </summary>
    public static class DioramaUtilities
    {
        /// <summary>
        /// Generates a random color
        /// </summary>
        /// <returns>A random RGBA color</returns>
        public static Color32 GetRandomColor() => new Color32((byte)Random.Range(0, 256), (byte)Random.Range(0, 256), (byte)Random.Range(0, 256), 255);
    }
}