/*

  -----------------------------------------------------------------------------
  ⚠️  WARNING – DO NOT MODIFY THIS FILE UNLESS YOU KNOW WHAT YOU'RE DOING
  -----------------------------------------------------------------------------


===============================================================================
  DBOX SDK WRAPPER FOR MOTION SEAT CONTROL - DboxSdkWrapper.cs
===============================================================================

  This file defines a low-level interop layer between the managed C# code 
  and the native DBOX SDK DLL (DboxSdkWrapper64.dll). It allows the application 
  to control and configure a motion seat in real-time through direct native calls.

  -----------------------------------------------------------------------------
  FUNCTIONAL OVERVIEW:
  -----------------------------------------------------------------------------

  1. INITIALIZATION AND CONTROL FLOW
     - Initialize, open, start and stop the motion seat system
     - Close and terminate when shutting down the application
     - Check status using IsInitialized / IsOpened / IsStarted

  2. REAL-TIME MOTION FEEDBACK (PostFrameUpdate)
     - Send physical values such as acceleration, roll, pitch and velocity

  3. CONFIGURATION
     - PostSimConfig: defines static geometry and motion limits
     - PostSimUpdate: updates motion intensity and reactivity at runtime

  4. SPECIAL EFFECTS / EVENTS
     - Send effects to simulate physical sensations (e.g. rail switch, collision)
     - Each effect has its own structure and intensity parameters

  -----------------------------------------------------------------------------
  TECHNICAL NOTES:
  -----------------------------------------------------------------------------

  - All structures are declared using [StructLayout(LayoutKind.Sequential)] 
    to match the native memory layout expected by the SDK.
  
  - Each function uses [DllImport] to call the corresponding unmanaged method 
    from DboxSdkWrapper64.dll with Cdecl calling convention.

  - All values must remain within the valid ranges defined by the DBOX SDK:
      e.g. MaxHeave ∈ [0–34.5], Intensity ∈ [0–10], Reactivity ∈ [0–4], etc.

  - This wrapper depends strictly on the memory layout and exported symbols
    of the `DboxSdkWrapper64.dll` provided by D-BOX.

  - Any change in struct order, types, or method signatures may break
    compatibility and cause runtime crashes or undefined behavior.

  - These DLLs are typically version-validated and must not be altered
    unless updated by D-BOX officially.

  -----------------------------------------------------------------------------
  AUTHOR / MAINTAINER:
  -----------------------------------------------------------------------------

  - Developer: DAVAIL Nicolas (ALSTOM), MARCAL Thomas (ALSTOM), ALBE Alexis (DBOX)
  - Last updated: 24/04/2025
  - Project: Alstom T&S - DBOX Motion Seat Controller (C#)

===============================================================================
*/

using System.IO;
using System;
using System.Runtime.InteropServices;

namespace DBox.Internal
{

    /// <summary>
    /// Provides native bindings to the DBOX SDK (DboxSdkWrapper64.dll).
    /// </summary>
    internal static class DboxSdkWrapper
    {
        #region DboxSdkWrapper

        // ============================================================================
        // =                                Load DLL                                  =
        // ============================================================================

        #region Load DLL

        private const string DllName = "DboxSdkWrapper64.dll";

