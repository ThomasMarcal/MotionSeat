using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Alstom.MotionSeatPlugin.TCP;
using DBox.MotionSeat;
using DBox.TCP;
using Newtonsoft.Json;
using static DBox.MotionSeat.MotionSeat;

namespace Alstom.MotionSeatPlugin
{
    internal partial class MotionSeatControl : Form
    {

        /// <summary>
        /// Receive instruction from other plugins and send them back updated information about the seat.
        /// </summary>

        private PluginTCPListener tcpListener = new PluginTCPListener(StaticClientConfig.LocalIP, StaticClientConfig.LocalPort);

        //private PluginTCPListener tcpListener = new PluginTCPListener("192.168.1.19", 8083);


        /// <summary>
        /// Connect to a remote plugin's <see cref="tcpListener"/> and send instruction to the plugin.
        /// </summary>
        private PluginTCPClient tcpClientCAB1 = new PluginTCPClient(StaticClientConfig.RemoteIP, StaticClientConfig.RemotePort, StaticClientConfig.RemoteName);


        /// <summary>
        /// Assign the IP and ports of the tcp listener and clients with what is written in the ./TcpRemoteControl.ini file. 
        /// </summary>
        /*
        private void ReadTCPAddresses()
        {
            string filepath = "./TcpRemoteControl.ini";
            if (File.Exists(filepath))
            {
                string[] lines = File.ReadAllLines(filepath);
                foreach (string line in lines)
                {
                    if (line.Trim().StartsWith("#") || line=="") { continue; }
                    string[] split = line.Split('=').Select(s => s.Trim()).ToArray();
                    if (split.Length!=2) { continue; }
                    IPEndPoint ipep = GetIPEndPoint(split[1]);
                    switch (split[0])
                    {
                        case "LISTENER":
                            tcpListener.CloseDialog();
                            tcpListener = new PluginTCPListener(ipep.Address.ToString(), ipep.Port);
                            Console.WriteLine($"Changed IP and port of PluginTcpListener to {split[1]}");
                            break;
                        case "SEAT0":
                            tcpClientCAB1.QuitDialog();
                            tcpClientCAB1 = new PluginTCPClient(ipep.Address.ToString(),ipep.Port,"TCPtoSeat1Client");
                            Console.WriteLine($"Changed IP and port of TCPtoSeat1Client to {split[1]}");
                            break;
                        case "SEAT1":
                            tcpClientCAB2.QuitDialog();
                            tcpClientCAB2 = new PluginTCPClient(ipep.Address.ToString(), ipep.Port, "TCPtoSeat2Client");
                            Console.WriteLine($"Changed IP and port of TCPtoSeat2Client to {split[1]}");
                            break;
                        case "SEAT2":
                            tcpClientCAB3.QuitDialog();
                            tcpClientCAB3 = new PluginTCPClient(ipep.Address.ToString(), ipep.Port, "TCPtoSeat3Client");
                            Console.WriteLine($"Changed IP and port of TCPtoSeat3Client to {split[1]}");
                            break;
                        case "SEAT3":
                            tcpClientCAB4.QuitDialog();
                            tcpClientCAB4 = new PluginTCPClient(ipep.Address.ToString(), ipep.Port, "TCPtoSeat4Client");
                            Console.WriteLine($"Changed IP and port of TCPtoSeat4Client to {split[1]}");
                            break;
                        default:
                            break;
                    }
                }
            }

        }
        */

        private IPEndPoint GetIPEndPoint(string ip)
        {
            string[] result = ip.Split(':').Select(c => c.Trim()).ToArray();
            try
            {
                return new IPEndPoint(IPAddress.Parse(result[0]), int.Parse(result[1]));
            }
            catch (Exception) { return new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6000); }
        }


        /// <summary>
        /// Close the TCP objects running within this <see cref="MotionSeatControl"/>
        /// </summary>
        private void DisconnectTCPSockets()
        {
            tcpClientCAB1.QuitDialog();
            tcpListener.CloseDialog();

        }

        /// <summary>
        /// A list of command you can execute on a remote plugin.
        /// </summary>
        internal enum RemoteCommandType
        {
            START, STOP,
            MOTION_ON, MOTION_OFF,
            REACTIVITY_ERROR, REACTIVITY_SECURE, REACTIVITY_NRM, REACTIVITY_HIGH,
            SECURERESETPOSITION,
            DATA,
            NONE
        }

