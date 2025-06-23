using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DBox.MotionSeat;
using static DBox.MotionSeat.MotionSeat;

namespace Alstom.MotionSeatPlugin
{

    /// <summary>
    /// Represents the Control Form used to drive the given <see cref="MotionSeat"/>.
    /// </summary>
    internal partial class MotionSeatControl
    {

        #region Fields 

        /// <summary>
        /// The motion seat object of this very plugin that performs the calls on the dbox API.<br/>
        /// It is associated with this <see cref="MotionSeatControl"/> in constructor and can't be changed.
        /// </summary>
        private readonly MotionSeat motionSeat;

        /// <summary>
        /// Local cached motion data to be sent to <see cref="motionSeat"/> each UI tick.  
        /// Avoids allocating a temporary array on every update.
        /// </summary>
        private float
            localAccelerationX, localAccelerationY, localAccelerationZ,
            localRailPitch, localRailRoll,
            localVelocityX, localVelocityY, localVelocityZ;


        /// <summary>
        /// Index of the session (cabin) being controlled:  
        /// –1 = this plugin’s own seat (local mode),  
        ///  0 = remote seat sessions (INST controls CAB).  
        /// </summary>
        internal int SessionIndex {get; private set;} = -1;

        /// <summary>
        /// A <b>copy</b> of the seat info of the seat currently observed, could be the one of this plugin (<see cref="motionSeat"/>) but could be a different one thanks to <see cref="SessionIndex"/>.<br/>
        /// Use always this to retrieve info from the managed seat. Don't forget to call <see cref="UpdateSeatInfoWithIndex"/> to get the latest value available.
        /// </summary>
        private SeatInfo seatInfoCopy;

        /// <summary>
        /// Gets/sets the “motion enabled” flag on the observed seat.  
        /// When reading, <see cref="seatInfoCopy"/> must be up to date.  
        /// When writing, writes directly to the local <see cref="motionSeat.SeatInfo"/> (remote use sends TCP).
        /// </summary>
        public bool MotionFlag
        {
            get { return seatInfoCopy.MotionFlag; } // Get value of the local or remote
            set { motionSeat.SeatInfo.MotionFlag = value; } // Write directly on the seat
        }

        /// <summary>
        /// Converts between a raw float reactivity value and <see cref="ReactivityLevels"/>.  
        /// Use as an indexer: <c>ReactivityConverter[someFloat]</c> or `<c>ReactivityConverter[someLevel]</c>`.
        /// </summary>
        private readonly ReactivityConverter ReactivityConverter;

        /// <summary>
        /// Gets/sets the reactivity level of the observed seat.  
        /// Reading returns <see cref="ReactivityLevels"/> from the cached <see cref="seatInfoCopy"/>.  
        /// Writing pushes the corresponding float via <see cref="motionSeat.WriteSendSensitivitySettings"/>.
        /// </summary>
        private ReactivityLevels Reactivity
        {
            get {return ReactivityConverter[seatInfoCopy.Reactivity];}
            set {motionSeat.WriteSendSensitivitySettings(ReactivityConverter[value]);}
        }


        /// <summary>
        /// Shortcut to get the last known <see cref="SeatStatus"/> of the observed seat.  
        /// Make sure <see cref="seatInfoCopy"/> is refreshed first.
        /// </summary>
        private SeatStatus Status
        {
            get { return seatInfoCopy.Status; }
        }

        /// <summary>
        /// Prevents the start sequence from proceeding until the user has explicitly confirmed.  
        /// </summary>
        public bool allowToStart = false;

        #endregion

        #region UI Caching

        /// <summary>
        /// Cache of the last-rendered reactivity to avoid redundant graphics updates each tick.  
        /// </summary>
        private ReactivityLevels MotionSeatReactivity_cache;

        /// <summary>
        /// Cache of the last-rendered seat status to avoid redundant graphics updates.  
        /// </summary>
        private SeatStatus MotionSeatStatus_cache;

        /// <summary>
        /// Cache of the last-rendered motion-allowed flag to avoid redundant graphics updates.  
        /// </summary>
        private bool MotionSeatAllowMotion_cache;

        /// <summary>
        /// Preloaded button images to avoid reloading <c>Properties.Resources.xxx</c> on each draw.  
        /// </summary>
        private readonly Image systemOff, systemStarting, systemOn,
                               motionOff, motionOn,
                               response0, response0b, response1, response2;

        #endregion