        /// <summary>
        /// Check if the DBOX SDK DLL is present in the application directory.
        /// </summary>
        public static bool IsDllPresent()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DllName);
            return File.Exists(path);
        }

        #endregion

        // ============================================================================
        // =                         D-BOX Manager Methods                            =
        // ============================================================================

        #region D-BOX Manager Methods 

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int InitializeDbox();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TerminateDbox();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int OpenDbox();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CloseDbox();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int StartDbox();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int StopDbox();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ResetState();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool IsInitialized();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool IsOpened();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool IsStarted();

        #endregion

        // ============================================================================
        // =                D-BOX Manager Methods - DYNAMIC UPDATES                   =
        // ============================================================================

        #region D-BOX Manager Methods - DYNAMIC UPDATES

        // FRAME_UPDATES - AT RUNTIME
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PostFrameUpdate([MarshalAs(UnmanagedType.Struct)] DboxStructs.FrameUpdate oFrameUpdate);

        // CONFIGURATION_UPDATES - AT RUNTIME
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PostSimUpdate([MarshalAs(UnmanagedType.Struct)] DboxStructs.SimUpdate oSimUpdate);

        #endregion

        // ============================================================================
        // =                D-BOX Manager Methods - STATIC CONFIGURATION              =
        // ============================================================================

        #region D-BOX Manager Methods - STATIC CONFIGURATION

        //SIM_CONFIG - WHEN INIT
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PostSimConfig([MarshalAs(UnmanagedType.Struct)] DboxStructs.SimConfig oSimConfig);

        #endregion

        // ============================================================================
        // =                D-BOX Manager Methods - EVENT TRIGGERS                    =
        // ============================================================================

        #region D-BOX Manager Methods - EVENT TRIGGERS

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PostActionSwitch([MarshalAs(UnmanagedType.Struct)] DboxStructs.SwitchEvent oSwitchEvent);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PostActionEventBogieHit([MarshalAs(UnmanagedType.Struct)] DboxStructs.EventBogieHit oEventBogieHit);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PostActionEventBogieContinuous([MarshalAs(UnmanagedType.Struct)] DboxStructs.EventBogieContinuous oEventBogieContinuous);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PostActionEventSkid([MarshalAs(UnmanagedType.Struct)] DboxStructs.EventSkid oEventSkid);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PostActionEventImpact([MarshalAs(UnmanagedType.Struct)] DboxStructs.EventImpact oEventImpact);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PostActionEventEnvironmentalContinuous([MarshalAs(UnmanagedType.Struct)] DboxStructs.EventEnvironmentalContinuous oEventEnvironmentalContinuous);

        #endregion

        #endregion
    }

    /// <summary>
    /// Contains all struct definitions used for communicating with the DBOX SDK.
    /// DBOX FRAME_UPDATE, CONFIG_UPDATE, and ACTION_WITH_DATA Structures definitions 
    /// </summary>
    internal static class DboxStructs
    {
        #region DboxStructs

        // ============================================================================
        // =                             BASIC STRUCTURES                             =
        // ============================================================================

        #region BASIC STRUCTURES

        [StructLayout(LayoutKind.Sequential)]
        public struct XyzFloat
        {
            public float X;
            public float Y;
            public float Z;
        }

        #endregion

        // ============================================================================
        // =                            MOTION FRAME UPDATE                           =
        // ============================================================================

        #region MOTION FRAME UPDATE

        [StructLayout(LayoutKind.Sequential)]
        public struct FrameUpdate
        {
            public XyzFloat Acceleration;    // X = lateral, Y = longitudinal, Z = vertical
            public float Roll;               // Rotation around Y axis
            public float Pitch;              // Rotation around X axis
            public XyzFloat VelocityXYZ;     // Velocity in km/h (range: 0–180)
        }

        #endregion

        // ============================================================================
        // =                           RUNTIME CONFIGURATION                          =
        // ============================================================================

        #region RUNTIME CONFIGURATION

        // SIM_UPDATE Structure
        [StructLayout(LayoutKind.Sequential)]
        public struct SimUpdate
        {
            public XyzFloat accelIntensity;      // Motion amplitude [0–10]
            public XyzFloat accelReactivity;     // Response speed / smoothing => soft/brutal reactivity [0–4]
            public float rollIntensity;          // Final contribution of roll => amount of railway roll used in the final calculation [0–10]
            public float rollReactivity;         // [0–4]
            public float pitchIntensity;         // Final contribution of pitch => amount of railway pitch used in the final calculation [0–10]
            public float pitchReactivity;        // [0–4]
            public float switchIntensity;        // Reaction to sharp events like switches => soft/brutal response to quick event such as railway switch and discontinuities [0–10]
            // ⚠ No switch reactivity because the event is 'non-physical' and there is no continuous update ⚠        
        }

        #endregion

        // ============================================================================
        // =                           STATIC CONFIGURATION                           =
        // ============================================================================

        #region STATIC CONFIGURATION

        [StructLayout(LayoutKind.Sequential)]
        public struct SimConfig
        {
            public float triangleHeight;         // Distance between front/middle back actuators [mm]
            public float triangleWidth;          // Distance between left/right back actuators [mm]
            public float MaxPitchAngle;          // Maximum pitch angle allowed [degrees]
            public float restPositionOffset;     // Neutral vertical offset => default heave considered as neutral position [0–34.5 mm]
            public float maxHeave;               // Maximun vertical movement range authorized (from -maxHeave to +maxHeave , 0 = restPositionOffset) [+/- maxHeave] [0-34.5 mm]
            public int enveloppe;                // Enable motion limits => whether or not to contains heave in the enveloppe (0 = off, 1 = on)
            public float MaxRollAngle;           // Maximum roll angle allowed [degrees]
        }

        #endregion

        // ============================================================================
        // =                              EVENT STRUCTURES                            =
        // ============================================================================

        #region EVENT STRUCTURES

        [StructLayout(LayoutKind.Sequential)]
        public struct SwitchEvent
        {
            public float intensity;              // Global intensity multiplier => intensity of the event, multiplied by the associated SimUpdate.eventIntensity
            public XyzFloat orientation;         // Direction of motion => directional vector
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EventBogieHit
        {
            public float Intensity;              // Intensity of the bump
            public int IsFront;                  // 1 = front bogie, 0 = rear bogie
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EventBogieContinuous
        {
            public int StartStop;                // 1 = start, 0 = stop
            public float IntensityFront;         // Intensity for the front bogie
            public float IntensityBack;          // Intensity for the rear bogie
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EventSkid
        {
            public int StartStop;                // 1 = start, 0 = stop
            public float Amplitude;              // Amplitude of the sinusoid
            public float Frequency;              // Frequency of the sinusoid in Hz
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EventImpact
        {
            public float IntensityShake;         // Horizontal shake component => intensity of the first effect
            public float IntensityBump;          // Vertical bump component => intensity of the second effect
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EventEnvironmentalContinuous
        {
            public int StartStop;                // 1 = start, 0 = stop
            public float RollIntensity;          // Roll effect contribution
            public float PitchIntensity;         // Pitch effect contribution
        }

        #endregion

        #endregion
    }

}