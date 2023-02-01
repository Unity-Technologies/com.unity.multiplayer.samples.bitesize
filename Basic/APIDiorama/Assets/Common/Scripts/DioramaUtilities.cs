using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Unity.Netcode.Samples.APIDiorama
{
    /// <summary>
    /// A class of utilities used across Dioramas
    /// </summary>
    public static class DioramaUtilities
    {
        static readonly string[] s_Usernames = new string[] { "MaryDaBest", "BobTheBell", "FranklyVal", "Georgie96", "OP Morgan" };

        /// <summary>
        /// Generates a random color
        /// </summary>
        /// <returns>A random RGBA color</returns>
        public static Color32 GetRandomColor() => new Color32((byte)UnityEngine.Random.Range(0, 256), (byte)UnityEngine.Random.Range(0, 256), (byte)UnityEngine.Random.Range(0, 256), 255);
        public static string GetRandomUsername() => s_Usernames[UnityEngine.Random.Range(0, s_Usernames.Length)];

        /// <summary>
        /// Filders some 'bad words' from a string. In a production environment, consider using an external service for complex tasks like this.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string FilterBadWords(string input)
        {
            Regex regex = new Regex(@"\b(\w+)\b", RegexOptions.Compiled);
            var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"potato", "*****"},
                {"duck", "@%!$"},
                {"pineapple", "$*%*!"}
            };
            return regex.Replace(input, match => replacements.ContainsKey(match.Groups[1].Value) ? replacements[match.Groups[1].Value]
                                                                                                 : match.Groups[1].Value);
        }
    }
}