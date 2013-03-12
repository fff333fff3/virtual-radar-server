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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualRadar.Interface.Presenter;
using VirtualRadar.Interface.View;
using Moq;
using InterfaceFactory;
using VirtualRadar.Interface.Settings;
using Test.Framework;
using VirtualRadar.Localisation;
using System.Net;
using VirtualRadar.Interface;
using System.IO.Ports;

namespace Test.VirtualRadar.Library.Presenter
{
    [TestClass]
    public class OptionsPresenterTests
    {
        #region Private class - ConfigurationProperty
        /// <summary>
        /// An internal class that simplifies the checking of large groups of configuration properties.
        /// </summary>
        /// <remarks>
        /// The main benefit of using this class is that it keeps the number of columns required in the spreadsheet data
        /// under control. The down-side is that we only check one property at a time, which means we're not checking for
        /// any side-effects.
        /// </remarks>
        class ConfigurationProperty
        {
            public string Name;
            public Func<ExcelWorksheetData, string, object> GetSpreadsheetConfig;
            public Func<ExcelWorksheetData, string, object> GetSpreadsheetView;
            public Func<Configuration, object>              GetConfig;
            public Action<Configuration, object>            SetConfig;
            public Func<IOptionsView, object>               GetView;
            public Action<IOptionsView, object>             SetView;

            public ConfigurationProperty()
            {
            }

            public ConfigurationProperty(string name, Func<ExcelWorksheetData, string, object> getSpreadsheet, Func<Configuration, object> getConfig, Action<Configuration, object> setConfig, Func<IOptionsView, object> getView, Action<IOptionsView, object> setView)
            {
                Name = name;
                GetSpreadsheetConfig = GetSpreadsheetView = getSpreadsheet;
                GetConfig = getConfig;
                SetConfig = setConfig;
                GetView = getView;
                SetView = setView;
            }

            public ConfigurationProperty(string name, Func<ExcelWorksheetData, string, object> getSpreadsheetConfig, Func<ExcelWorksheetData, string, object> getSpreadsheetView, Func<Configuration, object> getConfig, Action<Configuration, object> setConfig, Func<IOptionsView, object> getView, Action<IOptionsView, object> setView) : this(name, getSpreadsheetConfig, getConfig, setConfig, getView, setView)
            {
                GetSpreadsheetView = getSpreadsheetView;
            }

            public override string ToString()
            {
                return Name ?? base.ToString();
            }
        }
        #endregion

        #region TestContext, fields etc.
        public TestContext TestContext { get; set; }

        private IClassFactory _ClassFactorySnapshot;
        private IOptionsPresenter _Presenter;
        private Mock<IOptionsPresenterProvider> _Provider;
        private Mock<IOptionsView> _View;
        private Configuration _Configuration;
        private Mock<IConfigurationStorage> _ConfigurationStorage;

        [TestInitialize]
        public void TestInitialise()
        {
            _ClassFactorySnapshot = Factory.TakeSnapshot();

            _ConfigurationStorage = TestUtilities.CreateMockSingleton<IConfigurationStorage>();
            _Configuration = new Configuration();
            _ConfigurationStorage.Setup(c => c.Load()).Returns(_Configuration);

            _Presenter = Factory.Singleton.Resolve<IOptionsPresenter>();
            _Provider = new Mock<IOptionsPresenterProvider>() { DefaultValue = DefaultValue.Mock }.SetupAllProperties();
            _Provider.Setup(p => p.FileExists(It.IsAny<string>())).Returns(true);
            _Provider.Setup(p => p.FolderExists(It.IsAny<string>())).Returns(true);
            _Provider.Setup(p => p.TestNetworkConnection(It.IsAny<string>(), It.IsAny<int>())).Returns((Exception)null);
            _Provider.Setup(p => p.TestSerialConnection(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<StopBits>(), It.IsAny<Parity>(), It.IsAny<Handshake>())).Returns((Exception)null);
            _Presenter.Provider = _Provider.Object;
            _View = new Mock<IOptionsView>() { DefaultValue = DefaultValue.Mock }.SetupAllProperties();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Factory.RestoreSnapshot(_ClassFactorySnapshot);
        }
        #endregion

        #region Configuration Property Declarations
        private readonly List<ConfigurationProperty> _BaseStationProperties = new List<ConfigurationProperty>() {
            new ConfigurationProperty("Address",                        (w, c) => w.EString(c),                   r => r.BaseStationSettings.Address,                  (r, v) => r.BaseStationSettings.Address = (string)v,                r => r.BaseStationAddress,               (r, v) => r.BaseStationAddress = (string)v),
            new ConfigurationProperty("AutoReconnectAtStartup",         (w, c) => w.Bool(c),                      r => r.BaseStationSettings.AutoReconnectAtStartup,   (r, v) => r.BaseStationSettings.AutoReconnectAtStartup = (bool)v,   r => r.AutoReconnectAtStartup,           (r, v) => r.AutoReconnectAtStartup = (bool)v),
            new ConfigurationProperty("Port",                           (w, c) => w.Int(c),                       r => r.BaseStationSettings.Port,                     (r, v) => r.BaseStationSettings.Port = (int)v,                      r => r.BaseStationPort,                  (r, v) => r.BaseStationPort = (int)v),
            new ConfigurationProperty("ComPort",                        (w, c) => w.String(c),                    r => r.BaseStationSettings.ComPort,                  (r, v) => r.BaseStationSettings.ComPort = (string)v,                r => r.SerialComPort,                    (r, v) => r.SerialComPort = (string)v),
            new ConfigurationProperty("BaudRate",                       (w, c) => w.Int(c),                       r => r.BaseStationSettings.BaudRate,                 (r, v) => r.BaseStationSettings.BaudRate = (int)v,                  r => r.SerialBaudRate,                   (r, v) => r.SerialBaudRate = (int)v),
            new ConfigurationProperty("DataBits",                       (w, c) => w.Int(c),                       r => r.BaseStationSettings.DataBits,                 (r, v) => r.BaseStationSettings.DataBits = (int)v,                  r => r.SerialDataBits,                   (r, v) => r.SerialDataBits = (int)v),
            new ConfigurationProperty("StopBits",                       (w, c) => w.ParseEnum<StopBits>(c),       r => r.BaseStationSettings.StopBits,                 (r, v) => r.BaseStationSettings.StopBits = (StopBits)v,             r => r.SerialStopBits,                   (r, v) => r.SerialStopBits = (StopBits)v),
            new ConfigurationProperty("Parity",                         (w, c) => w.ParseEnum<Parity>(c),         r => r.BaseStationSettings.Parity,                   (r, v) => r.BaseStationSettings.Parity = (Parity)v,                 r => r.SerialParity,                     (r, v) => r.SerialParity = (Parity)v),
            new ConfigurationProperty("Handshake",                      (w, c) => w.ParseEnum<Handshake>(c),      r => r.BaseStationSettings.Handshake,                (r, v) => r.BaseStationSettings.Handshake = (Handshake)v,           r => r.SerialHandshake,                  (r, v) => r.SerialHandshake = (Handshake)v),
            new ConfigurationProperty("StartupText",                    (w, c) => w.EString(c),                   r => r.BaseStationSettings.StartupText,              (r, v) => r.BaseStationSettings.StartupText = (string)v,            r => r.SerialStartupText,                (r, v) => r.SerialStartupText = (String)v),
            new ConfigurationProperty("ShutdownText",                   (w, c) => w.EString(c),                   r => r.BaseStationSettings.ShutdownText,             (r, v) => r.BaseStationSettings.ShutdownText = (string)v,           r => r.SerialShutdownText,               (r, v) => r.SerialShutdownText = (String)v),
            new ConfigurationProperty("DatabaseFileName",               (w, c) => w.EString(c),                   r => r.BaseStationSettings.DatabaseFileName,         (r, v) => r.BaseStationSettings.DatabaseFileName = (string)v,       r => r.BaseStationDatabaseFileName,      (r, v) => r.BaseStationDatabaseFileName = (string)v),
            new ConfigurationProperty("OperatorFlagsFolder",            (w, c) => w.EString(c),                   r => r.BaseStationSettings.OperatorFlagsFolder,      (r, v) => r.BaseStationSettings.OperatorFlagsFolder = (string)v,    r => r.OperatorFlagsFolder,              (r, v) => r.OperatorFlagsFolder = (string)v),
            new ConfigurationProperty("SilhouettesFolder",              (w, c) => w.EString(c),                   r => r.BaseStationSettings.SilhouettesFolder,        (r, v) => r.BaseStationSettings.SilhouettesFolder = (string)v,      r => r.SilhouettesFolder,                (r, v) => r.SilhouettesFolder = (string)v),
            new ConfigurationProperty("PicturesFolder",                 (w, c) => w.EString(c),                   r => r.BaseStationSettings.PicturesFolder,           (r, v) => r.BaseStationSettings.PicturesFolder = (string)v,         r => r.PicturesFolder,                   (r, v) => r.PicturesFolder = (string)v),
            new ConfigurationProperty("DataSource",                     (w, c) => w.ParseEnum<DataSource>(c),     r => r.BaseStationSettings.DataSource,               (r, v) => r.BaseStationSettings.DataSource = (DataSource)v,         r => r.BaseStationDataSource,            (r, v) => r.BaseStationDataSource = (DataSource)v),
            new ConfigurationProperty("ConnectionType",                 (w, c) => w.ParseEnum<ConnectionType>(c), r => r.BaseStationSettings.ConnectionType,           (r, v) => r.BaseStationSettings.ConnectionType = (ConnectionType)v, r => r.BaseStationConnectionType,        (r, v) => r.BaseStationConnectionType = (ConnectionType)v),
        };

