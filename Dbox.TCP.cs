/*

  -----------------------------------------------------------------------------
  ⚠️  WARNING – DO NOT MODIFY THIS FILE UNLESS YOU KNOW EXACTLY WHAT YOU'RE DOING
  -----------------------------------------------------------------------------

===============================================================================
  DBOX TCP MONITORING CLIENT – Dbox.TCP.cs
===============================================================================

  This class provides a fully managed TCP client designed to interface with 
  the DBOX Haptic Monitoring Server. It enables the retrieval, parsing, and 
  monitoring of real-time seat feedback data via XML-based TCP communication.

  The MonitoringClient is intended to be embedded in simulation or plugin 
  environments (e.g., MotionBridge, UI controllers) to track seat telemetry,
  status values, or perform low-level field queries via string-based requests.

  -------------------------------------------------------------------------------
  FUNCTIONAL OVERVIEW:
  -------------------------------------------------------------------------------

  - Establishes and manages a TCP socket connection to the DBOX Haptic Bridge
  - Automatically reconnects on failure (if enabled)
  - Sends ASCII-formatted XML commands and receives responses
  - Handles asynchronous message transmission and reception
  - Parses XML to extract float, string or description data from <Field> entries

  The client is lightweight, headless, and fully asynchronous to allow seamless
  integration into user interfaces, background workers, or plugin controllers.

  -------------------------------------------------------------------------------
  TECHNICAL DEPENDENCIES:
  -------------------------------------------------------------------------------

  - System.Net.Sockets.TcpClient for connection handling
  - System.Xml.Linq for XML parsing and field extraction
  - Encoding: ASCII (message encoding/decoding)
  - Compatible with DBOX Field API format (e.g., <Request Cmd="GetStatus" />)

  ⚠️  WARNING:
  - Always check for null or empty responses before attempting to parse
  - Use culture-safe float conversion if localization is enabled
  - Avoid blocking UI threads with synchronous calls

  -------------------------------------------------------------------------------
  AUTHOR / MAINTAINER:
  -------------------------------------------------------------------------------

  - Developer: DAVAIL Nicolas (ALSTOM), MARCAL Thomas (ALSTOM), ALBE Alexis (DBOX)
  - Last updated: 22/04/2025
  - Project: Alstom T&S - DBOX Motion Seat Integration Plugin

===============================================================================
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DBox.TCP
{
    /// <summary>
    /// Lightweight asynchronous TCP client for communicating with the DBOX Haptic Monitoring Server.
    /// Handles connection, message exchange, and XML-based data parsing (status, values, descriptions).
    /// Suitable for use in simulation bridges or real-time UI monitoring.
    /// </summary>
    internal class MonitoringClient
    {

        // ============================================================================
        // =                               Fields                                     =
        // ============================================================================

        #region Fields

        /// <summary>
        /// The internal TCP client used for socket communication with the monitoring server.
        /// </summary>
        private TcpClient client;

        /// <summary>
        /// Stream used to send and receive data through the TCP connection.
        /// </summary>
        private NetworkStream stream;

        /// <summary>
        /// Indicates whether the client is currently connected to the remote server.
        /// </summary>
        internal bool Connected { get; private set; } = false;

        /// <summary>
        /// If true, the client will automatically try to reconnect if the connection is lost.
        /// </summary>
        internal bool Autoreconnect { get; set; } = true;

        /// <summary>
        /// IP address or hostname of the remote monitoring server (default is localhost on DBOX Haptic Bridge).
        /// </summary>
        internal string Server { get; private set; } = "127.0.0.1";

        /// <summary>
        /// TCP port to connect to on the remote server (default is 40001 on DBOX Haptic Bridge).
        /// </summary>
        internal int Port { get; private set; } = 40001;

        /// <summary>
        /// Event triggered when a message is received from the server.
        /// The received string is passed as the event argument.
        /// </summary>
        internal event Action<string> MessageReceived;

        #endregion

        // ============================================================================
        // =                            Constructor(s)                                =
        // ============================================================================

        #region Constructor(s)

        /// <summary>
        /// Default constructor. Initializes the client with default parameters:
        /// Server = "127.0.0.1", Port = 40001, Autoreconnect = true.
        /// </summary>
        public MonitoringClient()
        {
            // Defaults are already set in field declarations.
        }


        /// <summary>
        /// Parameterized constructor.
        /// Allows configuration of the server address, port, and autoreconnect behavior.
        /// </summary>
        /// <param name="serverAddress">IP or hostname of the remote server.</param>
        /// <param name="serverPort">TCP port to connect to.</param>
        /// <param name="autoreconnect">Whether the client should try to reconnect automatically if disconnected.</param>
        public MonitoringClient(string serverAddress, int serverPort, bool autoreconnect = true)
        {
            Server = serverAddress;
            Port = serverPort;
            Autoreconnect = autoreconnect;
        }

        #endregion

        // ============================================================================
        // =                             TCP Functions                                =
        // ============================================================================

        #region TCP Functions

        /// <summary>
        /// Ensures that the TCP connection is established. 
        /// If not connected, attempts to connect based on the <see cref="Autoreconnect"/> flag.
        /// </summary>
        /// <returns><c>true</c> if the client is already connected or successfully reconnected; otherwise, <c>false</c>.</returns>
        private async Task<bool> EnsureConnected()
        {
            if (Connected)
                return true;

            if (Autoreconnect)
            {
                Console.WriteLine("[Client] Autoreconnect is enabled. Attempting connection...");
                return await Connect();
            }

            Console.WriteLine("[Client] Not connected and autoreconnect is disabled.");
            return false;
        }


        /// <summary>
        /// Attempts to establish a TCP connection to the configured server.
        /// </summary>
        /// <returns><c>true</c> if the connection succeeds; otherwise, <c>false</c>.</returns>
        internal async Task<bool> Connect()
        {
            try
            {
                Disconnect();

                client = new TcpClient();
                Console.WriteLine($"[Client] Connecting to {Server}:{Port}...");
                await client.ConnectAsync(Server, Port);
                stream = client.GetStream();

                Connected = true;
                Console.WriteLine("[Client] Connection established.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR][Connect] Failed to connect: {ex.Message}");
                Disconnect();
                return false;
            }
        }


        /// <summary>
        /// Closes the current connection and disposes the internal client and stream.
        /// </summary>
        /// <param name="force">
        /// If <c>true</c>, forces disconnection even if the client is not marked as connected.
        /// </param>
        internal void Disconnect(bool force = false)
        {
            if (Connected || force)
            {
                try
                {
                    stream?.Close();
                    client?.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARN][Disconnect] Exception while closing stream/client: {ex.Message}");
                }

                stream = null;
                client = null;
                Connected = false;

                Console.WriteLine("[Client] Disconnected.");
            }
        }


        /// <summary>
        /// Finalizer. Ensures graceful disconnection when the object is garbage-collected.
        /// </summary>
        ~MonitoringClient()
        {
            Disconnect(force: true);
        }

        #endregion

        // ============================================================================
        // =                          Messages Functions                              =
        // ============================================================================

        #region Messages Functions

        /// <summary>
        /// Sends a message to the server and waits for the response.
        /// Handles TCP transmission and reception asynchronously.
        /// </summary>
        /// <param name="message">The message to be sent (as plain ASCII).</param>
        /// <param name="receptionBufferLength">Optional: the size of the buffer for receiving the response (default = 4096 bytes).</param>
        /// <returns>The string response from the server, or an error message if communication fails.</returns>
        /// <remarks>
        /// This method ensures the connection is alive before sending.
        /// In case of communication failure, the connection will be closed and an error message returned.
        /// </remarks>
        
        internal async Task<string> SendMessageWaitForAnswer(string message, int receptionBufferLength = 10000000)
        {
            if (!await EnsureConnected())
            {
                return "[SendMessage] Unable to connect to the server.";
            }

            try
            {
                byte[] data = Encoding.ASCII.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
                //Console.WriteLine("[Client] Message sent to server.");

                byte[] buffer = new byte[receptionBufferLength];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    Console.WriteLine("[Client] Warning: Server closed the connection.");
                    Disconnect();
                    return "[SendMessage] Connection closed by server.";
                }

                string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                //Console.WriteLine($"[Client] Response received: {response}");
                MessageReceived?.Invoke(response);
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR][SendMessage] {ex.Message}");
                Disconnect();
                return $"[SendMessage Exception] {ex.Message}";
            }
        }
        /*

        internal async Task<string> SendMessageWaitForAnswer(string message, string endTag = "</Reply>")
        {
            if (!await EnsureConnected())
                return "[SendMessage] Unable to connect to the server.";

            try
            {
                // Envoi du message
                byte[] data = Encoding.ASCII.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
                Console.WriteLine("[Client] Message sent to server.");

                // Lecture en boucle jusqu'à détection du endTag
                var buffer = new byte[1024];
                var responseBuilder = new StringBuilder();

                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        Console.WriteLine("[Client] Server closed the connection.");
                        Disconnect();
                        return "[SendMessage] Connection closed by server.";
                    }

                    string part = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    responseBuilder.Append(part);

                    // Vérifie si le message contient la fin du XML
                    if (responseBuilder.ToString().Contains(endTag))
                        break;
                }

                string fullResponse = responseBuilder.ToString();
                MessageReceived?.Invoke(fullResponse);
                return fullResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR][SendMessage] {ex.Message}");
                Disconnect();
                return $"[SendMessage Exception] {ex.Message}";
            }
        }
        */


        /// <summary>
        /// Helper method to send a message in one step (connect + send + receive).
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="bufferSize">Optional: buffer size for response (default = 4096 bytes).</param>
        /// <returns>Server response or error message.</returns>
        /// <remarks>
        /// This method is a shortcut to simplify message exchange.
        /// </remarks>
        internal async Task<string> ConnectAndSend(string message)
        {
            return await SendMessageWaitForAnswer(message);
        }

        #endregion

        // ============================================================================
        // =                       Monitoring Data & Parse                            =
        // ============================================================================

        #region Monitoring Data & Parsing

        /// <summary>
        /// Sends a predefined XML request to the monitoring server to retrieve seat status data.
        /// </summary>
        /// <returns>
        /// A raw XML string response containing the monitoring data, or an error message if the communication fails.
        /// </returns>
        internal async Task<string> GetMonitoringData()
        {
            string request = "<Request Cmd=\"GetStatus\" FieldId=\"6000\" />\r\n";
            return await SendMessageWaitForAnswer(request);
        }


        /// <summary>
        /// Retrieves the description of a field identified by a specific Id (first occurrence only).
        /// </summary>
        /// <param name="inputData">The raw XML string received from the server.</param>
        /// <param name="fieldId">The Id of the desired field.</param>
        /// <returns>The field's description, or null if not found or parsing fails.</returns>
        internal static string GetFieldDescriptionById(string inputData, string fieldId)
        {
            if (string.IsNullOrWhiteSpace(inputData)) return null;

            try
            {
                XElement xmlParsed = XElement.Parse(inputData);

                var field = xmlParsed.Descendants("Field")
                                     .FirstOrDefault(x => x.Attribute("Id")?.Value == fieldId);

                return field?.Attribute("Description")?.Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetFieldDescriptionById ERROR] {ex.Message}");
                return null;
            }
        }


        /// <summary>
        /// Extracts all float values from fields that match a specific Id.
        /// </summary>
        /// <param name="inputData">The raw XML string received from the server.</param>
        /// <param name="fieldId">The Id of the fields to match.</param>
        /// <returns>An array of parsed float values, or null on failure.</returns>
        internal static float[] GetFieldFloatValuesById(string inputData, string fieldId)
        {
            if (string.IsNullOrWhiteSpace(inputData)) return null;

            try
            {
                XElement xmlParsed = XElement.Parse(inputData);

                var fields = xmlParsed.Descendants("Field")
                                      .Where(x => x.Attribute("Id")?.Value == fieldId);

                List<float> results = new List<float>();

                foreach (var f in fields)
                {
                    string rawValue = f.Attribute("Value")?.Value?.Replace('.', ',');
                    if (float.TryParse(rawValue, out float value))
                    {
                        results.Add(value);
                    }
                }

                return results.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetFieldFloatValuesById ERROR] {ex.Message}");
                return null;
            }
        }

        #endregion

    }
}
