using System;
using System.Linq;

namespace Unity.DedicatedGameServerSample.Runtime
{
    internal class CommandLineArgumentsParser
    {
        /// <summary>
        /// Port, assigned to the spawned process (most likely a game server)
        /// </summary>
        public int ServerPort { get; private set; }

        readonly string[] m_Args;
        Arguments m_Names;

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

            m_Names = new Arguments();
            //args = new string[] { "Game.exe", $"{names.ServerPort}", "9999" }; //uncomment to test behaviour in the editor, where the command line is not available
            // todo use Dedicated Server package's CLI Arguments defaults for this
            ServerPort = ExtractValueInt(m_Names.ServerPort, -1);
        }

        /// <summary>
        /// Extracts a value for command line arguments provided
        /// </summary>
        /// <param name="argName"></param>
        /// <param name="defaultValue"></param>
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

        internal class Arguments
        {
            internal string ServerPort => "-port";
        }
    }
}
