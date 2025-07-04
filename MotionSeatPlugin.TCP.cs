using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DBox.MotionSeat;
using Newtonsoft.Json;

namespace Alstom.MotionSeatPlugin.TCP
{
    internal static class GlobalConfig
    {
        public static bool EnableLogs = false;
    }

    // ============================================================================
    // =                        PluginTCPListener Class                           =
    // ============================================================================

    #region PluginTCPListener Class

    /// <summary>
    /// TCP Listener wrapper class that handles incoming single-client connections asynchronously.
    /// </summary>
    internal class PluginTCPListener : PluginTCP
    {

        private readonly IPAddress listenerAddress;
        private readonly int listenerPort;
        private TcpListener listener;
        private bool isStarted;
        private readonly byte[] monitoringBuffer = new byte[1];
        protected CancellationTokenSource connectionMonitorCTS = new CancellationTokenSource();
        internal Action OnClientDisconnected { get; set; }

        internal PluginTCPListener(string localAddress, int port, string name = "PluginTCPListener")
        {
            this.name = name;
            listenerAddress = IPAddress.Parse(localAddress);
            listenerPort = port;
        }

        /// <summary>
        /// Starts the TCP listener and accepts a single client connection at a time.
        /// Launches a background task to monitor client disconnection.
        /// </summary>

        internal async void OpenDialog()
        {
            if (isStarted)
            {
                if (GlobalConfig.EnableLogs)
                    Console.WriteLine($"[{name}] Listener already started. Restarting...");
                CloseDialog();
            }

            listener = new TcpListener(listenerAddress, listenerPort);
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            try
            {
                listener.Start(1);
                isStarted = true;

                connectionMonitorCTS?.Cancel();
                connectionMonitorCTS?.Dispose();
                connectionMonitorCTS = new CancellationTokenSource();

                Console.WriteLine($"[{name}] TCP listener started on <{listenerAddress}:{listenerPort}>.");
            }
            catch (Exception ex)
            {
                if (GlobalConfig.EnableLogs)
                    Console.WriteLine($"[{name}] ERROR - Failed to start listener: {FormatException(ex)}");
                return;
            }

            while (isStarted)
            {
                try
                {
                    if (GlobalConfig.EnableLogs)
                        Console.WriteLine($"[{name}] Waiting for an incoming connection...");
                    var connectedClient = await listener.AcceptTcpClientAsync();
                    SetClient(connectedClient);
                    if (GlobalConfig.EnableLogs)
                        Console.WriteLine($"[{name}] Client connected from <{Client?.Client?.RemoteEndPoint}>.");

                    _ = MonitorConnectionAsync();
                }
                catch (ObjectDisposedException)
                {
                    if (GlobalConfig.EnableLogs)
                        Console.WriteLine($"[{name}] Listener stopped. Exiting accept loop.");
                    break;
                }
                catch (Exception ex)
                {
                    if (GlobalConfig.EnableLogs)
                        Console.WriteLine($"[{name}] ERROR - Accepting client failed: {FormatException(ex)}");
                }
            }
        }

        /// <summary>
        /// Monitors the TCP connection for the listener.
        /// Detects disconnection and triggers cleanup and a callback if applicable.
        /// </summary>
        private async Task MonitorConnectionAsync()
        {
            if (Client == null) return;

            var token = connectionMonitorCTS.Token;

            try
            {
                var stream = Client.GetStream();

                while (Client.Connected && !token.IsCancellationRequested)
                {
                    await Task.Delay(100, token);

                    if (Client.Client.Poll(0, SelectMode.SelectRead) && Client.Client.Available == 0)
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                if (GlobalConfig.EnableLogs)
                    Console.WriteLine($"[{name}] Monitor cancelled.");
            }
            catch (Exception ex)
            {
                if (GlobalConfig.EnableLogs)
                    Console.WriteLine($"[{name}] Monitor error: {FormatException(ex)}");
            }

            CloseClient();
            ClearClient();
            OnClientDisconnected?.Invoke();
        }

        /// <summary>
        /// Stops the TCP listener, closes active client connection, and cancels monitoring tasks.
        /// </summary>
        internal void CloseDialog()
        {
            isStarted = false;

            connectionMonitorCTS?.Cancel();
            connectionMonitorCTS?.Dispose();
            connectionMonitorCTS = new CancellationTokenSource();

            CloseClient();

            try
            {
                listener?.Stop();
                Console.WriteLine($"[{name}] Listener closed.");
            }
            catch (Exception ex)
            {
                if (GlobalConfig.EnableLogs)
                    Console.WriteLine($"[{name}] ERROR - Failed to close listener: {FormatException(ex)}");
            }
            finally
            {
                isStarted = false;
                listener = null;
            }
        }
    }

