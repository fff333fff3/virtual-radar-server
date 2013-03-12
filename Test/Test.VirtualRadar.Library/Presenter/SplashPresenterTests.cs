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
using System.Net;
using System.Net.Sockets;
using System.Text;
using InterfaceFactory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Test.Framework;
using VirtualRadar.Interface;
using VirtualRadar.Interface.BaseStation;
using VirtualRadar.Interface.Database;
using VirtualRadar.Interface.Listener;
using VirtualRadar.Interface.Presenter;
using VirtualRadar.Interface.Settings;
using VirtualRadar.Interface.StandingData;
using VirtualRadar.Interface.View;
using VirtualRadar.Interface.WebServer;
using VirtualRadar.Interface.WebSite;
using VirtualRadar.Localisation;

namespace Test.VirtualRadar.Library.Presenter
{
    [TestClass]
    public class SplashPresenterTests
    {
        #region TestContext, Fields, TestInitialise, TestCleanup
        public TestContext TestContext { get; set; }

        private IClassFactory _ClassFactorySnapshot;
        private ISplashPresenter _Presenter;
        private Mock<ISplashPresenterProvider> _Provider;
        private Mock<ISplashView> _View;

        private Configuration _Configuration;
        private Mock<IConfigurationStorage> _ConfigurationStorage;
        private Mock<ILog> _Log;
        private Mock<IHeartbeatService> _HearbeatService;
        private Mock<IBaseStationDatabase> _BaseStationDatabase;
        private Mock<IAutoConfigBaseStationDatabase> _AutoConfigBaseStationDatabase;
        private Mock<IStandingDataManager> _StandingDataManager;
        private Mock<IAutoConfigListener> _AutoConfigListener;
        private Mock<IListener> _Listener;
        private Mock<IBaseStationAircraftList> _BaseStationAircraftList;
        private Mock<IWebServer> _WebServer;
        private Mock<IAutoConfigWebServer> _AutoConfigWebServer;
        private Mock<IWebSite> _WebSite;
        private Mock<ISimpleAircraftList> _FlightSimulatorXAircraftList;
        private Mock<IUniversalPlugAndPlayManager> _UniversalPlugAndPlayManager;
        private Mock<IConnectionLogger> _ConnectionLogger;
        private Mock<ILogDatabase> _LogDatabase;
        private Mock<IBackgroundDataDownloader> _BackgroundDataDownloader;
        private Mock<IPluginManager> _PluginManager;
        private Mock<IApplicationInformation> _ApplicationInformation;
        private Mock<IAutoConfigPictureFolderCache> _AutoConfigPictureFolderCache;
        private Mock<IRebroadcastServerManager> _RebroadcastServerManager;
        private Mock<IStatistics> _Statistics;

        private EventRecorder<EventArgs<Exception>> _BackgroundExceptionEvent;

        public interface IPluginBackgroundThreadCatcher : IPlugin, IBackgroundThreadExceptionCatcher
        {
        }