        /// <summary>
        /// Get the <see cref="PluginTCPClient"/> used to contact the right seat according to <see cref="SessionIndex"/>.
        /// </summary>
        /// <returns></returns>
        private PluginTCPClient GetTCPClientAtIndex()
        {
            if (SessionIndex == 0) { return tcpClientCAB1; }
            else return null;
        }

        /// <summary>
        /// Ask the remote seat to do the provided command. If <see cref="SessionIndex"/> is below 0, will execute directly on the local seat instead.
        /// </summary>
        /// <param name="command">the command to execute </param>
        /// <returns> <see langword="true"/> if the command is sent, <see langword="false"/> otherwise.</returns>
        internal async Task<bool> SendRemoteCommand(RemoteCommandType command)
        {
            

            if (SessionIndex < 0) { ExecuteRemoteCommandOnLocal(command); Console.WriteLine($"Command sent local: {command}"); return true; }
            else
            {
                PluginTCPClient client = GetTCPClientAtIndex();

                if (client != null)
                {
                    int bytes;
                    switch (command)
                    {
                        case RemoteCommandType.START:
                            bytes = await client.SendOnly("0");
                            Console.WriteLine($"Command 0 sent on remote: {command}({bytes})byte(s)");
                            return bytes>0;
                        case RemoteCommandType.STOP:
                            bytes = await client.SendOnly("1");
                            Console.WriteLine($"Command 1 sent on remote: {command}({bytes}byte(s))");
                            return bytes > 0;
                        case RemoteCommandType.MOTION_ON:
                            bytes = await client.SendOnly("2");
                            Console.WriteLine($"Command 2 sent on remote: {command}({bytes})byte(s)");
                            return bytes > 0;   
                        case RemoteCommandType.MOTION_OFF:
                            bytes = await client.SendOnly("3");
                            Console.WriteLine($"Command 3 sent on remote: {command}({bytes})byte(s)");
                            return bytes > 0;
                        case RemoteCommandType.REACTIVITY_ERROR:
                            bytes = await client.SendOnly("4");
                            Console.WriteLine($"Command 4 sent on remote: {command}({bytes})byte(s)");
                            return bytes > 0;
                        case RemoteCommandType.REACTIVITY_SECURE:
                            bytes = await client.SendOnly("5");
                            Console.WriteLine($"Command 5 sent on remote: {command}({bytes})byte(s)");
                            return bytes > 0;
                        case RemoteCommandType.REACTIVITY_NRM:
                            bytes = await client.SendOnly("6");
                            Console.WriteLine($"Command 6 sent on remote: {command}({bytes})byte(s)");
                            return bytes > 0;
                        case RemoteCommandType.REACTIVITY_HIGH:
                            bytes = await client.SendOnly("7");
                            Console.WriteLine($"Command 7 sent on remote: {command}({bytes})byte(s)");
                            return bytes > 0;
                        case RemoteCommandType.SECURERESETPOSITION:
                            bytes = await client.SendOnly("8");
                            Console.WriteLine($"Command 8 sent on remote: {command}({bytes})byte(s)");
                            return bytes > 0;
                        case RemoteCommandType.DATA:
                            bytes = await client.SendOnly("9");

                            //Console.WriteLine($"Command 9 sent on remote: {command}({bytes})byte(s)"); //do not log those one as we request them each remote tick
                            return bytes > 0;
                        default:
                            return false;

                    }
                }
                else { return false; }
            }

        }