    #endregion

    // ============================================================================
    // =                         PluginTCPClient Class                            =
    // ============================================================================

    #region PluginTCPClient Class

    /// <summary>
    /// TCP Client wrapper that handles automatic reconnection.
    /// </summary>
    internal class PluginTCPClient : PluginTCP
    {
        private readonly string listenerIp;
        private readonly int listenerPort;
        private bool connectionLock;
        private bool isMonitoringConnection = false;
        private static readonly byte[] connectionCheckBuffer = new byte[1];
        private CancellationTokenSource monitoringCTS = new CancellationTokenSource();
        private bool autoReconnectEnabled = true;

        internal Action OnConnected { get; set; }
        internal Action OnDisconnected { get; set; }


        internal PluginTCPClient(string connectToIP, int connectToPort, string name = "PluginTCPClient")
        {
            this.name = name;
            listenerIp = connectToIP;
            listenerPort = connectToPort;
        }

        /// <summary>
        /// Connects to a remote TCP listener with automatic retry on failure.
        /// Initializes monitoring of the client connection.
        /// </summary>
        internal async Task JoinDialog()
        {
            if (GlobalConfig.EnableLogs)
                Console.WriteLine($"[{name}] Attempting to connect to <{listenerIp}:{listenerPort}>.");

            if (Client != null)
                QuitDialog();

            while (Client == null || !Client.Connected)
            {
                if (connectionLock)
                {
                    if (GlobalConfig.EnableLogs)
                        Console.WriteLine($"[{name}] Connection already in progress. Retrying...");
                    await Task.Delay(5000);
                    continue;
                }

                connectionLock = true;

                try
                {
                    var client = new TcpClient();
                    if (GlobalConfig.EnableLogs)
                        Console.WriteLine($"[{name}] Connecting to listener at <{listenerIp}:{listenerPort}>...");

                    await client.ConnectAsync(listenerIp, listenerPort);
                    SetClient(client);

                    if (GlobalConfig.EnableLogs)
                        Console.WriteLine($"[{name}] Connected: <{Client.Client.LocalEndPoint}> -> <{Client.Client.RemoteEndPoint}>");

                    monitoringCTS?.Cancel();
                    monitoringCTS?.Dispose();
                    monitoringCTS = new CancellationTokenSource();

                    _ = MonitorClientConnectionAsync(monitoringCTS.Token);

                    return;
                }
                catch (Exception ex)
                {
                    if (GlobalConfig.EnableLogs)
                        Console.WriteLine($"[{name}] Connection failed: {FormatException(ex)}");
                }
                finally
                {
                    connectionLock = false;
                }

                await Task.Delay(200);
            }
        }

        /// <summary>
        /// Monitors the TCP client connection asynchronously.
        /// Detects connection loss and triggers cleanup and automatic reconnection if enabled.
        /// </summary>
        /// <param name="token">A cancellation token to stop monitoring early.</param>
        private async Task MonitorClientConnectionAsync(CancellationToken token)
        {
            if (Client == null || !Client.Connected || isMonitoringConnection)
                return;

            isMonitoringConnection = true;

            try
            {
                var stream = Client.GetStream();

                while (Client.Connected && !token.IsCancellationRequested)
                {
                    await Task.Delay(1000, token);

                    if (Client.Client.Poll(0, SelectMode.SelectRead) && Client.Client.Available == 0)
                        break;

                }
            }
            catch (OperationCanceledException)
            {
                if (GlobalConfig.EnableLogs)
                    Console.WriteLine($"[{name}] Monitoring cancelled by token.");
            }
            catch (Exception ex)
            {
                if (GlobalConfig.EnableLogs)
                    Console.WriteLine($"[{name}] Failed to monitor connection: {FormatException(ex)}");
            }
            finally
            {
                isMonitoringConnection = false;
            }

            Console.WriteLine($"[{name}] Client disconnected.");
            CloseClient();
            ClearClient();
            OnDisconnected?.Invoke();

            if (autoReconnectEnabled)
            {
                if (GlobalConfig.EnableLogs)
                    Console.WriteLine($"[{name}] Attempting reconnection in 3s...");
                await Task.Delay(3000);
                _ = JoinDialog();
            }
        }

        /// <summary>
        /// Terminates the current TCP client connection and monitoring task.
        /// </summary>
        internal void QuitDialog()
        {
            if (Client != null)
            {
                try
                {
                    monitoringCTS?.Cancel();
                }
                catch { }
                finally
                {
                    monitoringCTS?.Dispose();
                    monitoringCTS = new CancellationTokenSource();
                }
                if (GlobalConfig.EnableLogs)
                    Console.WriteLine($"[{name}] Closing client connection...");
                CloseClient();
            }
            else
            {
                if (GlobalConfig.EnableLogs)
                    Console.WriteLine($"[{name}] No active client connection to close.");
            }
        }
    }

