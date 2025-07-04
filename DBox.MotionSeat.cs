/*

  -----------------------------------------------------------------------------
  ⚠️  WARNING – DO NOT MODIFY THIS FILE UNLESS YOU KNOW WHAT YOU'RE DOING
  -----------------------------------------------------------------------------


===============================================================================
  MOTION SEAT CONTROLLER FOR D-BOX SYSTEM - MotionSeat.cs
===============================================================================

  This class provides high-level control and integration of a D-BOX motion seat 
  system into a simulation environment. It wraps the DBOX SDK (via DboxSdkWrapper)
  and handles communication, configuration, motion effects, and monitoring.

  -----------------------------------------------------------------------------
  FUNCTIONAL OVERVIEW:
  -----------------------------------------------------------------------------

  - Initialization and shutdown of the motion seat
  - Real-time control of pitch, roll, velocity, acceleration
  - Runtime configuration of reactivity and intensity
  - Execution of discrete motion effects (bogie hits, skid, impact, etc.)
  - TCP-based monitoring via MonitoringClient

  All motion and event updates are internally synchronized with the D-BOX SDK 
  using the defined structures in DboxStructs. The state of the system is tracked 
  through a SeatInfo object.

  -----------------------------------------------------------------------------
  TECHNICAL DEPENDENCIES:
  -----------------------------------------------------------------------------

  - This class depends directly on the native DBOX SDK DLL:
      --> DboxSdkWrapper64.dll
  - It must only be used with a **validated and matching version** of the DLL
  - Do NOT alter the struct layouts or method call logic unless the SDK 
    documentation explicitly allows it

  This controller assumes strict compatibility with the memory layout and API 
  behavior of the DBOX SDK. Changing the order of parameters, types, or 
  method sequences may result in malfunction, seat failure, or undefined motion.

  Always test with hardware in a controlled environment before deployment.

  -----------------------------------------------------------------------------
  AUTHOR / MAINTAINER:
  -----------------------------------------------------------------------------

  - Developer: DAVAIL Nicolas (ALSTOM), MARCAL Thomas (ALSTOM), ALBE Alexis (DBOX)
  - Last updated: 14/04/2025
  - Project: Alstom T&S - DBOX Motion Seat Controller (C#)

===============================================================================
*/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Management;
using DBox.Internal;
using DBox.TCP;

namespace DBox.MotionSeat
{
    /// <summary>
    /// Provides full control over a DBOX motion seat system: initialization, motion updates, effects, and TCP monitoring.
    /// </summary>
    internal class MotionSeat
    {
        // ============================================================================
        // =                                  Fields                                  =
        // ============================================================================

        #region Fields

        /// <summary>
        /// Allows you to connect, send and receive monitoring info and commands from the dbox system.
        /// </summary>
        /// <remarks>Not ready for now.</remarks>
        internal MonitoringClient TCPMonitoring { get; } = new MonitoringClient();

        /// <summary>
        /// Various information about the seat. Refer to each field's documentation to know what you should or should not do.
        /// Holds runtime information and states of the motion seat.
        /// </summary>
        internal SeatInfo SeatInfo { get; } = new SeatInfo(); // Don't allow to change it to avoid losing data by replacing it with a new one.

        #endregion

        // ============================================================================
        // =                              D-BOX Members                               =
        // ============================================================================

        #region D-BOX Members

        /// <summary>
        /// control the DBox seat motion update behaviour
        /// </summary>
        private DboxStructs.FrameUpdate oFrameUpdate = new DboxStructs.FrameUpdate();

        /// <summary>
        /// static configuration of the simulation, e.g. roll and pitch limits.
        /// </summary>
        private DboxStructs.SimConfig oSimConfig = new DboxStructs.SimConfig();

        /// <summary>
        /// Runtime configuration of seat sensitivity / reactivity / motion intensity
        /// </summary>
        private DboxStructs.SimUpdate oSimUpdate = new DboxStructs.SimUpdate();


