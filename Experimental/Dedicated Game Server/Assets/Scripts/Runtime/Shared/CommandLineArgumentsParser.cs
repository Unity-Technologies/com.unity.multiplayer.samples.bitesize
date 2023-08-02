using System;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    internal class CommandLineArgumentsParser
    {
        #region Arguments
        /// <summary>
        /// Port, assigned to the spawned process (most likely a game server)
        /// </summary>
        public int ServerPort { get; private set; }
        #endregion

        readonly string[] args;
        Arguments names;

        public CommandLineArgumentsParser() : this(Environment.GetCommandLineArgs()) { }
        public CommandLineArgumentsParser(string[] arguments)
        {
            args = arguments;
            // Android fix
            if (args == null)
            {
                args = new string[0];
            }

            names = new Arguments();
            //args = new string[] { "Game.exe", $"{names.ServerPort}", "9999" }; //uncomment to test behaviour in the editor, where the command line is not available
            ServerPort = ExtractValueInt(names.ServerPort, -1);
        }

        #region Helper methods

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
                if (!args.Contains(argName))
                {
                    return defaultValue;
                }

                var index = args.ToList().FindIndex(0, a => a.Equals(argName));
                return args[index + 1];
            }

            foreach (var argument in args)
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

        #endregion

        internal class Arguments
        {
            internal string ServerPort => "-port";
        }
    }
}
