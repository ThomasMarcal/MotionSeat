using System;
using System.Drawing;
using System.Threading.Tasks;
using Alstom.MotionSeatPlugin.TCP;
using DBox.MotionSeat;

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

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {

            if (SessionIndex != -1)
                return;
            

            try
            {
                // Envoie uniquement les consignes si l’état est correct
                if (Status == SeatStatus.PLAYING && MotionFlag)
                {
                    Update_Local_Seat_Continuous();
                }

                if (this.Visible)
                {
                    Update_UI();
                    TickIndicator.BackColor = evenTick ? color1 : color2;
                    evenTick = !evenTick;
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

            _ = Task.Run(async () =>
            {
                monitoringTcpLock = true;
                TCPIndicator.BackColor = color2;

                try
                {
                    await motionSeat.GetFullMonitoringPayload().ConfigureAwait(false);
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
            });
        }



        private int remoteTickCounter = 0; // Counter for timing DATA command (every 100ms tick)

        /// <summary>
        /// Timer callback that runs every 100ms to handle remote seat control.
        /// Receives remote TCP commands and executes them on the local seat.
        /// The 'DATA' command is rate-limited to once every 1000ms.
        /// </summary>
        private async void RemoteTimer_Tick(object sender, EventArgs e)
        {
            /*
            if (SessionIndex != 0)
                return;
            */

            // UI feedback – clignotement visuel
            remoteTick = !remoteTick;
            RemoteIndicator.BackColor = remoteTick ? color1 : color2;

            // Déléguer à une tâche séparée pour ne jamais bloquer le timer
            _ = HandleRemoteCommandAsync();
        }

        private async Task HandleRemoteCommandAsync()
        {
            try
            {
                await UpdateSeatInfoWithIndex();

                string command = await tcpListener.ReadOnly();

                if (!string.IsNullOrWhiteSpace(command))
                {
                    var commandType = MessageConverter.GetCommandType(command);
                    ExecuteRemoteCommandOnLocal(commandType);
                }

                await Task.Delay(5);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REMOTE ERROR] {ex.Message}");
            }
        }

        #endregion

    }
}