        // Events

        #region Events

        /// <summary>
        /// Triggers short bump effect on switch crossings.
        /// </summary>
        private DboxStructs.SwitchEvent oSwitchEvent = new DboxStructs.SwitchEvent();

        /// <summary>
        /// Triggers single bogie bump (front or rear).
        /// </summary>
        private DboxStructs.EventBogieHit oEventBogieHit = new DboxStructs.EventBogieHit();

        /// <summary>
        /// Enables continuous bogie bumps for rail seams.
        /// </summary>
        private DboxStructs.EventBogieContinuous oEventBogieContinuous = new DboxStructs.EventBogieContinuous();

        /// <summary>
        /// Simulates wheel slip effect with vibration.
        /// </summary>
        private DboxStructs.EventSkid oEventSkid = new DboxStructs.EventSkid();

        /// <summary>
        /// Simulates a sudden impact or collision.
        /// </summary>
        private DboxStructs.EventImpact oEventImpact = new DboxStructs.EventImpact();

        /// <summary>
        /// Activates continuous environmental motion feedback.
        /// </summary>
        private DboxStructs.EventEnvironmentalContinuous oEventEnvironmentalContinuous = new DboxStructs.EventEnvironmentalContinuous();

        #endregion

        #endregion

        // ============================================================================
        // =                            Constructor(s)                                =
        // ============================================================================

        #region Constructor(s)

        /// <summary>
        /// Constructs a new MotionSeat instance and initializes internal seat config.
        /// </summary>
        internal MotionSeat()
        {
            InitSeatInfo();    // Initialize seat status and runtime flags
            InitSimConfig();   // Initialize default static simulation parameters
        }


        /// <summary>
        /// Initializes internal seat status and flags (used at startup and shutdown).
        /// </summary>
        private void InitSeatInfo()
        {
            SeatInfo.Status = SeatStatus.UNKNOWN;      // Default unknown state
            SeatInfo.ErrorCode = 0;                    // No error at initialization
            SeatInfo.MotionFlag = false;               // Motion is not active
            SeatInfo.HasbeenDeInit = false;            // Not yet de-initialized
        }


        /// <summary>
        /// Defines the mechanical and geometric limits of the seat platform.
        /// This configuration is uploaded to the seat once at startup.
        /// </summary>
        private void InitSimConfig()
        {
            oSimConfig.enveloppe = 1;                  // Enable motion envelope constraint
            oSimConfig.maxHeave = 34.5f;               // Max vertical movement (mm)
            oSimConfig.MaxPitchAngle = 6f;             // Max pitch angle (degrees) [2.6° for IR, 6° for NGT]
            oSimConfig.MaxRollAngle = 6f;              // Max roll angle (degrees) [2.6° for IR, 6° for NGT]
            oSimConfig.restPositionOffset = 25.5f;     // Default vertical neutral offset
            oSimConfig.triangleHeight = 525;           // Distance between front/rear actuators (mm)
            oSimConfig.triangleWidth = 415;            // Distance between left/right actuators (mm)
        }

        #endregion

        // ============================================================================
        // =         Motion Seat Functions (Communication, Write data, Events)        =
        // ============================================================================

        #region Motion Seat Functions (Communication, Write data, Events)

        #region Communication