        /// <summary>
        /// Constructs a new MotionSeatControl.
        /// </summary>
        /// <param name="motionSeat">
        /// The MotionSeat instance that wraps the D-BOX SDK. 
        /// It must be non-null and remains constant for this form’s lifetime.
        /// </param>
        internal MotionSeatControl(MotionSeat motionSeat)
        {
            InitializeComponent();
            RedirectConsoleOutput(); // Also send Console.WriteLine to a UI label.

            // Assign the injected MotionSeat and ensure it's valid.
            this.motionSeat = motionSeat ?? throw new ArgumentNullException(nameof(motionSeat));

            // Preload all button/indicator images once to avoid repeated allocations.
            systemOff = Properties.Resources.OFF;
            systemStarting = Properties.Resources.STARTING;
            systemOn = Properties.Resources.ON;
            motionOff = Properties.Resources.MotionDisabled;
            motionOn = Properties.Resources.MotionEnabled;
            response0 = Properties.Resources.Response0;
            response0b = Properties.Resources.Response0b;
            response1 = Properties.Resources.Response1;
            response2 = Properties.Resources.Response2;

            // Ensure the seat starts with motion disabled and "secure" reactivity.
            motionSeat.SeatInfo.MotionFlag = false;
            motionSeat.WriteSendSensitivitySettings(ReactivityConverter[ReactivityLevels.SECURE]);

            // Default to controlling this plugin's own seat.
            SessionIndex = -1;

            // Kick off the initial seat-info refresh asynchronously.
            UpdateSeatInfoWithIndex();

            // Wire up TCP disconnect handling before opening any dialogs.
            AttachDisconnectionHandler();

            // Configure TCP client or server based on the configured client name.
            ConfigureDialogsBasedOnClientName();

            // Refresh seat-info once more after connecting dialogs.
            UpdateSeatInfoWithIndex();

            // Restore last window position if available.
            TryRestoreWindowPosition();
        }

        // ─── Helper Method ───────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to restore the form’s last saved location from disk.
        /// </summary>
        /// <summary>
        /// Attempts to restore the form’s last saved location from disk. Defaults to (0,0) if missing or invalid.
        /// </summary>
        private void TryRestoreWindowPosition()
        {
            const string savePath = "MotionSeatPluginSaved\\last.txt";
            Point fallback = new Point(0, 0);

            try
            {
                if (!File.Exists(savePath))
                {
                    Location = fallback;
                    return;
                }

                string content = File.ReadAllText(savePath);
                var parts = content.Split(',');

                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int x) &&
                    int.TryParse(parts[1], out int y))
                {
                    Location = new Point(x, y);
                }
                else
                {
                    Console.WriteLine("Invalid window position format. Using (0,0).");
                    Location = fallback;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restoring window position: {ex.Message}");
                Location = fallback;
            }
        }


        /// <summary>
        /// Attaches the appropriate TCP disconnection callback based on the configured client role.
        /// </summary>
        /// <remarks>
        /// If StaticClientConfig.LocalName starts with "INST", this plugin is a TCP client and we subscribe
        /// to tcpClientCAB1.OnDisconnected. If it starts with "CAB", this plugin is a TCP listener/server
        /// and we subscribe to tcpListener.OnClientDisconnected. Logs an informational message if the
        /// client name is missing or unrecognized.
        /// </remarks>
        private void AttachDisconnectionHandler()
        {
            string clientName = StaticClientConfig.LocalName;

            if (string.IsNullOrWhiteSpace(clientName))
            {
                Console.WriteLine("[MotionSeatControl] Client name is empty. Cannot attach disconnection handler.");
                return;
            }

            if (clientName.StartsWith("INST", StringComparison.OrdinalIgnoreCase))
            {
                // This plugin is acting as a TCP client.
                // Use async void so that exceptions inside HandleTcpDisconnection get logged.
                tcpClientCAB1.OnDisconnected = async () =>
                {
                    await HandleTcpDisconnection();
                };
                Console.WriteLine("[MotionSeatControl] Disconnection handler attached for TCP client (INST).");
            }
            else if (clientName.StartsWith("CAB", StringComparison.OrdinalIgnoreCase))
            {
                // This plugin is acting as a TCP listener/server.
                tcpListener.OnClientDisconnected = async () =>
                {
                    await HandleTcpDisconnection();
                };
                Console.WriteLine("[MotionSeatControl] Disconnection handler attached for TCP listener (CAB).");
            }
            else
            {
                Console.WriteLine($"[MotionSeatControl] Unknown client type '{clientName}'. No disconnection handler attached.");
            }
        }



