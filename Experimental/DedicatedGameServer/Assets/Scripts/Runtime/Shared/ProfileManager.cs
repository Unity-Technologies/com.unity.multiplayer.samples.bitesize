using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using System.Security.Cryptography;
using System.Text;
#endif

using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    public class ProfileManager
    {
        const string k_AuthProfileCommandLineArg = "-AuthProfile";
        
        public static ProfileManager Singleton
        {
            get
            {
                if (s_Singleton == null)
                {
                    s_Singleton = new ProfileManager();
                }

                return s_Singleton;
            }
        }

        static ProfileManager s_Singleton;

        string m_Profile;

        public string Profile
        {
            get
            {
                if (m_Profile == null)
                {
                    m_Profile = GetProfile();
                }

                return m_Profile;
            }
        }

        List<string> m_AvailableProfiles;

        static string GetProfile()
        {
            var arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (arguments[i] == k_AuthProfileCommandLineArg)
                {
                    var profileId = arguments[i + 1];
                    return profileId;
                }
            }

#if UNITY_EDITOR

            // When running in the Editor make a unique ID from the Application.dataPath.
            // This will work for cloning projects manually, or with Virtual Projects.
            // Since only a single instance of the Editor can be open for a specific
            // dataPath, uniqueness is ensured.
            var hashedBytes = new MD5CryptoServiceProvider()
                .ComputeHash(Encoding.UTF8.GetBytes(Application.dataPath));
            Array.Resize(ref hashedBytes, 16);
            // Authentication service only allows profile names of maximum 30 characters. We're generating a GUID based
            // on the project's path. Truncating the first 30 characters of said GUID string suffices for uniqueness.
            return new Guid(hashedBytes).ToString("N")[..30];
#else
            return "";
#endif
        }
    }
}