        /// <summary>
        /// Initializes the DBOX SDK, opens communication, applies configuration, and starts motion.
        /// </summary>
        /// <param name="debug">If true, skips error code checks (for testing/debug builds).</param>
        /// <returns><c>true</c> if all steps complete successfully, otherwise <c>false</c>.</returns>
        internal async Task<bool> ConnectInit(bool debug = false)
        {
            SeatInfo.Status = SeatStatus.UNKNOWN;

            try
            {
                // Step 1 - Initialize DBOX SDK
                SeatInfo.ErrorCode = DboxSdkWrapper.InitializeDbox();
                Console.WriteLine(SeatInfo.ErrorCode == 0
                    ? "✅ Initialization : OK"
                    : $"⚠ Initialization : ERROR - Fault Code {SeatInfo.ErrorCode}");
                await Task.Delay(1000);
                if (SeatInfo.ErrorCode != 0 && !debug) return false;

                // Step 2 - Open communication with hardware
                SeatInfo.Status = SeatStatus.STOPPED;
                SeatInfo.ErrorCode = DboxSdkWrapper.OpenDbox();
                Console.WriteLine(SeatInfo.ErrorCode == 0
                    ? "✅ DBOX Open : OK"
                    : $"⚠ DBOX Open : ERROR - Fault Code {SeatInfo.ErrorCode}");
                await Task.Delay(1000);
                if (SeatInfo.ErrorCode != 0 && !debug) return false;

                // Step 3 - Apply mechanical config
                SeatInfo.Status = SeatStatus.INITIALISING;
                SeatInfo.ErrorCode = DboxSdkWrapper.PostSimConfig(oSimConfig);
                Console.WriteLine(SeatInfo.ErrorCode == 0
                    ? "✅ DBOX Config : OK"
                    : $"⚠ DBOX Config : ERROR - Fault Code {SeatInfo.ErrorCode}");
                await Task.Delay(200);
                if (SeatInfo.ErrorCode != 0 && !debug) return false;

                // Step 4 - Start motion
                SeatInfo.ErrorCode = DboxSdkWrapper.StartDbox();
                Console.WriteLine(SeatInfo.ErrorCode == 0
                    ? "✅ DBOX Start : OK"
                    : $"⚠ DBOX Start : ERROR - Fault Code {SeatInfo.ErrorCode}");
                await Task.Delay(1000);
                if (SeatInfo.ErrorCode != 0 && !debug) return false;

                SeatInfo.Status = SeatStatus.PLAYING;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConnectInit ERROR] {ex.Message}");
                SeatInfo.Status = SeatStatus.ERROR;
                return false;
            }
        }


