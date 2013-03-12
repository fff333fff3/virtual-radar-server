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
using System.Diagnostics;
using System.Linq;
using System.Text;
using InterfaceFactory;
using VirtualRadar.Interface;
using VirtualRadar.Interface.BaseStation;
using VirtualRadar.Interface.Database;
using VirtualRadar.Interface.Listener;
using VirtualRadar.Interface.Presenter;
using VirtualRadar.Interface.View;
using VirtualRadar.Interface.WebServer;
using VirtualRadar.Localisation;

namespace VirtualRadar.Library.Presenter
{
    /// <summary>
    /// The default implementation of <see cref="IShutdownPresenter"/>.
    /// </summary>
    class ShutdownPresenter : IShutdownPresenter
    {
        /// <summary>
        /// The view that this presenter is controlling.
        /// </summary>
        private IShutdownView _View;

        /// <summary>
        /// See interface docs.
        /// </summary>
        public IUniversalPlugAndPlayManager UPnpManager { get; set; }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public IBaseStationAircraftList BaseStationAircraftList { get; set; }

        /// <summary>
        /// See interface docs.
        /// </summary>
        /// <param name="view"></param>
        public void Initialise(IShutdownView view)
        {
            _View = view;
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public void ShutdownApplication()
        {
            ShutdownRebroadcastServers();
            ShutdownBaseStationListener();
            ShutdownPlugins();
            ShutdownUPnpManager();
            ShutdownBaseStationAircraftList();
            ShutdownConnectionLogger();
            ShutdownWebServer();
            ShutdownBaseStationDatabase();
            ShutdownLogDatabase();
        }

        private void ShutdownRebroadcastServers()
        {
            _View.ReportProgress(Strings.ShuttingDownRebroadcastServer);
            Factory.Singleton.Resolve<IRebroadcastServerManager>().Singleton.Dispose();
        }

        private void ShutdownBaseStationListener()
        {
            _View.ReportProgress(Strings.ShuttingDownBaseStationListener);
            Factory.Singleton.Resolve<IAutoConfigListener>().Singleton.Dispose();
        }

        private void ShutdownPlugins()
        {
            var plugins = Factory.Singleton.Resolve<IPluginManager>().Singleton.LoadedPlugins;
            foreach(var plugin in plugins) {
                _View.ReportProgress(String.Format(Strings.ShuttingDownPlugin, plugin.Name));

                try {
                    plugin.Shutdown();
                } catch(Exception ex) {
                    Debug.WriteLine(String.Format("ShutdownPresenter.ShutdownPlugins caught exception: {0}", ex.ToString()));
                    Factory.Singleton.Resolve<ILog>().Singleton.WriteLine("Plugin {0} threw an exception during shutdown: {1}", plugin.Name, ex.ToString());
                }
            }
        }

        private void ShutdownUPnpManager()
        {
            _View.ReportProgress(Strings.ShuttingDownUPnpManager);
            if(UPnpManager != null) UPnpManager.Dispose();
        }

        private void ShutdownBaseStationAircraftList()
        {
            _View.ReportProgress(Strings.ShuttingDownBaseStationAircraftList);
            if(BaseStationAircraftList != null) BaseStationAircraftList.Dispose();
        }

        private void ShutdownConnectionLogger()
        {
            _View.ReportProgress(Strings.ShuttingDownConnectionLogger);
            Factory.Singleton.Resolve<IConnectionLogger>().Singleton.Dispose();
        }

        private void ShutdownWebServer()
        {
            _View.ReportProgress(Strings.ShuttingDownWebServer);
            Factory.Singleton.Resolve<IAutoConfigWebServer>().Singleton.Dispose();
        }

        private void ShutdownBaseStationDatabase()
        {
            _View.ReportProgress(Strings.ShuttingDownBaseStationDatabase);
            Factory.Singleton.Resolve<IAutoConfigBaseStationDatabase>().Singleton.Dispose();
        }

        private void ShutdownLogDatabase()
        {
            _View.ReportProgress(Strings.ShuttingDownLogDatabase);
            Factory.Singleton.Resolve<ILogDatabase>().Singleton.Dispose();
        }
    }
}