        private readonly List<ConfigurationProperty> _AudioProperties = new List<ConfigurationProperty>() {
            new ConfigurationProperty("Enabled",    (w, c) => w.Bool(c),    r => r.AudioSettings.Enabled,   (r, v) => r.AudioSettings.Enabled = (bool)v,     r => r.AudioEnabled,      (r, v) => r.AudioEnabled = (bool)v),
            new ConfigurationProperty("VoiceName",  (w, c) => w.EString(c), r => r.AudioSettings.VoiceName, (r, v) => r.AudioSettings.VoiceName = (string)v, r => r.TextToSpeechVoice, (r, v) => r.TextToSpeechVoice = (string)v),
            new ConfigurationProperty("VoiceRate",  (w, c) => w.Int(c),     r => r.AudioSettings.VoiceRate, (r, v) => r.AudioSettings.VoiceRate = (int)v,    r => r.TextToSpeechSpeed, (r, v) => r.TextToSpeechSpeed = (int)v),
        };

        private readonly List<ConfigurationProperty> _GeneralProperties = new List<ConfigurationProperty>() {
            new ConfigurationProperty("CheckAutomatically",      (w, c) => w.Bool(c),   r => r.VersionCheckSettings.CheckAutomatically,     (r, v) => r.VersionCheckSettings.CheckAutomatically = (bool)v,      r => r.CheckForNewVersions,           (r, v) => r.CheckForNewVersions = (bool)v),
            new ConfigurationProperty("CheckPeriodDays",         (w, c) => w.Int(c),    r => r.VersionCheckSettings.CheckPeriodDays,        (r, v) => r.VersionCheckSettings.CheckPeriodDays = (int)v,          r => r.CheckForNewVersionsPeriodDays, (r, v) => r.CheckForNewVersionsPeriodDays = (int)v),
            new ConfigurationProperty("AutoUpdateEnabled",       (w, c) => w.Bool(c),   r => r.FlightRouteSettings.AutoUpdateEnabled,       (r, v) => r.FlightRouteSettings.AutoUpdateEnabled = (bool)v,        r => r.DownloadFlightRoutes,          (r, v) => r.DownloadFlightRoutes = (bool)v),
            new ConfigurationProperty("DisplayTimeoutSeconds",   (w, c) => w.Int(c),    r => r.BaseStationSettings.DisplayTimeoutSeconds,   (r, v) => r.BaseStationSettings.DisplayTimeoutSeconds = (int)v,     r => r.DisplayTimeoutSeconds,         (r, v) => r.DisplayTimeoutSeconds = (int)v),
            new ConfigurationProperty("TrackingTimeoutSeconds",  (w, c) => w.Int(c),    r => r.BaseStationSettings.TrackingTimeoutSeconds,  (r, v) => r.BaseStationSettings.TrackingTimeoutSeconds = (int)v,    r => r.TrackingTimeoutSeconds,        (r, v) => r.TrackingTimeoutSeconds = (int)v),
            new ConfigurationProperty("ShortTrailLengthSeconds", (w, c) => w.Int(c),    r => r.GoogleMapSettings.ShortTrailLengthSeconds,   (r, v) => r.GoogleMapSettings.ShortTrailLengthSeconds = (int)v,     r => r.ShortTrailLengthSeconds,       (r, v) => r.ShortTrailLengthSeconds = (int)v),
        };

        private readonly List<ConfigurationProperty> _GoogleMapOptions = new List<ConfigurationProperty>() {
            new ConfigurationProperty("InitialMapLatitude",     (w, c) => w.Double(c),                  r => r.GoogleMapSettings.InitialMapLatitude,        (r, v) => r.GoogleMapSettings.InitialMapLatitude = (double)v,        r => r.InitialGoogleMapLatitude,       (r, v) => r.InitialGoogleMapLatitude = (double)v),
            new ConfigurationProperty("InitialMapLongitude",    (w, c) => w.Double(c),                  r => r.GoogleMapSettings.InitialMapLongitude,       (r, v) => r.GoogleMapSettings.InitialMapLongitude = (double)v,       r => r.InitialGoogleMapLongitude,      (r, v) => r.InitialGoogleMapLongitude = (double)v),
            new ConfigurationProperty("InitialMapType",         (w, c) => w.EString(c),                 r => r.GoogleMapSettings.InitialMapType,            (r, v) => r.GoogleMapSettings.InitialMapType = (string)v,            r => r.InitialGoogleMapType,           (r, v) => r.InitialGoogleMapType = (string)v),
            new ConfigurationProperty("InitialMapZoom",         (w, c) => w.Int(c),                     r => r.GoogleMapSettings.InitialMapZoom,            (r, v) => r.GoogleMapSettings.InitialMapZoom = (int)v,               r => r.InitialGoogleMapZoom,           (r, v) => r.InitialGoogleMapZoom = (int)v),
            new ConfigurationProperty("InitialRefreshSeconds",  (w, c) => w.Int(c),                     r => r.GoogleMapSettings.InitialRefreshSeconds,     (r, v) => r.GoogleMapSettings.InitialRefreshSeconds = (int)v,        r => r.InitialGoogleMapRefreshSeconds, (r, v) => { r.InitialGoogleMapRefreshSeconds = r.MinimumGoogleMapRefreshSeconds = (int)v; }),
            new ConfigurationProperty("MinimumRefreshSeconds",  (w, c) => w.Int(c),                     r => r.GoogleMapSettings.MinimumRefreshSeconds,     (r, v) => r.GoogleMapSettings.MinimumRefreshSeconds = (int)v,        r => r.MinimumGoogleMapRefreshSeconds, (r, v) => { r.MinimumGoogleMapRefreshSeconds = r.InitialGoogleMapRefreshSeconds = (int)v; }),
            new ConfigurationProperty("InitialDistanceUnit",    (w, c) => w.ParseEnum<DistanceUnit>(c), r => r.GoogleMapSettings.InitialDistanceUnit,       (r, v) => r.GoogleMapSettings.InitialDistanceUnit = (DistanceUnit)v, r => r.InitialDistanceUnit,            (r, v) => r.InitialDistanceUnit = (DistanceUnit)v),
            new ConfigurationProperty("InitialHeightUnit",      (w, c) => w.ParseEnum<HeightUnit>(c),   r => r.GoogleMapSettings.InitialHeightUnit,         (r, v) => r.GoogleMapSettings.InitialHeightUnit = (HeightUnit)v,     r => r.InitialHeightUnit,              (r, v) => r.InitialHeightUnit = (HeightUnit)v),
            new ConfigurationProperty("InitialSpeedUnit",       (w, c) => w.ParseEnum<SpeedUnit>(c),    r => r.GoogleMapSettings.InitialSpeedUnit,          (r, v) => r.GoogleMapSettings.InitialSpeedUnit = (SpeedUnit)v,       r => r.InitialSpeedUnit,               (r, v) => r.InitialSpeedUnit = (SpeedUnit)v),
            new ConfigurationProperty("PreferIataAirportCodes", (w, c) => w.Bool(c),                    r => r.GoogleMapSettings.PreferIataAirportCodes,    (r, v) => r.GoogleMapSettings.PreferIataAirportCodes = (bool)v,      r => r.PreferIataAirportCodes,         (r, v) => r.PreferIataAirportCodes = (bool)v),
        };