        /// <summary>
        /// Should be called when a remote command request has been received on this plugin.
        /// </summary>
        /// <param name="command"></param>
        internal async void ExecuteRemoteCommandOnLocal(RemoteCommandType command)
        {
            if (command == RemoteCommandType.NONE) { return; }

            if (command == RemoteCommandType.DATA)
            {
                var payload = await motionSeat.GetFullMonitoringPayload();
                string json = JsonConvert.SerializeObject(payload);
                await tcpListener.SendOnly(json);
                return;
            }

            if (SessionIndex != -1) //If you received one of the following commands, it means you're supposed to handle a seat from here, so your index should be -1
            { SetSessionIndex(-1); }//Otherwise you would send again a new remote command that should be executed on the -1 plugin, and so on , and so on...
            Console.WriteLine($"Remote command requested: {command}");
            switch (command)
            {
                case RemoteCommandType.START:
                    await StartSeatAtIndex(bypassAcknowledge:true);
                    break;
                case RemoteCommandType.STOP:
                    await StopSeatAtIndex();
                    break;
                case RemoteCommandType.MOTION_ON:
                    EnableOrDisableMotionAtIndex(forceState: 1);
                    break;
                case RemoteCommandType.MOTION_OFF:
                    EnableOrDisableMotionAtIndex(forceState: 0);
                    break;
                case RemoteCommandType.REACTIVITY_ERROR:
                    SetReactivityAtIndex(ReactivityLevels.ERROR); //this write the reactivity directly on the seat using motionSeat.writeSensitivitySettings(.,.)
                    break;
                case RemoteCommandType.REACTIVITY_SECURE:
                    SetReactivityAtIndex(ReactivityLevels.SECURE);//this write the reactivity directly on the seat using motionSeat.writeSensitivitySettings(.,.)
                    break;
                case RemoteCommandType.REACTIVITY_NRM:
                    SetReactivityAtIndex(ReactivityLevels.NORMAL);//this write the reactivity directly on the seat using motionSeat.writeSensitivitySettings(.,.)
                    break;
                case RemoteCommandType.REACTIVITY_HIGH:
                    SetReactivityAtIndex(ReactivityLevels.HIGH);//this write the reactivity directly on the seat using motionSeat.writeSensitivitySettings(.,.)
                    break;
                case RemoteCommandType.SECURERESETPOSITION:
                    SecureZeroSeatAtIndex();
                    break;
                default:
                    break;

            }

        }

        /// <summary>
        /// Get the SeatInfo of a remote seat, according to <see cref="SessionIndex"/>
        /// </summary>
        /// <returns>The <see cref="SeatInfo"/> object if possible, or null otherwise.</returns>
        /// 
        /*
        private async Task<SeatInfo> GetSeatInfoAtIndex()
        {
            if (SessionIndex < 0)
            {
                // Si local, on retourne directement une copie
                extrasCache = null;
                return motionSeat.SeatInfo.Clone();
            }

            PluginTCPClient tcpClient = GetTCPClientAtIndex();

            bool result = await SendRemoteCommand(RemoteCommandType.DATA);
            if (!result) return null;

            string message = await tcpClient.ReadOnly();
            if (string.IsNullOrWhiteSpace(message)) return null;

            try
            {
                var payload = JsonConvert.DeserializeObject<SeatPayload>(message);
                if (payload == null) return null;

                seatInfoCopy = payload.SeatInfo;
                extrasCache = payload.Extras;
                return seatInfoCopy;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[JSON ERROR] {ex.Message}");
                return null;
            }
        }
        */


        public static bool IsDBoxDevicesConnected()
        {
            bool motionPortFound = false;
            bool controlPortFound = false;

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity");

                foreach (ManagementObject device in searcher.Get())
                {
                    string name = device["Name"]?.ToString() ?? "";
                    string description = device["Description"]?.ToString() ?? "";
                    string manufacturer = device["Manufacturer"]?.ToString() ?? "";

                    // Vérification du périphérique Motion Port
                    if (name == "D-BOX Haptic Bridge Motion Port" &&
                        description == "D-BOX Haptic Bridge Motion Port" &&
                        manufacturer == "D-BOX Technologies")
                    {
                        motionPortFound = true;
                    }

                    // Vérification du périphérique Control Port
                    if (name == "D-BOX Haptic Bridge Control Port" &&
                        description == "D-BOX Haptic Bridge Control Port" &&
                        manufacturer == "D-BOX Technologies")
                    {
                        controlPortFound = true;
                    }

                    // Si les deux sont trouvés, on arrête la recherche
                    if (motionPortFound && controlPortFound)
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors de la recherche des périphériques : " + ex.Message);
            }

            return motionPortFound && controlPortFound;
        }

