﻿// Copyright © 2010 onwards, Andrew Whewell
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
using VirtualRadar.Interface;
using VirtualRadar.Library.webservice.virtualradarserver.co.uk;
using InterfaceFactory;

namespace VirtualRadar.Library
{
    /// <summary>
    /// Implements <see cref="IExternalIPAddressService"/> using a webservice running on the VRS website.
    /// </summary>
    sealed class ExternalIPAddressService : IExternalIPAddressService
    {
        /// <summary>
        /// An implementation of the provider that calls the web service to get the external IP address.
        /// </summary>
        class DefaultProvider : IExternalIPAddressServiceProvider
        {
            public string ExternalIpAddress()
            {
                return new ClientSupport().ExternalIpAddress();
            }
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public IExternalIPAddressServiceProvider Provider { get; set; }

        private static readonly IExternalIPAddressService _Singleton = new ExternalIPAddressService();
        /// <summary>
        /// See interface docs.
        /// </summary>
        public IExternalIPAddressService Singleton { get { return _Singleton; } }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public string Address { get; private set; }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public event EventHandler<EventArgs<string>> AddressUpdated;

        /// <summary>
        /// Raises <see cref="AddressUpdated"/>.
        /// </summary>
        /// <param name="args"></param>
        private void OnAddressUpdated(EventArgs<string> args)
        {
            if(AddressUpdated != null) AddressUpdated(this, args);
        }

        /// <summary>
        /// Creates a new object.
        /// </summary>
        public ExternalIPAddressService()
        {
            Provider = new DefaultProvider();
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        /// <returns></returns>
        public string GetExternalIPAddress()
        {
            Address = Provider.ExternalIpAddress();
            OnAddressUpdated(new EventArgs<string>(Address));

            return Address;
        }
    }
}