        private readonly List<ConfigurationProperty> _InternetClientOptions = new List<ConfigurationProperty>() {
            new ConfigurationProperty("CanRunReports",                  (w, c) => w.Bool(c),    r => r.InternetClientSettings.CanRunReports,                    (r, v) => r.InternetClientSettings.CanRunReports = (bool)v,                 r => r.InternetClientCanRunReports,     (r, v) => r.InternetClientCanRunReports = (bool)v),
            new ConfigurationProperty("CanPlayAudio",                   (w, c) => w.Bool(c),    r => r.InternetClientSettings.CanPlayAudio,                     (r, v) => r.InternetClientSettings.CanPlayAudio = (bool)v,                  r => r.InternetClientCanPlayAudio,      (r, v) => r.InternetClientCanPlayAudio = (bool)v),
            new ConfigurationProperty("CanShowPictures",                (w, c) => w.Bool(c),    r => r.InternetClientSettings.CanShowPictures,                  (r, v) => r.InternetClientSettings.CanShowPictures = (bool)v,               r => r.InternetClientCanSeePictures,    (r, v) => r.InternetClientCanSeePictures= (bool)v),
            new ConfigurationProperty("TimeoutMinutes",                 (w, c) => w.Int(c),     r => r.InternetClientSettings.TimeoutMinutes,                   (r, v) => r.InternetClientSettings.TimeoutMinutes = (int)v,                 r => r.InternetClientTimeoutMinutes,    (r, v) => r.InternetClientTimeoutMinutes= (int)v),
            new ConfigurationProperty("CanShowPinText",                 (w, c) => w.Bool(c),    r => r.InternetClientSettings.CanShowPinText,                   (r, v) => r.InternetClientSettings.CanShowPinText = (bool)v,                r => r.InternetClientCanSeeLabels,      (r, v) => r.InternetClientCanSeeLabels= (bool)v),
            new ConfigurationProperty("AllowInternetProximityGadgets",  (w, c) => w.Bool(c),    r => r.InternetClientSettings.AllowInternetProximityGadgets,    (r, v) => r.InternetClientSettings.AllowInternetProximityGadgets = (bool)v, r => r.AllowInternetProximityGadgets,   (r, v) => r.AllowInternetProximityGadgets= (bool)v),
            new ConfigurationProperty("CanSubmitRoutes",                (w, c) => w.Bool(c),    r => r.InternetClientSettings.CanSubmitRoutes,                  (r, v) => r.InternetClientSettings.CanSubmitRoutes = (bool)v,               r => r.InternetClientCanSubmitRoutes,   (r, v) => r.InternetClientCanSubmitRoutes = (bool)v),
        };

        private readonly List<ConfigurationProperty> _WebServerOptions = new List<ConfigurationProperty>() {
            new ConfigurationProperty("AuthenticationScheme",       (w, c) => w.ParseEnum<AuthenticationSchemes>(c), (w, c) => w.Bool(c), r => r.WebServerSettings.AuthenticationScheme,      (r, v) => r.WebServerSettings.AuthenticationScheme = (AuthenticationSchemes)v, r => r.WebServerUserMustAuthenticate,   (r, v) => { r.WebServerUserMustAuthenticate = (bool)v; r.WebServerUserName = "a"; }),
            new ConfigurationProperty("BasicAuthenticationUser",    (w, c) => w.EString(c),                                               r => r.WebServerSettings.BasicAuthenticationUser,   (r, v) => r.WebServerSettings.BasicAuthenticationUser = (string)v,             r => r.WebServerUserName,               (r, v) => r.WebServerUserName = (string)v),
            new ConfigurationProperty("EnableUPnp",                 (w, c) => w.Bool(c),                                                  r => r.WebServerSettings.EnableUPnp,                (r, v) => r.WebServerSettings.EnableUPnp = (bool)v,                            r => r.EnableUPnpFeatures,              (r, v) => r.EnableUPnpFeatures = (bool)v),
            new ConfigurationProperty("IsOnlyInternetServerOnLan",  (w, c) => w.Bool(c),                                                  r => r.WebServerSettings.IsOnlyInternetServerOnLan, (r, v) => r.WebServerSettings.IsOnlyInternetServerOnLan = (bool)v,             r => r.IsOnlyVirtualRadarServerOnLan,   (r, v) => r.IsOnlyVirtualRadarServerOnLan = (bool)v),
            new ConfigurationProperty("AutoStartUPnP",              (w, c) => w.Bool(c),                                                  r => r.WebServerSettings.AutoStartUPnP,             (r, v) => r.WebServerSettings.AutoStartUPnP = (bool)v,                         r => r.AutoStartUPnp,                   (r, v) => r.AutoStartUPnp = (bool)v),
            new ConfigurationProperty("UPnpPort",                   (w, c) => w.Int(c),                                                   r => r.WebServerSettings.UPnpPort,                  (r, v) => r.WebServerSettings.UPnpPort = (int)v,                               r => r.UPnpPort,                        (r, v) => r.UPnpPort = (int)v),
        };

        private readonly List<ConfigurationProperty> _RawDecodingOptions = new List<ConfigurationProperty>() {
            new ConfigurationProperty("ReceiverLocationId",                  (w, c) => w.Int(c),    r => r.RawDecodingSettings.ReceiverLocationId,                  (r, v) => r.RawDecodingSettings.ReceiverLocationId = (int)v,                     r => r.RawDecodingReceiverLocationId,                  (r, v) => r.RawDecodingReceiverLocationId = (int)v),
            new ConfigurationProperty("ReceiverRange",                       (w, c) => w.Int(c),    r => r.RawDecodingSettings.ReceiverRange,                       (r, v) => r.RawDecodingSettings.ReceiverRange = (int)v,                          r => r.RawDecodingReceiverRange,                       (r, v) => r.RawDecodingReceiverRange = (int)v),
            new ConfigurationProperty("IgnoreMilitaryExtendedSquitter",      (w, c) => w.Bool(c),   r => r.RawDecodingSettings.IgnoreMilitaryExtendedSquitter,      (r, v) => r.RawDecodingSettings.IgnoreMilitaryExtendedSquitter = (bool)v,        r => r.RawDecodingIgnoreMilitaryExtendedSquitter,      (r, v) => r.RawDecodingIgnoreMilitaryExtendedSquitter = (bool)v),
            new ConfigurationProperty("AirborneGlobalPositionLimit",         (w, c) => w.Int(c),    r => r.RawDecodingSettings.AirborneGlobalPositionLimit,         (r, v) => r.RawDecodingSettings.AirborneGlobalPositionLimit = (int)v,            r => r.RawDecodingAirborneGlobalPositionLimit,         (r, v) => r.RawDecodingAirborneGlobalPositionLimit = (int)v),
            new ConfigurationProperty("FastSurfaceGlobalPositionLimit",      (w, c) => w.Int(c),    r => r.RawDecodingSettings.FastSurfaceGlobalPositionLimit,      (r, v) => r.RawDecodingSettings.FastSurfaceGlobalPositionLimit = (int)v,         r => r.RawDecodingFastSurfaceGlobalPositionLimit,      (r, v) => r.RawDecodingFastSurfaceGlobalPositionLimit = (int)v),
            new ConfigurationProperty("SlowSurfaceGlobalPositionLimit",      (w, c) => w.Int(c),    r => r.RawDecodingSettings.SlowSurfaceGlobalPositionLimit,      (r, v) => r.RawDecodingSettings.SlowSurfaceGlobalPositionLimit = (int)v,         r => r.RawDecodingSlowSurfaceGlobalPositionLimit,      (r, v) => r.RawDecodingSlowSurfaceGlobalPositionLimit = (int)v),
            new ConfigurationProperty("AcceptableAirborneSpeed",             (w, c) => w.Double(c), r => r.RawDecodingSettings.AcceptableAirborneSpeed,             (r, v) => r.RawDecodingSettings.AcceptableAirborneSpeed = (double)v,             r => r.RawDecodingAcceptableAirborneSpeed,             (r, v) => r.RawDecodingAcceptableAirborneSpeed = (double)v),
            new ConfigurationProperty("AcceptableAirSurfaceTransitionSpeed", (w, c) => w.Double(c), r => r.RawDecodingSettings.AcceptableAirSurfaceTransitionSpeed, (r, v) => r.RawDecodingSettings.AcceptableAirSurfaceTransitionSpeed = (double)v, r => r.RawDecodingAcceptableAirSurfaceTransitionSpeed, (r, v) => r.RawDecodingAcceptableAirSurfaceTransitionSpeed = (double)v),
            new ConfigurationProperty("AcceptableSurfaceSpeed",              (w, c) => w.Double(c), r => r.RawDecodingSettings.AcceptableSurfaceSpeed,              (r, v) => r.RawDecodingSettings.AcceptableSurfaceSpeed = (double)v,              r => r.RawDecodingAcceptableSurfaceSpeed,              (r, v) => r.RawDecodingAcceptableSurfaceSpeed = (double)v),
            new ConfigurationProperty("SuppressReceiverRangeCheck",          (w, c) => w.Bool(c),   r => r.RawDecodingSettings.SuppressReceiverRangeCheck,          (r, v) => r.RawDecodingSettings.SuppressReceiverRangeCheck = (bool)v,            r => r.RawDecodingSuppressReceiverRangeCheck,          (r, v) => r.RawDecodingSuppressReceiverRangeCheck = (bool)v),
            new ConfigurationProperty("UseLocalDecodeForInitialPosition",    (w, c) => w.Bool(c),   r => r.RawDecodingSettings.UseLocalDecodeForInitialPosition,    (r, v) => r.RawDecodingSettings.UseLocalDecodeForInitialPosition = (bool)v,      r => r.RawDecodingUseLocalDecodeForInitialPosition,    (r, v) => r.RawDecodingUseLocalDecodeForInitialPosition = (bool)v),
            new ConfigurationProperty("IgnoreCallsignsInBds20",              (w, c) => w.Bool(c),   r => r.RawDecodingSettings.IgnoreCallsignsInBds20,              (r, v) => r.RawDecodingSettings.IgnoreCallsignsInBds20 = (bool)v,                r => r.RawDecodingIgnoreCallsignsInBds20,              (r, v) => r.RawDecodingIgnoreCallsignsInBds20 = (bool)v),
            new ConfigurationProperty("AcceptIcaoInNonPICount",              (w, c) => w.Int(c),    r => r.RawDecodingSettings.AcceptIcaoInNonPICount,              (r, v) => r.RawDecodingSettings.AcceptIcaoInNonPICount = (int)v,                 r => r.AcceptIcaoInNonPICount,                         (r, v) => r.AcceptIcaoInNonPICount = (int)v),
            new ConfigurationProperty("AcceptIcaoInNonPISeconds",            (w, c) => w.Int(c),    r => r.RawDecodingSettings.AcceptIcaoInNonPISeconds,            (r, v) => r.RawDecodingSettings.AcceptIcaoInNonPISeconds = (int)v,               r => r.AcceptIcaoInNonPISeconds,                       (r, v) => r.AcceptIcaoInNonPISeconds = (int)v),
            new ConfigurationProperty("AcceptIcaoInPI0Count",                (w, c) => w.Int(c),    r => r.RawDecodingSettings.AcceptIcaoInPI0Count,                (r, v) => r.RawDecodingSettings.AcceptIcaoInPI0Count = (int)v,                   r => r.AcceptIcaoInPI0Count,                           (r, v) => r.AcceptIcaoInPI0Count = (int)v),
            new ConfigurationProperty("AcceptIcaoInPI0Seconds",              (w, c) => w.Int(c),    r => r.RawDecodingSettings.AcceptIcaoInPI0Seconds,              (r, v) => r.RawDecodingSettings.AcceptIcaoInPI0Seconds = (int)v,                 r => r.AcceptIcaoInPI0Seconds,                         (r, v) => r.AcceptIcaoInPI0Seconds = (int)v),
        };
        #endregion

