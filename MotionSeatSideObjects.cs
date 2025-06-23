/*
  -----------------------------------------------------------------------------
  ⚠️  WARNING – DO NOT MODIFY THIS FILE UNLESS YOU KNOW EXACTLY WHAT YOU'RE DOING
  -----------------------------------------------------------------------------

===============================================================================
  DBOX MOTION SEAT – SeatInfo.cs
===============================================================================

  This file defines core data structures, enums, and helpers used for managing
  and monitoring the state of a DBOX motion seat system. These definitions are 
  used across the MotionSeat control logic and monitoring services.

  It includes:
  - Enums for seat reactivity, status, and event masking
  - Conversion helpers for reactivity logic
  - Vector and pitch/roll representations
  - A data structure representing the full state of the seat (`SeatInfo`)

  -------------------------------------------------------------------------------
  FUNCTIONAL OVERVIEW:
  -------------------------------------------------------------------------------

  - Define reactivity levels and convert between enum/float
  - Track seat state, configuration, and telemetry
  - Support motion targets: acceleration, velocity, pitch, roll
  - Wrap external monitoring feedback from DBOX (via TCP)

  -------------------------------------------------------------------------------
  TECHNICAL DEPENDENCIES:
  -------------------------------------------------------------------------------

  - Newtonsoft.Json (for serializing and logging state externally)
  - Used within DBox.MotionSeat namespace and referenced by control forms

  WARNING:
  - Use provided methods from MotionSeat to write data. Most SeatInfo fields 
    should be considered read-only externally.

  -------------------------------------------------------------------------------
  AUTHOR / MAINTAINER:
  -------------------------------------------------------------------------------

  - Developer: DAVAIL Nicolas (ALSTOM), MARCAL Thomas (ALSTOM), ALBE Alexis (DBOX)
  - Last updated: 23/04/2025
  - Project: Alstom T&S - DBOX Motion Seat Control & Monitoring

===============================================================================
*/

using System;
using Newtonsoft.Json;

namespace DBox.MotionSeat
{

    // ============================================================================
    // =                 Alstom – D-BOX Motion Seat Structures                    =
    // ============================================================================

    #region Alstom – D-BOX Motion Seat Structures

    /// <summary>
    /// Level of reactivity associated with the seat's reactivity value.
    /// </summary>
    internal enum ReactivityLevels
    {
        /// <summary>
        /// Low level, for safety.
        /// </summary>
        SECURE,
        /// <summary>
        /// Normal level of feeling
        /// </summary>
        NORMAL,
        /// <summary>
        /// exagerated motion, for maximum feelings.
        /// </summary>
        HIGH,
        /// <summary>
        /// Sensitivity not found
        /// </summary>
        ERROR
    };

    /// <summary>
    /// Convert the value of the specified <c>ReactivityConverter[<see cref="ReactivityLevels"/>]</c> as <see cref="float"/><br/> 
    /// or <c>ReactivityConverter[<see cref="float"/>]</c> as <see cref="ReactivityLevels"/>.<br/>
    /// Use it like an indexer.
    /// </summary>
    internal struct ReactivityConverter
    {
        private const float SecureReactivity = 0.1f;
        private const float NormalReactivity = 0.5f;
        private const float HighReactivity = 0.8f;
        private const float ErrorReactivity = 0.05f; //cannot be the same as Secure as it is const, it will cause issue in switch statement.

        /// <summary>
        /// Get the associated <see cref="float"/>
        /// </summary>
        internal float this[ReactivityLevels lvl]
        {
            get
            {
                switch (lvl)
                {
                    case ReactivityLevels.SECURE:
                        return SecureReactivity;
                    case ReactivityLevels.NORMAL:
                        return NormalReactivity;
                    case ReactivityLevels.HIGH:
                        return HighReactivity;
                    case ReactivityLevels.ERROR:
                        return ErrorReactivity;
                    default:
                        return ErrorReactivity;
                }
            }
        }

        /// <summary>
        /// Get the associated <see cref="ReactivityLevels"/>.
        /// </summary>
        internal ReactivityLevels this[float lvl]
        {
            get
            {
                switch (lvl)
                {
                    case SecureReactivity:
                        return ReactivityLevels.SECURE;
                    case NormalReactivity:
                        return ReactivityLevels.NORMAL;
                    case HighReactivity:
                        return ReactivityLevels.HIGH;
                    case ErrorReactivity:
                        return ReactivityLevels.ERROR;
                    default:
                        return ReactivityLevels.ERROR;
                }
            }
        }
    }

    /// <summary>
    /// Type of event to send to the dbox system.<br/>
    /// Multiple event can be setup together as a mask
    /// </summary>
    [Flags]
    internal enum DBoxEventMask
    {
        /// <summary>
        /// No event
        /// </summary>
        NONE = 0,
        /// <summary>
        /// You cross a switch at a specified speed.
        /// </summary>
        SWITCH = 1,
        /// <summary>
        /// A single bump due to railway welding.
        /// </summary>
        HIT = 2,
        /// <summary>
        /// You are on a section on which you can feel continuously the welds between rails.
        /// </summary>
        CONTINUOUS = 4,
        /// <summary>
        /// You skid because you lost grip
        /// </summary>
        SKID = 8,
        /// <summary>
        /// You hit something
        /// </summary>
        IMPACT = 16,
        /// <summary>
        /// The rail are not totally flat and there's some continuous movements.
        /// </summary>
        ENV = 32
    }