        [TestInitialize]
        public void TestInitialise()
        {
            _ClassFactorySnapshot = Factory.TakeSnapshot();

            _ConfigurationStorage = TestUtilities.CreateMockSingleton<IConfigurationStorage>();
            _Configuration = new Configuration();
            _ConfigurationStorage.Setup(c => c.Load()).Returns(_Configuration);

            _AutoConfigListener = TestUtilities.CreateMockSingleton<IAutoConfigListener>();
            _AutoConfigListener.Setup(r => r.Listener).Returns((IListener)null);
            _AutoConfigListener.Setup(r => r.Initialise()).Callback(() => { _AutoConfigListener.Setup(r => r.Listener).Returns(_Listener.Object); });

            _Log = TestUtilities.CreateMockSingleton<ILog>();
            _HearbeatService = TestUtilities.CreateMockSingleton<IHeartbeatService>();
            _StandingDataManager = TestUtilities.CreateMockSingleton<IStandingDataManager>();
            _Listener = new Mock<IListener>(MockBehavior.Default) { DefaultValue = DefaultValue.Mock };
            _BaseStationAircraftList = TestUtilities.CreateMockImplementation<IBaseStationAircraftList>();
            _AutoConfigWebServer = TestUtilities.CreateMockSingleton<IAutoConfigWebServer>();
            _WebServer = new Mock<IWebServer>() { DefaultValue = DefaultValue.Mock }.SetupAllProperties();
            _AutoConfigWebServer.Setup(s => s.WebServer).Returns(_WebServer.Object);
            _WebSite = TestUtilities.CreateMockImplementation<IWebSite>();
            _FlightSimulatorXAircraftList = TestUtilities.CreateMockImplementation<ISimpleAircraftList>();
            _UniversalPlugAndPlayManager = TestUtilities.CreateMockImplementation<IUniversalPlugAndPlayManager>();
            _ConnectionLogger = TestUtilities.CreateMockSingleton<IConnectionLogger>();
            _LogDatabase = TestUtilities.CreateMockSingleton<ILogDatabase>();
            _BackgroundDataDownloader = TestUtilities.CreateMockSingleton<IBackgroundDataDownloader>();
            _PluginManager = TestUtilities.CreateMockSingleton<IPluginManager>();
            _ApplicationInformation = TestUtilities.CreateMockImplementation<IApplicationInformation>();
            _AutoConfigPictureFolderCache = TestUtilities.CreateMockSingleton<IAutoConfigPictureFolderCache>();
            _RebroadcastServerManager = TestUtilities.CreateMockSingleton<IRebroadcastServerManager>();
            _Statistics = TestUtilities.CreateMockSingleton<IStatistics>();

            _BackgroundExceptionEvent = new EventRecorder<EventArgs<Exception>>();

            _BaseStationDatabase = new Mock<IBaseStationDatabase>() { DefaultValue = DefaultValue.Mock }.SetupAllProperties();
            _AutoConfigBaseStationDatabase = TestUtilities.CreateMockSingleton<IAutoConfigBaseStationDatabase>();
            _AutoConfigBaseStationDatabase.Setup(a => a.Database).Returns(_BaseStationDatabase.Object);
            _BaseStationDatabase.Setup(d => d.FileName).Returns("x");
            _BaseStationDatabase.Setup(d => d.TestConnection()).Returns(true);

            _Provider = new Mock<ISplashPresenterProvider>() { DefaultValue = DefaultValue.Mock }.SetupAllProperties();
            _Provider.Setup(p => p.FolderExists(It.IsAny<string>())).Returns(true);

            _Presenter = Factory.Singleton.Resolve<ISplashPresenter>();
            _Presenter.Provider = _Provider.Object;

            _View = new Mock<ISplashView>() { DefaultValue = DefaultValue.Mock }.SetupAllProperties();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Factory.RestoreSnapshot(_ClassFactorySnapshot);
        }
        #endregion

        #region Constructor
        [TestMethod]
        public void SplashPresenter_Constructor_Initialises_To_Known_State_And_Properties_Work()
        {
            _Presenter = Factory.Singleton.Resolve<ISplashPresenter>();

            Assert.IsNotNull(_Presenter.Provider);
            TestUtilities.TestProperty(_Presenter, "Provider", _Presenter.Provider, _Provider.Object);
        }
        #endregion

        #region Initialise
        [TestMethod]
        public void SplashPresenter_Initialise_Sets_Application_Title()
        {
            _Presenter.Initialise(_View.Object);

            Assert.AreEqual(Strings.VirtualRadarServer, _View.Object.ApplicationName);
        }

        [TestMethod]
        public void SplashPresenter_Initialise_Sets_Application_Version()
        {
            _ApplicationInformation.Setup(p => p.ShortVersion).Returns("1.2.3");
            _Presenter.Initialise(_View.Object);

            Assert.AreEqual("1.2.3", _View.Object.ApplicationVersion);
        }

        [TestMethod]
        public void SplashPresenter_Initialise_Initialises_Statistics()
        {
            _Presenter.Initialise(_View.Object);

            _Statistics.Verify(r => r.Initialise(), Times.Once());
        }
        #endregion

