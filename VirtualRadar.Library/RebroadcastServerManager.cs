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
using VirtualRadar.Interface;
using VirtualRadar.Interface.Listener;
using InterfaceFactory;
using VirtualRadar.Interface.Settings;

namespace VirtualRadar.Library
{
    /// <summary>
    /// The default implementation of <see cref="IRebroadcastServerManager"/>.
    /// </summary>
    sealed class RebroadcastServerManager : IRebroadcastServerManager
    {
        #region Fields
        /// <summary>
        /// True when <see cref="Initialise"/> has hooked ConfigurationChanged.
        /// </summary>
        private bool _HookedConfigurationChanged;
        #endregion

        #region Properties
        private static readonly IRebroadcastServerManager _Singleton = new RebroadcastServerManager();
        /// <summary>
        /// See interface docs.
        /// </summary>
        public IRebroadcastServerManager Singleton { get { return _Singleton; } }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public IListener Listener { get; set; }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public List<IRebroadcastServer> RebroadcastServers { get; private set; }

        private bool _Online;
        /// <summary>
        /// See interface docs.
        /// </summary>
        public bool Online
        {
            get { return _Online; }
            set
            {
                if(_Online != value) {
                    _Online = value;
                    foreach(var server in RebroadcastServers) {
                        server.Online = value;
                    }
                    OnOnlineChanged(EventArgs.Empty);
                }
            }
        }
        #endregion

        #region Events exposed
        /// <summary>
        /// See interface docs.
        /// </summary>
        public event EventHandler<EventArgs<Exception>> ExceptionCaught;

