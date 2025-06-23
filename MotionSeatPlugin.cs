/*

  -----------------------------------------------------------------------------
  ⚠️  WARNING – DO NOT MODIFY THIS FILE UNLESS YOU KNOW EXACTLY WHAT YOU'RE DOING
  -----------------------------------------------------------------------------

===============================================================================
  SELENA MOTION PLUGIN – MotionSeatPlugin.cs
===============================================================================

  This class represents the integration layer between the D-BOX motion seat 
  and the SelenaMotionBridge system. It acts as the entry point and plugin 
  handler for DBOX-based motion feedback within the simulation environment.

  The plugin controls the initialization, simulation lifecycle, and dynamic 
  updates of the seat based on data streamed from Selena. It also manages 
  the UI form (`MotionSeatControl`) and synchronizes user feedback and events.

  -----------------------------------------------------------------------------
  FUNCTIONAL OVERVIEW:
  -----------------------------------------------------------------------------

  - Initializes and exposes the MotionSeat for local simulation control
  - Displays and manages the MotionSeatControl user interface
  - Listens to simulation lifecycle events (start, stop, shutdown)
  - Sends acceleration, pitch, roll, and environmental feedback to the seat
  - Handles emergency stop and remote status fallback
  - Returns plugin health and readiness status to SelenaMotionBridge

  The class implements the `IMotionPlugin` interface and must remain 
  compatible with the lifecycle methods defined by the bridge.

  -----------------------------------------------------------------------------
  TECHNICAL DEPENDENCIES:
  -----------------------------------------------------------------------------

  - Requires the MotionSeat SDK integration via:
      --> DBox.MotionSeat
  - UI form:
      --> MotionSeatControl.cs
  - Used in conjunction with:
      --> RailCommon.MotionPluginDefinition (interface definition)
      --> MotionBridge execution context

  WARNING:
  - Do NOT call methods from `motionSeat` directly, always go through `controlForm`
  - All command dispatching and status updates must go through this plugin

  -----------------------------------------------------------------------------
  AUTHOR / MAINTAINER:
  -----------------------------------------------------------------------------

  - Developer: DAVAIL Nicolas (ALSTOM), MARCAL Thomas (ALSTOM), ALBE Alexis (DBOX)
  - Last updated: 22/04/2025
  - Project: Alstom T&S - DBOX Motion Seat Integration Plugin

===============================================================================
*/

using RailCommon.MotionPluginDefinition;
using System;
using DBox.MotionSeat;
using System.Windows.Forms;

namespace Alstom.MotionSeatPlugin
{
    /// <summary>
    /// A class representing the MotionSeat Plugin, instantiated by the MotionBridge.
    /// Manages the motion seat lifecycle, user interface, and simulation feedback.
    /// </summary>
    public class MotionSeatPlugin : IMotionPlugin
    {
        // ============================================================================
        // =                               Fields                                     =
        // ============================================================================

        #region Fields

        /// <summary>
        /// The motion seat object controlled locally by this plugin.
        /// </summary>
        private MotionSeat motionSeat;

        /// <summary>
        /// The associated UI form to monitor and control the seat.
        /// </summary>
        private MotionSeatControl controlForm;

        /// <summary>
        /// Indicates whether a simulation is currently running.
        /// </summary>
        private static bool isPlaying;

        /// <summary>
        /// A constant scaling factor used to reduce the amplitude of real-world train dynamics 
        /// (acceleration, pitch, roll) before applying them to the motion seat.
        /// </summary>
        /// <remarks>
        /// This factor improves comfort by dampening extreme physical motions, 
        /// and ensures the simulation stays within the mechanical limits of the seat.
        /// <para>
        /// Typical real-life values for train acceleration and pitch may exceed what 
        /// the motion platform can physically reproduce. Applying this constant ensures:
        /// </para>
        /// <list type="bullet">
        ///   <item><description>Smoother transitions and realistic movement curves</description></item>
        ///   <item><description>Less mechanical stress and noise</description></item>
        ///   <item><description>Better experience for the user over long sessions</description></item>
        /// </list>
        /// You can tweak this value depending on the realism vs comfort tradeoff you're targeting.
        /// </remarks>
        private const double MotionScalingFactor = 1.5;

