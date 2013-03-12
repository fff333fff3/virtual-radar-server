﻿// Copyright © 2012 onwards, Andrew Whewell
// All rights reserved.
//
// Redistribution and use of this software in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//    * Neither the name of the author nor the names of the program's contributors may be used to endorse or promote products derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHORS OF THE SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VirtualRadar.Interface.Settings
{
    /// <summary>
    /// The data-transfer object that carries the configuration of the raw message decoder.
    /// </summary>
    [Serializable]
    public class RawDecodingSettings
    {
        /// <summary>
        /// Gets or sets the receiver's location for raw message local CPR decoding.
        /// </summary>
        public int ReceiverLocationId { get; set; }

        /// <summary>
        /// Gets or sets the range of the receiver in kilometres.
        /// </summary>
        public int ReceiverRange { get; set; }

        /// <summary>
        /// Gets or sets a value indicating that DF19/AF0 is to be interpretted as an extended squitter message.
        /// </summary>
        public bool IgnoreMilitaryExtendedSquitter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating that decoded locations that are further away than the receiver can see are still
        /// allowed through, disabling part of the ICAO reasonableness tests.
        /// </summary>
        public bool SuppressReceiverRangeCheck { get; set; }

        /// <summary>
        /// Gets or sets a value indicating that local decoding should be used to determine the initial position of an aircraft
        /// instead of global decoding of an odd / even frame.
        /// </summary>
        /// <remarks>
        /// If the aircraft is so far away that the local decode produces the wrong position then it will eventually be picked up
        /// by the ICAO reasonableness tests (perform global decode using CPR values not used in the initial decode and confirm
        /// that position corresponds). If it fails that test then the position is reset and the next initial fix is forced to
        /// be made using a global decode.
        /// </remarks>
        public bool UseLocalDecodeForInitialPosition { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of seconds that can elapse when performing global decoding on airborne position messages.
        /// </summary>
        public int AirborneGlobalPositionLimit { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of seconds that can elapse when performing global decoding on surface position messages for vehicles travelling over 25 km/h.
        /// </summary>
        public int FastSurfaceGlobalPositionLimit { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of seconds that can elapse when performing global decoding on surface position messages for vehicles travelling at or under 25 km/h.
        /// </summary>
        public int SlowSurfaceGlobalPositionLimit { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of kilometres an aircraft can travel while airborne over 30 seconds before a local position decode is deemed invalid.
        /// </summary>
        public double AcceptableAirborneSpeed { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of kilometres an aircraft can travel while landing or taking off over 30 seconds before a local position decode is deemed invalid.
        /// </summary>
        public double AcceptableAirSurfaceTransitionSpeed { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of kilometres an surface vehicle can travel over 30 seconds before a local position decode is deemed invalid.
        /// </summary>
        public double AcceptableSurfaceSpeed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating that callsigns should not be extracted from BDS2,0 messages.
        /// </summary>
        public bool IgnoreCallsignsInBds20 { get; set; }

        /// <summary>
        /// Gets or sets the number of times the same ICAO is seen in PI0 message before it is accepted as valid.
        /// </summary>
        /// <remarks>The minimum value for this is 1 - i.e. accept it immediately.</remarks>
        public int AcceptIcaoInPI0Count { get; set; }

        /// <summary>
        /// Gets or sets the number of seconds over which the same ICAO is seen in PI0 messages before it is accepted as valid.
        /// </summary>
        public int AcceptIcaoInPI0Seconds { get; set; }

        /// <summary>
        /// Gets or sets the number of times the same ICAO is seen in messages that do not have PI before it is accepted as valid.
        /// </summary>
        /// <remarks>If this is zero then ICAOs are never accepted from messages that do not have PI.</remarks>
        public int AcceptIcaoInNonPICount { get; set; }

        /// <summary>
        /// Gets or sets the number of seconds over which the same ICAO is seen in messages that do not have PI before it is accepted as valid.
        /// </summary>
        public int AcceptIcaoInNonPISeconds { get; set; }

        /// <summary>
        /// Creates a new object.
        /// </summary>
        public RawDecodingSettings()
        {
            AirborneGlobalPositionLimit = 10;
            FastSurfaceGlobalPositionLimit = 25;
            SlowSurfaceGlobalPositionLimit = 50;
            AcceptableAirborneSpeed = 15.0;
            AcceptableAirSurfaceTransitionSpeed = 5.0;
            AcceptableSurfaceSpeed = 3.0;
            ReceiverRange = 650;
            SuppressReceiverRangeCheck = true;
            AcceptIcaoInNonPICount = 0;
            AcceptIcaoInNonPISeconds = 5;
            AcceptIcaoInPI0Count = 1;
            AcceptIcaoInPI0Seconds = 1;
        }
    }
}