        /// <summary>
        /// Retrieves a full structured payload containing seat runtime info and parsed monitoring metadata.
        /// </summary>
        /// <returns>
        /// A <see cref="SeatPayload"/> object combining <see cref="SeatInfo"/> and extra descriptive data.
        /// </returns>
        /// <remarks>
        /// Ideal for TCP serialization, logging, diagnostics, or remote visualization.
        /// </remarks>
        /// 
        /*
        internal async Task<SeatPayload> GetFullMonitoringPayload()
        {
            try
            {
                bool DboxConnected = IsDBoxDevicesConnected();

                string xml = await TCPMonitoring.GetMonitoringData();

                string descOverallState = MonitoringClient.GetFieldDescriptionById(xml, "1") ?? "Unknown";
                string descStreamMode = MonitoringClient.GetFieldDescriptionById(xml, "5002") ?? "Unknown";
                List<float> weights = MonitoringClient.GetFieldFloatValuesById(xml, "1003")?.ToList() ?? new List<float>();

                if (weights.Count >= 3)
                {
                    Console.WriteLine($"Weights: FL = {weights[0]:F1} kg | FR = {weights[1]:F1} kg | Rear = {weights[2]:F1} kg");
                }
                else
                {
                    Console.WriteLine("⚠ Not enough weight data available.");
                }


                return new SeatPayload
                {
                    SeatInfo = this.SeatInfo,
                    Extras = new ExtraData
                    {
                        OverallState = descOverallState,
                        StreamMode = descStreamMode,
                        Weights = weights,
                        DBoxConnected = DboxConnected
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Monitoring ERROR] Failed to retrieve monitoring payload: {ex.Message}");
                return new SeatPayload
                {
                    SeatInfo = this.SeatInfo,
                    Extras = new ExtraData
                    {
                        OverallState = "Error",
                        StreamMode = "Unknown",
                        Weights = new List<float>(),
                        DBoxConnected = false
                    }
                };
            }
        }
        */


        private async Task<SeatInfo> GetSeatInfoAtIndex()
        {
            try
            {
                // If local (SessionIndex < 0), communicate directly with the local MonitoringClient
                if (SessionIndex < 0)
                {
                    // Send request to local monitoring client (direct TCP call to motionSeat TCPMonitoring)
                    string xml = await motionSeat.TCPMonitoring.GetMonitoringData();

                    if (string.IsNullOrWhiteSpace(xml))
                    {
                        Console.WriteLine("[SEATINFO] No monitoring data received from local seat.");
                        extrasCache = null;
                        return motionSeat.SeatInfo.Clone();
                    }

                    // Build Extras manually from local monitoring XML
                    string overallState = MonitoringClient.GetFieldDescriptionById(xml, "1") ?? "Unknown";
                    string streamMode = MonitoringClient.GetFieldDescriptionById(xml, "5002") ?? "Unknown";
                    var weights = MonitoringClient.GetFieldFloatValuesById(xml, "1003")?.ToList() ?? new List<float>();

                    extrasCache = new ExtraData
                    {
                        OverallState = overallState,
                        StreamMode = streamMode,
                        Weights = weights
                        
                    };

                    // Return a copy of the current local SeatInfo
                    return motionSeat.SeatInfo.Clone();
                }
                else
                {
                    // If remote, send command to remote seat
                    PluginTCPClient tcpClient = GetTCPClientAtIndex();

                    bool result = await SendRemoteCommand(RemoteCommandType.DATA);
                    if (!result)
                    {
                        //Console.WriteLine("[SEATINFO] Failed to request remote data.");
                        return null;
                    }

                    string message = await tcpClient.ReadOnly();
                    if (string.IsNullOrWhiteSpace(message))
                    {
                        //Console.WriteLine("[SEATINFO] No response from remote seat.");
                        return null;
                    }

                    var payload = JsonConvert.DeserializeObject<SeatPayload>(message);
                    if (payload == null)
                    {
                        //Console.WriteLine("[SEATINFO] Failed to parse remote seat data.");
                        return null;
                    }

                    seatInfoCopy = payload.SeatInfo;
                    extrasCache = payload.Extras;
                    return seatInfoCopy;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SEATINFO ERROR] {ex.Message}"); 
                return null;
            }
        }
    }
}