        #endregion

        // ============================================================================
        // =                             Constructor                                  =
        // ============================================================================

        #region Constructor

        /// <summary>
        /// Parameterless constructor called by the MotionBridge at runtime.
        /// Instantiates the motion seat and UI controller. Logs initialization status.
        /// </summary>
        public MotionSeatPlugin()
        {
            try
            {
                Console.WriteLine("[PLUGIN] Initializing MotionSeatPlugin...");

                // Initialize the core motion seat logic
                motionSeat = new MotionSeat();
                Console.WriteLine("[PLUGIN] MotionSeat instance created.");

                // Initialize the control UI form (injecting the motionSeat instance)
                controlForm = new MotionSeatControl(motionSeat);
                Console.WriteLine("[PLUGIN] Control form initialized.");

                // Default playback state
                isPlaying = false;
                Console.WriteLine("[PLUGIN] Playback status set to false.");

                Console.WriteLine("[PLUGIN] MotionSeatPlugin initialized successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PLUGIN] ERROR during plugin construction: {ex.Message}");
                throw;
            }
        }

        #endregion


        // ============================================================================
        // =                 Plugin Methods with SelenaMotionBridge                   =
        // ============================================================================

        #region Plugin Methods with SelenaMotionBridge   

        /// <summary>
        /// Called by SelenaMotionBridge when the plugin is initialized.
        /// Displays the UI without starting the motion seat.
        /// </summary>
        public void Initialise()
        {
            try
            {
                Console.WriteLine("[MOTIONBRIDGE] Initialise requested.");

                controlForm.DetailCheckbox.Checked = false;
                Console.WriteLine("[PLUGIN] Detail checkbox set to false.");

                controlForm.Show_UI();
                Console.WriteLine("[PLUGIN] UI displayed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PLUGIN ERROR] Failed to initialize UI: {ex.Message}");
            }
        }


        /// <summary>
        /// Called by the SelenaMotionBridge when the program is about to close (the bridge will hang a little for you to perform de-initialisation of the Motion System correctly).
        /// Cleans up resources and stops the seat.
        /// </summary>
        public async void Finalise()
        {
            Console.WriteLine("[MOTIONBRIDGE] Finalise requested. Stopping motion seat and cleaning up resources...");

            try
            {
                await controlForm.StopSeatAtIndex();
                Console.WriteLine("[PLUGIN] Seat stopped successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PLUGIN ERROR] Failed to stop seat during finalisation: {ex.Message}");
            }

            try
            {
                await controlForm.DisposeResources();
                Console.WriteLine("[PLUGIN] Resources disposed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PLUGIN ERROR] Failed to dispose resources: {ex.Message}");
            }

            Console.WriteLine("[PLUGIN] Finalisation complete. Plugin can now be safely closed.");
        }


        /// <summary>
        /// Called by the SelenaMotionBridge when the simulation starts.
        /// Updates internal playing state and toggles the simulation indicator.
        /// Does not start the motion seat directly.
        /// </summary>
        public void StartSimulation()
        {
            Console.WriteLine("[MOTIONBRIDGE] StartSimulation() called.");

            try
            {
                isPlaying = true;
                controlForm?.ToggleSimulationLight(isPlaying);
                Console.WriteLine("[PLUGIN] Simulation light turned ON.");
                Console.WriteLine("[MOTIONBRIDGE] The simulation has started.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PLUGIN ERROR] Failed to toggle simulation state: {ex.Message}");
            }
        }


        /// <summary>
        /// Called by the SelenaMotionBridge when the simulation stops.
        /// This method zeros the motion seat position (if applicable) and updates the simulation state indicator.
        /// </summary>
        public void StopSimulation()
        {
            Console.WriteLine("[MOTIONBRIDGE] Simulation stopped. Attempting to secure seat position...");

            try
            {
                // Attempt to reset the seat position if the control form is valid
                controlForm?.SecureZeroSeatAtIndex();

                isPlaying = false;
                controlForm?.ToggleSimulationLight(isPlaying);
                Console.WriteLine("[PLUGIN] Simulation light turned off.");
                Console.WriteLine("[PLUGIN] Seat secured.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to stop simulation properly: {ex.Message}");
            }
        }


