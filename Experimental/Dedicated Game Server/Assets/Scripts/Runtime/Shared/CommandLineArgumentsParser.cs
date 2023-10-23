using System;
using System.Linq;
using UnityEngine.DedicatedServer;

namespace Unity.DedicatedGameServerSample.Runtime
{
    internal class CommandLineArgumentsParser
    {
        public int Port { get; }
        const int k_DefaultPort = 7777;
        public int TargetFramerate { get; }
        const int k_DefaultTargetFramerate = 30;

        readonly string[] m_Args;

        /// <summary>
        /// Initializes the CommandLineArgumentsParser
        /// </summary>
        public CommandLineArgumentsParser() : this(Environment.GetCommandLineArgs()) { }
        
        /// <summary>
        /// Initializes the CommandLineArgumentsParser
        /// </summary>
        /// <param name="arguments">Arguments to process</param>
        public CommandLineArgumentsParser(string[] arguments)
        {
            m_Args = arguments;
            if (m_Args == null) // Android fix
            {
                m_Args = new string[0];
            }

            Port = Arguments.Port.HasValue ? Arguments.Port.Value : k_DefaultPort;
            TargetFramerate = Arguments.TargetFramerate.HasValue ? Arguments.TargetFramerate.Value : k_DefaultTargetFramerate;
        }

        /// <summary>
        /// Extracts a value for command line arguments provided
        /// </summary>
        /// <param name="argName"></param>
        /// <param name="defaultValue"></param>
        /// <param name="argumentAndValueAreSeparated"></param>
        /// <returns></returns>
        string ExtractValue(string argName, string defaultValue = null, bool argumentAndValueAreSeparated = true)
        {
            if (argumentAndValueAreSeparated)
            {
                if (!m_Args.Contains(argName))
                {
                    return defaultValue;
                }

                var index = m_Args.ToList().FindIndex(0, a => a.Equals(argName));
                return m_Args[index + 1];
            }

            foreach (var argument in m_Args)
            {
                if (argument.StartsWith(argName)) //I.E: "-epiclocale=it"
                {
                    return argument.Substring(argName.Length + 1, argument.Length - argName.Length - 1);
                }
            }
            return defaultValue;
        }

        int ExtractValueInt(string argName, int defaultValue = -1)
        {
            var number = ExtractValue(argName, defaultValue.ToString());
            return Convert.ToInt32(number);
        }
    }
}
