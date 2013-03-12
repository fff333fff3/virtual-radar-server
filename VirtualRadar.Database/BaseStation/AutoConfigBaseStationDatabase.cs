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
using VirtualRadar.Interface.Database;
using InterfaceFactory;
using VirtualRadar.Interface.Settings;

namespace VirtualRadar.Database.BaseStation
{
    /// <summary>
    /// The default implementation of <see cref="IAutoConfigBaseStationDatabase"/>.
    /// </summary>
    sealed class AutoConfigBaseStationDatabase : IAutoConfigBaseStationDatabase
    {
        private static readonly IAutoConfigBaseStationDatabase _Singleton = new AutoConfigBaseStationDatabase();
        /// <summary>
        /// See interface docs.
        /// </summary>
        public IAutoConfigBaseStationDatabase Singleton { get { return _Singleton; } }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public IBaseStationDatabase Database { get; private set; }

        /// <summary>
        /// Finalises the object.
        /// </summary>
        ~AutoConfigBaseStationDatabase()
        {
            Dispose(false);
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes or finalises the object. Note that the class is sealed.
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if(disposing && Database != null) Database.Dispose();
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public void Initialise()
        {
            Database = Factory.Singleton.Resolve<IBaseStationDatabase>();
            LoadConfiguration();

            Factory.Singleton.Resolve<IConfigurationStorage>().Singleton.ConfigurationChanged += ConfigurationStorage_ConfigurationChanged;
        }

        /// <summary>
        /// Loads the configuration into the database.
        /// </summary>
        private void LoadConfiguration()
        {
            var configuration = Factory.Singleton.Resolve<IConfigurationStorage>().Singleton.Load();
            Database.FileName = configuration.BaseStationSettings.DatabaseFileName;
        }

        /// <summary>
        /// Called when the configuration is changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ConfigurationStorage_ConfigurationChanged(object sender, EventArgs args)
        {
            LoadConfiguration();
        }
   }
}