        /// <summary>
        /// Called regularly by the SelenaMotionBridge to retrieve the current status of the plugin.
        /// This is used for monitoring purposes (e.g., displaying readiness, error states, etc.).
        /// </summary>
        /// <returns>
        /// A <see cref="MotionPluginStatus"/> object indicating the plugin's current state
        /// and any associated error code.
        /// </returns>
        public MotionPluginStatus GetStatus()
        {
            try
            {
                // If we are not on the local seat (-1), return a simplified status with fixed code.

                if (controlForm?.SessionIndex != -1 || motionSeat?.SeatInfo?.Status == SeatStatus.UNKNOWN)
                {
                    //Console.WriteLine("[STATUS] Plugin is running as remote controller. Reporting basic status.");
                    return new MotionPluginStatus(
                        isPlaying ? MotionPluginStatus.Status.Playing : MotionPluginStatus.Status.ReadyToPlay,
                        2 // Custom code for remote status
                    );
                }

                // Determine seat status from the local motion seat.
                var seatStatus = motionSeat?.SeatInfo?.Status ?? SeatStatus.UNKNOWN;
                var errorCode = motionSeat?.SeatInfo?.ErrorCode ?? 100;

                switch (seatStatus)
                {
                    case SeatStatus.UNKNOWN:
                        return new MotionPluginStatus(MotionPluginStatus.Status.Stopped, errorCode);

                    case SeatStatus.STOPPED:
                        return new MotionPluginStatus(MotionPluginStatus.Status.Stopped, errorCode);

                    case SeatStatus.INITIALISING:
                        return new MotionPluginStatus(MotionPluginStatus.Status.Initialising, errorCode);

                    case SeatStatus.PLAYING:
                        return new MotionPluginStatus(
                            isPlaying ? MotionPluginStatus.Status.Playing : MotionPluginStatus.Status.ReadyToPlay,
                            errorCode
                        );

                    case SeatStatus.SHUTDOWN:
                        return new MotionPluginStatus(MotionPluginStatus.Status.Finalising, errorCode);

                    case SeatStatus.ERROR:
                        goto default;

                    default:
                        // Fallback case: error state not covered explicitly
                        return new MotionPluginStatus(MotionPluginStatus.Status.Error, 101); // 101+ triggers emergency pause in Selena
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetStatus failed: {ex.Message}");
                return new MotionPluginStatus(MotionPluginStatus.Status.Error, 999); // 999: unexpected failure
            }
        }


        /// <summary>
        /// Maps a normalized input value (between 0 and 1) through a logistic sigmoid function
        /// to an intensity within the specified range.
        /// </summary>
        /// <param name="xNormalized">
        /// The input value normalized in the [0,1] range.
        /// </param>
        /// <param name="minIntensity">
        /// The minimum intensity output (corresponding to xNormalized = 0).
        /// </param>
        /// <param name="maxIntensity">
        /// The maximum intensity output (corresponding to xNormalized = 1).
        /// </param>
        /// <param name="k">
        /// The steepness factor of the sigmoid curve. Higher values produce a sharper transition.
        /// </param>
        /// <param name="x0">
        /// The midpoint of the sigmoid (where the output is approximately halfway between minIntensity and maxIntensity).
        /// Should also be within [0,1].
        /// </param>
        /// <returns>
        /// A float intensity value between minIntensity and maxIntensity.
        /// </returns>
        private float MapIntensitySigmoid(
            float xNormalized,
            float minIntensity = 0.7f,
            float maxIntensity = 2f,
            float k = 10f,
            float x0 = 0.5f
        )
        {
            // Logistic sigmoid function: S(x) = 1 / (1 + exp(-k * (x - x0)))
            float sigmoid = (float)(1f / (1f + Math.Exp(-k * (xNormalized - x0))));
            // Scale the sigmoid output to [minIntensity, maxIntensity]
            return minIntensity + (maxIntensity - minIntensity) * sigmoid;
        }

        /// <summary>
        /// Calculates the collision intensity (for both bump and shake) based on the provided speed.
        /// Uses a logistic sigmoid mapping to translate speed into an intensity value.
        /// </summary>
        /// <param name="speed">
        /// The current speed value (e.g., in m/s) to map to an intensity.
        /// </param>
        /// <param name="minSpeed">
        /// The minimum speed for which intensity mapping begins (any lower speed yields minIntensity).
        /// </param>
        /// <param name="maxSpeed">
        /// The maximum speed for which intensity mapping caps (any higher speed yields maxIntensity).
        /// </param>
        /// <param name="minIntensity">
        /// The minimum intensity output (when speed <= minSpeed).
        /// </param>
        /// <param name="maxIntensity">
        /// The maximum intensity output (when speed >= maxSpeed).
        /// </param>
        /// <param name="k">
        /// The steepness factor of the sigmoid curve.
        /// </param>
        /// <param name="midSpeed">
        /// The speed at which intensity is halfway between minIntensity and maxIntensity.
        /// </param>
        /// <returns>
        /// A float intensity value to use for both bump and shake, in [minIntensity, maxIntensity].
        /// </returns>
        private float CalculateIntensity(
            float speed,
            float minSpeed = 0f,
            float maxSpeed = 80f,
            float minIntensity = 0.7f,
            float maxIntensity = 2f,
            float k = 10f,
            float midSpeed = 20f
        )
        {
            // Normalize speed to [0,1]
            float x = (float)Utility.Clamp(0f, (speed - minSpeed) / (maxSpeed - minSpeed), 1f);
            // Translate the midpoint speed to normalized domain
            float x0 = (float)Utility.Clamp(0f, (midSpeed - minSpeed) / (maxSpeed - minSpeed), 1f);
            // Use the generic sigmoid mapper
            return MapIntensitySigmoid(x, minIntensity, maxIntensity, k, x0);
        }




        private bool _wasTrainAccident = false;
        private DateTime _lastDebugLogTime = DateTime.MinValue;
        private bool _accidentSequenceRunning = false;
        private DateTime _lastAccidentTime = DateTime.MinValue;
        public bool MotionFlag => controlForm?.MotionFlag ?? false;

        /// <summary>
        /// Called regularly by the SelenaMotionBridge to update the plugin with new dynamic data from the simulation.
        /// Sends motion commands and events to the seat only if the simulation is running and active.
        /// All continuous events are disabled during an accident.
        /// </summary>
        /// <param name="data">The current train dynamics and simulation state.</param>
        public void Update(MotionPluginInputData data)
        {
            if (controlForm == null || motionSeat == null)
            {
                Console.WriteLine("[WARN] Update skipped: controlForm or motionSeat is null.");
                return;
            }

            try
            {

                if (!isPlaying || !data.IsActive || !MotionFlag || motionSeat.SeatInfo.Status != SeatStatus.PLAYING || controlForm.SessionIndex != -1)
                    return;


                //controlForm.DoLocalEvent(DBoxEventMask.SWITCH | controlForm.GetLocalContinuousEvents(), false);

                double speedKmph = data.Speed_mps * 3.6;

                // 4) EFFET ENVIRONNEMENTAL CONTINU  
                //    – léger vrombissement moteur / ballast dès que > 5 km/h
                bool hasMotion = speedKmph >= 3f;
                controlForm.DoLocalEvent(DBoxEventMask.ENV | controlForm.GetLocalContinuousEvents(), hasMotion);

                // 5) EFFET PATINAGE (skid)  
                //    – freinage brusque (longitudinal < –1.5 m/s²)
                bool skidOn = data.Longitudinal_Acceleration_mps2 < -4f;
                controlForm.DoLocalEvent(DBoxEventMask.SKID | controlForm.GetLocalContinuousEvents(), skidOn);

                // 6) EFFET VIBRATION RYTHMIQUE (bogie continuous)  
                //    – si vitesse stable > 30 km/h et pas de virage serré (pitch/roll faibles)
                bool straightHighSpeed = speedKmph > 20f
                                       && Math.Abs(data.Pitch_rad) < 0.02f
                                       && Math.Abs(data.Roll_rad) < 0.02f;
                controlForm.DoLocalEvent(DBoxEventMask.CONTINUOUS | controlForm.GetLocalContinuousEvents(), straightHighSpeed);

                // HIT
                // --- Bump intensity calculation ---
                float bumpHit = CalculateIntensity(
                    (float)speedKmph,
                    minSpeed: 0f, maxSpeed: 120f,
                    minIntensity: 2f, maxIntensity: 3f,
                    k: 12f,
                    midSpeed: 20f
                );

                if (Math.Abs(data.FrontBogieLastSwitchCrossingServerTime_s - data.ServerTime_s) < 0.02f)
                    controlForm.DoLocalEvent(DBoxEventMask.HIT | controlForm.GetLocalContinuousEvents(), 1, bumpHit);
                    //controlForm.DoLocalEvent(DBoxEventMask.SWITCH | controlForm.GetLocalContinuousEvents(), true);

                if (Math.Abs(data.BackBogieLastSwitchCrossingServerTime_s - data.ServerTime_s) < 0.02f)
                    controlForm.DoLocalEvent(DBoxEventMask.HIT | controlForm.GetLocalContinuousEvents(), 0, bumpHit);

                if (Math.Abs(data.FrontBogieLastDetonatorCrossingServerTime_s - data.ServerTime_s) < 0.02f)
                {
                    controlForm.DoLocalEvent(DBoxEventMask.HIT | controlForm.GetLocalContinuousEvents(), 0, bumpHit);
                    controlForm.DoLocalEvent(DBoxEventMask.HIT | controlForm.GetLocalContinuousEvents(), 1, bumpHit);
                }



                // --- Bump intensity calculation ---
                float bump = CalculateIntensity(
                    (float)speedKmph,
                    minSpeed: 0f, maxSpeed: 80f,
                    minIntensity: 1.5f, maxIntensity: 3f,
                    k: 12f,
                    midSpeed: 20f
                );

                // --- Train accident detection ---
                bool isAccidentNow = data.TrainAccident;
                bool risingEdge = isAccidentNow && !_wasTrainAccident;

                if (isAccidentNow)
                {
                    // Calcul du pitch/roll normalisés
                    const float angleThreshold = 0.02f; // ~1.1°

                    bool isLeaning = Math.Abs(data.Pitch_rad) > angleThreshold
                                   || Math.Abs(data.Roll_rad) > angleThreshold;

                    // Si penché, on renvoie seulement le pitch/roll, sinon tout à zéro
                    float sendAccX = 0f;
                    float sendAccY = 0f;
                    float sendAccZ = 0f;
                    float sendPitch = isLeaning ? (float)data.Pitch_rad / (float)MotionScalingFactor : 0f;
                    float sendRoll = isLeaning ? (float)data.Roll_rad / (float)MotionScalingFactor : 0f;

                    // Envoi des targets
                    controlForm.SetLocalMotionTargets(sendAccX, sendAccY, sendAccZ, sendPitch, sendRoll);
                    controlForm.SetLocalTrainVelocity(0, 0, 0);

                    // On coupe tous les événements continus
                    controlForm.DoLocalEvent(DBoxEventMask.NONE, false);

                    // Séquence d’impact si nouvel accident
                    if (risingEdge)
                    {
                        _lastAccidentTime = DateTime.Now;
                        _accidentSequenceRunning = true;
                        _accidentStep = 0;
                    }
                    if (_accidentSequenceRunning
                        && (DateTime.Now - _lastAccidentTime).TotalMilliseconds >= 500)
                    {
                        float[] seq = new float[] { bump, bump * 0.7f, bump * 0.5f, bump * 0.3f };
                        if (_accidentStep < seq.Length)
                        {
                            controlForm.DoLocalEvent(
                                DBoxEventMask.IMPACT,
                                seq[_accidentStep],
                                seq[_accidentStep]
                            );
                            _lastAccidentTime = DateTime.Now;
                            _accidentStep++;
                        }
                        else
                        {
                            _accidentSequenceRunning = false;
                        }
                    }
                }
                else
                {
                    controlForm.SetLocalMotionTargets(
                        -data.Lateral_Acceleration_mps2 / MotionScalingFactor,
                        data.Longitudinal_Acceleration_mps2 / MotionScalingFactor,
                        data.Vertical_Acceleration_mps2,
                        data.Pitch_rad / MotionScalingFactor,
                        data.Roll_rad / MotionScalingFactor
                    );
                }

                controlForm.SpeedLabel.Text = $"Speed (km/h) : {speedKmph:0.00}";
                controlForm.OverallLabel.Text = isAccidentNow ? "Accident: TRUE" : "Accident: FALSE";
                _wasTrainAccident = isAccidentNow;

                if ((DateTime.Now - _lastDebugLogTime).TotalSeconds >= 10)
                {
                    _lastDebugLogTime = DateTime.Now;
                    controlForm.LogToDebug($@"[{DateTime.Now:HH:mm:ss.fff}]
                -- Selena Motion Data --
                Speed:               {data.Speed_mps:0.00} m/s
                Lateral Accel:       {data.Lateral_Acceleration_mps2:0.000} m/s²
                Longitudinal Accel:  {data.Longitudinal_Acceleration_mps2:0.000} m/s²
                Vertical Accel:      {data.Vertical_Acceleration_mps2:0.000} m/s²
                Pitch:               {data.Pitch_rad:0.000} rad
                Roll:                {data.Roll_rad:0.000} rad
                TrainAccident:       {data.TrainAccident}
                IsActive:            {data.IsActive}
                ServerTime:          {data.ServerTime_s:0.000} s
                FrontBogie @ Switch: {data.FrontBogieLastSwitchCrossingServerTime_s:0.000} s
                RearBogie @ Switch:  {data.BackBogieLastSwitchCrossingServerTime_s:0.000} s
                FrontBogie @ Det.:   {data.FrontBogieLastDetonatorCrossingServerTime_s:0.000} s
                -------------------------");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Update failed: {ex.Message}");
            }
        }

        // champs à ajouter dans ta classe :
        private int _accidentStep = 0;

        /// <summary>
        /// Called by the SelenaMotionBridge.
        /// Triggered when MotionBridge requests an emergency stop.
        /// If the seat is handled locally (SessionIndex == -1), the seat is immediately stopped.
        /// Otherwise, the stop is denied to prevent remote plugins from stopping local seats.
        /// </summary>
        public async void EmergencyStop()
        {
            Console.WriteLine("[MOTIONBRIDGE] Emergency Stop has been requested.");

            try
            {
                if (controlForm?.SessionIndex == -1)
                {
                    await controlForm.StopSeatAtIndex();
                    Console.WriteLine("[PLUGIN] Emergency stop completed. Seat should now be idle.");
                }
                else
                {
                    Console.WriteLine("[PLUGIN] Emergency stop request denied: current plugin does not manage the seat locally.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Emergency stop failed: {ex.Message}");
            }
        }


        /// <summary>
        /// Called by SelenaMotionBridge when the user clicks on the plugin's name.
        /// Toggles the visibility of the UI form (show if hidden, hide if visible).
        /// </summary>
        public void ShowHideWindow()
        {
            Console.WriteLine("[MOTIONBRIDGE] Show/Hide UI requested.");

            try
            {
                if (controlForm?.Visible == true)
                {
                    controlForm.Hide_UI();
                    Console.WriteLine("[PLUGIN] UI has been hidden.");
                }
                else
                {
                    controlForm?.Show_UI();
                    Console.WriteLine("[PLUGIN] UI is now visible.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to toggle UI visibility: {ex.Message}");
            }
        }

        #endregion

    }
}