        /// <summary>
        /// Stops motion and cleanly disconnects the Motion Seat.
        /// </summary>
        /// <param name="debug">If true, skips error checks (for debug builds).</param>
        /// <returns><c>true</c> if all shutdown steps succeed; otherwise, <c>false</c>.</returns>
        internal async Task<bool> StopDisconnect(bool debug = false)
        {
            try
            {
                // Step 1 - Stop the motion and return to default position
                SeatInfo.ErrorCode = DboxSdkWrapper.StopDbox();
                Console.WriteLine(SeatInfo.ErrorCode == 0
                    ? "✅ DBOX Stop : OK"
                    : $"⚠ DBOX Stop : ERROR - Fault Code {SeatInfo.ErrorCode}");
                await Task.Delay(1000);
                if (SeatInfo.ErrorCode != 0 && !debug) return false;

                // Step 2 - Close hardware communication
                SeatInfo.Status = SeatStatus.SHUTDOWN;
                SeatInfo.ErrorCode = DboxSdkWrapper.CloseDbox();
                Console.WriteLine(SeatInfo.ErrorCode == 0
                    ? "✅ DBOX Close : OK"
                    : $"⚠ DBOX Close : ERROR - Fault Code {SeatInfo.ErrorCode}");
                await Task.Delay(1000);
                if (SeatInfo.ErrorCode != 0 && !debug) return false;

                // Step 3 - Terminate and clean up
                SeatInfo.Status = SeatStatus.STOPPED;
                SeatInfo.ErrorCode = DboxSdkWrapper.TerminateDbox();
                Console.WriteLine(SeatInfo.ErrorCode == 0
                    ? "✅ DBOX Terminate : OK"
                    : $"⚠ DBOX Terminate : ERROR - Fault Code {SeatInfo.ErrorCode}");
                await Task.Delay(1000);
                if (SeatInfo.ErrorCode != 0 && !debug) return false;

                SeatInfo.Status = SeatStatus.UNKNOWN;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StopDisconnect ERROR] {ex.Message}");
                SeatInfo.Status = SeatStatus.ERROR;
                return false;
            }
        }


        /// <summary>
        /// Cleans up unmanaged resources before the application closes.
        /// Ensures the TCP monitoring connection is properly closed.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the cleanup was performed; <c>false</c> if it was already done.
        /// </returns>
        internal async Task<bool> DiposeBeforeClose()
        {
            try
            {
                if (!SeatInfo.HasbeenDeInit)
                {
                    Console.WriteLine("[CLEANUP] Disposing seat resources before shutdown...");
                    TCPMonitoring.Disconnect(); // Safe disconnect even if not previously closed
                    SeatInfo.HasbeenDeInit = true;
                    await Task.Delay(150);
                    Console.WriteLine("[CLEANUP] Seat cleanup completed successfully.");
                    return true;
                }

                Console.WriteLine("[CLEANUP] Resources were already disposed.");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLEANUP ERROR] Failed during DisposeBeforeClose: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Write functions

        /// <summary>
        /// Send the railway pitch to the Motion Seat. Need to call <see cref="SendMotionTargets"/> once all writing is done.
        /// </summary>
        internal void WritePitch(double pitch)
        {
            oFrameUpdate.Pitch = (float)pitch;
            SeatInfo.RailWayPitchRoll.pitch = oFrameUpdate.Pitch;
        }

        /// <summary>
        /// Send the railway roll to the Motion Seat. Need to call <see cref="SendMotionTargets"/> once all writing is done.
        /// </summary>
        internal void WriteRoll(double roll)
        {
            oFrameUpdate.Roll = (float)roll;
            SeatInfo.RailWayPitchRoll.roll = oFrameUpdate.Roll;
        }

        /// <summary>
        /// Send the acceleration to the Motion Seat. Need to call <see cref="SendMotionTargets"/> once all writing is done.
        /// </summary>
        /// <param name="x">right acceleration</param>
        /// <param name="y">forward acceleration</param>
        /// <param name="z">vertical acceleration</param>
        internal void WriteAcceleration(float x, float y, float z)
        {
            oFrameUpdate.Acceleration.X = x;
            oFrameUpdate.Acceleration.Y = y;
            oFrameUpdate.Acceleration.Z = z;

            SeatInfo.TrainAccelerationTargets = new Vector3(oFrameUpdate.Acceleration.X, oFrameUpdate.Acceleration.Y, oFrameUpdate.Acceleration.Z);
        }

        /// <summary>
        /// Send the velocity of the train to the motion seat. Need to call <see cref="SendMotionTargets"/> once all writing is done.
        /// </summary>
        /// <param name="x">right velocity</param>
        /// <param name="y">forward velocity</param>
        /// <param name="z">vertical velocity</param>
        internal void WriteVelocity(float x, float y, float z)
        {
            oFrameUpdate.VelocityXYZ.X = x;
            oFrameUpdate.VelocityXYZ.Y = y;
            oFrameUpdate.VelocityXYZ.Z = z;

            SeatInfo.TrainVelocityTargets = new Vector3(oFrameUpdate.VelocityXYZ.X, oFrameUpdate.VelocityXYZ.Y, oFrameUpdate.VelocityXYZ.Z);
        }

        /// <summary>
        /// Update the DBox Motion Seat's motion targets.
        /// Pushes the current motion frame (pitch, roll, accel, velocity) to the hardware.
        /// Must be called after setting motion vectors.
        /// </summary>
        internal void SendMotionTargets()
        {
            SeatInfo.ErrorCode = DboxSdkWrapper.PostFrameUpdate(oFrameUpdate);
        }

        /// <summary>
        /// Change the reactivity and optionnaly intensity of the Motion Seat.
        /// </summary>
        /// <param name="valueReactivity">Reactivity (or smoothing) of the motion, between 0.0 and 4.0</param>
        /// <param name="valueIntensity">Intensity of the motion, between 0 (no motion) and 10.0 (x10 motion range). You probably do not want to change this parameter.</param>
        internal void WriteSendSensitivitySettings(float valueReactivity, float valueIntensity = 1)
        {
            oSimUpdate.accelReactivity = new DboxStructs.XyzFloat { X = valueReactivity, Y = valueReactivity, Z = valueReactivity };
            oSimUpdate.pitchReactivity = valueReactivity;
            oSimUpdate.rollReactivity = valueReactivity;

            oSimUpdate.accelIntensity = new DboxStructs.XyzFloat { X = valueIntensity, Y = valueIntensity, Z = valueIntensity };
            oSimUpdate.rollIntensity = valueIntensity;
            oSimUpdate.pitchIntensity = valueIntensity;

            oSimUpdate.switchIntensity = valueIntensity;

            SeatInfo.ErrorCode = DboxSdkWrapper.PostSimUpdate(oSimUpdate);

            SeatInfo.Intensity = oSimUpdate.accelIntensity.X;     // Should be the same value everywhere so just use the X one.
            SeatInfo.Reactivity = oSimUpdate.accelReactivity.X;   // Should be the same value everywhere so just use the X one.
        }

        #endregion

        #region Events

        /// <summary>
        /// Do some wobbles to mimic some irregularity/discontinuity on the track when crossing a switch
        /// </summary>
        internal void DoSwitchMotion(bool isSwitch)
        {
            /*
            if (isSwitch)
            {
                oSwitchEvent.orientation.X = 2f;   // Left / Right
                oSwitchEvent.orientation.Y = 0.1f; // Up / Down
                oSwitchEvent.orientation.Z = 0.5f; // Forward / Backward
                oSwitchEvent.intensity = 1f;

                SeatInfo.ErrorCode = DboxSdkWrapper.PostActionSwitch(oSwitchEvent);
            }
            */
        }


        /*
        /// <summary>
        /// A single "tac tac" effect when crossing a switch; call it twice for both axletree (with is front true then false)
        /// </summary>
        internal void DoBogieSingleHit(int isFront=1)
        {
            oEventBogieHit.Intensity = 0.5f;
            oEventBogieHit.IsFront = isFront;
            SeatInfo.ErrorCode = DboxSdkWrapper.PostActionEventBogieHit(oEventBogieHit);
        }
        */


        /// <summary>
        /// A single "tac tac" effect when crossing a switch; call it twice for both axletree (with isFront true then false), with configurable intensity.
        /// </summary>
        /// <param name="isFront">1 for front axle, 0 for back axle</param>
        /// <param name="intensity">Intensity of the hit, typically between 0.0 and 1.0</param>
        internal void DoBogieSingleHit(int isFront = 1, float intensity = 0.5f)
        {
            oEventBogieHit.Intensity = intensity;
            oEventBogieHit.IsFront = isFront;
            SeatInfo.ErrorCode = DboxSdkWrapper.PostActionEventBogieHit(oEventBogieHit);
        }


        /// <summary>
        /// Continuous "tac tac" effect when crossing rail sections.
        /// </summary>
        internal void ActivateBogieContinuousHit(bool activated)
        {
            oEventBogieContinuous.IntensityFront = 2f;
            oEventBogieContinuous.IntensityBack = 2f;
            oEventBogieContinuous.StartStop = activated ? 1 : 0;
            //Console.WriteLine($"TAC TAC {activated}");
            SeatInfo.ErrorCode = DboxSdkWrapper.PostActionEventBogieContinuous(oEventBogieContinuous);
        }


        /// <summary>
        /// Mimic the slipping of the wheels on the rails
        /// </summary>
        internal void ActivateBogieSkid(bool activated)
        {
            oEventSkid.Frequency = Math.Min(50f * SeatInfo.TrainAccelerationTargets.y, 90f); // ⚠ MAX 90Hz
            oEventSkid.Amplitude = 0.002f; //⚠ MAX = 0.01 => Seat will go down a little
            oEventSkid.StartStop = activated ? 1 : 0;
            //Console.WriteLine($"Skid {activated}");
            SeatInfo.ErrorCode = DboxSdkWrapper.PostActionEventSkid(oEventSkid);
        }

        /// <summary>
        /// Mimic a collision with adjustable bump and shake intensities.
        /// </summary>
        /// <param name="intensityBump">Vertical bump intensity (typically between 0.0 and 1.0)</param>
        /// <param name="intensityShake">Lateral shake intensity (typically between 0.0 and 1.0)</param>
        internal void DoTrainImpact(float intensityBump = 2f, float intensityShake = 2f)
        {
            oEventImpact.IntensityBump = intensityBump;
            oEventImpact.IntensityShake = intensityShake;
            SeatInfo.ErrorCode = DboxSdkWrapper.PostActionEventImpact(oEventImpact);
        }


        /// <summary>
        /// Add environmental effects
        /// </summary>
        internal void DoEnvContinuous(bool activated)
        {
            oEventEnvironmentalContinuous.StartStop = activated ? 1 : 0;
            oEventEnvironmentalContinuous.PitchIntensity = 2f;
            oEventEnvironmentalContinuous.RollIntensity = 2f;
            SeatInfo.ErrorCode = DboxSdkWrapper.PostActionEventEnvironmentalContinuous(oEventEnvironmentalContinuous);

            SeatInfo.TrainVelocityTargets = new Vector3(oFrameUpdate.VelocityXYZ.X, oFrameUpdate.VelocityXYZ.Y, oFrameUpdate.VelocityXYZ.Z);
        }


        /// <summary>
        /// Get the continuous event currently activated (Bogie, Skid and / or Environmental)
        /// </summary>
        /// <returns></returns>
        internal DBoxEventMask GetContinuousEvents()
        {
            uint activatedEvents = 0;
            activatedEvents += (uint)oEventBogieContinuous.StartStop;
            activatedEvents += (uint)oEventSkid.StartStop;
            activatedEvents += (uint)oEventEnvironmentalContinuous.StartStop;
            return (DBoxEventMask)activatedEvents;
        }

        #endregion

        #endregion

        // ============================================================================
        // =                               Monitoring                                 =
        // ============================================================================

        #region Monitoring

        /// <summary>
        /// Encapsulates the full monitoring state of the DBOX seat, including core data and XML-extracted extras.
        /// </summary>
        /// <remarks>
        /// Use this object when you need to serialize, send, or process a full representation of seat state and health.
        /// </remarks>
        internal class SeatPayload
        {
            /// <summary>
            /// Main seat runtime information (motion state, error code, pitch/roll, etc.)
            /// </summary>
            public SeatInfo SeatInfo { get; set; }

            /// <summary>
            /// Additional descriptive metadata parsed from the monitoring XML.
            /// </summary>
            public ExtraData Extras { get; set; }
        }

        /// <summary>
        /// Represents auxiliary metadata extracted from the TCP XML monitoring feed.
        /// </summary>
        /// <remarks>
        /// Includes human-readable seat state, streaming mode, and actuator weights.
        /// </remarks>
        internal class ExtraData
        {
            /// <summary>
            /// Description of the seat's overall operating state (e.g. "Playing", "Stopped", "Error").
            /// </summary>
            public string OverallState { get; set; }

            /// <summary>
            /// Description of the current stream mode (e.g. "Simulation", "Idle").
            /// </summary>
            public string StreamMode { get; set; }

            /// <summary>
            /// Current weight distribution or actuator values reported by the seat (usually 3 values).
            /// </summary>
            public List<float> Weights { get; set; }

            public bool DBoxConnected { get; set; }
        }

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
                    //Console.WriteLine("⚠ Not enough weight data available.");
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

        #endregion

    }
}