        /// <summary>
        /// Handles a TCP disconnection event by stopping the motion seat
        /// and logging a specific message based on whether this plugin is acting as
        /// a TCP client ("INST") or a TCP listener/server ("CAB").
        /// </summary>
        private async Task HandleTcpDisconnection()
        {
            // Stop the seat safely when connection is lost
            await StopSeatAtIndex();

            // Retrieve the configured client name (e.g., "INST1", "CAB2", etc.)
            string clientName = StaticClientConfig.LocalName;

            if (string.IsNullOrWhiteSpace(clientName))
            {
                Console.WriteLine("[DISCONNECT] Unknown client name. TCP disconnection detected. Seat stopped.");
                return;
            }

            // Log a specific message based on the client type
            if (clientName.StartsWith("INST", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[DISCONNECT] TCP client (INST) disconnected. Seat stopped for safety.");
            }
            else if (clientName.StartsWith("CAB", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[DISCONNECT] TCP listener (CAB) disconnected. Seat stopped for safety.");
            }
            else
            {
                Console.WriteLine($"[DISCONNECT] TCP disconnection detected for client '{clientName}'. Seat stopped.");
            }
        }


        private void ConfigureDialogsBasedOnClientName()
        {
            try
            {
                string clientName = StaticClientConfig.LocalName;

                if (string.IsNullOrWhiteSpace(clientName))
                {
                    Console.WriteLine("[MotionSeatControl] Client name is empty. No dialog configuration applied.");
                    StaticClientConfig.LocalIP = "127.0.0.1";
                    tcpListener.OpenDialog();
                    return;
                }

                if (clientName.StartsWith("INST", StringComparison.OrdinalIgnoreCase))
                {
                    tcpClientCAB1.JoinDialog(); // INST = connect to remote seat
                    SetSessionIndex(0);         // configure UI for session 0
                    Console.WriteLine("[MotionSeatControl] JoinDialog activated for INST.");
                }
                else if (clientName.StartsWith("CAB", StringComparison.OrdinalIgnoreCase))
                {
                    tcpListener.OpenDialog(); // CAB = listen for instructions
                    Console.WriteLine("[MotionSeatControl] OpenDialog activated for CAB.");
                }
                else
                {
                    StaticClientConfig.LocalIP = "127.0.0.1";
                    tcpListener.OpenDialog();
                    Console.WriteLine($"[MotionSeatControl] Unknown client type for name '{clientName}'. No dialog opened.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MotionSeatControl] Error configuring dialogs: {ex.Message}");
            }
        }


        private void DisposeDialogsBasedOnClientName()
        {
            try
            {
                string clientName = StaticClientConfig.LocalName;

                if (string.IsNullOrWhiteSpace(clientName))
                {
                    Console.WriteLine("[MotionSeatControl] Client name is empty. Nothing to dispose.");
                    tcpListener.CloseDialog();
                    return;
                }

                if (clientName.StartsWith("INST", StringComparison.OrdinalIgnoreCase))
                {
                    tcpClientCAB1.QuitDialog(); // INST = ferme le client TCP
                    Console.WriteLine("[MotionSeatControl] CloseDialog called for TCP client (INST).");
                }
                else if (clientName.StartsWith("CAB", StringComparison.OrdinalIgnoreCase))
                {
                    tcpListener.CloseDialog(); // CAB = ferme le serveur TCP
                    Console.WriteLine("[MotionSeatControl] CloseDialog called for TCP listener (CAB).");
                }
                else
                {
                    tcpListener.CloseDialog();
                    Console.WriteLine($"[MotionSeatControl] Unknown client type for name '{clientName}'. TCP listener closed by default.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MotionSeatControl] Error disposing dialogs: {ex.Message}");
            }
        }


        /// <summary>
        /// Changes the seat being managed by this plugin. 
        /// Use -1 for local seat control, or 0 to connect to remote instructor control.
        /// </summary>
        internal async void SetSessionIndex(int sessionIndex)
        {
            if (sessionIndex == SessionIndex)
                return;

            LockUI();

            try
            {
                Console.WriteLine($"[MotionSeat] Changing SessionIndex from {SessionIndex} to {sessionIndex}");

                // Close any active TCP client connection before switching
                GetTCPClientAtIndex()?.QuitDialog();

                // Accept only sessionIndex -1 (local) or 0 (remote/instructor)
                SessionIndex = (sessionIndex == 0) ? 0 : -1;

                // If managing a remote seat, connect via TCP
                if (SessionIndex == 0)
                {
                    await GetTCPClientAtIndex()?.JoinDialog();
                }

                // Refresh seat info after switching session
                await UpdateSeatInfoWithIndex();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SetSessionIndex ERROR] {ex.Message}");
            }
            finally
            {
                UnlockUI();
            }
        }




        /// <summary>
        /// Updates the local copy of seat info using the seat managed at <see cref="SessionIndex"/>.
        /// Also refreshes <see cref="MotionFlag"/>, <see cref="Reactivity"/>, and <see cref="Status"/>.
        /// </summary>
        private async Task UpdateSeatInfoWithIndex()
        {
            try
            {
                var newInfo = await GetSeatInfoAtIndex();
                if (newInfo != null)
                {
                    seatInfoCopy = newInfo;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateSeatInfoWithIndex ERROR] {ex.Message}");
            }
        }


        /// <summary>
        /// Start or stop the motion seat you're managing depending on its state.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> StartOrStopSeat()
        {
            await UpdateSeatInfoWithIndex(); //to be up to date before reading the seat status.
            bool result = false;
            
            
            if (Status == SeatStatus.UNKNOWN || Status == SeatStatus.ERROR)
            
            { result = await StartSeatAtIndex(); }
            
            
            else if (Status == SeatStatus.PLAYING || Status == SeatStatus.STOPPED || Status == SeatStatus.INITIALISING)
            { result = await StopSeatAtIndex(); }
            return result;
        }



        /// <summary>
        /// Enable or disable the motion flag of the motion seat you're managing.
        /// </summary>
        /// <param name="debug">Bypass seat status verification.</param>
        /// <param name="forceState">0 = force disable, 1 = force enable, -1 = auto toggle.</param>
        private async void EnableOrDisableMotionAtIndex(bool debug = false, int forceState = -1)
        {
            LockUI();

            try
            {
                await UpdateSeatInfoWithIndex();

                bool isPlaying = Status == SeatStatus.PLAYING;
                bool canToggle = isPlaying || debug || MotionFlag || forceState != -1;
                if (!canToggle)
                    return;

                if (MotionFlag || forceState == 0)
                {
                    SecureZeroSeatAtIndex();
                    MotionFlag = false;
                    ResponseButton.BackgroundImage = response0;
                }
                else if (!MotionFlag || forceState == 1)
                {
                    if (SessionIndex < 0)
                    {
                        MotionFlag = true;
                        Reactivity = ReactivityLevels.NORMAL;
                        ResponseButton.BackgroundImage = response1;
                        await Task.Delay(150);
                    }
                    else
                    {
                        await SendRemoteCommand(RemoteCommandType.MOTION_ON);
                        ResponseButton.BackgroundImage = response1;
                        await Task.Delay(150);
                    }
                }

                await UpdateSeatInfoWithIndex();

                Console.WriteLine($"Simulation motion allowed: {MotionFlag} {(SessionIndex < 0 ? "" : "(remote sync may lag)")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MOTION FLAG ERROR] {ex.Message}");
            }
            finally
            {
                UnlockUI();
            }
        }



        /// <summary>
        /// Change the reactivity of the seat.
        /// </summary>
        /// <param name="reactivity">the reactivity the seat should follow</param>
        internal async void SetReactivityAtIndex(ReactivityLevels reactivity)
        {
            if (reactivity == Reactivity) { return; } //no need to change!
            if (SessionIndex<0)
            {
                Reactivity = reactivity;
            }
            else
            {
                switch (reactivity)
                {
                    case ReactivityLevels.SECURE:
                        await SendRemoteCommand(RemoteCommandType.REACTIVITY_SECURE);
                        break;
                    case ReactivityLevels.NORMAL:
                        await SendRemoteCommand(RemoteCommandType.REACTIVITY_NRM);
                        break;
                    case ReactivityLevels.HIGH:
                        await SendRemoteCommand(RemoteCommandType.REACTIVITY_HIGH);
                        break;
                    case ReactivityLevels.ERROR:
                        await SendRemoteCommand(RemoteCommandType.REACTIVITY_ERROR);
                        break;
                    default:
                        break;
                }


            }
        }


        /// <summary>
        /// Connect, start and initialise the Motion Seat you're managing.
        /// </summary>
        /// <param name="bypassAcknowledge">Use this only when starting the seat from a remote command.</param>
        /// <returns></returns>
        private async Task<bool> StartSeatAtIndex(bool bypassAcknowledge=false)
        {
            LockUI();
            allowToStart = false;
            if (!bypassAcknowledge)
            {
                Pop_Up_MotionSeat popUp = new Pop_Up_MotionSeat(this); // "this" représente ici l'instance courante de MotionSeatControl
                popUp.ShowDialog();
            }
            else { allowToStart = true; }
            bool result = false;

            if (allowToStart == true) //this is mainly set by clicking on the StartCheckButton
            {
                if (SessionIndex < 0)
                {
                    result = await motionSeat.ConnectInit(debug: false);
                    
                    //motionSeat.SeatInfo.Status = SeatStatus.PLAYING;
                    //result = true;

                    //motionSeat.TCPMonitoring.Autoreconnect = true; //use that in final config
                    //UseTCPCheckbox.Checked = true; // will toggle tcp timer accordingly

                    if (result && motionSeat.SeatInfo.Status == SeatStatus.PLAYING)
                    {
                        Reactivity = ReactivityLevels.NORMAL;
                        result = true;
                        ResponseButton.BackgroundImage = response1;
                    }
                    MotionFlag = false;
                    Console.WriteLine($"Start and Connect Seat: {(result?"OK":"NOK")}");
                    await UpdateSeatInfoWithIndex();

                }
                else
                {
                    result = await SendRemoteCommand(RemoteCommandType.START);
                    Console.WriteLine($"You're managing seat n°{SessionIndex}. Start command sent: {(result ? "OK" : "NOK")}");
                }
            }

            UnlockUI();
            return result;
        }

        /// <summary>
        /// Wait for the <see cref="allowToStart"/> boolean to be true.
        /// </summary>
        /// <remarks>Pressing the <see cref="StartCheckButton"/> change the <see cref="allowToStart"/>'s value.</remarks>
        private async Task WaitForAcknoledgment(int iteration, int delayMs)
        {
            for (int i = 0; i < iteration; i++)
            {
                if (allowToStart == true) { return; }
                await Task.Delay(delayMs);
            }
            return;
        }


        /// <summary>
        /// Stop and disconnect the Motion Seat you're managing.
        /// </summary>
        /// <returns></returns>
        internal async Task<bool> StopSeatAtIndex()
        {
            LockUI();
            bool result;

            if (motionSeat.SeatInfo.MotionFlag)
            {
                // Appelle ta méthode de désactivation
                EnableOrDisableMotionAtIndex(false);
                await Task.Delay(500); // petite pause pour laisser le SDK s'ajuster
            }

            if (SessionIndex < 0)
            {
                SecureZeroSeatAtIndex(); //motion locked with this one
                await Task.Delay(1000);//wait for motion to be done
                result = await motionSeat.StopDisconnect(debug: false);
                Console.WriteLine($"Stop And Disconnect Seat: {(result ? "OK" : "NOK")}");
            }
            else
            {
                result = await SendRemoteCommand(RemoteCommandType.STOP);
                Console.WriteLine($"You're managing seat n°{SessionIndex}. Stop command sent: {(result ? "OK" : "NOK")}");
            }
            UnlockUI();
            return result;
        }


        /// <summary>
        /// Dispose additionnal resources of the seat and the control form.
        /// </summary>
        /// <returns></returns>
        internal async Task<bool> DisposeResources()
        {
            DisconnectTCPSockets();
            bool result = await motionSeat.DiposeBeforeClose();
            DisposeDialogsBasedOnClientName();
            Console.WriteLine("Seat resources disposed. Plugin shall close soon (you can't access seat resources anymore)");
            MotionSeatControl_FormClosing(null,null); //to save directly the position of the form, as the closing event might not be called if the process is killed.
            return result;
        }


        /// <summary>
        /// Cache the new motion targets to reach by this very plugin's motion seat.<br/>
        /// They will be sent to the Motion Seat at next Form's logical tick, in <see cref="UpdateTimer_Tick(object, EventArgs)"/>.<br/>
        /// </summary>
        /// <remarks>Won't work if motion locked.</remarks>
        internal void SetLocalMotionTargets(double rightAccelerationMps2, double forwardAccelerationMps2, double verticalAccelerationMps2 = 0d, double compensationPitchRad = 0d, double compensationRolRad = 0d)
        {
            if (motionSeat.SeatInfo.MotionFlag)
            {
                localAccelerationX = (float)rightAccelerationMps2;
                localAccelerationY = (float)forwardAccelerationMps2;
                localAccelerationZ = (float)verticalAccelerationMps2;

                localRailPitch = (float)compensationPitchRad;
                localRailRoll = (float)compensationRolRad;
            }

        }

        internal void SetLocalTrainVelocity(double velocityX, double velocityY, double velocityZ)
        {
            this.localVelocityX = (float)velocityX;
            this.localVelocityY = (float)velocityY;
            this.localVelocityZ = (float)velocityZ;
        }

        /// <summary>
        /// Do some event(s) on this very plugin's motion seat. Will replace last DoLocalEvent() activated events.
        /// </summary>
        /// <param name="intArg">some effects differ if it's the front bogie (1) or back bogie (0)</param>
        internal void DoLocalEvent(DBoxEventMask eventType, params object[] args)
        {
            Console.WriteLine($"Request to do the following event(s):{eventType}");
            if (motionSeat.SeatInfo.MotionFlag)
            {
                
                /*
                if ((eventType & DBoxEventMask.SWITCH) > 0)
                {motionSeat.DoSwitchMotion(true);}
                */

                // HIT
                if ((eventType & DBoxEventMask.HIT) != 0)
                {
                    // Par défaut : bogie avant, intensité 0.5f
                    int bogieIndex = 1;
                    float intensity = 0.5f;

                    // Si on a au moins un argument et c'est un int → bogieIndex
                    if (args.Length > 0 && args[0] is int idx)
                        bogieIndex = idx;

                    // Si on a un deuxième argument et c'est un float → intensity
                    if (args.Length > 1 && args[1] is float f)
                        intensity = f;

                    motionSeat.DoBogieSingleHit(bogieIndex, intensity);
                }

                if ((eventType & DBoxEventMask.CONTINUOUS) > 0)
                {motionSeat.ActivateBogieContinuousHit(true);}
                else
                { motionSeat.ActivateBogieContinuousHit(false);}

                if ((eventType & DBoxEventMask.SKID)>0)
                {motionSeat.ActivateBogieSkid(true);}
                else
                { motionSeat.ActivateBogieSkid(false); }

                /*
                if ((eventType & DBoxEventMask.IMPACT)>0)
                {motionSeat.DoTrainImpact();
                }
                */

                // --- IMPACT
                if ((eventType & DBoxEventMask.IMPACT) != 0)
                {

                    motionSeat.DoTrainImpact();
                    /*
                    // Si on a deux floats, on les prend comme bump et shake
                    if (args.Length >= 2 && args[args.Length - 2] is float bump && args[args.Length - 1] is float shake)
                    {
                        motionSeat.DoTrainImpact(bump, shake);
                    }
                    else
                    {
                        // Pas assez d'arguments → version sans args
                        motionSeat.DoTrainImpact();
                    }
                    */
                }

                if ((eventType & DBoxEventMask.ENV) > 0)
                { motionSeat.DoEnvContinuous(true); }
                else
                { motionSeat.DoEnvContinuous(false); }
            }
            else { Console.WriteLine("But Motion Lock is activated!"); }
        }

        /// <summary>
        /// Return the continuous event that are currently activated on this very plugin's motion seat.
        /// </summary>
        internal DBoxEventMask GetLocalContinuousEvents()
        {
            return motionSeat.GetContinuousEvents();
        }

        /// <summary>
        /// Set back the Motion Seat you're managing to rest position and toggle off the motion flag of the seat.
        /// </summary>
        /// <param name="from_motionoff">Toggle this when calling this from <see cref="EnableOrDisableMotionAtIndex(bool, int)"/></param>
        /// <remarks>Lower also the reactivity to <see cref="ReactivityLevels.SECURE"/></remarks>
        internal async void SecureZeroSeatAtIndex(bool from_motionoff=false)
        {
            if (SessionIndex < 0)
            {
                MotionFlag = true;
                Reactivity = ReactivityLevels.SECURE;
                SetLocalMotionTargets(0, 0, 0, 0, 0);
                MotionFlag = false;
                await UpdateSeatInfoWithIndex();
            }
            else
            {
                if (!from_motionoff)
                {
                    await SendRemoteCommand(RemoteCommandType.SECURERESETPOSITION);
                }
            }
        }




        /// <summary>
        /// To set the correct StartStopButton graphics.
        /// </summary>
        /// <returns></returns>
        private static StartPhase StatusToSSButton(SeatStatus status)
        {
            switch (status)
            {
                case SeatStatus.ERROR:
                    return StartPhase.STARTING;
                case SeatStatus.INITIALISING:
                    return StartPhase.STARTING;

                case SeatStatus.PLAYING:
                    return StartPhase.ON;
                case SeatStatus.SHUTDOWN:
                    return StartPhase.ON;

                case SeatStatus.STOPPED:
                    return StartPhase.OFF;
                case SeatStatus.UNKNOWN:
                    return StartPhase.OFF;

                default:
                    return StartPhase.OFF;
            }
        }


        // Place these fields in your class (if not already present)
        private float lastAccX, lastAccY, lastAccZ;
        private float lastPitch, lastRoll;
        private float lastVelX, lastVelY, lastVelZ;

        /// <summary>
        /// Update the <see cref="motionSeat"/> with the new cached values.
        /// </summary>
        private void Update_Local_Seat_Continuous()
        {
            // 1) Replace individual invalid values with last known valid ones
            if (float.IsNaN(localAccelerationX) || float.IsInfinity(localAccelerationX)) localAccelerationX = lastAccX;
            else lastAccX = localAccelerationX;

            if (float.IsNaN(localAccelerationY) || float.IsInfinity(localAccelerationY)) localAccelerationY = lastAccY;
            else lastAccY = localAccelerationY;

            if (float.IsNaN(localAccelerationZ) || float.IsInfinity(localAccelerationZ)) localAccelerationZ = lastAccZ;
            else lastAccZ = localAccelerationZ;

            if (float.IsNaN(localRailPitch) || float.IsInfinity(localRailPitch)) localRailPitch = lastPitch;
            else lastPitch = localRailPitch;

            if (float.IsNaN(localRailRoll) || float.IsInfinity(localRailRoll)) localRailRoll = lastRoll;
            else lastRoll = localRailRoll;

            if (float.IsNaN(localVelocityX) || float.IsInfinity(localVelocityX)) localVelocityX = lastVelX;
            else lastVelX = localVelocityX;

            if (float.IsNaN(localVelocityY) || float.IsInfinity(localVelocityY)) localVelocityY = lastVelY;
            else lastVelY = localVelocityY;

            if (float.IsNaN(localVelocityZ) || float.IsInfinity(localVelocityZ)) localVelocityZ = lastVelZ;
            else lastVelZ = localVelocityZ;

            // 2) Determine occupancy from the latest weight measurements
            bool isOccupied = false;
            float normalizedWeight = 0f;

            if (extrasCache?.Weights != null && extrasCache.Weights.Count > 0)
            {
                float totalWeight = extrasCache.Weights.Sum();
                normalizedWeight = totalWeight / 10f;
                isOccupied = normalizedWeight > 150f;
            }
            else
            {
                isOccupied = true; // Treat as occupied if unknown
            }

            // 3) Compute speed magnitude (m/s)
            double speedMagnitude = Math.Sqrt(
                localVelocityX * localVelocityX +
                localVelocityY * localVelocityY +
                localVelocityZ * localVelocityZ
            );

            // 4) Define thresholds
            const double speedThreshold = 3.0;
            const float accThreshold = 0.05f;
            const float angleThreshold = 0.05f;

            // 5) Detect high accel or large angles
            bool highAccel = Math.Abs(localAccelerationX) > accThreshold
                          || Math.Abs(localAccelerationY) > accThreshold
                          || Math.Abs(localAccelerationZ) > accThreshold;

            bool largeAngle = Math.Abs(localRailPitch) > angleThreshold
                           || Math.Abs(localRailRoll) > angleThreshold;

            // 6) Determine if zeroing is needed
            bool shouldZeroAll = !isOccupied
                || (speedMagnitude < speedThreshold && !(highAccel || largeAngle));

            if (shouldZeroAll)
            {
                localAccelerationX = 0f;
                localAccelerationY = 0f;
                localAccelerationZ = 0f;
                localRailPitch = 0f;
                localRailRoll = 0f;
                localVelocityX = 0f;
                localVelocityY = 0f;
                localVelocityZ = 0f;
            }

            // 7) Apply motion values
            motionSeat.WriteAcceleration(localAccelerationX, localAccelerationY, localAccelerationZ);
            motionSeat.WritePitch(localRailPitch);
            motionSeat.WriteRoll(localRailRoll);
            motionSeat.WriteVelocity(localVelocityX, localVelocityY, localVelocityZ);

            // 8) Send motion only if active and authorized
            if (motionSeat.SeatInfo.Status == SeatStatus.PLAYING && MotionFlag)
            {
                motionSeat.SendMotionTargets();
            }
        }


        /// <summary>
        /// Update the UI if it is visible.
        /// </summary>
        /// 
        private ExtraData extrasCache = null;

        private void Update_UI()
        {
            // Update des caches d'état pour éviter les redessins inutiles
            if (MotionSeatStatus_cache != Status)
            {
                MotionSeatStatus_cache = Status;
                UpdateStartStopButtonGraphics(StatusToSSButton(Status));
            }
            if (MotionSeatReactivity_cache != Reactivity)
            {
                MotionSeatReactivity_cache = Reactivity;
                UpdateResponseButtonGraphics(Reactivity);
            }
            if (MotionSeatAllowMotion_cache != MotionFlag)
            {
                MotionSeatAllowMotion_cache = MotionFlag;
                UpdateMotionLockButtonGraphics(!MotionFlag);
            }

            // Affichage des données dynamiques du siège
            Vector3 acceleration = seatInfoCopy.TrainAccelerationTargets;

            if (DetailCheckbox.Checked)
            {
                //MotionSeatStatusLabel.Text = $"{seatInfoCopy.Status} , < {(SessionIndex < 0 ? "Local Seat" : $"Remote Seat: {SessionIndex}")} > , Error code : {seatInfoCopy.ErrorCode}  -  {(tcpListener?.Client?.Connected ?? false ? "LISTENER CONNECTED" : "LISTENER NOT CONNECTED")} - {(GetTCPClientAtIndex()?.Client?.Connected ?? false ? "CLIENT CONNECTED" : "CLIENT NOT CONNECTED")}";
            }
            else
            {
                //MotionSeatStatusLabel.Text = $"{seatInfoCopy.Status} , < {(SessionIndex < 0 ? "Local Seat" : $"Remote Seat: {SessionIndex}")} >";
            }

            MotionSeatData.Text =
                $"AccX : {Math.Round(acceleration.x, 3)} m/s²\n" +
                $"AccY : {Math.Round(acceleration.y, 3)} m/s²\n" +
                $"AccZ : {Math.Round(acceleration.z, 3)} m/s²\n" +
                $"Rail Pitch : {seatInfoCopy.RailWayPitchRoll.pitch} rad\n" +
                $"Rail Roll : {seatInfoCopy.RailWayPitchRoll.roll} rad\n";

            // ====================== 🎯 AJOUT - Affichage des extras =======================
            if (extrasCache != null)
            {
                OverallLabel.Text = $"Overall State : {extrasCache.OverallState}";
                StreamModeLabel.Text = $"Stream Mode : {extrasCache.StreamMode}";
                isCommunicatedLabel.Text = $"Communication : {extrasCache.OverallState}";
                isDBoxConnected.Text = $"DBox Connected : {extrasCache.DBoxConnected}";

                WeightsLabel.Text = extrasCache.Weights.ToString();

                // Format output (ex: "Axle 1: 123.4 kg | Axle 2: 125.0 kg | Axle 3: 121.2 kg")
                string formattedWeights = extrasCache.Weights.Count >= 3
                    ? $"Axle 1: {extrasCache.Weights[0]:F1} kg | Axle 2: {extrasCache.Weights[1]:F1} kg | Axle 3: {extrasCache.Weights[2]:F1} kg"
                    : "Weight data unavailable";

                // Display in label
                // WeightsLabel.Text = formattedWeights;

                // ==== NEW: Occupancy detection and conditional data send ====

                // If we have at least one weight measurement
                if (extrasCache.Weights != null && extrasCache.Weights.Count > 0)
                {
                    // 1) Sum all axle weights
                    float totalWeight = extrasCache.Weights.Sum();

                    // 2) Normalize by dividing by 10
                    float normalizedWeight = totalWeight / 10f;

                    // 3) Determine occupancy: threshold at 150
                    bool isOccupied = normalizedWeight > 150f;

                    // 4) Update UI
                    WeightsLabel.Text = isOccupied
                        ? "Seat occupied"
                        : "Seat empty";

                }
                else
                {
                    // No weight data available: use default value (e.g. zero) and do not lock the seat
                    WeightsLabel.Text = "No weight data";
                }
            }
            else
            {
                OverallLabel.Text = "Overall: Error";
                OverallLabel.Text = "Overall: Error";
                OverallLabel.Text = "Overall: Error";
            }
        }



    }
}
