using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Threading;
using Alstom.MotionSeatPlugin.TCP;
using System.Threading.Tasks;


namespace Alstom.MotionSeatPlugin
{
    /// <summary>
    /// UI controller class for real-time interaction with the D-BOX Motion Seat.
    /// Handles periodic logic and UI updates, TCP telemetry fetching, and remote control dispatch.
    /// </summary>
    internal partial class MotionSeatControl
    {

        // ============================================================================
        // =                                Fields                                    =
        // ============================================================================

        #region Fields

        private readonly Color color1 = Color.DarkGray;
        private readonly Color color2 = Color.LawnGreen;

        /// <summary>
        /// Indicates the current state of the local tick alternation.
        /// Used to toggle the TickIndicator color for visual feedback.
        /// </summary>
        private bool evenTick = false;

        /// <summary>
        /// Indicates the current state of the remote tick alternation.
        /// Used to toggle the RemoteIndicator color for remote activity feedback.
        /// </summary>
        private bool remoteTick = false;

        /// <summary>
        /// Prevents overlapping asynchronous calls during TCP monitoring updates.
        /// Acts as a basic async lock for the MonitoringTimer.
        /// </summary>
        private bool monitoringTcpLock = false;

        #endregion

        // ============================================================================
        // =                            Timer Functions                               =
        // ============================================================================

        #region Timer Functions

        /// <summary>
        /// Periodic timer for local seat update and UI blink indicator.
        /// </summary>
        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                Update_Local_Seat_Continuous();
                TickIndicator.BackColor = evenTick ? color1 : color2;
                evenTick = !evenTick;

                if (this.Visible)
                {
                    Update_UI();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateTimer ERROR] {ex.Message}");
            }
        }

        /// <summary>
        /// Periodic timer for fetching seat telemetry from DBOX TCP monitoring system.
        /// </summary>
        private async void MonitoringTimer_Tick(object sender, EventArgs e)
        {
            if (monitoringTcpLock) return;

            monitoringTcpLock = true;
            try
            {
                TCPIndicator.BackColor = color2;
                await motionSeat.GetFullMonitoringPayload();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MonitoringTimer ERROR] {ex.Message}");
            }
            finally
            {
                TCPIndicator.BackColor = color1;
                monitoringTcpLock = false;
            }
        }


        /// <summary>
        /// Timer callback that runs every 100ms to handle remote seat control.
        /// Receives remote TCP commands and executes them on the local seat.
        /// </summary>
        private async void RemoteTimer_Tick(object sender, EventArgs e)
        {
            if (SessionIndex != -1)
            {
                UpdateSeatInfoWithIndex();
                return;
            }
            else
            {
                try
                {
                    // Update seat index and tick counter
                    UpdateSeatInfoWithIndex();

                    // Read incoming message
                    string command = await tcpListener.ReadOnly();

                    if (!string.IsNullOrWhiteSpace(command))
                    {
                        var commandType = MessageConverter.GetCommandType(command);

                        
                        Console.WriteLine($"[REMOTE] Command received: {commandType}");
                        ExecuteRemoteCommandOnLocal(commandType);
                    }

                    // Toggle UI indicator light
                    remoteTick = !remoteTick;
                    RemoteIndicator.BackColor = remoteTick ? color1 : color2;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[REMOTE ERROR] Failed to execute remote command: {ex.Message}");
                }
            }
        }


        #endregion

    }
}