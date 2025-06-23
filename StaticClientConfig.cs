using System;
using System.IO;

namespace Alstom.MotionSeatPlugin
{
    /// <summary>
    /// Holds static configuration for the local PC and the remote PC it communicates with.
    /// </summary>
    internal static class StaticClientConfig
    {
        // ============================================================================
        // =                          Local Machine Info                              =
        // ============================================================================

        #region Local Machine

        /// <summary>
        /// Name of the local machine (this PC).
        /// </summary>
        public static string LocalName { get; set; } = "CAB";

        /// <summary>
        /// IP address of the local machine (this PC).
        /// </summary>
        public static string LocalIP { get; set; } = "192.168.1.19";

        /// <summary>
        /// TCP port used by the local listener.
        /// </summary>
        public static int LocalPort { get; set; } = 8083;

        #endregion

        // ============================================================================
        // =                         Remote Target Machine Info                       =
        // ============================================================================

        #region Remote Machine

        /// <summary>
        /// Name of the remote target machine to communicate with.
        /// </summary>
        public static string RemoteName { get; set; } = "INST";

        /// <summary>
        /// IP address of the remote target machine.
        /// </summary>
        public static string RemoteIP { get; set; } = "192.168.1.21";

        /// <summary>
        /// TCP port on the remote machine.
        /// </summary>
        public static int RemotePort { get; set; } = 8083;

        #endregion

        // ============================================================================
        // =                               Functions                                  =
        // ============================================================================

        #region Functions

        /// <summary>
        /// Charge les paramètres TCP depuis un fichier INI si présent.
        /// </summary>
        public static void LoadFromIniFile(string filePath = "TCPControl.ini")
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("INI non trouvé, configuration par défaut utilisée.");
                return;
            }

            string[] lines = File.ReadAllLines(filePath);
            string section = "";

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();

                if (line.StartsWith(";") || line == "") continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line.Substring(1, line.Length - 2).ToLowerInvariant();
                    continue;
                }

                string[] parts = line.Split(new[] { '=' }, 2, StringSplitOptions.None);
                if (parts.Length != 2) continue;

                string key = parts[0].Trim().ToLowerInvariant();
                string value = parts[1].Trim();

                switch (section)
                {
                    case "local":
                        if (key == "name") LocalName = value;
                        else if (key == "ip") LocalIP = value;
                        else if (key == "port" && int.TryParse(value, out int localPort)) LocalPort = localPort;
                        break;

                    case "remote":
                        if (key == "name") RemoteName = value;
                        else if (key == "ip") RemoteIP = value;
                        else if (key == "port" && int.TryParse(value, out int remotePort)) RemotePort = remotePort;
                        break;
                }
            }

            Console.WriteLine("TCPControl.ini chargé avec succès.");
        }

        #endregion

    }
}