        #region Check_ methods
        private void Check_Initialise_Copies_Values_From_Configuration_To_View(List<ConfigurationProperty> configurationProperties)
        {
            var worksheet = new ExcelWorksheetData(TestContext);

            if(worksheet.String("Direction") != "UIToConfig") {
                var name = worksheet.String("Name");
                var configurationProperty = configurationProperties.Where(r => r.Name == name).Single();
                var configurationValue = configurationProperty.GetSpreadsheetConfig(worksheet, "ConfigValue");
                var uiValue = configurationProperty.GetSpreadsheetView(worksheet, "UIValue");

                configurationProperty.SetConfig(_Configuration, configurationValue);

                _Presenter.Initialise(_View.Object);

                Assert.AreEqual(uiValue, configurationProperty.GetView(_View.Object), configurationProperty.Name);
            }
        }

        private void Check_SaveClicked_Copies_Values_From_View_To_Configuration(List<ConfigurationProperty> configurationProperties)
        {
            var worksheet = new ExcelWorksheetData(TestContext);

            if(worksheet.String("Direction") != "ConfigToUI") {
                var name = worksheet.String("Name");
                var configurationProperty = configurationProperties.Where(r => r.Name == name).Single();
                var configurationValue = configurationProperty.GetSpreadsheetConfig(worksheet, "ConfigValue");
                var uiValue = configurationProperty.GetSpreadsheetView(worksheet, "UIValue");

                _ConfigurationStorage.Setup(r => r.Save(_Configuration)).Callback(() => {
                    Assert.AreEqual(configurationValue, configurationProperty.GetConfig(_Configuration), name);
                });

                _Presenter.Initialise(_View.Object);
                configurationProperty.SetView(_View.Object, uiValue);
                if(configurationProperty.Name == "ConnectionType" && (ConnectionType)uiValue == ConnectionType.COM) _View.Object.SerialComPort = "COM3";

                _View.Raise(v => v.SaveClicked += null, EventArgs.Empty);

                _ConfigurationStorage.Verify(c => c.Save(_Configuration), Times.Once(), name);
            }
        }

        private void Check_ResetToDefaultClicked_Copies_Default_Configuration_To_UI(List<ConfigurationProperty> configurationProperties)
        {
            var worksheet = new ExcelWorksheetData(TestContext);

            if(worksheet.String("Direction") != "ConfigToUI") {
                var name = worksheet.String("Name");
                var configurationProperty = configurationProperties.Where(r => r.Name == name).Single();
                var configurationValue = configurationProperty.GetSpreadsheetConfig(worksheet, "ConfigValue");

                var defaultConfig = new Configuration();
                _ConfigurationStorage.Setup(r => r.Save(_Configuration)).Callback(() => {
                    Assert.AreEqual(configurationProperty.GetConfig(defaultConfig), configurationProperty.GetConfig(_Configuration), name);
                });

                configurationProperty.SetConfig(_Configuration, configurationValue);
                _Presenter.Initialise(_View.Object);
                _View.Raise(v => v.ResetToDefaultsClicked += null, EventArgs.Empty);
                _View.Raise(v => v.SaveClicked += null, EventArgs.Empty);

                _ConfigurationStorage.Verify(c => c.Save(_Configuration), Times.Once(), name);
            }
        }
        #endregion

        #region Constructor
        [TestMethod]
        public void OptionsPresenter_Constructor_Initialises_To_Known_State_And_Properties_Work()
        {
            var presenter = Factory.Singleton.Resolve<IOptionsPresenter>();

            Assert.IsNotNull(presenter.Provider);
            TestUtilities.TestProperty(presenter, "Provider", presenter.Provider, _Provider.Object);
        }
        #endregion

        #region Dispose
        [TestMethod]
        public void OptionsPresenter_Dispose_Calls_Provider_Dispose()
        {
            _Presenter.Dispose();

            _Provider.Verify(p => p.Dispose(), Times.Once());
        }
        #endregion

