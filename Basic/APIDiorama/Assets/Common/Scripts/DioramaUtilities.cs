using UnityEngine;

namespace Unity.Netcode.Samples.APIDiorama
{
    /// <summary>
    /// A class of utilities used across Dioramas
    /// </summary>
    public static class DioramaUtilities
    {
        static readonly string[] s_Usernames = new string[] { "MorwennaDaBest", "SamTheBell", "FranklyJil", "FernandoCutter", "LP Morgan" };

        /// <summary>
        /// Generates a random color
        /// </summary>
        /// <returns>A random RGBA color</returns>
        public static Color32 GetRandomColor() => new Color32((byte)Random.Range(0, 256), (byte)Random.Range(0, 256), (byte)Random.Range(0, 256), 255);
        public static string GetRandomUsername() => s_Usernames[Random.Range(0, s_Usernames.Length)];
    }
}