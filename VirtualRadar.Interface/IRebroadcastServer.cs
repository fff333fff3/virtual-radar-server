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
using VirtualRadar.Interface.Listener;
using VirtualRadar.Interface.Settings;

namespace VirtualRadar.Interface
{
    /// <summary>
    /// The interface for objects that can rebroadcast messages or bytes received by an <see cref="IListener"/>.
    /// </summary>
    public interface IRebroadcastServer : IBackgroundThreadExceptionCatcher, IDisposable
    {
        /// <summary>
        /// Gets or sets the listener to take messages and bytes from.
        /// </summary>
        IListener Listener { get; set; }

        /// <summary>
        /// Gets or sets the object that will do the actual network handling for us.
        /// </summary>
        IBroadcastProvider BroadcastProvider { get; set; }

        /// <summary>
        /// Gets or sets the format that the server is going to send information in.
        /// </summary>
        RebroadcastFormat Format { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating that the server is online.
        /// </summary>
        /// <remarks>
        /// Setting this to false does not disconnect the clients attached to the <see cref="BroadcastProvider"/>,
        /// it just stops sending bytes to them. To disconnect the clients you need to dispose of the provider
        /// and create a new one after you go offline.
        /// </remarks>
        bool Online { get; set; }

        /// <summary>
        /// Raised when <see cref="Online"/> changes.
        /// </summary>
        event EventHandler OnlineChanged;

        /// <summary>
        /// Initialises the listener and provider. After this has been called no changes should be made to any
        /// properties other than <see cref="Online"/>.
        /// </summary>
        void Initialise();
    }
}