    #endregion

    // ============================================================================
    // =                            PluginTCP Class                               =
    // ============================================================================

    #region PluginTCP Class

    /// <summary>
    /// Base class that provides TCP communication helpers for listener and client classes.
    /// </summary>
    internal class PluginTCP
    {
        internal TcpClient Client { get; private set; }
        protected string name = "<PluginTCP - Should Be Overwritten>";
        private bool readLock;
        private bool writeLock;
        protected bool consoleLog = false;
        private StreamReader reader;
        private StreamWriter writer;

        /// <summary>
        /// Initializes the internal stream reader and writer from the given TCP client.
        /// Configures basic timeouts and disables delay.
        /// </summary>
        /// <param name="client">The TCP client to configure.</param>
        protected void SetClient(TcpClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            Client.ReceiveTimeout = 100;
            Client.SendTimeout = 100;
            Client.NoDelay = true;

            var stream = client.GetStream();
            reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
            writer = new StreamWriter(stream, Encoding.ASCII, 1024, leaveOpen: true)
            {
                AutoFlush = true
            };
            if (GlobalConfig.EnableLogs)
                Console.WriteLine($"[{name}] Client initialized.");
        }

        /// <summary>
        /// Closes the TCP client and its associated streams.
        /// Cleans up internal references.
        /// </summary>
        protected void CloseClient()
        {
            try
            {
                reader?.Dispose();
                writer?.Dispose();
                Client?.GetStream()?.Close();
                Client?.Close();
                if (GlobalConfig.EnableLogs)
                    Console.WriteLine($"[{name}] Client connection closed.");
            }
            catch (Exception ex)
            {
                if (GlobalConfig.EnableLogs)
                    Console.WriteLine($"[{name}] Error closing client connection: {FormatException(ex)}");
            }
            finally
            {
                reader = null;
                writer = null;
            }
        }

        /// <summary>
        /// Sends a newline-terminated message to the connected peer.
        /// Returns the length of the sent message, or 0 on failure.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <returns>The number of characters sent including newline, or 0.</returns>
        internal async Task<int> SendOnly(string message)
        {
            if (Client == null || !Client.Connected || writer == null || writeLock)
                return 0;

            writeLock = true;
            try
            {
                await writer.WriteLineAsync(message);

                if (Client.Client.Poll(0, SelectMode.SelectRead) && Client.Client.Available == 0)
                {
                    if (GlobalConfig.EnableLogs)
                        Console.WriteLine($"[{name}] Socket closed after send.");
                    throw new IOException("Remote socket closed.");
                }

                if (GlobalConfig.EnableLogs)
                    Console.WriteLine($"[{name}] Sent \"{message}\" from <{Client.Client.LocalEndPoint}> to <{Client.Client.RemoteEndPoint}>.");

                return message.Length + 1;
            }
            catch (Exception ex)
            {
                if (GlobalConfig.EnableLogs)
                    Console.WriteLine($"[{name}] Failed to send message: {FormatException(ex)}");
                return 0;
            }
            finally
            {
                writeLock = false;
            }
        }

        /// <summary>
        /// Reads a single line from the TCP stream if data is available.
        /// Returns an empty string on failure or no data.
        /// </summary>
        /// <returns>The message received, or an empty string.</returns>
        /// 
        private readonly SemaphoreSlim readLockSlim = new SemaphoreSlim(1, 1);


        internal async Task<string> ReadOnly(int timeoutMs = 2000, CancellationToken externalToken = default)
        {
            if (Client == null || !Client.Connected || reader == null || readLock)
                return string.Empty;

            //readLock = true;
            await readLockSlim.WaitAsync();
            try
            {
                using (CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken))
                {
                    cts.CancelAfter(timeoutMs);

                    Task<string> readTask = reader.ReadLineAsync();
                    Task delayTask = Task.Delay(Timeout.Infinite, cts.Token);
                    Task completed = await Task.WhenAny(readTask, delayTask);
                    if (completed != readTask)
                    {
                        if (GlobalConfig.EnableLogs) 
                            Console.WriteLine($"[{name}] ReadOnly timed out after {timeoutMs}ms.");
                        return string.Empty;
                    }

                    string line = await readTask;
                    if (GlobalConfig.EnableLogs)
                        Console.WriteLine($"[{name}] Received \"{line}\" from <{Client.Client.RemoteEndPoint}>.");
                    return line ?? string.Empty;
                }
            }
            catch (OperationCanceledException)
            {
                if (GlobalConfig.EnableLogs)
                    Console.WriteLine($"[{name}] ReadOnly cancelled.");
                return string.Empty;
            }
            catch (Exception ex)
            {
                if (GlobalConfig.EnableLogs) 
                    Console.WriteLine($"[{name}] Error in ReadOnly: {ex.Message}");
                return string.Empty;
            }
            finally
            {
                readLockSlim.Release();
            }
        }