        /// <summary>
        /// Raises <see cref="ExceptionCaught"/>.
        /// </summary>
        /// <param name="args"></param>
        private void OnExceptionCaught(EventArgs<Exception> args)
        {
            if(ExceptionCaught != null) ExceptionCaught(this, args);
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public event EventHandler OnlineChanged;

        /// <summary>
        /// Raises <see cref="OnlineChanged"/>.
        /// </summary>
        /// <param name="args"></param>
        private void OnOnlineChanged(EventArgs args)
        {
            if(OnlineChanged != null) OnlineChanged(this, args);
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public event EventHandler<BroadcastEventArgs> ClientConnected;

        /// <summary>
        /// Raises <see cref="ClientConnected"/>.
        /// </summary>
        /// <param name="args"></param>
        private void OnClientConnected(BroadcastEventArgs args)
        {
            if(ClientConnected != null) ClientConnected(this, args);
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public event EventHandler<BroadcastEventArgs> ClientDisconnected;

        /// <summary>
        /// Raises <see cref="ClientDisconnected"/>.
        /// </summary>
        /// <param name="args"></param>
        private void OnClientDisconnected(BroadcastEventArgs args)
        {
            if(ClientDisconnected != null) ClientDisconnected(this, args);
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public event EventHandler<BroadcastEventArgs> BroadcastSent;

        /// <summary>
        /// Raises <see cref="BroadcastSent"/>.
        /// </summary>
        /// <param name="args"></param>
        private void OnBroadcastSent(BroadcastEventArgs args)
        {
            if(BroadcastSent != null) BroadcastSent(this, args);
        }
        #endregion

        #region Constructors and finaliser
        /// <summary>
        /// Creates a new object.
        /// </summary>
        public RebroadcastServerManager()
        {
            RebroadcastServers = new List<IRebroadcastServer>();
        }

        /// <summary>
        /// Finalises the object.
        /// </summary>
        ~RebroadcastServerManager()
        {
            Dispose(false);
        }
        #endregion

        #region Dispose
        /// <summary>
        /// See interface docs.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalises or disposes of the object.
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if(disposing) {
                if(_HookedConfigurationChanged) {
                    Factory.Singleton.Resolve<IConfigurationStorage>().Singleton.ConfigurationChanged -= ConfigurationStorage_ConfigurationChanged;
                    _HookedConfigurationChanged = false;
                }

                foreach(var server in RebroadcastServers) {
                    ReleaseServer(server);
                }
                RebroadcastServers.Clear();
            }
        }
        #endregion

        #region Initialise, LoadConfiguration
        /// <summary>
        /// See interface docs.
        /// </summary>
        public void Initialise()
        {
            if(Listener == null) throw new InvalidOperationException("The Listener must be supplied before calling Initialise");
            if(_HookedConfigurationChanged) throw new InvalidOperationException("Initialise has already been called");

            var configurationStorage = LoadConfiguration();
            configurationStorage.ConfigurationChanged += ConfigurationStorage_ConfigurationChanged;
            _HookedConfigurationChanged = true;
        }

        /// <summary>
        /// Loads and applies the configuration.
        /// </summary>
        private IConfigurationStorage LoadConfiguration()
        {
            var result = Factory.Singleton.Resolve<IConfigurationStorage>().Singleton;
            var configuration = result.Load();

            var unusedServers = new List<IRebroadcastServer>(RebroadcastServers);

            foreach(var rebroadcastSettings in configuration.RebroadcastSettings) {
                if(rebroadcastSettings.Enabled) {
                    int indexExistingServer = unusedServers.FindIndex(r => r.Format == rebroadcastSettings.Format && r.BroadcastProvider.Port == rebroadcastSettings.Port);
                    if(indexExistingServer != -1) unusedServers.RemoveAt(indexExistingServer);
                    else {
                        var server = Factory.Singleton.Resolve<IRebroadcastServer>();
                        server.Listener = Listener;
                        server.BroadcastProvider = Factory.Singleton.Resolve<IBroadcastProvider>();
                        server.BroadcastProvider.Port = rebroadcastSettings.Port;
                        server.BroadcastProvider.BroadcastSent += BroadcastProvider_BroadcastSent;
                        server.BroadcastProvider.ClientConnected += BroadcastProvider_ClientConnected;
                        server.BroadcastProvider.ClientDisconnected += BroadcastProvider_ClientDisconnected;
                        server.BroadcastProvider.ExceptionCaught += BroadcastProvider_ExceptionCaught;
                        server.Format = rebroadcastSettings.Format;

                        RebroadcastServers.Add(server);
                        server.Initialise();

                        if(Online) server.Online = true;
                    }
                }
            }

            foreach(var unusedServer in unusedServers) {
                RebroadcastServers.Remove(unusedServer);
                ReleaseServer(unusedServer);
            }

            return result;
        }
        #endregion

        #region ReleaseServer
        /// <summary>
        /// Disposes of the server and the associated broadcast provider.
        /// </summary>
        /// <param name="server"></param>
        private void ReleaseServer(IRebroadcastServer server)
        {
            var broadcastProvider = server.BroadcastProvider;
            server.Dispose();
            broadcastProvider.BroadcastSent -= BroadcastProvider_BroadcastSent;
            broadcastProvider.ClientConnected -= BroadcastProvider_ClientConnected;
            broadcastProvider.ClientDisconnected -= BroadcastProvider_ClientDisconnected;
            broadcastProvider.ExceptionCaught -= BroadcastProvider_ExceptionCaught;
            broadcastProvider.Dispose();
        }
        #endregion

        #region Events subscribed
        /// <summary>
        /// Raised when the configuration has been changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ConfigurationStorage_ConfigurationChanged(object sender, EventArgs args)
        {
            LoadConfiguration();
        }

        /// <summary>
        /// Raised when a broadcast provider sends some bytes to a client.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void BroadcastProvider_BroadcastSent(object sender, BroadcastEventArgs args)
        {
            OnBroadcastSent(args);
        }

        /// <summary>
        /// Raised when a client connects to a broadcast provider.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void BroadcastProvider_ClientConnected(object sender, BroadcastEventArgs args)
        {
            OnClientConnected(args);
        }

        /// <summary>
        /// Raised when a client disconnects from a broadcast provider.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void BroadcastProvider_ClientDisconnected(object sender, BroadcastEventArgs args)
        {
            OnClientDisconnected(args);
        }

        /// <summary>
        /// Raised when a broadcast provider catches an exception that needs to be reported.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void BroadcastProvider_ExceptionCaught(object sender, EventArgs<Exception> args)
        {
            OnExceptionCaught(args);
        }
        #endregion
    }
}