        #region Initialise
        [TestMethod]
        [DataSource("Data Source='LibraryTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "AudioOptions$")]
        public void OptionsPresenter_Initialise_Copies_Values_From_Configuration_To_Audio_Options_UI()
        {
            Check_Initialise_Copies_Values_From_Configuration_To_View(_AudioProperties);
        }

        [TestMethod]
        [DataSource("Data Source='LibraryTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "BaseStationOptions$")]
        public void OptionsPresenter_Initialise_Copies_Values_From_Configuration_To_BaseStation_Options_UI()
        {
            Check_Initialise_Copies_Values_From_Configuration_To_View(_BaseStationProperties);
        }

        [TestMethod]
        public void OptionsPresenter_Initialise_Copies_ReceiverLocations_From_Configuration_To_BaseStation_Options_UI()
        {
            var line1 = new ReceiverLocation() { UniqueId = 1, Name = "A", Latitude = 1.2, Longitude = 3.4 };
            var line2 = new ReceiverLocation() { UniqueId = 2, Name = "B", Latitude = 5.6, Longitude = 7.8 };
            _Configuration.ReceiverLocations.AddRange(new ReceiverLocation[] { line1, line2 });

            _Presenter.Initialise(_View.Object);

            Assert.AreEqual(2, _View.Object.RawDecodingReceiverLocations.Count);
            Assert.IsTrue(_View.Object.RawDecodingReceiverLocations.Contains(line1));
            Assert.IsTrue(_View.Object.RawDecodingReceiverLocations.Contains(line2));
        }

        [TestMethod]
        [DataSource("Data Source='LibraryTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "GeneralOptions$")]
        public void OptionsPresenter_Initialise_Copies_Values_From_Configuration_To_General_Options_UI()
        {
            Check_Initialise_Copies_Values_From_Configuration_To_View(_GeneralProperties);
        }

        [TestMethod]
        [DataSource("Data Source='LibraryTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "GoogleMapOptions$")]
        public void OptionsPresenter_Initialise_Copies_Values_From_Configuration_To_GoogleMap_Options_UI()
        {
            Check_Initialise_Copies_Values_From_Configuration_To_View(_GoogleMapOptions);
        }

        [TestMethod]
        [DataSource("Data Source='LibraryTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "InternetClientOptions$")]
        public void OptionsPresenter_Initialise_Copies_Values_From_Configuration_To_InternetClient_Options_UI()
        {
            Check_Initialise_Copies_Values_From_Configuration_To_View(_InternetClientOptions);
        }

        [TestMethod]
        [DataSource("Data Source='LibraryTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "WebServerOptions$")]
        public void OptionsPresenter_Initialise_Copies_Values_From_Configuration_To_WebServer_Options_UI()
        {
            Check_Initialise_Copies_Values_From_Configuration_To_View(_WebServerOptions);
        }

        [TestMethod]
        [DataSource("Data Source='LibraryTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "RawDecodingOptions$")]
        public void OptionsPresenter_Initialise_Copies_Values_From_Configuration_To_RawDecoding_Options_UI()
        {
            Check_Initialise_Copies_Values_From_Configuration_To_View(_RawDecodingOptions);
        }

        [TestMethod]
        public void OptionsPresenter_Initialise_Copies_Empty_Password_Hash_From_Configuration_To_WebServer_Options_UI()
        {
            _Presenter.Initialise(_View.Object);

            Assert.AreEqual(true, _View.Object.WebServerPasswordHasChanged);
        }

        [TestMethod]
        public void OptionsPresenter_Initialise_Copies_NonEmpty_Password_Hash_From_Configuration_To_WebServer_Options_UI()
        {
            _Configuration.WebServerSettings.BasicAuthenticationPasswordHash = new Hash("abc");

            _Presenter.Initialise(_View.Object);

            Assert.AreEqual(false, _View.Object.WebServerPasswordHasChanged);
        }

        [TestMethod]
        public void OptionsPresenter_Initialise_Populates_Audio_Options_Page_With_Voice_Names()
        {
            var names = new List<string>();
            _Provider.Setup(p => p.GetVoiceNames()).Returns(names);

            _Presenter.Initialise(_View.Object);

            _View.Verify(v => v.PopulateTextToSpeechVoices(names), Times.Once());
            _View.Verify(v => v.PopulateTextToSpeechVoices(It.IsAny<IEnumerable<string>>()), Times.Once());
        }

        [TestMethod]
        public void OptionsPresenter_Initialise_Copies_RebroadcastSettings_From_Configuration_To_General_Options_UI()
        {
            var line1 = new RebroadcastSettings() { Enabled = true, Name = "A", Port = 12, Format = RebroadcastFormat.Passthrough };
            var line2 = new RebroadcastSettings() { Enabled = true, Name = "B", Port = 17, Format = RebroadcastFormat.Port30003 };

            _Configuration.RebroadcastSettings.AddRange(new RebroadcastSettings[] { line1, line2 });

            _Presenter.Initialise(_View.Object);

            Assert.AreEqual(2, _View.Object.RebroadcastSettings.Count);
            Assert.IsTrue(_View.Object.RebroadcastSettings.Contains(line1));
            Assert.IsTrue(_View.Object.RebroadcastSettings.Contains(line2));
        }
        #endregion

        #region SaveClicked
        [TestMethod]
        [DataSource("Data Source='LibraryTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "ValidateOptionsView$")]
        public void OptionsPresenter_SaveClicked_Validates_UI_Before_Save()
        {
            DoValidationTest(() => { _View.Raise(v => v.SaveClicked += null, EventArgs.Empty); });
        }

        [TestMethod]
        [DataSource("Data Source='LibraryTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "ValidateOptionsView$")]
        public void OptionsPresenter_SaveClicked_Validates_When_Values_Changed()
        {
            DoValidationTest(() => { _View.Raise(v => v.ValuesChanged += null, EventArgs.Empty); });
        }

        [TestMethod]
        [DataSource("Data Source='LibraryTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "ValidateOptionsView$")]
        public void OptionsPresenter_SaveClicked_Validation_Suppresses_FileName_Check_If_Already_Tested_FileName()
        {
            // Things were getting slow when the same filename keeps getting validated every time a value changes
            // so now the presenter is only expected to test a name if it has not already been tested.
            DoValidationTest(() => { _View.Raise(v => v.ValuesChanged += null, EventArgs.Empty); }, true);
        }

        private void DoValidationTest(Action triggerValidation, bool doSuppressExcessiveFileSystemCheck = false)
        {
            var worksheet = new ExcelWorksheetData(TestContext);

            List<ValidationResult> validationResults = new List<ValidationResult>();
            _View.Setup(v => v.ShowValidationResults(It.IsAny<IEnumerable<ValidationResult>>())).Callback((IEnumerable<ValidationResult> results) => {
                foreach(var validationResult in results) validationResults.Add(validationResult);
            });

            int countFileExistsCalls = 0;
            _Provider.Setup(p => p.FileExists(It.IsAny<string>())).Returns(false);
            _Provider.Setup(p => p.FileExists(null)).Callback(() => {throw new NullReferenceException(); });
            _Provider.Setup(p => p.FileExists("FileExists")).Callback(() => countFileExistsCalls++).Returns(true);

            int countFolderExistsCalls = 0;
            _Provider.Setup(p => p.FolderExists(It.IsAny<string>())).Returns(false);
            _Provider.Setup(p => p.FolderExists(null)).Callback(() => {throw new NullReferenceException(); });
            _Provider.Setup(p => p.FolderExists("FolderExists")).Callback(() => countFolderExistsCalls++).Returns(true);

            _Presenter.Initialise(_View.Object);

            _View.Object.BaseStationDatabaseFileName = null;
            _View.Object.OperatorFlagsFolder = null;
            _View.Object.SilhouettesFolder = null;

            for(var i = 1;i <= 3;++i) {
                var uiFieldColumn = String.Format("UIField{0}", i);
                var valueColumn = String.Format("Value{0}", i);
                if(worksheet.String(uiFieldColumn) != null) {
                    switch(worksheet.String(uiFieldColumn)) {
                        case "AcceptableAirborneSpeed":             _View.Object.RawDecodingAcceptableAirborneSpeed = worksheet.Double(valueColumn); break;
                        case "AcceptableAirSurfaceTransitionSpeed": _View.Object.RawDecodingAcceptableAirSurfaceTransitionSpeed = worksheet.Double(valueColumn); break;
                        case "AcceptableSurfaceSpeed":              _View.Object.RawDecodingAcceptableSurfaceSpeed = worksheet.Double(valueColumn); break;
                        case "AcceptIcaoInNonPICount":              _View.Object.AcceptIcaoInNonPICount = worksheet.Int(valueColumn); break;
                        case "AcceptIcaoInNonPISeconds":            _View.Object.AcceptIcaoInNonPISeconds = worksheet.Int(valueColumn); break;
                        case "AcceptIcaoInPI0Count":                _View.Object.AcceptIcaoInPI0Count = worksheet.Int(valueColumn); break;
                        case "AcceptIcaoInPI0Seconds":              _View.Object.AcceptIcaoInPI0Seconds = worksheet.Int(valueColumn); break;
                        case "AirborneGlobalPositionLimit":         _View.Object.RawDecodingAirborneGlobalPositionLimit = worksheet.Int(valueColumn); break;
                        case "BaseStationAddress":                  _View.Object.BaseStationAddress = worksheet.EString(valueColumn); break;
                        case "BaseStationConnectionType":           _View.Object.BaseStationConnectionType = worksheet.ParseEnum<ConnectionType>(valueColumn); break;
                        case "BaseStationPort":                     _View.Object.BaseStationPort = worksheet.Int(valueColumn); break;
                        case "CheckForNewVersionsPeriodDays":       _View.Object.CheckForNewVersionsPeriodDays = worksheet.Int(valueColumn); break;
                        case "DatabaseFileName":                    _View.Object.BaseStationDatabaseFileName = worksheet.EString(valueColumn); break;
                        case "DisplayTimeoutSeconds":               _View.Object.DisplayTimeoutSeconds = worksheet.Int(valueColumn); break;
                        case "FastSurfaceGlobalPositionLimit":      _View.Object.RawDecodingFastSurfaceGlobalPositionLimit = worksheet.Int(valueColumn); break;
                        case "FlagsFolder":                         _View.Object.OperatorFlagsFolder = worksheet.EString(valueColumn); break;
                        case "InitialGoogleMapLatitude":            _View.Object.InitialGoogleMapLatitude = worksheet.Double(valueColumn); break;
                        case "InitialGoogleMapLongitude":           _View.Object.InitialGoogleMapLongitude = worksheet.Double(valueColumn); break;
                        case "InitialGoogleMapZoom":                _View.Object.InitialGoogleMapZoom = worksheet.Int(valueColumn); break;
                        case "InitialRefreshSeconds":               _View.Object.InitialGoogleMapRefreshSeconds = worksheet.Int(valueColumn); break;
                        case "InternetClientTimeoutMinutes":        _View.Object.InternetClientTimeoutMinutes = worksheet.Int(valueColumn); break;
                        case "MinimumRefreshSeconds":               _View.Object.MinimumGoogleMapRefreshSeconds = worksheet.Int(valueColumn); break;
                        case "PicturesFolder":                      _View.Object.PicturesFolder = worksheet.EString(valueColumn); break;
                        case "ReceiverRange":                       _View.Object.RawDecodingReceiverRange = worksheet.Int(valueColumn); break;
                        case "SerialBaudRate":                      _View.Object.SerialBaudRate = worksheet.Int(valueColumn); break;
                        case "SerialComPort":                       _View.Object.SerialComPort = worksheet.EString(valueColumn); break;
                        case "SerialDataBits":                      _View.Object.SerialDataBits = worksheet.Int(valueColumn); break;
                        case "ShortTrailLengthSeconds":             _View.Object.ShortTrailLengthSeconds = worksheet.Int(valueColumn); break;
                        case "SilhouettesFolder":                   _View.Object.SilhouettesFolder = worksheet.EString(valueColumn); break;
                        case "SlowSurfaceGlobalPositionLimit":      _View.Object.RawDecodingSlowSurfaceGlobalPositionLimit = worksheet.Int(valueColumn); break;
                        case "TextToSpeechSpeed":                   _View.Object.TextToSpeechSpeed = worksheet.Int(valueColumn); break;
                        case "TrackingTimeoutSeconds":              _View.Object.TrackingTimeoutSeconds = worksheet.Int(valueColumn); break;
                        case "UPnpPort":                            _View.Object.UPnpPort = worksheet.Int(valueColumn); break;
                        case "WebAuthenticateUser":                 _View.Object.WebServerUserMustAuthenticate = worksheet.Bool(valueColumn); break;
                        case "WebUserName":                         _View.Object.WebServerUserName = worksheet.EString(valueColumn); break;
                        default:                                    throw new NotImplementedException();
                    }
                }
            }

            triggerValidation();
            _View.Verify(v => v.ShowValidationResults(It.IsAny<IEnumerable<ValidationResult>>()), Times.Once());

            if(doSuppressExcessiveFileSystemCheck) {
                validationResults.Clear();

                triggerValidation();
                _View.Verify(v => v.ShowValidationResults(It.IsAny<IEnumerable<ValidationResult>>()), Times.Exactly(2));

                Assert.IsTrue(countFileExistsCalls < 2);
                Assert.IsTrue(countFolderExistsCalls < 2);
            }

            var validationErrorSummary = new StringBuilder();
            foreach(var validationResult in validationResults) {
                if(validationErrorSummary.Length != 0) validationErrorSummary.Append("; ");
                validationErrorSummary.AppendFormat("{0}:{1}", validationResult.Field, validationResult.Message);
            }

            Assert.AreEqual(worksheet.Int("CountErrors"), validationResults.Count(), validationErrorSummary.ToString());
            if(validationResults.Count() > 0) {
                Assert.IsTrue(validationResults.Where(r => r.Field == worksheet.ParseEnum<ValidationField>("Field") &&
                                                           r.Message == worksheet.EString("Message") &&
                                                           r.IsWarning == worksheet.Bool("IsWarning")).Any(),
                              validationErrorSummary.ToString());
            }
        }

        [TestMethod]
        public void OptionsPresenter_SaveClicked_Will_Revalidate_FileSystem_Entries_If_They_Change()
        {
            int countCheckExistsCalls = 0;
            _Provider.Setup(p => p.FileExists(It.IsAny<string>())).Callback(() => ++countCheckExistsCalls).Returns(false);
            _Provider.Setup(p => p.FolderExists(It.IsAny<string>())).Callback(() => ++countCheckExistsCalls).Returns(false);

            _Presenter.Initialise(_View.Object);
            _View.Object.BaseStationDatabaseFileName = null;
            _View.Object.OperatorFlagsFolder = null;
            _View.Object.SilhouettesFolder = null;
            _View.Object.PicturesFolder = null;

            foreach(var fillFileSystemValue in new Action<string>[] {
                (s) => { _View.Object.BaseStationDatabaseFileName = s; },
                (s) => { _View.Object.OperatorFlagsFolder = s; },
                (s) => { _View.Object.SilhouettesFolder = s; },
                (s) => { _View.Object.PicturesFolder = s; } }) {

                countCheckExistsCalls = 0;

                fillFileSystemValue("DNE1");
                _View.Raise(v => v.ValuesChanged += null, EventArgs.Empty);
                Assert.AreEqual(1, countCheckExistsCalls);

                _View.Raise(v => v.ValuesChanged += null, EventArgs.Empty);
                Assert.AreEqual(1, countCheckExistsCalls);

                fillFileSystemValue("DNE2");
                _View.Raise(v => v.ValuesChanged += null, EventArgs.Empty);
                Assert.AreEqual(2, countCheckExistsCalls);

                fillFileSystemValue(null);
            }
        }

        [TestMethod]
        public void OptionsPresenter_SaveClicked_Failed_Validation_Prevents_Overwriting_Of_Configuration()
        {
            _Configuration.BaseStationSettings.Address = "An address";
            _Presenter.Initialise(_View.Object);
            _View.Object.BaseStationAddress = null;

            _View.Raise(v => v.SaveClicked += null, EventArgs.Empty);

            Assert.AreEqual("An address", _Configuration.BaseStationSettings.Address);
        }

        [TestMethod]
        public void OptionsPresenter_SaveClicked_Failed_Validation_Prevents_Save_Of_Configuration()
        {
            _Presenter.Initialise(_View.Object);
            _View.Object.BaseStationAddress = null;

            _View.Raise(v => v.SaveClicked += null, EventArgs.Empty);

            _ConfigurationStorage.Verify(c => c.Save(It.IsAny<Configuration>()), Times.Never());
        }

        [TestMethod]
        public void OptionsPresenter_SaveClicked_Validation_With_Only_Warnings_Allows_Save_Of_Configuration()
        {
            _Presenter.Initialise(_View.Object);
            _View.Object.SilhouettesFolder = "Does not exist";
            _Provider.Setup(p => p.FileExists(It.IsAny<string>())).Returns(false);

            _View.Raise(v => v.SaveClicked += null, EventArgs.Empty);

            _ConfigurationStorage.Verify(c => c.Save(It.IsAny<Configuration>()), Times.Once());
        }

        [TestMethod]
        [DataSource("Data Source='LibraryTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "BaseStationOptions$")]
        public void OptionsPresenter_SaveClicked_Copies_Values_From_BaseStation_Options_UI_To_Configuration_Before_Save()
        {
            Check_SaveClicked_Copies_Values_From_View_To_Configuration(_BaseStationProperties);
        }

        [TestMethod]
        public void OptionsPresenter_SaveClicked_Copies_ReceiverLocations_From_BaseStation_Options_UI_To_Configuration_Before_Save()
        {
            _Configuration.ReceiverLocations.Add(new ReceiverLocation() { UniqueId = 3, Name = "Old garbage, should be cleared", Latitude = 1, Longitude = 2 });
            _Presenter.Initialise(_View.Object);

            var line1 = new ReceiverLocation() { UniqueId = 1, Name = "A", Latitude = 1.2, Longitude = 3.4 };
            var line2 = new ReceiverLocation() { UniqueId = 2, Name = "B", Latitude = 5.6, Longitude = 7.8 };
            _View.Object.RawDecodingReceiverLocations.Clear();
            _View.Object.RawDecodingReceiverLocations.AddRange(new ReceiverLocation[] { line1, line2 });

            _ConfigurationStorage.Setup(c => c.Save(_Configuration)).Callback(() => {
                Assert.AreEqual(2, _Configuration.ReceiverLocations.Count);
                Assert.IsTrue(_Configuration.ReceiverLocations.Contains(line1));
                Assert.IsTrue(_Configuration.ReceiverLocations.Contains(line2));
            });

            _View.Raise(v => v.SaveClicked += null, EventArgs.Empty);

            _ConfigurationStorage.Verify(c => c.Save(_Configuration), Times.Once());
        }

        [TestMethod]
        [DataSource("Data Source='LibraryTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "AudioOptions$")]
        public void OptionsPresenter_SaveClicked_Copies_Values_From_Audio_Options_UI_To_Configuration_Before_Save()
        {
            Check_SaveClicked_Copies_Values_From_View_To_Configuration(_AudioProperties);
        }

        [TestMethod]
        [DataSource("Data Source='LibraryTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "GeneralOptions$")]
        public void OptionsPresenter_SaveClicked_Copies_Values_From_General_Options_UI_To_Configuration_Before_Save()
        {
            Check_SaveClicked_Copies_Values_From_View_To_Configuration(_GeneralProperties);
        }

        [TestMethod]
        public void OptionsPresenter_SaveClicked_Copies_RebroadcastSettings_From_Options_UI_To_Configuration_Before_Save()
        {
            _Configuration.RebroadcastSettings.Add(new RebroadcastSettings() { Enabled = true, Name = "Will be deleted", Format = RebroadcastFormat.Passthrough, Port = 100 });
            _Presenter.Initialise(_View.Object);

            var line1 = new RebroadcastSettings() { Enabled = true, Name = "X1", Format = RebroadcastFormat.Passthrough, Port = 9000 };
            var line2 = new RebroadcastSettings() { Enabled = false, Name = "Y1", Format = RebroadcastFormat.Port30003, Port = 9001 };
            _View.Object.RebroadcastSettings.Clear();
            _View.Object.RebroadcastSettings.AddRange(new RebroadcastSettings[] { line1, line2 });

            _ConfigurationStorage.Setup(c => c.Save(_Configuration)).Callback(() => {
                Assert.AreEqual(2, _Configuration.RebroadcastSettings.Count);
                Assert.IsTrue(_Configuration.RebroadcastSettings.Contains(line1));
                Assert.IsTrue(_Configuration.RebroadcastSettings.Contains(line2));
            });

            _View.Raise(v => v.SaveClicked += null, EventArgs.Empty);

            _ConfigurationStorage.Verify(c => c.Save(_Configuration), Times.Once());
        }

        [TestMethod]
        [DataSource("Data Source='LibraryTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "GoogleMapOptions$")]
        public void OptionsPresenter_SaveClicked_Copies_Values_From_GoogleMap_Options_UI_To_Configuration_Before_Save()
        {
            Check_SaveClicked_Copies_Values_From_View_To_Configuration(_GoogleMapOptions);
        }

        [TestMethod]
        [DataSource("Data Source='LibraryTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "InternetClientOptions$")]
        public void OptionsPresenter_SaveClicked_Copies_Values_From_InternetClient_Options_UI_To_Configuration_Before_Save()
        {
            Check_SaveClicked_Copies_Values_From_View_To_Configuration(_InternetClientOptions);
        }

        [TestMethod]
        [DataSource("Data Source='LibraryTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "WebServerOptions$")]
        public void OptionsPresenter_SaveClicked_Copies_Values_From_WebServer_Options_UI_To_Configuration_Before_Save()
        {
            Check_SaveClicked_Copies_Values_From_View_To_Configuration(_WebServerOptions);
        }

        [TestMethod]
        [DataSource("Data Source='LibraryTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "RawDecodingOptions$")]
        public void OptionsPresenter_SaveClicked_Copies_Values_From_RawDecoding_Options_UI_To_Configuration_Before_Save()
        {
            Check_SaveClicked_Copies_Values_From_View_To_Configuration(_RawDecodingOptions);
        }

        [TestMethod]
        public void OptionsPresenter_SaveClicked_Copies_WebServer_Password_Hash_Correctly_If_User_Enters_Null_Password()
        {
            _Configuration.WebServerSettings.BasicAuthenticationPasswordHash = new Hash("Old password");
            _Presenter.Initialise(_View.Object);

            _View.Object.WebServerPasswordHasChanged = true;
            _View.Object.WebServerPassword = null;

            _View.Raise(v => v.SaveClicked += null, EventArgs.Empty);

            Assert.AreEqual(null, _Configuration.WebServerSettings.BasicAuthenticationPasswordHash);
        }

        [TestMethod]
        public void OptionsPresenter_SaveClicked_Copies_WebServer_Password_Hash_Correctly_If_User_Enters_Empty_Password()
        {
            _Configuration.WebServerSettings.BasicAuthenticationPasswordHash = new Hash("Old password");
            _Presenter.Initialise(_View.Object);

            _View.Object.WebServerPasswordHasChanged = true;
            _View.Object.WebServerPassword = "";

            _View.Raise(v => v.SaveClicked += null, EventArgs.Empty);

            Assert.IsTrue(_Configuration.WebServerSettings.BasicAuthenticationPasswordHash.PasswordMatches(""));
        }

        [TestMethod]
        public void OptionsPresenter_SaveClicked_Copies_WebServer_Password_Hash_Correctly_If_User_Enters_NonEmpty_Password()
        {
            _Presenter.Initialise(_View.Object);

            _View.Object.WebServerPasswordHasChanged = true;
            _View.Object.WebServerPassword = "hello";

            _View.Raise(v => v.SaveClicked += null, EventArgs.Empty);

            Assert.IsTrue(_Configuration.WebServerSettings.BasicAuthenticationPasswordHash.PasswordMatches("hello"));
        }

        [TestMethod]
        public void OptionsPresenter_SaveClicked_Copies_WebServer_Password_Hash_Correctly_If_User_Does_Not_Change_Password()
        {
            _Configuration.WebServerSettings.BasicAuthenticationPasswordHash = new Hash("Blomp");
            _Presenter.Initialise(_View.Object);

            _View.Raise(v => v.SaveClicked += null, EventArgs.Empty);

            Assert.IsTrue(_Configuration.WebServerSettings.BasicAuthenticationPasswordHash.PasswordMatches("Blomp"));
        }
        #endregion

        #region ResetToDefaultsClicked
        [TestMethod]
        public void OptionsPresenter_ResetToDefaultsClicked_Does_Not_Save_Default_Values()
        {
            _Presenter.Initialise(_View.Object);
            _View.Raise(v => v.ResetToDefaultsClicked += null, EventArgs.Empty);

            _ConfigurationStorage.Verify(c => c.Save(It.IsAny<Configuration>()), Times.Never());
        }

        [TestMethod]
        [DataSource("Data Source='LibraryTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "BaseStationOptions$")]
        public void OptionsPresenter_ResetToDefaultsClicked_Copies_Default_BaseStation_Configuration_To_UI()
        {
            Check_ResetToDefaultClicked_Copies_Default_Configuration_To_UI(_BaseStationProperties);
        }

        [TestMethod]
        [DataSource("Data Source='LibraryTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "AudioOptions$")]
        public void OptionsPresenter_ResetToDefaultsClicked_Copies_Default_Audio_Configuration_To_UI()
        {
            Check_ResetToDefaultClicked_Copies_Default_Configuration_To_UI(_AudioProperties);
        }

        [TestMethod]
        [DataSource("Data Source='LibraryTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "GeneralOptions$")]
        public void OptionsPresenter_ResetToDefaultsClicked_Copies_Default_General_Configuration_To_UI()
        {
            Check_ResetToDefaultClicked_Copies_Default_Configuration_To_UI(_GeneralProperties);
        }

        [TestMethod]
        [DataSource("Data Source='LibraryTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "GoogleMapOptions$")]
        public void OptionsPresenter_ResetToDefaultsClicked_Copies_Default_GoogleMap_Configuration_To_UI()
        {
            Check_ResetToDefaultClicked_Copies_Default_Configuration_To_UI(_GoogleMapOptions);
        }

        [TestMethod]
        [DataSource("Data Source='LibraryTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "InternetClientOptions$")]
        public void OptionsPresenter_ResetToDefaultsClicked_Copies_Default_InternetClient_Configuration_To_UI()
        {
            Check_ResetToDefaultClicked_Copies_Default_Configuration_To_UI(_InternetClientOptions);
        }

        [TestMethod]
        [DataSource("Data Source='LibraryTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "WebServerOptions$")]
        public void OptionsPresenter_ResetToDefaultsClicked_Copies_Default_WebServer_Configuration_To_UI()
        {
            Check_ResetToDefaultClicked_Copies_Default_Configuration_To_UI(_WebServerOptions);
        }

        [TestMethod]
        [DataSource("Data Source='LibraryTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "RawDecodingOptions$")]
        public void OptionsPresenter_ResetToDefaultsClicked_Copies_Default_RawDecoding_Configuration_To_UI()
        {
            Check_ResetToDefaultClicked_Copies_Default_Configuration_To_UI(_RawDecodingOptions);
        }

        [TestMethod]
        public void OptionsPresenter_ResetToDefaultsClicked_Does_Not_Delete_ReceiverLocations()
        {
            // Personally I would be more than a little upset to enter half a dozen locations and then find that the button
            // had wiped them all out! :) We should preserve the receiver locations when the user clicks reset to default.
            var line1 = new ReceiverLocation() { UniqueId = 1, Name = "A", Latitude = 1.2, Longitude = 3.4 };
            var line2 = new ReceiverLocation() { UniqueId = 2, Name = "B", Latitude = 5.6, Longitude = 7.8 };
            _Configuration.ReceiverLocations.AddRange(new ReceiverLocation[] { line1, line2 });

            _Presenter.Initialise(_View.Object);
            _View.Raise(v => v.ResetToDefaultsClicked += null, EventArgs.Empty);

            Assert.AreEqual(2, _View.Object.RawDecodingReceiverLocations.Count);
            Assert.IsTrue(_View.Object.RawDecodingReceiverLocations.Contains(line1));
            Assert.IsTrue(_View.Object.RawDecodingReceiverLocations.Contains(line2));
        }
        #endregion

        #region UseIcaoRawDecodingSettingsClicked
        [TestMethod]
        public void OptionsPresenter_UseIcaoRawDecodingSettingsClicked_Fills_View_With_Recommended_ICAO_Settings_For_Raw_Decoding()
        {
            _Presenter.Initialise(_View.Object);

            _View.Object.RawDecodingAcceptableAirborneSpeed = 9999;
            _View.Object.RawDecodingAcceptableAirSurfaceTransitionSpeed = 9999;
            _View.Object.RawDecodingAcceptableSurfaceSpeed = 9999;
            _View.Object.RawDecodingAirborneGlobalPositionLimit = 9999;
            _View.Object.RawDecodingFastSurfaceGlobalPositionLimit = 9999;
            _View.Object.RawDecodingSlowSurfaceGlobalPositionLimit = 9999;
            _View.Object.RawDecodingSuppressReceiverRangeCheck = true;
            _View.Object.RawDecodingUseLocalDecodeForInitialPosition = true;

            _View.Raise(v => v.UseIcaoRawDecodingSettingsClicked += null, EventArgs.Empty);

            Assert.AreEqual(11.112, _View.Object.RawDecodingAcceptableAirborneSpeed);
            Assert.AreEqual(4.63, _View.Object.RawDecodingAcceptableAirSurfaceTransitionSpeed);
            Assert.AreEqual(1.389, _View.Object.RawDecodingAcceptableSurfaceSpeed);
            Assert.AreEqual(10, _View.Object.RawDecodingAirborneGlobalPositionLimit);
            Assert.AreEqual(25, _View.Object.RawDecodingFastSurfaceGlobalPositionLimit);
            Assert.AreEqual(50, _View.Object.RawDecodingSlowSurfaceGlobalPositionLimit);
            Assert.AreEqual(false, _View.Object.RawDecodingSuppressReceiverRangeCheck);
            Assert.AreEqual(false, _View.Object.RawDecodingUseLocalDecodeForInitialPosition);
        }
        #endregion

        #region UseRecommendedRawDecodingSettingsClicked
        [TestMethod]
        public void OptionsPresenter_UseRecommendedRawDecodingSettingsClicked_Fills_View_With_Default_Settings_For_Raw_Decoding()
        {
            _Presenter.Initialise(_View.Object);

            _View.Object.RawDecodingAcceptableAirborneSpeed = 9999;
            _View.Object.RawDecodingAcceptableAirSurfaceTransitionSpeed = 9999;
            _View.Object.RawDecodingAcceptableSurfaceSpeed = 9999;
            _View.Object.RawDecodingAirborneGlobalPositionLimit = 9999;
            _View.Object.RawDecodingFastSurfaceGlobalPositionLimit = 9999;
            _View.Object.RawDecodingSlowSurfaceGlobalPositionLimit = 9999;
            _View.Object.RawDecodingSuppressReceiverRangeCheck = false;
            _View.Object.RawDecodingUseLocalDecodeForInitialPosition = true;

            _View.Raise(v => v.UseRecommendedRawDecodingSettingsClicked += null, EventArgs.Empty);

            var defaultValue = new RawDecodingSettings();

            Assert.AreEqual(defaultValue.AcceptableAirborneSpeed, _View.Object.RawDecodingAcceptableAirborneSpeed);
            Assert.AreEqual(defaultValue.AcceptableAirSurfaceTransitionSpeed, _View.Object.RawDecodingAcceptableAirSurfaceTransitionSpeed);
            Assert.AreEqual(defaultValue.AcceptableSurfaceSpeed, _View.Object.RawDecodingAcceptableSurfaceSpeed);
            Assert.AreEqual(defaultValue.AirborneGlobalPositionLimit, _View.Object.RawDecodingAirborneGlobalPositionLimit);
            Assert.AreEqual(defaultValue.FastSurfaceGlobalPositionLimit, _View.Object.RawDecodingFastSurfaceGlobalPositionLimit);
            Assert.AreEqual(defaultValue.SlowSurfaceGlobalPositionLimit, _View.Object.RawDecodingSlowSurfaceGlobalPositionLimit);
            Assert.AreEqual(true, _View.Object.RawDecodingSuppressReceiverRangeCheck);
            Assert.AreEqual(false, _View.Object.RawDecodingUseLocalDecodeForInitialPosition);
        }
        #endregion

        #region TestConnectionClicked
        [TestMethod]
        public void OptionsPresenter_TestConnectionClicked_Shows_Correct_Result_When_Network_Connection_Works()
        {
            _Presenter.Initialise(_View.Object);

            _View.Object.BaseStationConnectionType = ConnectionType.TCP;
            _View.Object.BaseStationAddress = "my address";
            _View.Object.BaseStationPort = 100;

            _View.Raise(v => v.TestConnectionClicked += null, EventArgs.Empty);

            _Provider.Verify(p => p.TestNetworkConnection(It.IsAny<string>(), It.IsAny<int>()), Times.Once());
            _Provider.Verify(p => p.TestNetworkConnection("my address", 100), Times.Once());

            _View.Verify(v => v.ShowTestConnectionResults(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            _View.Verify(v => v.ShowTestConnectionResults(Strings.CanConnectWithSettings, Strings.ConnectedSuccessfully), Times.Once());
        }

        [TestMethod]
        public void OptionsPresenter_TestConnectionClicked_Shows_Correct_Result_When_Serial_Connection_Works()
        {
            _Presenter.Initialise(_View.Object);

            _View.Object.BaseStationConnectionType = ConnectionType.COM;
            _View.Object.SerialBaudRate = 2400;
            _View.Object.SerialComPort = "COM99";
            _View.Object.SerialDataBits = 8;
            _View.Object.SerialHandshake = Handshake.RequestToSendXOnXOff;
            _View.Object.SerialParity = Parity.Mark;
            _View.Object.SerialStopBits = StopBits.One;

            _View.Raise(v => v.TestConnectionClicked += null, EventArgs.Empty);

            _Provider.Verify(p => p.TestSerialConnection(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<StopBits>(), It.IsAny<Parity>(), It.IsAny<Handshake>()), Times.Once());
            _Provider.Verify(p => p.TestSerialConnection("COM99", 2400, 8, StopBits.One, Parity.Mark, Handshake.RequestToSendXOnXOff), Times.Once());

            _View.Verify(v => v.ShowTestConnectionResults(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            _View.Verify(v => v.ShowTestConnectionResults(Strings.CanConnectWithSettings, Strings.ConnectedSuccessfully), Times.Once());
        }

        [TestMethod]
        public void OptionsPresenter_TestConnectionClicked_Shows_Correct_Result_When_Network_Connection_Does_Not_Work()
        {
            _Presenter.Initialise(_View.Object);

            _View.Object.BaseStationConnectionType = ConnectionType.TCP;
            _View.Object.BaseStationAddress = "addr";
            _View.Object.BaseStationPort = 10021;

            var exception = new InvalidOperationException("Exception text");
            _Provider.Setup(p => p.TestNetworkConnection(It.IsAny<string>(), It.IsAny<int>())).Returns(exception);

            _View.Raise(v => v.TestConnectionClicked += null, EventArgs.Empty);

            _View.Verify(v => v.ShowTestConnectionResults(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            _View.Verify(v => v.ShowTestConnectionResults(String.Format("{0} {1}", Strings.CannotConnectWithSettings, exception.Message), Strings.CannotConnect), Times.Once());
        }

        [TestMethod]
        public void OptionsPresenter_TestConnectionClicked_Shows_Correct_Result_When_Serial_Connection_Does_Not_Work()
        {
            _Presenter.Initialise(_View.Object);

            _View.Object.BaseStationConnectionType = ConnectionType.COM;
            _View.Object.SerialBaudRate = 2400;
            _View.Object.SerialComPort = "COM99";
            _View.Object.SerialDataBits = 8;
            _View.Object.SerialHandshake = Handshake.RequestToSendXOnXOff;
            _View.Object.SerialParity = Parity.Mark;
            _View.Object.SerialStopBits = StopBits.One;

            var exception = new InvalidOperationException("Exception text");
            _Provider.Setup(p => p.TestSerialConnection(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<StopBits>(), It.IsAny<Parity>(), It.IsAny<Handshake>())).Returns(exception);

            _View.Raise(v => v.TestConnectionClicked += null, EventArgs.Empty);

            _View.Verify(v => v.ShowTestConnectionResults(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            _View.Verify(v => v.ShowTestConnectionResults(String.Format("{0} {1}", Strings.CannotConnectWithSettings, exception.Message), Strings.CannotConnect), Times.Once());
        }

        [TestMethod]
        public void OptionsPresenter_TestConnectionClicked_Shows_GUI_Is_Busy_While_Testing()
        {
            var previousState = new Object();
            _View.Setup(v => v.ShowBusy(It.IsAny<bool>(), It.IsAny<object>())).Returns((bool isBusy, object prev) => {
                return isBusy ? previousState : null;
            });

            _Provider.Setup(p => p.TestNetworkConnection(It.IsAny<string>(), It.IsAny<int>())).Callback(() => {
                _View.Verify(v => v.ShowBusy(true, null), Times.Once());
            });

            _View.Setup(v => v.ShowTestConnectionResults(It.IsAny<string>(), It.IsAny<string>())).Callback(() => {
                _View.Verify(v => v.ShowBusy(false, previousState), Times.Once());
            });

            _Presenter.Initialise(_View.Object);
            _View.Object.BaseStationConnectionType = ConnectionType.TCP;

            _View.Raise(v => v.TestConnectionClicked += null, EventArgs.Empty);

            _View.Verify(v => v.ShowBusy(It.IsAny<bool>(), It.IsAny<object>()), Times.Exactly(2));
        }
        #endregion

        #region TestTextToSpeechSettingsClicked
        [TestMethod]
        public void OptionsPresenter_TestTextToSpeechSettingsClicked_Runs_Text_To_Speech_Test()
        {
            _Presenter.Initialise(_View.Object);

            _View.Object.TextToSpeechVoice = "the voice";
            _View.Object.TextToSpeechSpeed = 90;
            _View.Raise(v => v.TestTextToSpeechSettingsClicked += null, EventArgs.Empty);

            _Provider.Verify(p => p.TestTextToSpeech("the voice", 90), Times.Once());
            _Provider.Verify(p => p.TestTextToSpeech(It.IsAny<string>(), It.IsAny<int>()), Times.Once());
        }
        #endregion
    }
}