        #region StartApplication
        #region Parsing command-line arguments
        [TestMethod]
        public void SplashPresenter_StartApplication_Reports_Parsing_Command_Line_Parameters()
        {
            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _View.Verify(v => v.ReportProgress(Strings.SplashScreenParsingCommandLineParameters), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Reports_Problems_With_Unknown_Command_Line_Parameters()
        {
            foreach(string parameter in new string[] { "culture", "-culture", "workingFolder", "-workingFolder", "-SHOWCONFIGFOLDER" }) {
                TestCleanup();
                TestInitialise();

                _Presenter.Initialise(_View.Object);
                _Presenter.CommandLineArgs = new string[] { parameter };
                _Presenter.StartApplication();

                _View.Verify(v => v.ReportProblem(String.Format(Strings.UnrecognisedCommandLineParameterFull, parameter), Strings.UnrecognisedCommandLineParameterTitle, true), Times.Once());
            }
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Does_Not_Stop_On_Acceptable_Command_Line_Parameters()
        {
            foreach(string parameter in new string[] { "-culture:", "-culture:X", "-culture:de-DE", "-CULTURE:en-US", "-WORKINGFOLDER:X", "-showConfigFolder" }) {
                TestCleanup();
                TestInitialise();

                _Presenter.Initialise(_View.Object);
                _Presenter.CommandLineArgs = new string[] { parameter };
                _Presenter.StartApplication();

                _View.Verify(v => v.ReportProblem(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never());
            }
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Stops_Application_Working_Folder_Command_Line_Argument_Specifies_Invalid_Folder()
        {
            _Presenter.CommandLineArgs = new string[] { "-workingfolder:x" };
            _Provider.Setup(p => p.FolderExists("x")).Returns(false);

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _View.Verify(v => v.ReportProblem(String.Format(Strings.FolderDoesNotExistFull, "x"), Strings.FolderDoesNotExistTitle, true), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Overrides_Configuration_Folder_If_Command_Line_Argument_Requests_It()
        {
            _Presenter.CommandLineArgs = new string[] { "-workingfolder:x" };
            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            Assert.AreEqual("x", _ConfigurationStorage.Object.Folder);
        }
        #endregion

        #region Initialising the log
        [TestMethod]
        public void SplashPresenter_StartApplication_Reports_Initialising_The_Log()
        {
            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _View.Verify(v => v.ReportProgress(Strings.SplashScreenInitialisingLog), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Truncates_The_Log()
        {
            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _Log.Verify(g => g.Truncate(100), Times.Once());
            _Log.Verify(g => g.Truncate(It.IsAny<int>()), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Records_Startup_In_Log()
        {
            _ApplicationInformation.Setup(p => p.FullVersion).Returns("5.4.3.2");
            _ConfigurationStorage.Setup(c => c.Folder).Returns(@"c:\abc");

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _Log.Verify(g => g.WriteLine("Program started, version {0}", "5.4.3.2"), Times.Once());
            _Log.Verify(g => g.WriteLine("Working folder {0}", @"c:\abc"), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Records_Custom_Working_Folder_In_Log()
        {
            _Presenter.CommandLineArgs = new string[] { "-workingFolder:xyz" };

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _Log.Verify(g => g.WriteLine("Working folder {0}", @"xyz"), Times.Once());
            _Log.Verify(g => g.WriteLine("Working folder {0}", It.IsAny<string>()), Times.Once());
        }
        #endregion

        #region Loading the configuration for the first time
        [TestMethod]
        public void SplashPresenter_StartApplication_Does_First_Load_Of_Configuration()
        {
            string currentSection = null;
            _View.Setup(v => v.ReportProgress(It.IsAny<string>())).Callback((string section) => { currentSection = section; });

            bool firstLoad = true;
            _ConfigurationStorage.Setup(c => c.Load()).Returns(_Configuration).Callback(() => {
                if(firstLoad) Assert.AreEqual(Strings.SplashScreenLoadingConfiguration, currentSection);
                firstLoad = false;
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _ConfigurationStorage.Verify(c => c.Load(), Times.AtLeastOnce());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Offers_User_Chance_To_Reset_Configuration_If_It_Cannot_Be_Loaded()
        {
            // A bug in an early version of VRS lead to configuration files that would throw an exception on load which, if left
            // unhandled, could prevent the application from loading at all
            _ConfigurationStorage.Setup(c => c.Load()).Returns(() => { throw new InvalidOperationException("Blah"); });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            string message = String.Format(Strings.InvalidConfigurationFileFull, "Blah", _ConfigurationStorage.Object.Folder);
            _View.Verify(v => v.YesNoPrompt(message, Strings.InvalidConfigurationFileTitle, true), Times.Once());
            _View.Verify(v => v.YesNoPrompt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Will_Reset_Configuration_If_User_Requests_It_After_Load_Throws()
        {
            _ConfigurationStorage.Setup(c => c.Load()).Returns(() => { throw new InvalidOperationException("Blah"); });
            _View.Setup(v => v.YesNoPrompt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(true);
            _View.Setup(v => v.ReportProblem(Strings.DefaultSettingsSavedFull, Strings.DefaultSettingsSavedTitle, true)).Callback(() => {
                _ConfigurationStorage.Verify(c => c.Save(It.IsAny<Configuration>()), Times.Once());
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _View.Verify(v => v.ReportProblem(Strings.DefaultSettingsSavedFull, Strings.DefaultSettingsSavedTitle, true), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Will_Just_Quit_If_User_Requests_It_After_Load_Throws()
        {
            _ConfigurationStorage.Setup(c => c.Load()).Returns(() => { throw new InvalidOperationException("Blah"); });
            _View.Setup(v => v.YesNoPrompt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(false);

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _ConfigurationStorage.Verify(c => c.Save(It.IsAny<Configuration>()), Times.Never());
            _View.Verify(v => v.ReportProblem(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never());
            _Provider.Verify(p => p.AbortApplication(), Times.Once());
        }
        #endregion

        #region Heartbeat timer
        [TestMethod]
        public void SplashPresenter_StartApplication_Starts_The_HeartbeatService()
        {
            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _HearbeatService.Verify(h => h.Start(), Times.Once());
        }
        #endregion

        #region BaseStation database connection
        [TestMethod]
        public void SplashPresenter_StartApplication_Initialises_AutoConfigBaseStationDatabase()
        {
            string currentSection = null;
            _View.Setup(v => v.ReportProgress(It.IsAny<string>())).Callback((string section) => { currentSection = section; });

            _AutoConfigBaseStationDatabase.Setup(a => a.Initialise()).Callback(() => {
                Assert.AreEqual(Strings.SplashScreenOpeningBaseStationDatabase, currentSection);
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _AutoConfigBaseStationDatabase.Verify(a => a.Initialise(), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Tests_The_BaseStation_Database_Connection()
        {
            _BaseStationDatabase.Setup(d => d.TestConnection()).Callback(() => {
                _AutoConfigBaseStationDatabase.Verify(a => a.Initialise(), Times.Once());
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _BaseStationDatabase.Verify(d => d.TestConnection(), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Does_Not_Open_BaseStation_Database_Until_After_Configuration_Has_Been_Checked()
        {
            _BaseStationDatabase.Setup(v => v.TestConnection()).Callback(() => {
                _ConfigurationStorage.Verify(c => c.Load(), Times.Once());
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _BaseStationDatabase.Verify(d => d.TestConnection(), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Does_Not_Try_Opening_BaseStation_Database_If_FileName_Is_Null()
        {
            _BaseStationDatabase.Setup(d => d.FileName).Returns((string)null);

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _BaseStationDatabase.Verify(d => d.TestConnection(), Times.Never());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Reports_Problem_Opening_BaseStation_Database()
        {
            _BaseStationDatabase.Setup(d => d.FileName).Returns("xyz");
            _BaseStationDatabase.Setup(d => d.TestConnection()).Returns(false);

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _View.Verify(v => v.ReportProblem(String.Format(Strings.CannotOpenBaseStationDatabaseFull, "xyz"), Strings.CannotOpenBaseStationDatabaseTitle, false), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Does_Not_Report_Problem_Opening_BaseStation_Database_If_FileName_Is_Null()
        {
            _BaseStationDatabase.Setup(d => d.FileName).Returns((string)null);
            _BaseStationDatabase.Setup(d => d.TestConnection()).Returns(false);

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _View.Verify(v => v.ReportProblem(It.IsAny<string>(), Strings.CannotOpenBaseStationDatabaseTitle, It.IsAny<bool>()), Times.Never());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Does_Not_Report_Problem_If_BaseStation_Database_Can_Be_Opened()
        {
            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _View.Verify(v => v.ReportProblem(It.IsAny<string>(), Strings.CannotOpenBaseStationDatabaseTitle, It.IsAny<bool>()), Times.Never());
        }
        #endregion

        #region Picture folder cache
        [TestMethod]
        public void SplashPresenter_StartApplication_Initialises_Picture_Folder_Cache()
        {
            string currentSection = null;
            _View.Setup(v => v.ReportProgress(It.IsAny<string>())).Callback((string section) => { currentSection = section; });
            _AutoConfigPictureFolderCache.Setup(a => a.Initialise()).Callback(() => {
                Assert.AreEqual(Strings.SplashScreenStartingPictureFolderCache, currentSection);
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _AutoConfigPictureFolderCache.Verify(a => a.Initialise(), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Initialises_Picture_Folder_Cache_After_Loading_Configuration()
        {
            _AutoConfigPictureFolderCache.Setup(a => a.Initialise()).Callback(() => {
                _ConfigurationStorage.Verify(c => c.Load(), Times.Once());
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _AutoConfigPictureFolderCache.Verify(a => a.Initialise(), Times.Once());
        }
        #endregion

        #region Standing data
        [TestMethod]
        public void SplashPresenter_StartApplication_Loads_Standing_Data()
        {
            string currentSection = null;
            _View.Setup(v => v.ReportProgress(It.IsAny<string>())).Callback((string section) => { currentSection = section; });
            _StandingDataManager.Setup(m => m.Load()).Callback(() => {
                Assert.AreEqual(Strings.SplashScreenLoadingStandingData, currentSection);
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _StandingDataManager.Verify(m => m.Load(), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Loads_Standing_Data_After_Loading_Configuration()
        {
            _StandingDataManager.Setup(m => m.Load()).Callback(() => {
                _ConfigurationStorage.Verify(c => c.Load(), Times.Once());
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _StandingDataManager.Verify(m => m.Load(), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Reports_Exceptions_Raised_During_Load_Of_Standing_Data()
        {
            var exception = new InvalidOperationException("oops");
            _StandingDataManager.Setup(m => m.Load()).Callback(() => { throw exception; });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _Log.Verify(g => g.WriteLine("Exception caught during load of standing data: {0}", exception.ToString()), Times.Once());
            _View.Verify(v => v.ReportProblem(String.Format(Strings.CannotLoadFlightRouteDataFull, exception.Message), Strings.CannotLoadFlightRouteDataTitle, false), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Starts_Background_Downloader()
        {
            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _BackgroundDataDownloader.Verify(b => b.Start(), Times.Once());
        }
        #endregion

        #region AutoConfigListener
        [TestMethod]
        public void SplashPresenter_StartApplication_Initialises_AutoConfigListener()
        {
            string currentSection = null;
            _View.Setup(v => v.ReportProgress(It.IsAny<string>())).Callback((string section) => { currentSection = section; });
            _AutoConfigListener.Setup(m => m.Initialise()).Callback(() => {
                Assert.AreEqual(Strings.SplashScreenConnectingToBaseStation, currentSection);
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _AutoConfigListener.Verify(b => b.Initialise(), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Initialises_AutoConfigListener_After_Loading_Configuration()
        {
            _AutoConfigListener.Setup(m => m.Initialise()).Callback(() => {
                _ConfigurationStorage.Verify(c => c.Load(), Times.Once());
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _StandingDataManager.Verify(m => m.Load(), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Hooks_AutoConfigListener_Background_Exception_Event()
        {
            _Presenter.BackgroundThreadExceptionHandler = _BackgroundExceptionEvent.Handler;
            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _AutoConfigListener.Raise(b => b.ExceptionCaught += null, new EventArgs<Exception>(new InvalidOperationException()));

            Assert.AreEqual(1, _BackgroundExceptionEvent.CallCount);
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Connects_AutoConfigListener()
        {
            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _Listener.Verify(b => b.Connect(It.IsAny<bool>()), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Connects_AutoConfigListener_Passing_AutoReconnectAtStartup_Configuration_Setting()
        {
            foreach(var autoReconnectAtStartup in new bool[] { true, false }) {
                TestCleanup();
                TestInitialise();

                _Configuration.BaseStationSettings.AutoReconnectAtStartup = autoReconnectAtStartup;

                _Presenter.Initialise(_View.Object);
                _Presenter.StartApplication();

                _Listener.Verify(b => b.Connect(autoReconnectAtStartup), Times.Once());
            }
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Reports_Failure_To_Connect_To_Data_Feed()
        {
            _Listener.Setup(b => b.Connect(false)).Callback(() => { throw new InvalidOperationException("msg here"); });

            _Configuration.BaseStationSettings.AutoReconnectAtStartup = false;
            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _View.Verify(v => v.ReportProblem(String.Format(Strings.CannotConnectToBaseStationFull, "msg here"), Strings.CannotConnectToBaseStationTitle, false), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Reports_Failure_To_Connect_To_Data_Feed_Regardless_Of_AutoReconnect_Setting()
        {
            _Listener.Setup(b => b.Connect(true)).Callback(() => { throw new InvalidOperationException("msg here"); });

            _Configuration.BaseStationSettings.AutoReconnectAtStartup = true;
            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _View.Verify(v => v.ReportProblem(String.Format(Strings.CannotConnectToBaseStationFull, "msg here"), Strings.CannotConnectToBaseStationTitle, false), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Logs_Failure_To_Connect_To_Data_Feed()
        {
            var exception = new InvalidOperationException("msg here");
            _Listener.Setup(b => b.Connect(false)).Callback(() => { throw exception; });

            _Configuration.BaseStationSettings.AutoReconnectAtStartup = false;
            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _Log.Verify(g => g.WriteLine("Could not connect to data feed: {0}", exception.ToString()), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Logs_Failure_To_Connect_To_Data_Feed_Regardless_Of_AutoReconnect_Setting()
        {
            var exception = new InvalidOperationException("msg here");
            _Listener.Setup(b => b.Connect(true)).Callback(() => { throw exception; });

            _Configuration.BaseStationSettings.AutoReconnectAtStartup = true;
            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _Log.Verify(g => g.WriteLine("Could not connect to data feed: {0}", exception.ToString()), Times.Once());
        }
        #endregion

        #region BaseStationAircraftList
        [TestMethod]
        public void SplashPresenter_StartApplication_Starts_BaseStationAircraftList()
        {
            string currentSection = null;
            _View.Setup(v => v.ReportProgress(It.IsAny<string>())).Callback((string section) => { currentSection = section; });
            _BaseStationAircraftList.Setup(a => a.Start()).Callback(() => {
                Assert.AreEqual(Strings.SplashScreenInitialisingAircraftList, currentSection);
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _BaseStationAircraftList.Verify(a => a.Start(), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Starts_BaseStationAircraftList_After_Configuration_Has_Loaded()
        {
            _BaseStationAircraftList.Setup(a => a.Start()).Callback(() => {
                _ConfigurationStorage.Verify(c => c.Load(), Times.Once());
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Starts_BaseStationAircraftList_After_Properties_Have_Been_Set()
        {
            _BaseStationAircraftList.Setup(a => a.Start()).Callback(() => {
                Assert.AreSame(_BaseStationDatabase.Object, _BaseStationAircraftList.Object.BaseStationDatabase);
                Assert.AreSame(_Listener.Object, _BaseStationAircraftList.Object.Listener);
                Assert.AreSame(_StandingDataManager.Object, _BaseStationAircraftList.Object.StandingDataManager);
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Hooks_BaseStationAircraftList_Background_Exception_Event()
        {
            _Presenter.BackgroundThreadExceptionHandler = _BackgroundExceptionEvent.Handler;
            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            var exception = new InvalidOperationException();
            _BaseStationAircraftList.Raise(b => b.ExceptionCaught += null, new EventArgs<Exception>(new InvalidOperationException()));

            Assert.AreEqual(1, _BackgroundExceptionEvent.CallCount);
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Copies_BaseStationAircraftList_To_View()
        {
            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            Assert.AreEqual(_BaseStationAircraftList.Object, _View.Object.BaseStationAircraftList);
        }
        #endregion

        #region Web WebServer and Web Site
        [TestMethod]
        public void SplashPresenter_StartApplication_Initialises_AutoConfigWebServer()
        {
            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _AutoConfigWebServer.Verify(a => a.Initialise(), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Hooks_Web_Server_Background_Exception_Event()
        {
            _Presenter.BackgroundThreadExceptionHandler = _BackgroundExceptionEvent.Handler;
            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            var exception = new InvalidOperationException();
            _WebServer.Raise(b => b.ExceptionCaught += null, new EventArgs<Exception>(new InvalidOperationException()));

            Assert.AreEqual(1, _BackgroundExceptionEvent.CallCount);
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Attches_ConnectionLogger_To_Server()
        {
            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            Assert.AreSame(_WebServer.Object, _ConnectionLogger.Object.WebServer);
            Assert.AreSame(_LogDatabase.Object, _ConnectionLogger.Object.LogDatabase);
            _ConnectionLogger.Verify(c => c.Start(), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Attches_ConnectionLogger_ExceptionCaught_Handler()
        {
            _Presenter.BackgroundThreadExceptionHandler = _BackgroundExceptionEvent.Handler;
            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            var exception = new InvalidOperationException();
            _ConnectionLogger.Raise(b => b.ExceptionCaught += null, new EventArgs<Exception>(new InvalidOperationException()));

            Assert.AreEqual(1, _BackgroundExceptionEvent.CallCount);
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Attaches_Web_Site_To_Web_Server()
        {
            string currentSection = null;
            _View.Setup(v => v.ReportProgress(It.IsAny<string>())).Callback((string section) => { currentSection = section; });
            _WebSite.Setup(s => s.AttachSiteToServer(_WebServer.Object)).Callback(() => {
                Assert.AreEqual(Strings.SplashScreenStartingWebServer, currentSection);
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _WebSite.Verify(s => s.AttachSiteToServer(_WebServer.Object), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Attaches_Web_Site_After_Configuration_Has_Loaded()
        {
            _WebSite.Setup(s => s.AttachSiteToServer(_WebServer.Object)).Callback(() => {
                _ConfigurationStorage.Verify(c => c.Load(), Times.Once());
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Sets_Properties_On_Web_Site()
        {
            _WebSite.Setup(s => s.AttachSiteToServer(_WebServer.Object)).Callback(() => {
                Assert.AreSame(_BaseStationAircraftList.Object, _WebSite.Object.BaseStationAircraftList);
                Assert.AreSame(_BaseStationDatabase.Object, _WebSite.Object.BaseStationDatabase);
                Assert.AreSame(_FlightSimulatorXAircraftList.Object, _WebSite.Object.FlightSimulatorAircraftList);
                Assert.AreSame(_StandingDataManager.Object, _WebSite.Object.StandingDataManager);
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Starts_Web_Server_Once_Site_And_Server_Are_Initialised()
        {
            _WebServer.SetupSet(s => s.Online = true).Callback(() => {
                _AutoConfigWebServer.Verify(a => a.Initialise(), Times.Once());
                _WebSite.Verify(s => s.AttachSiteToServer(_WebServer.Object), Times.Once());
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _WebServer.VerifySet(s => s.Online = true, Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Picks_Up_HttpListenerExceptions_When_Starting_WebServer()
        {
            var exception = new HttpListenerException();
            _WebServer.SetupSet(s => s.Online = true).Callback(() => {
                throw exception;
            });
            _WebServer.Setup(a => a.Port).Returns(123);

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _Log.Verify(g => g.WriteLine("Caught exception when starting web server: {0}", exception.ToString()), Times.Once());
            _View.Verify(v => v.ReportProblem(String.Format(Strings.CannotStartWebServerFull, 123), Strings.CannotStartWebServerTitle, false), Times.Once());
            _View.Verify(v => v.ReportProblem(Strings.SuggestUseDifferentPortFull, Strings.SuggestUseDifferentPortTitle, false), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Picks_Up_SocketExceptions_When_Starting_WebServer()
        {
            var exception = new SocketException();
            _WebServer.SetupSet(s => s.Online = true).Callback(() => {
                throw exception;
            });
            _WebServer.Setup(a => a.Port).Returns(123);

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _Log.Verify(g => g.WriteLine("Caught exception when starting web server: {0}", exception.ToString()), Times.Once());
            _View.Verify(v => v.ReportProblem(String.Format(Strings.CannotStartWebServerFull, 123), Strings.CannotStartWebServerTitle, false), Times.Once());
            _View.Verify(v => v.ReportProblem(Strings.SuggestUseDifferentPortFull, Strings.SuggestUseDifferentPortTitle, false), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Copies_Web_Site_Flight_Simulator_Aircraft_List_To_View()
        {
            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            Assert.AreEqual(_FlightSimulatorXAircraftList.Object, _View.Object.FlightSimulatorXAircraftList);
        }
        #endregion

        #region RebroadcastManager
        [TestMethod]
        public void SplashPresenter_StartApplication_Initialises_RebroadcastManager()
        {
            string currentSection = null;
            _View.Setup(v => v.ReportProgress(It.IsAny<string>())).Callback((string section) => { currentSection = section; });
            _RebroadcastServerManager.Setup(r => r.Initialise()).Callback(() => {
                Assert.AreEqual(Strings.SplashScreenStartingRebroadcastServers, currentSection);
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _RebroadcastServerManager.Verify(r => r.Initialise(), Times.Once());
            Assert.AreEqual(true, _RebroadcastServerManager.Object.Online);
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Initialises_RebroadcastManager_After_Loading_Configuration()
        {
            _RebroadcastServerManager.Setup(m => m.Initialise()).Callback(() => {
                _ConfigurationStorage.Verify(c => c.Load(), Times.Once());
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _StandingDataManager.Verify(m => m.Load(), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Initialises_RebroadcastManager_After_Initialising_Listener()
        {
            _RebroadcastServerManager.Setup(m => m.Initialise()).Callback(() => {
                _AutoConfigListener.Verify(r => r.Initialise(), Times.Once());
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _AutoConfigListener.Verify(r => r.Initialise(), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Sets_Listener_On_RebroadcastManager_Before_Initialising()
        {
            _RebroadcastServerManager.Setup(m => m.Initialise()).Callback(() => {
                Assert.AreSame(_Listener.Object, _RebroadcastServerManager.Object.Listener);
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _RebroadcastServerManager.Verify(r => r.Initialise(), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Hooks_RebroadcastManager_Background_Exception_Event()
        {
            _Presenter.BackgroundThreadExceptionHandler = _BackgroundExceptionEvent.Handler;
            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            var exception = new InvalidOperationException();
            _RebroadcastServerManager.Raise(b => b.ExceptionCaught += null, new EventArgs<Exception>(new InvalidOperationException()));

            Assert.AreEqual(1, _BackgroundExceptionEvent.CallCount);
        }
        #endregion

        #region UPnP Manager
        [TestMethod]
        public void SplashPresenter_StartApplication_Starts_UPnP_Manager()
        {
            string currentSection = null;
            _View.Setup(v => v.ReportProgress(It.IsAny<string>())).Callback((string section) => { currentSection = section; });
            _UniversalPlugAndPlayManager.Setup(s => s.Initialise()).Callback(() => {
                Assert.AreEqual(Strings.SplashScreenInitialisingUPnPManager, currentSection);
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _UniversalPlugAndPlayManager.Verify(s => s.Initialise(), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Starts_UPnP_Manager_After_Loading_Configuration()
        {
            _UniversalPlugAndPlayManager.Setup(s => s.Initialise()).Callback(() => {
                _ConfigurationStorage.Verify(c => c.Load(), Times.Once());
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Sets_Properties_On_UPnP_Manager()
        {
            _UniversalPlugAndPlayManager.Setup(s => s.Initialise()).Callback(() => {
                Assert.AreSame(_WebServer.Object, _UniversalPlugAndPlayManager.Object.WebServer);
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Puts_Server_Onto_Internet_If_Configuration_Allows()
        {
            _Configuration.WebServerSettings.AutoStartUPnP = true;

            string currentSection = null;
            _View.Setup(v => v.ReportProgress(It.IsAny<string>())).Callback((string section) => { currentSection = section; });
            _UniversalPlugAndPlayManager.Setup(s => s.PutServerOntoInternet()).Callback(() => {
                _UniversalPlugAndPlayManager.Verify(m => m.Initialise(), Times.Once());
                Assert.AreEqual(Strings.SplashScreenStartingUPnP, currentSection);
            });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _UniversalPlugAndPlayManager.Verify(s => s.PutServerOntoInternet(), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Does_Not_Put_Server_Onto_Internet_If_Configuration_Forbids()
        {
            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            _UniversalPlugAndPlayManager.Verify(s => s.PutServerOntoInternet(), Times.Never());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Copies_UPnP_Manager_To_View()
        {
            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            Assert.AreEqual(_UniversalPlugAndPlayManager.Object, _View.Object.UPnpManager);
        }
        #endregion

        #region Plugins
        [TestMethod]
        public void SplashPresenter_StartApplication_Calls_Startup_On_All_Loaded_Plugins()
        {
            var plugin1 = new Mock<IPlugin>() { DefaultValue = DefaultValue.Mock }.SetupAllProperties();
            var plugin2 = new Mock<IPlugin>() { DefaultValue = DefaultValue.Mock }.SetupAllProperties();
            _PluginManager.Setup(p => p.LoadedPlugins).Returns(new IPlugin[] { plugin1.Object, plugin2.Object });

            string currentSection = null;
            _View.Setup(v => v.ReportProgress(It.IsAny<string>())).Callback((string section) => { currentSection = section; });
            plugin1.Setup(p => p.Startup(It.IsAny<PluginStartupParameters>())).Callback(() => { Assert.AreEqual(Strings.SplashScreenStartingPlugins, currentSection); });
            plugin2.Setup(p => p.Startup(It.IsAny<PluginStartupParameters>())).Callback(() => { Assert.AreEqual(Strings.SplashScreenStartingPlugins, currentSection); });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            plugin1.Verify(p => p.Startup(It.IsAny<PluginStartupParameters>()), Times.Once());
            plugin2.Verify(p => p.Startup(It.IsAny<PluginStartupParameters>()), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Reports_Problems_Starting_A_Plugin_To_User()
        {
            var plugin1 = new Mock<IPlugin>() { DefaultValue = DefaultValue.Mock }.SetupAllProperties();
            var plugin2 = new Mock<IPlugin>() { DefaultValue = DefaultValue.Mock }.SetupAllProperties();
            plugin1.Setup(p => p.Name).Returns("P1");
            plugin2.Setup(p => p.Name).Returns("P2");
            _PluginManager.Setup(p => p.LoadedPlugins).Returns(new IPlugin[] { plugin1.Object, plugin2.Object });

            var exception = new InvalidOperationException();
            plugin1.Setup(p => p.Startup(It.IsAny<PluginStartupParameters>())).Callback(() => { throw exception; });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            plugin1.Verify(p => p.Startup(It.IsAny<PluginStartupParameters>()), Times.Once());
            plugin2.Verify(p => p.Startup(It.IsAny<PluginStartupParameters>()), Times.Once());

            _View.Verify(v => v.ReportProblem(String.Format(Strings.PluginThrewExceptionFull, "P1", exception.Message), Strings.PluginThrewExceptionTitle, false), Times.Once());
            _View.Verify(v => v.ReportProblem(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once());

            _Log.Verify(g => g.WriteLine("Caught exception when starting {0}: {1}", new object[] { "P1", exception.ToString() }), Times.Once());
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Sends_Correct_Parameters_To_Plugin_Startup()
        {
            var plugin = new Mock<IPlugin>() { DefaultValue = DefaultValue.Mock }.SetupAllProperties();
            _PluginManager.Setup(p => p.LoadedPlugins).Returns(new IPlugin[] { plugin.Object });

            PluginStartupParameters parameters = null;  // we can't just test within Startup.Callback because exceptions from there are caught by design, they won't stop the test
            plugin.Setup(p => p.Startup(It.IsAny<PluginStartupParameters>())).Callback((PluginStartupParameters p) => { parameters = p; });

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            plugin.Verify(p => p.Startup(It.IsAny<PluginStartupParameters>()), Times.Once());
            Assert.AreSame(_BaseStationAircraftList.Object, parameters.AircraftList);
            Assert.AreSame(_FlightSimulatorXAircraftList.Object, parameters.FlightSimulatorAircraftList);
            Assert.AreSame(_UniversalPlugAndPlayManager.Object, parameters.UPnpManager);
            Assert.AreSame(_WebSite.Object, parameters.WebSite);
        }

        [TestMethod]
        public void SplashPresenter_StartApplication_Hooks_ExceptionCaught_For_Plugins_That_Need_To_Raise_Background_Exceptions_On_GUI_Thread()
        {
            var plugin = new Mock<IPluginBackgroundThreadCatcher>()  { DefaultValue = DefaultValue.Mock }.SetupAllProperties();
            _PluginManager.Setup(p => p.LoadedPlugins).Returns(new IPlugin[] { plugin.Object });

            _Presenter.BackgroundThreadExceptionHandler += _BackgroundExceptionEvent.Handler;

            _Presenter.Initialise(_View.Object);
            _Presenter.StartApplication();

            var exception = new InvalidOperationException();
            plugin.Raise(p => p.ExceptionCaught += null, new EventArgs<Exception>(exception));

            Assert.AreEqual(1, _BackgroundExceptionEvent.CallCount);
            Assert.AreSame(exception, _BackgroundExceptionEvent.Args.Value);
            Assert.AreSame(plugin.Object, _BackgroundExceptionEvent.Sender);
        }
        #endregion
        #endregion
    }
}