    /// <summary>
    /// The various status the Motion Seat can take.
    /// </summary>
    internal enum SeatStatus
    {
        /// <summary>
        /// The motion seat is not available (might be off)
        /// </summary>
        UNKNOWN,
        /// <summary>
        /// The motion seat is powered, connected, but not yet initialised. Next step should be <see cref="INITIALISING"/>.
        /// </summary>
        STOPPED,
        /// <summary>
        /// The motion seat is doing its initialisation. Next step should be <see cref="PLAYING"/>.
        /// </summary>
        INITIALISING,
        /// <summary>
        /// The motion seat is ready and will react to motion request
        /// </summary>
        PLAYING,
        /// <summary>
        /// The motion seat is about to stop and disconnect. Next step should be <see cref="UNKNOWN"/>
        /// </summary>
        SHUTDOWN,
        /// <summary>
        /// The motion seat has encountered an error, use the <see cref="MotionSeat.ErrorCode"/> to get additionnal info. Next step after clearing error should be <see cref="STOPPED"/>
        /// </summary>
        ERROR
    }

    /// <summary>
    /// A 3-dimensionnal vector.
    /// </summary>
    internal struct Vector3
    {
        [JsonProperty] internal float x;
        [JsonProperty] internal float y;
        [JsonProperty] internal float z;

        /// <summary>
        /// Create a new <see cref="Vector3"/>
        /// </summary>
        internal Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    /// <summary>
    /// A 2-dimensionnal vector representing pitch and roll
    /// </summary>
    internal struct PitchRoll
    {
        [JsonProperty] internal float roll;
        [JsonProperty] internal float pitch;

        /// <summary>
        /// Create a new <see cref="PitchRoll"/>
        /// </summary>
        internal PitchRoll(float roll, float pitch)
        {
            this.roll = roll;
            this.pitch = pitch;
        }

    }

    #endregion

    // ============================================================================
    // =                          SeatInfo Class                                  =
    // ============================================================================

    #region SeatInfo Class

    /// <summary>
    /// A class containing all info the motion seat should publicly transmit.
    /// </summary>
    internal class SeatInfo
    {

        #region SeatInfo JsonProperty Enum

        /// <summary>
        /// Current status of the motion seat.
        /// </summary>
        /// <remarks> Apart within <see cref="MotionSeat"/>, you should only read this.</remarks>
        [JsonProperty] internal SeatStatus Status;

        /// <summary>
        /// Error code of the motionseat.
        /// </summary>
        /// <remarks> Apart within <see cref="MotionSeat"/>, you should only read this.</remarks>
        [JsonProperty] internal int ErrorCode;

        /// <summary>
        /// <see langword="true"/> if the seat can move, <see langword="false"/> otherwise.<br/>
        /// </summary>
        /// <remarks><b>WARNING:</b> this has to be used by you within the object that control the motion seat, because it is not used internally (the motion seat does not care of this flag)</remarks>
        [JsonProperty] internal bool MotionFlag;

        /// <summary>
        /// The reactivity of movements.
        /// </summary>
        /// <remarks> Apart within <see cref="MotionSeat"/>, you should only read this. To set it, use <see cref="MotionSeat.WriteSendSensitivitySettings(float, float)"/>.</remarks>
        [JsonProperty] internal float Reactivity;
        /// <summary>
        /// the intensity of movements.
        /// </summary>
        /// <remarks> Apart within <see cref="MotionSeat"/>, you should only read this. To set it, use <see cref="MotionSeat.WriteSendSensitivitySettings(float, float)"/>.</remarks>
        [JsonProperty] internal float Intensity;

        /// <summary>
        /// Acceleration the seat is targetting
        /// </summary>
        /// <remarks> Apart within <see cref="MotionSeat"/>, you should only read this. To set it, use <see cref="MotionSeat.WriteAcceleration(float, float, float)"/>.</remarks>
        [JsonProperty] internal Vector3 TrainAccelerationTargets;

        /// <summary>
        /// Pitch and roll of the track
        /// </summary>
        /// <remarks> Apart within <see cref="MotionSeat"/>, you should only read this. To set it, use <see cref="MotionSeat.WritePitch(double)"/> and <see cref="MotionSeat.WriteRoll(double)"/></remarks>
        [JsonProperty] internal PitchRoll RailWayPitchRoll;

        /// <summary>
        /// Speed the train is supposed to go
        /// </summary>
        /// <remarks> Apart within <see cref="MotionSeat"/>, you should only read this. Use events such as <see cref="MotionSeat.DoEnvContinuous(bool)"/> or <see cref="MotionSeat.DoSwitchMotion()"/> to set it.</remarks>
        [JsonProperty] internal Vector3 TrainVelocityTargets;

        /// <summary>
        /// get or set if the seat has been deinit and can't be started anymore without restarting the plugin.
        /// </summary>
        /// <remarks> Apart within <see cref="MotionSeat"/>, you should only read this.</remarks>
        [JsonProperty] internal bool HasbeenDeInit;

        /// <summary>
        /// Return a copy of this <see cref="SeatInfo"/>.
        /// </summary>
        /// <returns></returns>
        internal SeatInfo Clone()
        {
            return (SeatInfo)MemberwiseClone();
        }

        #endregion

    }

    #endregion

}