        /// <summary>
        /// Sends a message and waits for a single-line response from the remote peer.
        /// Returns the response or an empty string on failure.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <returns>The received response.</returns>
        internal async Task<string> SendMessageWaitForResponseAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || Client == null || !Client.Connected)
            {
                if (GlobalConfig.EnableLogs)
                    Console.WriteLine($"[{name}] Cannot send message: invalid client or empty input.");
                return string.Empty;
            }

            int sent = await SendOnly(message);
            if (sent == 0)
            {
                if (GlobalConfig.EnableLogs)
                    Console.WriteLine($"[{name}] Failed to send message: \"{message}\".");
                return string.Empty;
            }

            string response = await ReadOnly();
            return response;
        }

        /// <summary>
        /// Formats an exception for consistent logging.
        /// </summary>
        /// <param name="e">The exception to format.</param>
        /// <returns>A formatted string with type, method, and message.</returns>
        protected string FormatException(Exception e)
        {
            return $"{e.GetType().Name}({e.TargetSite}):\n{e.Message}";
        }

        /// <summary>
        /// Clears the current TCP client reference.
        /// </summary>
        protected void ClearClient()
        {
            if (Client != null)
            {
                if (GlobalConfig.EnableLogs)
                    Console.WriteLine($"[{name}] Clearing TCP client reference.");
            }

            Client = null;
        }
    }

    #endregion

    // ============================================================================
    // =                       Message Converter Class                            =
    // ============================================================================

    #region Message Converter

    /// <summary>
    /// A class to convert <see cref="string"/> message in different object you're likely to use.
    /// </summary>
    internal static class MessageConverter
    {

        /// <summary>
        /// Convert a compatible <see cref="string"/> message in a <see cref="MotionSeatControl.RemoteCommandType"/>.
        /// </summary>
        /// <returns>The <see cref="MotionSeatControl.RemoteCommandType"/> associated with the message.</returns>
        internal static MotionSeatControl.RemoteCommandType GetCommandType(string message)
        {
            if (message.Length >= 1 && int.TryParse(message.Substring(0, 1), out int value))
            {
                switch (value)
                {
                    case 0:
                        return MotionSeatControl.RemoteCommandType.START;
                    case 1:
                        return MotionSeatControl.RemoteCommandType.STOP;
                    case 2:
                        return MotionSeatControl.RemoteCommandType.MOTION_ON;
                    case 3:
                        return MotionSeatControl.RemoteCommandType.MOTION_OFF;
                    case 4:
                        return MotionSeatControl.RemoteCommandType.REACTIVITY_ERROR;
                    case 5:
                        return MotionSeatControl.RemoteCommandType.REACTIVITY_SECURE;
                    case 6:
                        return MotionSeatControl.RemoteCommandType.REACTIVITY_NRM;
                    case 7:
                        return MotionSeatControl.RemoteCommandType.REACTIVITY_HIGH;
                    case 8:
                        return MotionSeatControl.RemoteCommandType.SECURERESETPOSITION;
                    case 9:
                        return MotionSeatControl.RemoteCommandType.DATA;
                    default:
                        return MotionSeatControl.RemoteCommandType.NONE;
                }
            }
            else
            { return MotionSeatControl.RemoteCommandType.NONE; }
        }

        /// <summary>
        /// Serialize a <see cref="SeatInfo"/> into a <see cref="string"/>
        /// </summary>
        /// <param name="seatInfo">The <see cref="SeatInfo"/> to serialize</param>
        /// <returns>The serialized object as a string</returns>
        internal static string SerializeSeatInfo(SeatInfo seatInfo)
        {
            return JsonConvert.SerializeObject(seatInfo);
        }

        /// <summary>
        /// Deserialize a <see cref="string"/> into a <see cref="SeatInfo"/>
        /// </summary>
        /// <param name="serializedSeatInfo">The <see cref="string"/> to deserialize</param>
        /// <returns>The <see cref="SeatInfo"/> object generated from the string, or null if not possible</returns>
        internal static SeatInfo DecodeSeatInfo(string serializedSeatInfo)
        {
            if (string.IsNullOrWhiteSpace(serializedSeatInfo))
                return null;

            try
            {
                return JsonConvert.DeserializeObject<SeatInfo>(serializedSeatInfo);
            }
            catch
            {
                return null;
            }
        }
    }

    #endregion

}