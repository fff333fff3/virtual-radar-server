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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using InterfaceFactory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Test.Framework;
using VirtualRadar.Interface;
using VirtualRadar.Interface.BaseStation;
using VirtualRadar.Interface.Database;
using VirtualRadar.Interface.Listener;
using VirtualRadar.Interface.Settings;
using VirtualRadar.Interface.StandingData;
using VirtualRadar.Library;

namespace Test.VirtualRadar.Library.BaseStation
{
    [TestClass]
    public class BaseStationAircraftListTests
    {
        #region TestContext, Fields, TestInitialise, TestCleanup
        public TestContext TestContext { get; set; }

        private const int MinutesBetweenDetailRefresh = 1;

        private IClassFactory _ClassFactorySnapshot;

        private Mock<IRuntimeEnvironment> _RuntimeEnvironment;
        private IBaseStationAircraftList _AircraftList;
        private Mock<IBaseStationAircraftListProvider> _Provider;
        private Mock<IListener> _Port30003Listener;
        private Mock<IBaseStationDatabase> _BaseStationDatabase;
        private BaseStationMessage _BaseStationMessage;
        private BaseStationAircraft _BaseStationAircraft;
        private Mock<IStandingDataManager> _StandingDataManager;
        private Route _Route;
        private Airport _Heathrow;
        private Airport _Helsinki;
        private Airport _JohnFKennedy;
        private Airport _Boston;
        private BaseStationMessageEventArgs _BaseStationMessageEventArgs;
        private EventRecorder<EventArgs<Exception>> _ExceptionCaughtEvent;
        private EventRecorder<EventArgs> _CountChangedEvent;
        private Exception _BackgroundException;
        private Configuration _Configuration;
        private Mock<IConfigurationStorage> _ConfigurationStorage;
        private Mock<IAircraftPictureManager> _AircraftPictureManager;
        private Mock<IHeartbeatService> _HeartbeatService;
        private Mock<IAutoConfigPictureFolderCache> _AutoConfigPictureFolderCache;
        private Mock<IDirectoryCache> _PictureDirectoryCache;

        [TestInitialize]
        public void TestInitialise()
        {
            _BackgroundException = null;

            _ClassFactorySnapshot = Factory.TakeSnapshot();

            _RuntimeEnvironment = TestUtilities.CreateMockSingleton<IRuntimeEnvironment>();
            _RuntimeEnvironment.Setup(r => r.IsTest).Returns(true);

            _ConfigurationStorage = TestUtilities.CreateMockSingleton<IConfigurationStorage>();
            _Configuration = new Configuration();
            _ConfigurationStorage.Setup(m => m.Load()).Returns(_Configuration);

            _AircraftPictureManager = TestUtilities.CreateMockSingleton<IAircraftPictureManager>();
            _HeartbeatService = TestUtilities.CreateMockSingleton<IHeartbeatService>();
            _AutoConfigPictureFolderCache = TestUtilities.CreateMockSingleton<IAutoConfigPictureFolderCache>();
            _PictureDirectoryCache = new Mock<IDirectoryCache>() { DefaultValue = DefaultValue.Mock }.SetupAllProperties();
            _AutoConfigPictureFolderCache.Setup(a => a.DirectoryCache).Returns(_PictureDirectoryCache.Object);

            _AircraftList = Factory.Singleton.Resolve<IBaseStationAircraftList>();
            _AircraftList.ExceptionCaught += AircraftListExceptionCaughtHandler;

            _Provider = new Mock<IBaseStationAircraftListProvider>().SetupAllProperties();
            _Provider.Setup(m => m.UtcNow).Returns(DateTime.UtcNow);

            _Port30003Listener = new Mock<IListener>().SetupAllProperties();

            _BaseStationDatabase = new Mock<IBaseStationDatabase>().SetupAllProperties();
            _StandingDataManager = TestUtilities.CreateMockSingleton<IStandingDataManager>();

            _AircraftList.Provider = _Provider.Object;
            _AircraftList.Listener = _Port30003Listener.Object;
            _AircraftList.BaseStationDatabase = _BaseStationDatabase.Object;
            _AircraftList.StandingDataManager = _StandingDataManager.Object;

            _BaseStationMessage = new BaseStationMessage();
            _BaseStationMessage.MessageType = BaseStationMessageType.Transmission;
            _BaseStationMessage.Icao24 = "4008F6";
            _BaseStationMessageEventArgs = new BaseStationMessageEventArgs(_BaseStationMessage);

            _BaseStationAircraft = new BaseStationAircraft();
            _BaseStationDatabase.Setup(m => m.GetAircraftByCode("4008F6")).Returns(_BaseStationAircraft);

            _Heathrow = new Airport() { IcaoCode = "EGLL", IataCode = "LHR", Name = "Heathrow", Country = "UK", };
            _JohnFKennedy = new Airport() { IcaoCode = "KJFK", IataCode = "JFK", Country = "USA", };
            _Helsinki = new Airport() { IataCode = "HEL", };
            _Boston = new Airport() { IcaoCode = "KBOS", };
            _Route = new Route() { From = _Heathrow, To = _JohnFKennedy, Stopovers = { _Helsinki }, };

            _ExceptionCaughtEvent = new EventRecorder<EventArgs<Exception>>();
            _CountChangedEvent = new EventRecorder<EventArgs>();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Factory.RestoreSnapshot(_ClassFactorySnapshot);

            if(_AircraftList != null) _AircraftList.Dispose();
            _AircraftList = null;
            Assert.IsNull(_BackgroundException, _BackgroundException == null ? "" : _BackgroundException.ToString());
        }

        private void AircraftListExceptionCaughtHandler(object sender, EventArgs<Exception> args)
        {
            _BackgroundException = args.Value;
        }
        #endregion

        #region Constructor and Properties
        [TestMethod]
        public void BaseStationAircraftList_Constructor_Initialises_To_Known_State_And_Properties_Work()
        {
            _AircraftList.Dispose();
            _AircraftList = Factory.Singleton.Resolve<IBaseStationAircraftList>();
            Assert.IsNotNull(_AircraftList.Provider);

            TestUtilities.TestProperty(_AircraftList, r => r.BaseStationDatabase, null, _BaseStationDatabase.Object);
            TestUtilities.TestProperty(_AircraftList, r => r.Listener, null, _Port30003Listener.Object);
            TestUtilities.TestProperty(_AircraftList, r => r.Provider, _AircraftList.Provider, _Provider.Object);
            Assert.AreEqual(AircraftListSource.BaseStation, _AircraftList.Source);
            TestUtilities.TestProperty(_AircraftList, r => r.StandingDataManager, null, _StandingDataManager.Object);
            Assert.AreEqual(0, _AircraftList.Count);
        }

        [TestMethod]
        public void BaseStationAircraftList_BaseStationMessageRelay_Stops_Picking_Up_Messages_When_MessageRelay_Changed()
        {
            _AircraftList.Start();
            _AircraftList.Listener = new Mock<IListener>() { DefaultValue = DefaultValue.Mock }.SetupAllProperties().Object;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.IsNull(_AircraftList.FindAircraft(0x4008F6));
        }

        [TestMethod]
        public void BaseStationAircraftList_Count_Reflects_Number_Of_Aircraft_Being_Tracked()
        {
            _AircraftList.Start();

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            Assert.AreEqual(1, _AircraftList.Count);

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            Assert.AreEqual(1, _AircraftList.Count);

            _BaseStationMessage.Icao24 = "7";
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            Assert.AreEqual(2, _AircraftList.Count);
        }
        #endregion

        #region ExceptionCaught
        [TestMethod]
        public void BaseStationAircraftList_ExceptionCaught_Is_Raised_When_Exception_Is_Raised_During_Message_Processing()
        {
            _AircraftList.ExceptionCaught -= AircraftListExceptionCaughtHandler;
            _AircraftList.ExceptionCaught += _ExceptionCaughtEvent.Handler;
            InvalidOperationException exception = new InvalidOperationException();
            _StandingDataManager.Setup(m => m.FindCodeBlock(It.IsAny<string>())).Callback(() => { throw exception; });

            _AircraftList.Start();
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.AreEqual(1, _ExceptionCaughtEvent.CallCount);
            Assert.AreSame(_AircraftList, _ExceptionCaughtEvent.Sender);
            Assert.AreSame(exception, _ExceptionCaughtEvent.Args.Value);
        }

        [TestMethod]
        public void BaseStationAircraftList_ExceptionCaught_Is_Raised_When_Exception_Is_Raised_During_Picture_FileName_Lookup()
        {
            _AircraftList.ExceptionCaught -= AircraftListExceptionCaughtHandler;
            _AircraftList.ExceptionCaught += _ExceptionCaughtEvent.Handler;
            InvalidOperationException exception = new InvalidOperationException();
            _AircraftPictureManager.Setup(m => m.FindPicture(It.IsAny<IDirectoryCache>(), It.IsAny<string>(), It.IsAny<string>())).Callback(() => { throw exception; });

            _AircraftList.Start();
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.AreEqual(1, _ExceptionCaughtEvent.CallCount);
            Assert.AreSame(_AircraftList, _ExceptionCaughtEvent.Sender);
            Assert.AreSame(exception, _ExceptionCaughtEvent.Args.Value);
        }

        [TestMethod]
        public void BaseStationAircraftList_ExceptionCaught_Is_Not_Raised_When_Background_Thread_Stops()
        {
            // A ThreadAbortExecption should just stop the background thread silently

            _AircraftList.ExceptionCaught -= AircraftListExceptionCaughtHandler;
            _AircraftList.ExceptionCaught += _ExceptionCaughtEvent.Handler;
            InvalidOperationException exception = new InvalidOperationException();

            _AircraftList.Dispose();
            _AircraftList = null;

            Assert.AreEqual(0, _ExceptionCaughtEvent.CallCount);
        }
        #endregion

        #region CountChanged
        [TestMethod]
        public void BaseStationAircraftList_CountChanged_Raised_When_Aircraft_First_Tracked()
        {
            _AircraftList.CountChanged += _CountChangedEvent.Handler;
            _CountChangedEvent.EventRaised += (s, a) => { Assert.AreEqual(1, _AircraftList.Count); };
            _AircraftList.Start();

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.AreEqual(1, _CountChangedEvent.CallCount);
            Assert.AreSame(_AircraftList, _CountChangedEvent.Sender);
            Assert.AreNotEqual(null, _CountChangedEvent.Args);
        }

        [TestMethod]
        public void BaseStationAircraftList_CountChanged_Not_Raised_If_Aircraft_Already_Tracked()
        {
            _AircraftList.CountChanged += _CountChangedEvent.Handler;
            _CountChangedEvent.EventRaised += (s, a) => { Assert.AreEqual(1, _AircraftList.Count); };
            _AircraftList.Start();

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.AreEqual(1, _CountChangedEvent.CallCount);
        }
        #endregion

        #region Dispose
        [TestMethod]
        public void BaseStationAircraftList_Dispose_Unhooks_BaseStationMessageRelay()
        {
            _AircraftList.Start();
            _AircraftList.Dispose();
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.IsNull(_AircraftList.FindAircraft(0x4008F6));
        }
        #endregion

        #region Start
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void BaseStationAircraftList_Start_Throws_If_BaseStationMessageRelay_Not_Set()
        {
            _AircraftList.Listener = null;
            _AircraftList.Start();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void BaseStationAircraftList_Start_Throws_If_BaseStationDatabase_Not_Set()
        {
            _AircraftList.BaseStationDatabase = null;
            _AircraftList.Start();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void BaseStationAircraftList_Start_Throws_If_StandingDataManager_Not_Set()
        {
            _AircraftList.StandingDataManager = null;
            _AircraftList.Start();
        }        
        #endregion

        #region FindAircraft
        [TestMethod]
        public void BaseStationAircraftList_FindAircraft_Returns_Null_If_Aircraft_Does_Not_Exist()
        {
            Assert.IsNull(_AircraftList.FindAircraft(1));
        }

        [TestMethod]
        public void BaseStationAircraftList_FindAircraft_Returns_Aircraft_Matching_UniqueId()
        {
            _AircraftList.Start();

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.IsNotNull(_AircraftList.FindAircraft(0x4008F6));
        }

        [TestMethod]
        public void BaseStationAircraftList_FindAircraft_Returns_Clone()
        {
            // The object returned by FindAircraft must not be affected by any further messages arriving from the listener
            _AircraftList.Start();

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            IAircraft aircraft = _AircraftList.FindAircraft(0x4008F6);
            Assert.IsNotNull(aircraft);

            _BaseStationMessage.Squawk = 1234;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.IsNull(aircraft.Squawk);
        }
        #endregion

        #region TakeSnapshot
        [TestMethod]
        public void BaseStationAircraftList_TakeSnapshot_Returns_Empty_List_When_No_Aircraft_Are_Visible()
        {
            var time = DateTime.UtcNow;
            _Provider.Setup(m => m.UtcNow).Returns(time);

            _AircraftList.Start();

            long timeStamp, dataVersion;
            var list = _AircraftList.TakeSnapshot(out timeStamp, out dataVersion);

            Assert.AreEqual(0, list.Count);
            Assert.AreEqual(time.Ticks, timeStamp);
            Assert.AreEqual(-1L, dataVersion);
        }

        [TestMethod]
        public void BaseStationAircraftList_TakeSnapshot_Returns_List_Of_Known_Aircraft()
        {
            _AircraftList.Start();

            _BaseStationMessage.Icao24 = "123456";
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _BaseStationMessage.Icao24 = "ABCDEF";
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            long o1, o2;
            var list = _AircraftList.TakeSnapshot(out o1, out o2);
            Assert.AreEqual(2, list.Count);
            Assert.IsTrue(list.Where(ac => ac.Icao24 == "123456").Any());
            Assert.IsTrue(list.Where(ac => ac.Icao24 == "ABCDEF").Any());
        }

        [TestMethod]
        public void BaseStationAircraftList_TakeSnapshot_Fills_In_Current_Time_And_Latest_DataVersion()
        {
            _AircraftList.Start();

            var time = DateTime.UtcNow;
            _Provider.Setup(m => m.UtcNow).Returns(time);

            _BaseStationMessage.Icao24 = "1";
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _BaseStationMessage.Icao24 = "2";
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            long timeStamp, dataVersion;
            var list = _AircraftList.TakeSnapshot(out timeStamp, out dataVersion);
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(time.Ticks, timeStamp);
            Assert.AreEqual(time.Ticks + 1, dataVersion);
        }

        [TestMethod]
        public void BaseStationAircraftList_TakeSnapshot_Returns_Aircraft_Clones()
        {
            // Messages that come in after the list has been established must not affect the aircraft in the snapshot

            _AircraftList.Start();

            _BaseStationMessage.Icao24 = "123456";
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            long o1, o2;
            List<IAircraft> list = _AircraftList.TakeSnapshot(out o1, out o2);
            Assert.AreEqual(1, list.Count);

            _BaseStationMessage.Squawk = 1234;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.AreEqual(null, list[0].Squawk);
        }

        [TestMethod]
        public void BaseStationAircraftList_TakeSnapshot_Does_Not_Show_Aircraft_Past_The_Hide_Threshold()
        {
            _Configuration.BaseStationSettings.DisplayTimeoutSeconds = 10;
            var time = DateTime.Now;

            _AircraftList.Start();

            _Provider.Setup(m => m.UtcNow).Returns(time);
            _BaseStationMessage.Icao24 = "1";
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _Provider.Setup(m => m.UtcNow).Returns(time.AddSeconds(10));
            long o1, o2;
            Assert.AreEqual(1, _AircraftList.TakeSnapshot(out o1, out o2).Count);

            _Provider.Setup(m => m.UtcNow).Returns(time.AddSeconds(10).AddMilliseconds(1));
            Assert.AreEqual(0, _AircraftList.TakeSnapshot(out o1, out o2).Count);
        }

        [TestMethod]
        public void BaseStationAircraftList_TakeSnapshot_Takes_Account_Of_Changes_To_Hide_Threshold()
        {
            _Configuration.BaseStationSettings.DisplayTimeoutSeconds = 10;
            var time = DateTime.Now;

            _AircraftList.Start();

            _Provider.Setup(m => m.UtcNow).Returns(time);
            _BaseStationMessage.Icao24 = "1";
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _Configuration.BaseStationSettings.DisplayTimeoutSeconds = 9;
            _ConfigurationStorage.Raise(c => c.ConfigurationChanged += null, EventArgs.Empty);

            _Provider.Setup(m => m.UtcNow).Returns(time.AddSeconds(9).AddMilliseconds(1));
            long o1, o2;
            Assert.AreEqual(0, _AircraftList.TakeSnapshot(out o1, out o2).Count);
        }

        [TestMethod]
        public void BaseStationAircraftList_TakeSnapshot_Does_Not_Delete_Aircraft_When_Hidden()
        {
            _Configuration.BaseStationSettings.DisplayTimeoutSeconds = 10;
            var time = DateTime.Now;

            _AircraftList.Start();

            _Provider.Setup(m => m.UtcNow).Returns(time);
            _BaseStationMessage.Icao24 = "1";
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _Provider.Setup(m => m.UtcNow).Returns(time.AddSeconds(10).AddMilliseconds(1));
            long o1, o2;
            _AircraftList.TakeSnapshot(out o1, out o2);

            Assert.IsNotNull(_AircraftList.FindAircraft(1));
        }
        #endregion

        #region MessageReceived
        #region Basics
        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Adds_Aircraft_To_List()
        {
            _AircraftList.Start();

            _BaseStationMessage.Icao24 = "7";
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _BaseStationMessage.Icao24 = "5";
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            var aircraft1 = _AircraftList.FindAircraft(7);
            Assert.IsNotNull(aircraft1);
            Assert.AreEqual("7", aircraft1.Icao24);

            var aircraft2 = _AircraftList.FindAircraft(5);
            Assert.IsNotNull(aircraft2);
            Assert.AreEqual("5", aircraft2.Icao24);
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Increments_Count_Of_Messages()
        {
            _AircraftList.Start();

            _BaseStationMessage.Icao24 = "7";

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            Assert.AreEqual(1, _AircraftList.FindAircraft(7).CountMessagesReceived);

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            Assert.AreEqual(2, _AircraftList.FindAircraft(7).CountMessagesReceived);
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Only_Responds_To_Transmission_Messages()
        {
            _AircraftList.Start();
            foreach(BaseStationMessageType messageType in Enum.GetValues(typeof(BaseStationMessageType))) {
                _BaseStationMessage.Icao24 = ((int)messageType).ToString();
                _BaseStationMessage.MessageType = messageType;
                _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            }

            long o1, o2;
            var list = _AircraftList.TakeSnapshot(out o1, out o2);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual((int)BaseStationMessageType.Transmission, list[0].UniqueId);
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Ignored_If_AircraftList_Not_Yet_Started()
        {
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.IsNull(_AircraftList.FindAircraft(0x4008f6));
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_UniqueId_Is_Derived_From_Icao24()
        {
            _AircraftList.Start();

            _BaseStationMessage.Icao24 = "ABC123";
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            long unused1, unused2;
            var list = _AircraftList.TakeSnapshot(out unused1, out unused2);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(0xABC123, list[0].UniqueId);
        }

        [TestMethod]
        [DataSource("Data Source='AircraftTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "InvalidIcao$")]
        public void BaseStationAircraftList_MessageReceived_Ignores_Messages_With_Invalid_Icao24()
        {
            var worksheet = new ExcelWorksheetData(TestContext);

            _AircraftList.Start();

            _BaseStationMessage.Icao24 = worksheet.EString("Icao");
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            bool expectValid = worksheet.Bool("IsValid");
            if(!expectValid) {
                long snapshotTime, snapshotDataVersion;
                Assert.AreEqual(0, _AircraftList.TakeSnapshot(out snapshotTime, out snapshotDataVersion).Count);
            } else {
                Assert.IsNotNull(_AircraftList.FindAircraft(worksheet.Int("UniqueId")));
            }
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Updates_LastUpdate_Time()
        {
            var messageTime1 = new DateTime(2001, 1, 1, 10, 20, 21);
            var messageTime2 = new DateTime(2001, 1, 1, 10, 20, 22);

            _AircraftList.Start();

            _Provider.Setup(m => m.UtcNow).Returns(messageTime1);
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            Assert.AreEqual(messageTime1, _AircraftList.FindAircraft(0x4008f6).LastUpdate);

            _Provider.Setup(m => m.UtcNow).Returns(messageTime2);
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            Assert.AreEqual(messageTime2, _AircraftList.FindAircraft(0x4008f6).LastUpdate);
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Sets_FirstSeen_On_First_Message_For_Aircraft()
        {
            var messageTime1 = new DateTime(2001, 1, 1, 10, 20, 21);
            var messageTime2 = new DateTime(2001, 1, 1, 10, 20, 22);

            _AircraftList.Start();

            _Provider.Setup(m => m.UtcNow).Returns(messageTime1);
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            Assert.AreEqual(messageTime1, _AircraftList.FindAircraft(0x4008f6).FirstSeen);

            _Provider.Setup(m => m.UtcNow).Returns(messageTime2);
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            Assert.AreEqual(messageTime1, _AircraftList.FindAircraft(0x4008f6).FirstSeen);
        }
        #endregion

        #region DataVersion handling
        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_DataVersion_Updated()
        {
            _BaseStationDatabase.Setup(d => d.GetAircraftByCode(It.IsAny<string>())).Returns((BaseStationAircraft)null);

            _AircraftList.Start();
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            var aircraft = _AircraftList.FindAircraft(0x4008f6);
            Assert.AreNotEqual(0, aircraft.DataVersion);
            Assert.AreEqual(aircraft.DataVersion, aircraft.Icao24Changed);
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_DataVersion_Uses_UtcNow()
        {
            _BaseStationDatabase.Setup(d => d.GetAircraftByCode(It.IsAny<string>())).Returns((BaseStationAircraft)null);
            var dateTime = new DateTime(1925);
            _Provider.Setup(m => m.UtcNow).Returns(dateTime);

            _AircraftList.Start();
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            var aircraft = _AircraftList.FindAircraft(0x4008f6);
            Assert.AreEqual(1925, aircraft.DataVersion);
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_DataVersion_Increments_If_UtcNow_Is_Same_As_Current_DataVersion()
        {
            // This can happen if two messages are processed before the clock tick is updated by the O/S
            _Provider.Setup(m => m.UtcNow).Returns(new DateTime(100));
            _BaseStationDatabase.Setup(d => d.GetAircraftByCode(It.IsAny<string>())).Returns((BaseStationAircraft)null);

            _AircraftList.Start();
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            var aircraft = _AircraftList.FindAircraft(0x4008f6);
            Assert.AreEqual(101, aircraft.DataVersion);
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_DataVersion_Increments_If_UtcNow_Is_Before_Current_DataVersion()
        {
            // This can happen if the clock is reset while the program is running. DataVersion must never go backwards.
            _AircraftList.Start();
            _Provider.Setup(m => m.UtcNow).Returns(new DateTime(100));
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            _Provider.Setup(m => m.UtcNow).Returns(new DateTime(90));
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            var aircraft = _AircraftList.FindAircraft(0x4008f6);
            Assert.IsTrue(aircraft.DataVersion > 100);
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_DataVersion_Is_Maintained_Across_All_Aircraft()
        {
            // The dataversion needs to increment for each aircraft so that a single dataversion value can be sent to the browser
            // and then, when it's sent back to us by the browser, we know for certain what has changed since the last time the
            // browser was sent the aircraft list.
            _AircraftList.Start();
            _Provider.Setup(m => m.UtcNow).Returns(new DateTime(100));
            _BaseStationMessageEventArgs.Message.Icao24 = "1";
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            _BaseStationMessageEventArgs.Message.Icao24 = "2";
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.AreEqual(100, _AircraftList.FindAircraft(1).DataVersion);
            Assert.AreEqual(101, _AircraftList.FindAircraft(2).DataVersion);
        }
        #endregion

        #region Message fields transcribed to aircraft correctly
        [TestMethod]
        [DataSource("Data Source='AircraftTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "TranslateMessage$")]
        public void BaseStationAircraftList_MessageReceived_Translates_Message_Properties_Into_New_Aircraft_Correctly()
        {
            var worksheet = new ExcelWorksheetData(TestContext);

            _AircraftList.Start();

            object messageObject;
            PropertyInfo messageProperty;
            var messageColumn = worksheet.String("MessageColumn");
            if(!messageColumn.StartsWith("S:")) {
                messageObject = _BaseStationMessage;
                messageProperty = typeof(BaseStationMessage).GetProperty(messageColumn);
            } else {
                messageColumn = messageColumn.Substring(2);
                messageObject = _BaseStationMessage.Supplementary = new BaseStationSupplementaryMessage();
                messageProperty = typeof(BaseStationSupplementaryMessage).GetProperty(messageColumn);
            }

            var aircraftProperty = typeof(IAircraft).GetProperty(worksheet.String("AircraftColumn"));

            var culture = new CultureInfo("en-GB");
            var messageValue = TestUtilities.ChangeType(worksheet.EString("MessageValue"), messageProperty.PropertyType, culture);
            var aircraftValue = TestUtilities.ChangeType(worksheet.EString("AircraftValue"), aircraftProperty.PropertyType, culture);

            messageProperty.SetValue(messageObject, messageValue, null);

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            var aircraft = _AircraftList.FindAircraft(0x4008F6);
            Assert.AreEqual(aircraftValue, aircraftProperty.GetValue(aircraft, null));
        }

        [TestMethod]
        [DataSource("Data Source='AircraftTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "TranslateMessage$")]
        public void BaseStationAircraftList_MessageReceived_Translates_Message_Properties_Into_Existing_Aircraft_Correctly()
        {
            var worksheet = new ExcelWorksheetData(TestContext);

            _AircraftList.Start();

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            object messageObject;
            PropertyInfo messageProperty;
            var messageColumn = worksheet.String("MessageColumn");
            if(!messageColumn.StartsWith("S:")) {
                messageObject = _BaseStationMessage;
                messageProperty = typeof(BaseStationMessage).GetProperty(messageColumn);
            } else {
                messageColumn = messageColumn.Substring(2);
                messageObject = _BaseStationMessage.Supplementary = new BaseStationSupplementaryMessage();
                messageProperty = typeof(BaseStationSupplementaryMessage).GetProperty(messageColumn);
            }

            var aircraftProperty = typeof(IAircraft).GetProperty(worksheet.String("AircraftColumn"));

            var culture = new CultureInfo("en-GB");
            var messageValue = TestUtilities.ChangeType(worksheet.EString("MessageValue"), messageProperty.PropertyType, culture);
            var aircraftValue = TestUtilities.ChangeType(worksheet.EString("AircraftValue"), aircraftProperty.PropertyType, culture);

            messageProperty.SetValue(messageObject, messageValue, null);

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            var aircraft = _AircraftList.FindAircraft(0x4008F6);
            Assert.AreEqual(aircraftValue, aircraftProperty.GetValue(aircraft, null));
        }

        [TestMethod]
        [DataSource("Data Source='AircraftTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "TranslateMessage$")]
        public void BaseStationAircraftList_MessageReceived_Does_Not_Null_Out_Existing_Message_Values()
        {
            // We need to make sure that we cope when we get a message that sets a value and then another message with a null for the same value
            var worksheet = new ExcelWorksheetData(TestContext);

            _AircraftList.Start();

            object messageObject;
            PropertyInfo messageProperty;
            var messageColumn = worksheet.String("MessageColumn");
            if(!messageColumn.StartsWith("S:")) {
                messageObject = _BaseStationMessage;
                messageProperty = typeof(BaseStationMessage).GetProperty(messageColumn);
            } else {
                messageColumn = messageColumn.Substring(2);
                messageObject = _BaseStationMessage.Supplementary = new BaseStationSupplementaryMessage();
                messageProperty = typeof(BaseStationSupplementaryMessage).GetProperty(messageColumn);
            }

            var aircraftProperty = typeof(IAircraft).GetProperty(worksheet.String("AircraftColumn"));

            var culture = new CultureInfo("en-GB");
            var messageValue = TestUtilities.ChangeType(worksheet.EString("MessageValue"), messageProperty.PropertyType, culture);
            var aircraftValue = TestUtilities.ChangeType(worksheet.EString("AircraftValue"), aircraftProperty.PropertyType, culture);

            messageProperty.SetValue(messageObject, messageValue, null);
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            var aircraft = _AircraftList.FindAircraft(0x4008F6);
            Assert.AreEqual(aircraftValue, aircraftProperty.GetValue(aircraft, null));

            messageProperty.SetValue(messageObject, null, null);
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            var outerAircraft = _AircraftList.FindAircraft(0x4008F6);
            Assert.AreEqual(aircraftValue, aircraftProperty.GetValue(outerAircraft, null));
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Sets_IsTransmittingTrack_Once_Track_Is_Seen()
        {
            _AircraftList.Start();

            _BaseStationMessage.Track = null;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            var aircraft = _AircraftList.FindAircraft(0x4008f6);
            Assert.IsFalse(aircraft.IsTransmittingTrack);

            _BaseStationMessage.Track = 100f;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            aircraft = _AircraftList.FindAircraft(0x4008f6);
            Assert.IsTrue(aircraft.IsTransmittingTrack);

            _BaseStationMessage.Track = null;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.IsTrue(_AircraftList.FindAircraft(0x4008f6).IsTransmittingTrack);
        }
        #endregion

        #region Emergency derived from squawk
        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Message_Emergency_Is_Ignored()
        {
            // The reason it is ignored is because some feed aggregators would incorrectly set the Emergency flag even though
            // the squawk was clearly indicating that there was no emergency. This renders the flag unusable, we need to
            // figure the correct value out for ourselves. Luckily it isn't difficult :)

            for(int i = 0;i < 2;++i) {
                TestCleanup();
                TestInitialise();

                _BaseStationMessage.Emergency = i == 0;
                _AircraftList.Start();
                _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

                var aircraft = _AircraftList.FindAircraft(0x4008f6);
                Assert.IsNull(aircraft.Emergency);
            }
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Emergency_Is_Derived_From_Squawk()
        {
            _AircraftList.Start();

            // Run through every possible code. In real life the squawk is octal presented as decimal so some of these
            // values would never appear (0008, 0080 etc.) but C# doesn't do octal very well and the code doesn't really
            // care if they're valid octal or not
            for(int i = -1;i < 7777;++i) {
                _BaseStationMessage.Squawk = i == -1 ? (int?)null : i;

                _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

                bool? expected = i == -1 ? (bool?)null : i == 7500 || i == 7600 || i == 7700;
                var aircraft = _AircraftList.FindAircraft(0x4008f6);
                Assert.AreEqual(expected, aircraft.Emergency, i.ToString());
            }
        }
        #endregion

        #region Track calculation
        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Calculates_Track_If_Position_Transmitted_Without_Track()
        {
            _AircraftList.Start();

            _BaseStationMessage.Latitude = 51;
            _BaseStationMessage.Longitude = 6;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _BaseStationMessage.Longitude = 7;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.AreEqual(89.6F, (float)_AircraftList.FindAircraft(0x4008f6).Track);
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Calculates_Track_If_Position_Transmitted_With_Exclusively_Zero_Track()
        {
            _AircraftList.Start();

            _BaseStationMessage.Latitude = 51;
            _BaseStationMessage.Longitude = 6;
            _BaseStationMessage.Track = 0f;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _BaseStationMessage.Longitude = 7;
            _BaseStationMessage.Track = 0f;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.AreEqual(89.6F, (float)_AircraftList.FindAircraft(0x4008f6).Track);
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Continues_To_Use_Calculated_Track_Between_Position_Updates()
        {
            _AircraftList.Start();

            _BaseStationMessage.Latitude = 51;
            _BaseStationMessage.Longitude = 6;
            _BaseStationMessage.Track = 0f;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _BaseStationMessage.Longitude = 7;
            _BaseStationMessage.Track = 0f;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.AreEqual(89.6F, (float)_AircraftList.FindAircraft(0x4008f6).Track);

            _BaseStationMessage.Latitude = null;
            _BaseStationMessage.Longitude = null;
            _BaseStationMessage.Track = 0f;
            _BaseStationMessage.Callsign = "Changed";
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            var aircraft = _AircraftList.FindAircraft(0x4008f6);
            Assert.AreEqual("Changed", aircraft.Callsign);
            Assert.AreEqual(89.6F, (float)aircraft.Track);
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Calculates_Track_Only_After_Aircraft_Has_Travelled_At_Least_250_Metres_When_Airborne()
        {
            _AircraftList.Start();

            _BaseStationMessage.Latitude = 51;
            _BaseStationMessage.Longitude = 6;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _BaseStationMessage.Longitude = 6.0035; // distance should be 244.9 metres
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.AreEqual(null, _AircraftList.FindAircraft(0x4008f6).Track);

            _BaseStationMessage.Longitude = 6.0036; // distance from first contact now 251 metres
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.AreEqual(90F, (float)_AircraftList.FindAircraft(0x4008f6).Track);

            _BaseStationMessage.Latitude = 51.002;
            _BaseStationMessage.Longitude = 6.005; // distance from last calculated track now 243 metres
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.AreEqual(90F, (float)_AircraftList.FindAircraft(0x4008f6).Track);  // <-- track shouldn't be recalculated until we're > 250 metres from last calculated position

            _BaseStationMessage.Latitude = 51.0021; // distance from last calculated track now 253 metres
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.AreEqual(22.8F, (float)_AircraftList.FindAircraft(0x4008f6).Track);
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Calculates_Track_Only_After_Aircraft_Has_Travelled_At_Least_10_Metres_When_On_Ground()
        {
            _AircraftList.Start();

            _BaseStationMessage.Latitude = 51;
            _BaseStationMessage.Longitude = 6;
            _BaseStationMessage.OnGround = true;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _BaseStationMessage.Longitude = 6.000141; // distance should be 9.9 metres at 90°
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.AreEqual(null, _AircraftList.FindAircraft(0x4008f6).Track);

            _BaseStationMessage.Longitude = 6.000157; // distance from first contact now 10.1 metres
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.AreEqual(90F, (float)_AircraftList.FindAircraft(0x4008f6).Track, 0.0001f);

            _BaseStationMessage.Latitude = 51.000089;
            _BaseStationMessage.Longitude = 6.000161; // distance from last calculated track now 9.9 metres at 1.6°
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.AreEqual(90F, (float)_AircraftList.FindAircraft(0x4008f6).Track, 0.0001f);  // <-- track shouldn't be recalculated until we're > 5 metres from last calculated position

            _BaseStationMessage.Latitude = 51.000099;
            _BaseStationMessage.Longitude = 6.000161; // distance from last calculated track now 10.1 metres
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.AreEqual(1.5F, (float)_AircraftList.FindAircraft(0x4008f6).Track);  // rounding errors force it to 1.5
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Does_Not_Calculate_Airborne_Track_If_Aircraft_Transmits_Track()
        {
            _AircraftList.Start();

            _BaseStationMessage.Latitude = 51;
            _BaseStationMessage.Longitude = 6;
            _BaseStationMessage.Track = 35;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _BaseStationMessage.Latitude = 52;
            _BaseStationMessage.Track = null;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _BaseStationMessage.Latitude = 53;
            _BaseStationMessage.Track = null;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.AreEqual(35f, _AircraftList.FindAircraft(0x4008f6).Track); // If this is 0 then it erroneously calculated the track based on the two position updates with no track
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Calculates_Ground_Track_Even_If_Aircraft_Previously_Transmitted_Track()
        {
            _AircraftList.Start();

            _BaseStationMessage.Latitude = 51;
            _BaseStationMessage.Longitude = 6;
            _BaseStationMessage.OnGround = true;
            _BaseStationMessage.Track = 35;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _BaseStationMessage.Longitude = 7;
            _BaseStationMessage.Track = null;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _BaseStationMessage.Longitude = 8;
            _BaseStationMessage.Track = null;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.AreEqual(89.6F, (float)_AircraftList.FindAircraft(0x4008f6).Track); // If this is 35 then it did not calculate the track based on the two position updates with no track
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Calculates_Ground_Track_If_Aircraft_Track_Has_Locked_On_Ground()
        {
            // 757s appear to have a problem with transmitting the correct track when the aircraft starts on the ground. The track
            // transmitted in SurfacePosition records remains as-at the heading it was pointing in when the aircraft started until
            // the aircraft becomes airborne. The reverse is not true - after a 757-200 lands it will transmit the correct track
            // in surface position messages (unless the speed drops below ~7.5 knots, when it stops transmitting track altogether).

            _AircraftList.Start();

            _BaseStationMessage.Latitude = 51;
            _BaseStationMessage.Longitude = 6;
            _BaseStationMessage.OnGround = true;
            _BaseStationMessage.Track = 2.5F;
            _BaseStationMessage.OnGround = true;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            // Move 90° for 10 metres
            _BaseStationMessage.Longitude = 6.000143;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.AreEqual(90F, (float)_AircraftList.FindAircraft(0x4008f6).Track);
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Resets_Locks_Ground_Track_Timer_After_Thirty_Minutes()
        {
            // Another kludge for Fedex's 757 fleet. When they touch down at an airport the track will be correctly transmitted
            // during the taxi to the apron. The aircraft will then sit there for a few hours and continue transmissions. When it
            // starts up and taxis to take-off the track will be locked. Because the ground track was originally changing the lock
            // will not be detected and we get the same screwy track as before.
            //
            // The kludge is to record the time of the first ground track and then reset it every 30 minutes. That way the original
            // good track is forgotten about by the time the aircraft taxis to takeoff.

            _AircraftList.Start();

            var now = DateTime.UtcNow;
            _Provider.Setup(r => r.UtcNow).Returns(() => { return now; });

            _BaseStationMessage.Latitude = 51;
            _BaseStationMessage.Longitude = 6;
            _BaseStationMessage.OnGround = true;
            _BaseStationMessage.Track = 2.5F;
            _BaseStationMessage.OnGround = true;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            // Transmit a new track on the ground a minute later after moving 270° and 10 metres
            _BaseStationMessage.Track = 270.0F;
            _BaseStationMessage.Longitude = 5.999857;
            now = now.AddMinutes(1);
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            // Wait 29 minutes from the original transmission (we've already added 1 minute so we add another 28)
            // and send another message 90° and 10 metres away from the original transmission
            now = now.AddMinutes(28);
            _BaseStationMessage.Longitude = 6.0;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            // The track will still show 270 because the 30 minute timeout hasn't expired
            Assert.AreEqual(270F, (float)_AircraftList.FindAircraft(0x4008f6).Track);

            // Send another message 1 minute and 1 millisecond later, another 10 metres and 90° from the original
            // position. This should reset the detection of a locked track but because we don't have any points to
            // compare to we'll still be trusting the track from the aircraft.
            now = now.AddMinutes(1).AddMilliseconds(1);
            _BaseStationMessage.Longitude = 6.000143;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            Assert.AreEqual(270F, (float)_AircraftList.FindAircraft(0x4008f6).Track);

            // Finally if we send another message a second later then we should see the calculated track
            now = now.AddSeconds(1);
            _BaseStationMessage.Longitude = 6.000286;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.AreEqual(90F, (float)_AircraftList.FindAircraft(0x4008f6).Track);
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Reports_Previously_Calculated_Ground_Track_For_Surface_Aircraft_With_Locked_Tracks()
        {
            _AircraftList.Start();

            _BaseStationMessage.Latitude = 51;
            _BaseStationMessage.Longitude = 6;
            _BaseStationMessage.OnGround = true;
            _BaseStationMessage.Track = 2.5F;
            _BaseStationMessage.OnGround = true;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            // Move 90° for 10 metres
            _BaseStationMessage.Longitude = 6.000143;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            // Move 100° for 9 metres. This is below the track calculation threshold.
            _BaseStationMessage.Latitude = 50.999986;
            _BaseStationMessage.Longitude = 6.000270;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.AreEqual(90F, (float)_AircraftList.FindAircraft(0x4008f6).Track);  // if this is 100 then the track was erroneously calculated. If it's 2.5 it didn't reuse the previously calculated track.
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Reports_Previously_Calculated_Ground_Track_For_Surface_Aircraft_That_Do_Not_Move()
        {
            _AircraftList.Start();

            _BaseStationMessage.Latitude = 51;
            _BaseStationMessage.Longitude = 6;
            _BaseStationMessage.OnGround = true;
            _BaseStationMessage.Track = 2.5F;
            _BaseStationMessage.OnGround = true;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            // Move 90° for 10 metres
            _BaseStationMessage.Longitude = 6.000143;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            // Now don't move at all
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            Assert.AreEqual(90F, (float)_AircraftList.FindAircraft(0x4008f6).Track);  // if this is 2.5 then the frozen track was used when the aircraft reported the same position twice
        }
        #endregion

        #region Trail lists
        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Calls_UpdateCoordinates_On_Aircraft()
        {
            _Configuration.GoogleMapSettings.ShortTrailLengthSeconds = 74;
            var time = DateTime.Now;
            _Provider.Setup(m => m.UtcNow).Returns(time);

            var aircraft = TestUtilities.CreateMockImplementation<IAircraft>();
            aircraft.Setup(a => a.UpdateCoordinates(It.IsAny<DateTime>(), It.IsAny<int>())).Callback(() => {
                Assert.AreEqual(1.0001, aircraft.Object.Latitude);
                Assert.AreEqual(1.0002, aircraft.Object.Longitude);
                Assert.AreEqual(1, aircraft.Object.Track);
            });

            _AircraftList.Start();

            _BaseStationMessage.Latitude = 1.0001;
            _BaseStationMessage.Longitude = 1.0002;
            _BaseStationMessage.Track = 1;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            aircraft.Verify(a => a.UpdateCoordinates(It.IsAny<DateTime>(), It.IsAny<int>()), Times.Once());
            aircraft.Verify(a => a.UpdateCoordinates(time, 74), Times.Once());
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Does_Not_Call_UpdateCoordinates_If_No_Position_Is_In_Message()
        {
            _Configuration.GoogleMapSettings.ShortTrailLengthSeconds = 74;
            var time = DateTime.Now;
            _Provider.Setup(m => m.UtcNow).Returns(time);

            var aircraft = TestUtilities.CreateMockImplementation<IAircraft>();

            _AircraftList.Start();

            _BaseStationMessage.Latitude = null;
            _BaseStationMessage.Longitude = null;
            _BaseStationMessage.Track = 1;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            aircraft.Verify(a => a.UpdateCoordinates(It.IsAny<DateTime>(), It.IsAny<int>()), Times.Never());
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Picks_Up_Configuration_Changes_To_Short_Coordinates_Length()
        {
            _Configuration.GoogleMapSettings.ShortTrailLengthSeconds = 74;
            var time = DateTime.Now;
            _Provider.Setup(m => m.UtcNow).Returns(time);

            var aircraft = TestUtilities.CreateMockImplementation<IAircraft>();

            _AircraftList.Start();

            _Configuration.GoogleMapSettings.ShortTrailLengthSeconds = 92;
            _ConfigurationStorage.Raise(c => c.ConfigurationChanged += null, EventArgs.Empty);

            _BaseStationMessage.Latitude = 1.0001;
            _BaseStationMessage.Longitude = 1.0002;
            _BaseStationMessage.Track = 1;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            aircraft.Verify(a => a.UpdateCoordinates(time, 92), Times.Once());
        }
        #endregion

        #region Database lookup
        [TestMethod]
        [DataSource("Data Source='AircraftTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "DatabaseFetch$")]
        public void BaseStationAircraftList_MessageReceived_Fetches_Values_From_Database_For_New_Aircraft()
        {
            var worksheet = new ExcelWorksheetData(TestContext);
            _AircraftList.Start();

            var databaseProperty = typeof(BaseStationAircraft).GetProperty(worksheet.String("DatabaseColumn"));
            var aircraftProperty = typeof(IAircraft).GetProperty(worksheet.String("AircraftColumn"));

            var culture = new CultureInfo("en-GB");
            var databaseValue = TestUtilities.ChangeType(worksheet.EString("DatabaseValue"), databaseProperty.PropertyType, culture);
            var aircraftValue = TestUtilities.ChangeType(worksheet.EString("AircraftValue"), aircraftProperty.PropertyType, culture);

            databaseProperty.SetValue(_BaseStationAircraft, databaseValue, null);

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            var aircraft = _AircraftList.FindAircraft(0x4008F6);
            Assert.AreEqual(aircraftValue, aircraftProperty.GetValue(aircraft, null));
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Fetches_Count_Of_Flights_From_Database_For_New_Aircraft()
        {
            _AircraftList.Start();

            SearchBaseStationCriteria criteria = null;
            _BaseStationDatabase.Setup(m => m.GetCountOfFlightsForAircraft(_BaseStationAircraft, It.IsAny<SearchBaseStationCriteria>()))
                .Callback((BaseStationAircraft a, SearchBaseStationCriteria c) => { criteria = c; })
                .Returns(42);

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            var aircraft = _AircraftList.FindAircraft(0x4008F6);
            Assert.AreEqual(42, aircraft.FlightsCount);

            Assert.IsNull(criteria.Callsign);
            Assert.IsNull(criteria.Country);
            Assert.AreEqual(DateTime.MinValue, criteria.FromDate);
            Assert.IsNull(criteria.Icao);
            Assert.IsFalse(criteria.IsEmergency);
            Assert.IsNull(criteria.Operator);
            Assert.IsNull(criteria.Registration);
            Assert.AreEqual(DateTime.MaxValue, criteria.ToDate);
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Uses_OperatorCode_From_Database_To_Fetch_Route_When_Callsign_Has_No_Operator_Code()
        {
            _AircraftList.Start();

            _BaseStationMessage.Callsign = "1234";
            _BaseStationAircraft.OperatorFlagCode = "WJA";
            _StandingDataManager.Setup(r => r.FindRoute("WJA1234")).Returns(_Route);

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            var aircraft = _AircraftList.FindAircraft(0x4008F6);

            Assert.IsTrue(aircraft.Origin.Contains("EGLL"));
            Assert.IsTrue(aircraft.Destination.Contains("KJFK"));
            Assert.AreEqual(1, aircraft.Stopovers.Count);
            Assert.IsTrue(aircraft.Stopovers.First().Contains("HEL"));
            _StandingDataManager.Verify(r => r.FindRoute("1234"), Times.Once());
            _StandingDataManager.Verify(r => r.FindRoute("WJA1234"), Times.Once());
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Uses_OperatorCode_From_Database_To_Fetch_Route_When_Callsign_Has_No_Operator_Code_And_First_Message_Has_No_Callsign()
        {
            _AircraftList.Start();

            _BaseStationAircraft.OperatorFlagCode = "WJA";
            _StandingDataManager.Setup(r => r.FindRoute("1234")).Returns((Route)null);
            _StandingDataManager.Setup(r => r.FindRoute("WJA1234")).Returns(_Route);

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _BaseStationMessage.Callsign = "1234";
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            var aircraft = _AircraftList.FindAircraft(0x4008F6);

            Assert.IsTrue(aircraft.Origin.Contains("EGLL"));
            Assert.IsTrue(aircraft.Destination.Contains("KJFK"));
            Assert.AreEqual(1, aircraft.Stopovers.Count);
            Assert.IsTrue(aircraft.Stopovers.First().Contains("HEL"));
            _StandingDataManager.Verify(r => r.FindRoute("1234"), Times.Once());
            _StandingDataManager.Verify(r => r.FindRoute("WJA1234"), Times.Once());
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Does_Not_Use_OperatorCode_From_Database_To_Fetch_Route_When_Callsign_Is_Missing()
        {
            _AircraftList.Start();

            _BaseStationAircraft.OperatorFlagCode = "WJA";

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            var aircraft = _AircraftList.FindAircraft(0x4008F6);

            _StandingDataManager.Verify(r => r.FindRoute(It.IsAny<string>()), Times.Never());
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Does_Not_Use_OperatorCode_From_Database_To_Fetch_Route_When_OperatorFlagCode_Is_Missing()
        {
            _AircraftList.Start();

            _BaseStationMessage.Callsign = "1234";

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            var aircraft = _AircraftList.FindAircraft(0x4008F6);

            _StandingDataManager.Verify(r => r.FindRoute(It.IsAny<string>()), Times.Once());
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Does_Not_Use_OperatorCode_From_Database_To_Fetch_Route_When_Callsign_Has_OperatorCode()
        {
            _AircraftList.Start();

            _BaseStationMessage.Callsign = "ABC1234";
            _BaseStationAircraft.OperatorFlagCode = "WJA";

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            var aircraft = _AircraftList.FindAircraft(0x4008F6);

            _StandingDataManager.Verify(r => r.FindRoute(It.IsAny<string>()), Times.Once());
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Updates_DataVersion_When_Setting_Database_Details_On_Background_Thread()
        {
            _AircraftList.Start();
            _BaseStationAircraft.Registration = "Z";

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            IAircraft aircraft = _AircraftList.FindAircraft(0x4008F6);
            Assert.AreEqual("Z", aircraft.Registration);

            var dataVersionFromMessageParser = aircraft.Icao24Changed;         // will have been set when message processed
            var dataVersionFromDatabaseUpdate = aircraft.RegistrationChanged;  // database fetch must not block message processing, must be on background thread & set DV when updating values

            Assert.IsTrue(dataVersionFromDatabaseUpdate > dataVersionFromMessageParser);
        }

        [TestMethod]
        [DataSource("Data Source='AircraftTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "DatabaseFetch$")]
        public void BaseStationAircraftList_Heartbeat_Periodically_Refreshes_Database_Details()
        {
            var worksheet = new ExcelWorksheetData(TestContext);

            _Configuration.BaseStationSettings.DisplayTimeoutSeconds = (MinutesBetweenDetailRefresh * 60) + 1;
            _ConfigurationStorage.Raise(c => c.ConfigurationChanged += null, EventArgs.Empty);
            _AircraftList.Start();

            // Initialise timer
            var time = DateTime.UtcNow;
            _Provider.Setup(p => p.UtcNow).Returns(time);
            _HeartbeatService.Raise(h => h.FastTick += null, EventArgs.Empty);

            // Message received when no information exists in database
            _BaseStationDatabase.Setup(d => d.GetAircraftByCode("4008F6")).Returns((BaseStationAircraft)null);
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            _BaseStationDatabase.Verify(d => d.GetAircraftByCode("4008F6"), Times.Once());

            // Heartbeat 1 millisecond before refresh is allowed
            _Provider.Setup(p => p.UtcNow).Returns(time.AddMinutes(MinutesBetweenDetailRefresh).AddMilliseconds(-1));
            _HeartbeatService.Raise(h => h.FastTick += null, EventArgs.Empty);
            _BaseStationDatabase.Verify(d => d.GetAircraftByCode("4008F6"), Times.Once());

            // Setup database record
            _BaseStationDatabase.Setup(d => d.GetAircraftByCode("4008F6")).Returns(_BaseStationAircraft);
            var databaseProperty = typeof(BaseStationAircraft).GetProperty(worksheet.String("DatabaseColumn"));
            var aircraftProperty = typeof(IAircraft).GetProperty(worksheet.String("AircraftColumn"));
            var culture = new CultureInfo("en-GB");
            var databaseValue = TestUtilities.ChangeType(worksheet.EString("DatabaseValue"), databaseProperty.PropertyType, culture);
            var aircraftValue = TestUtilities.ChangeType(worksheet.EString("AircraftValue"), aircraftProperty.PropertyType, culture);

            databaseProperty.SetValue(_BaseStationAircraft, databaseValue, null);

            // Heartbeat on the refresh interval
            _Provider.Setup(p => p.UtcNow).Returns(time.AddMinutes(MinutesBetweenDetailRefresh));
            _HeartbeatService.Raise(h => h.FastTick += null, EventArgs.Empty);
            var aircraft = _AircraftList.FindAircraft(0x4008F6);
            Assert.AreEqual(aircraftValue, aircraftProperty.GetValue(aircraft, null));
        }

        [TestMethod]
        public void BaseStationAircraftList_Heartbeat_Periodic_Refetch_Copes_If_Still_No_Database_Record_For_Aircraft()
        {
            var worksheet = new ExcelWorksheetData(TestContext);

            _Configuration.BaseStationSettings.DisplayTimeoutSeconds = (MinutesBetweenDetailRefresh * 60) + 1;
            _ConfigurationStorage.Raise(c => c.ConfigurationChanged += null, EventArgs.Empty);
            _AircraftList.Start();

            var time = DateTime.UtcNow;
            _Provider.Setup(p => p.UtcNow).Returns(time);
            _HeartbeatService.Raise(h => h.FastTick += null, EventArgs.Empty);

            _BaseStationDatabase.Setup(d => d.GetAircraftByCode("4008F6")).Returns((BaseStationAircraft)null);
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _Provider.Setup(p => p.UtcNow).Returns(time.AddMinutes(MinutesBetweenDetailRefresh));
            _HeartbeatService.Raise(h => h.FastTick += null, EventArgs.Empty);

            var aircraft = _AircraftList.FindAircraft(0x4008F6);
            Assert.AreEqual(null, aircraft.Registration);
        }

        [TestMethod]
        public void BaseStationAircraftList_Heartbeat_Periodic_Refetch_Ignores_Aircraft_That_Have_A_Registration()
        {
            var worksheet = new ExcelWorksheetData(TestContext);

            _Configuration.BaseStationSettings.DisplayTimeoutSeconds = (MinutesBetweenDetailRefresh * 60) + 1;
            _ConfigurationStorage.Raise(c => c.ConfigurationChanged += null, EventArgs.Empty);
            _AircraftList.Start();

            var time = DateTime.UtcNow;
            _Provider.Setup(p => p.UtcNow).Returns(time);
            _HeartbeatService.Raise(h => h.FastTick += null, EventArgs.Empty);

            _BaseStationAircraft.Registration = "R";
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            _BaseStationDatabase.Verify(d => d.GetAircraftByCode(It.IsAny<string>()), Times.Once());

            _Provider.Setup(p => p.UtcNow).Returns(time.AddMinutes(MinutesBetweenDetailRefresh));
            _HeartbeatService.Raise(h => h.FastTick += null, EventArgs.Empty);

            _BaseStationDatabase.Verify(d => d.GetAircraftByCode(It.IsAny<string>()), Times.Once());
        }

        [TestMethod]
        public void BaseStationAircraftList_Heartbeat_Periodic_Refetch_Only_Attempts_Refetch_Once()
        {
            var worksheet = new ExcelWorksheetData(TestContext);

            _Configuration.BaseStationSettings.DisplayTimeoutSeconds = (MinutesBetweenDetailRefresh * 60 * 2) + 1;
            _ConfigurationStorage.Raise(c => c.ConfigurationChanged += null, EventArgs.Empty);
            _AircraftList.Start();

            var time = DateTime.UtcNow;
            _Provider.Setup(p => p.UtcNow).Returns(time);
            _HeartbeatService.Raise(h => h.FastTick += null, EventArgs.Empty);

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _Provider.Setup(p => p.UtcNow).Returns(time.AddMinutes(MinutesBetweenDetailRefresh));
            _HeartbeatService.Raise(h => h.FastTick += null, EventArgs.Empty);

            _Provider.Setup(p => p.UtcNow).Returns(time.AddMinutes(MinutesBetweenDetailRefresh * 2));
            _HeartbeatService.Raise(h => h.FastTick += null, EventArgs.Empty);

            _BaseStationDatabase.Verify(d => d.GetAircraftByCode(It.IsAny<string>()), Times.Exactly(2));
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Only_Fetches_Values_Once_From_Database_For_An_Aircraft()
        {
            // Note that the list will periodically refresh details for all aircraft. However that is a separate mechanism
            // that is driven off the heartbeat timer - this test shows that if two messages come in for the same aircraft
            // it will only trigger a single fetch from the database.

            _AircraftList.Start();

            _BaseStationMessage.Icao24 = "1";
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _BaseStationMessage.Icao24 = "2";
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _BaseStationMessage.Icao24 = "1";
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _BaseStationDatabase.Verify(m => m.GetAircraftByCode("1"), Times.Once());
            _BaseStationDatabase.Verify(m => m.GetAircraftByCode("2"), Times.Once());
        }
        #endregion

        #region Route lookup
        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Fetches_Route_For_Aircraft()
        {
            _AircraftList.Start();

            _BaseStationMessage.Callsign = "CALL123";
            _StandingDataManager.Setup(m => m.FindRoute("CALL123")).Returns(_Route);

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            var aircraft = _AircraftList.FindAircraft(0x4008F6);
            Assert.AreEqual("EGLL Heathrow, UK", aircraft.Origin);
            Assert.AreEqual("KJFK, USA", aircraft.Destination);
            Assert.AreEqual(1, aircraft.Stopovers.Count);
            Assert.AreEqual("HEL", aircraft.Stopovers.First());
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Copes_With_Routes_With_No_Stopovers()
        {
            _AircraftList.Start();
            _Route.Stopovers.Clear();
            _BaseStationMessage.Callsign = "CALL123";
            _StandingDataManager.Setup(m => m.FindRoute("CALL123")).Returns(_Route);

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            var aircraft = _AircraftList.FindAircraft(0x4008F6);
            Assert.IsNotNull(aircraft.Origin);
            Assert.AreEqual(0, aircraft.Stopovers.Count);
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Copes_With_Routes_With_Many_Stopovers()
        {
            _AircraftList.Start();
            _Route.Stopovers.Add(_Boston);
            _BaseStationMessage.Callsign = "CALL123";
            _StandingDataManager.Setup(m => m.FindRoute("CALL123")).Returns(_Route);

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            var aircraft = _AircraftList.FindAircraft(0x4008F6);
            Assert.IsNotNull(aircraft.Origin);
            Assert.AreEqual(2, aircraft.Stopovers.Count);
            Assert.AreEqual("HEL", aircraft.Stopovers.ElementAt(0));
            Assert.AreEqual("KBOS", aircraft.Stopovers.ElementAt(1));
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Only_Looks_Up_Route_Once()
        {
            _AircraftList.Start();
            _BaseStationMessage.Callsign = "CALL123";
            _StandingDataManager.Setup(m => m.FindRoute("CALL123")).Returns(_Route);

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _StandingDataManager.Verify(m => m.FindRoute("CALL123"), Times.Once());
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Updates_Route_When_Callsign_Changes()
        {
            _AircraftList.Start();

            var routeOut = new Route() { From = _Heathrow, To = _JohnFKennedy };
            var routeIn = new Route() { From = _JohnFKennedy, To = _Heathrow };

            _StandingDataManager.Setup(m => m.FindRoute("VRS1")).Returns(routeOut);
            _StandingDataManager.Setup(m => m.FindRoute("VRS2")).Returns(routeIn);

            _BaseStationMessage.Callsign = "VRS1";
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _BaseStationMessage.Callsign = "VRS2";
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            var aircraft = _AircraftList.FindAircraft(0x4008F6);
            Assert.AreEqual("EGLL Heathrow, UK", aircraft.Destination);
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Honours_Configuration_When_Describing_Airport()
        {
            _AircraftList.Start();

            _BaseStationMessage.Callsign = "CALL123";
            _StandingDataManager.Setup(m => m.FindRoute("CALL123")).Returns(_Route);
            _Configuration.GoogleMapSettings.PreferIataAirportCodes = true;
            _ConfigurationStorage.Raise(r => r.ConfigurationChanged += null, EventArgs.Empty);

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            var aircraft = _AircraftList.FindAircraft(0x4008F6);
            Assert.AreEqual("LHR Heathrow, UK", aircraft.Origin);
            Assert.AreEqual("JFK, USA", aircraft.Destination);
            Assert.AreEqual(1, aircraft.Stopovers.Count);
            Assert.AreEqual("HEL", aircraft.Stopovers.First());
        }
        #endregion

        #region Codeblock / Country / IsMilitary lookup
        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Looks_Up_Codeblock_Details()
        {
            var codeblock1 = new CodeBlock() { Country = "UK", IsMilitary = false, };
            _StandingDataManager.Setup(m => m.FindCodeBlock("1")).Returns(codeblock1);

            var dbRecord1 = new BaseStationAircraft();
            _BaseStationDatabase.Setup(m => m.GetAircraftByCode("1")).Returns(dbRecord1);
            dbRecord1.Country = "UNUSED";
            dbRecord1.ModeSCountry = "UNUSED";

            var codeBlock2 = new CodeBlock() { IsMilitary = true, };
            _StandingDataManager.Setup(m => m.FindCodeBlock("2")).Returns(codeBlock2);

            _AircraftList.Start();

            _BaseStationMessage.Icao24 = "1";
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _BaseStationMessage.Icao24 = "2";
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            var aircraft1 = _AircraftList.FindAircraft(1);
            Assert.AreEqual("UK", aircraft1.Icao24Country);
            Assert.AreEqual(false, aircraft1.IsMilitary);

            var aircraft2 = _AircraftList.FindAircraft(2);
            Assert.AreEqual(true, aircraft2.IsMilitary);
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Only_Looks_Up_Codeblock_Details_Once()
        {
            _AircraftList.Start();

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _StandingDataManager.Verify(m => m.FindCodeBlock("4008F6"), Times.Once());
        }
        #endregion

        #region ICAO8643 / Aircraft Type lookup
        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Looks_Up_Icao8643_Details()
        {
            var aircraftType = new AircraftType() {
                Engines = "C",
                EngineType = EngineType.Piston,
                Manufacturers = { "UNUSED 1" },
                Models = { "UNUSED 2" },
                Species = Species.Landplane,
                Type = "UNUSED",
                WakeTurbulenceCategory = WakeTurbulenceCategory.Medium,
            };
            _StandingDataManager.Setup(m => m.FindAircraftType("747")).Returns(aircraftType);

            _BaseStationAircraft.ICAOTypeCode = "747";
            _BaseStationAircraft.Manufacturer = "USED MAN";
            _BaseStationAircraft.Type = "USED MOD";

            _AircraftList.Start();
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            var aircraft = _AircraftList.FindAircraft(0x4008F6);
            Assert.AreEqual("C", aircraft.NumberOfEngines);
            Assert.AreEqual(EngineType.Piston, aircraft.EngineType);
            Assert.AreEqual(Species.Landplane, aircraft.Species);
            Assert.AreEqual(WakeTurbulenceCategory.Medium, aircraft.WakeTurbulenceCategory);
            Assert.AreEqual("747", aircraft.Type);
            Assert.AreEqual("USED MAN", aircraft.Manufacturer);
            Assert.AreEqual("USED MOD", aircraft.Model);
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Looks_Up_Icao8643_Details_Once()
        {
            // This only checks that a message for a new aircraft only triggers a single read. There is a separate
            // mechanism for refreshing the details from a periodic database check.
            _BaseStationAircraft.ICAOTypeCode = "ABC";

            _AircraftList.Start();
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _StandingDataManager.Verify(m => m.FindAircraftType("ABC"), Times.Once());
        }

        [TestMethod]
        public void BaseStationAircraftList_Heartbeat_Periodically_Refreshes_Icao8643_Details()
        {
            _Configuration.BaseStationSettings.DisplayTimeoutSeconds = (MinutesBetweenDetailRefresh * 60) + 1;
            _ConfigurationStorage.Raise(c => c.ConfigurationChanged += null, EventArgs.Empty);
            _BaseStationAircraft.ICAOTypeCode = "747";

            _AircraftList.Start();

            // Initialise timer
            var time = DateTime.UtcNow;
            _Provider.Setup(p => p.UtcNow).Returns(time);
            _HeartbeatService.Raise(h => h.FastTick += null, EventArgs.Empty);

            // Message received when no information exists in lookup service
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            // Add entry to lookup service
            var aircraftType = new AircraftType() {
                Engines = "C",
                EngineType = EngineType.Piston,
                Species = Species.Landplane,
                WakeTurbulenceCategory = WakeTurbulenceCategory.Medium,
            };
            _StandingDataManager.Setup(m => m.FindAircraftType("747")).Returns(aircraftType);

            // Heartbeat 1 millisecond before refresh is allowed
            _Provider.Setup(p => p.UtcNow).Returns(time.AddMinutes(MinutesBetweenDetailRefresh).AddMilliseconds(-1));
            _HeartbeatService.Raise(h => h.FastTick += null, EventArgs.Empty);
            Assert.AreEqual(null, _AircraftList.FindAircraft(0x4008F6).NumberOfEngines);

            // Heartbeat on the refresh interval
            _Provider.Setup(p => p.UtcNow).Returns(time.AddMinutes(MinutesBetweenDetailRefresh));
            _HeartbeatService.Raise(h => h.FastTick += null, EventArgs.Empty);

            var aircraft = _AircraftList.FindAircraft(0x4008F6);
            Assert.AreEqual("C", aircraft.NumberOfEngines);
            Assert.AreEqual(EngineType.Piston, aircraft.EngineType);
            Assert.AreEqual(Species.Landplane, aircraft.Species);
            Assert.AreEqual(WakeTurbulenceCategory.Medium, aircraft.WakeTurbulenceCategory);
            Assert.AreEqual("747", aircraft.Type);
        }

        [TestMethod]
        public void BaseStationAircraftList_Heartbeat_Copes_If_Icao8643_Details_Still_Missing()
        {
            _Configuration.BaseStationSettings.DisplayTimeoutSeconds = (MinutesBetweenDetailRefresh * 60) + 1;
            _ConfigurationStorage.Raise(c => c.ConfigurationChanged += null, EventArgs.Empty);
            _BaseStationAircraft.ICAOTypeCode = "747";

            _AircraftList.Start();

            var time = DateTime.UtcNow;
            _Provider.Setup(p => p.UtcNow).Returns(time);
            _HeartbeatService.Raise(h => h.FastTick += null, EventArgs.Empty);

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            Assert.AreEqual(null, _AircraftList.FindAircraft(0x4008F6).NumberOfEngines);

            _Provider.Setup(p => p.UtcNow).Returns(time.AddMinutes(MinutesBetweenDetailRefresh));
            _HeartbeatService.Raise(h => h.FastTick += null, EventArgs.Empty);

            var aircraft = _AircraftList.FindAircraft(0x4008F6);
            Assert.AreEqual(null, aircraft.NumberOfEngines);
        }

        [TestMethod]
        public void BaseStationAircraftList_Heartbeat_Does_Not_Refresh_ICAO8643_Details_For_Aircraft_With_Registration()
        {
            _Configuration.BaseStationSettings.DisplayTimeoutSeconds = (MinutesBetweenDetailRefresh * 60) + 1;
            _ConfigurationStorage.Raise(c => c.ConfigurationChanged += null, EventArgs.Empty);
            _BaseStationAircraft.Registration = "K";

            _AircraftList.Start();

            var time = DateTime.UtcNow;
            _Provider.Setup(p => p.UtcNow).Returns(time);
            _HeartbeatService.Raise(h => h.FastTick += null, EventArgs.Empty);

            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            Assert.AreEqual(null, _AircraftList.FindAircraft(0x4008F6).NumberOfEngines);

            _Provider.Setup(p => p.UtcNow).Returns(time.AddMinutes(MinutesBetweenDetailRefresh));
            _HeartbeatService.Raise(h => h.FastTick += null, EventArgs.Empty);

            _StandingDataManager.Verify(m => m.FindCodeBlock(It.IsAny<string>()), Times.Once());
        }
        #endregion

        #region Aircraft picture lookup
        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Looks_For_Aircraft_Pictures()
        {
            _BaseStationAircraft.Registration = "G-VROS";
            _AircraftPictureManager.Setup(p => p.FindPicture(_PictureDirectoryCache.Object, "4008F6", "G-VROS")).Returns("Fullpath");

            _AircraftList.Start();
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _AircraftPictureManager.Verify(m => m.FindPicture(_PictureDirectoryCache.Object, "4008F6", "G-VROS"), Times.Once());
            var aircraft = _AircraftList.FindAircraft(0x4008F6);
            Assert.AreEqual("Fullpath", aircraft.PictureFileName);
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Reloads_Aircraft_Pictures_If_PictureManager_Cache_Is_Cleared()
        {
            _BaseStationAircraft.Registration = "G-VROS";
            _AircraftPictureManager.Setup(p => p.FindPicture(_PictureDirectoryCache.Object, "4008F6", "G-VROS")).Returns((string)null);

            _AircraftList.Start();
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _AircraftPictureManager.Verify(m => m.FindPicture(_PictureDirectoryCache.Object, "4008F6", "G-VROS"), Times.Once());
            var aircraft = _AircraftList.FindAircraft(0x4008F6);
            Assert.AreEqual(null, aircraft.PictureFileName);

            _AircraftPictureManager.Setup(p => p.FindPicture(_PictureDirectoryCache.Object, "4008F6", "G-VROS")).Returns("Now it exists");
            _PictureDirectoryCache.Raise(p => p.CacheChanged += null, EventArgs.Empty);

            _AircraftPictureManager.Verify(m => m.FindPicture(_PictureDirectoryCache.Object, "4008F6", "G-VROS"), Times.Exactly(2));
            aircraft = _AircraftList.FindAircraft(0x4008F6);
            Assert.AreEqual("Now it exists", aircraft.PictureFileName);
        }

        [TestMethod]
        public void BaseStationAircraftList_MessageReceived_Searches_For_Aircraft_Picture_If_Database_Details_Are_Refreshed()
        {
            _AircraftPictureManager.Setup(p => p.FindPicture(_PictureDirectoryCache.Object, "4008F6", "G-VROS")).Returns("Exists");
            _BaseStationAircraft.Registration = null;

            var time = DateTime.Now;
            _Provider.Setup(p => p.UtcNow).Returns(time);

            _AircraftList.Start();
            _HeartbeatService.Raise(h => h.FastTick += null, EventArgs.Empty);
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _BaseStationAircraft.Registration = "G-VROS";

            _Provider.Setup(p => p.UtcNow).Returns(time.AddMinutes(MinutesBetweenDetailRefresh));
            _HeartbeatService.Raise(h => h.FastTick += null, EventArgs.Empty);

            _AircraftPictureManager.Verify(m => m.FindPicture(_PictureDirectoryCache.Object, "4008F6", "G-VROS"), Times.Once());
        }
        #endregion
        #endregion

        #region RefreshPicture
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void BaseStationAircraftList_RefreshPicture_Throws_If_Aircraft_Is_Null()
        {
            _AircraftList.Start();
            _AircraftList.RefreshPicture(null);
        }

        [TestMethod]
        public void BaseStationAircraftList_RefreshPicture_Queues_Aircraft_Up_For_Picture_Refresh()
        {
            _BaseStationAircraft.Registration = "G-VROS";
            _AircraftList.Start();
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            _AircraftPictureManager.Verify(m => m.FindPicture(_PictureDirectoryCache.Object, "4008F6", "G-VROS"), Times.Once());

            var aircraft = new Mock<IAircraft>() { DefaultValue = DefaultValue.Mock }.SetupAllProperties().Object;
            aircraft.UniqueId = 0x4008f6;

            _AircraftList.RefreshPicture(aircraft);

            _AircraftPictureManager.Verify(m => m.FindPicture(_PictureDirectoryCache.Object, "4008F6", "G-VROS"), Times.Exactly(2));
        }

        [TestMethod]
        public void BaseStationAircraftList_RefreshPicture_Does_Nothing_If_Aircraft_Not_In_List()
        {
            _AircraftList.Start();

            var aircraft = new Mock<IAircraft>() { DefaultValue = DefaultValue.Mock }.SetupAllProperties().Object;
            aircraft.UniqueId = 0x4008f6;

            _AircraftList.RefreshPicture(aircraft);

            _AircraftPictureManager.Verify(m => m.FindPicture(_PictureDirectoryCache.Object, "4008F6", "G-VROS"), Times.Never());
        }
        #endregion

        #region RefreshDatabaseDetails
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void BaseStationAircraftList_RefreshDatabaseDetails_Throws_If_Aircraft_Is_Null()
        {
            _AircraftList.Start();
            _AircraftList.RefreshDatabaseDetails(null);
        }

        [TestMethod]
        public void BaseStationAircraftList_RefreshDatabaseDetails_Queues_Aircraft_Up_For_Database_Fetch()
        {
            _AircraftList.Start();
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            _BaseStationDatabase.Verify(d => d.GetAircraftByCode("4008F6"), Times.Once());

            var aircraft = new Mock<IAircraft>() { DefaultValue = DefaultValue.Mock }.SetupAllProperties().Object;
            aircraft.UniqueId = 0x4008f6;

            _AircraftList.RefreshDatabaseDetails(aircraft);

            _BaseStationDatabase.Verify(d => d.GetAircraftByCode("4008F6"), Times.Exactly(2));
        }

        [TestMethod]
        public void BaseStationAircraftList_RefreshDatabaseDetails_Does_Nothing_If_The_Aircraft_Is_Not_In_The_List()
        {
            _AircraftList.Start();

            var aircraft = new Mock<IAircraft>() { DefaultValue = DefaultValue.Mock }.SetupAllProperties().Object;
            aircraft.UniqueId = 0x4008f6;

            _AircraftList.RefreshDatabaseDetails(aircraft);

            _BaseStationDatabase.Verify(d => d.GetAircraftByCode("4008F6"), Times.Never());
        }
        #endregion

        #region Listener.SourceChanged event
        [TestMethod]
        public void BaseStationAircraftList_SourceChanged_Clears_Aircraft_List()
        {
            _AircraftList.Start();

            _BaseStationMessage.Icao24 = "7";
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _Port30003Listener.Raise(b => b.SourceChanged += null, EventArgs.Empty);

            long out1, out2;
            Assert.AreEqual(0, _AircraftList.TakeSnapshot(out out1, out out2).Count);
        }

        [TestMethod]
        public void BaseStationAircraftList_SourceChanged_Raises_CountChanged()
        {
            _AircraftList.CountChanged += _CountChangedEvent.Handler;
            _AircraftList.Start();

            _Port30003Listener.Raise(b => b.SourceChanged += null, EventArgs.Empty);

            Assert.AreEqual(1, _CountChangedEvent.CallCount);
            Assert.AreSame(_AircraftList, _CountChangedEvent.Sender);
            Assert.AreNotEqual(null, _CountChangedEvent.Args);
        }
        #endregion

        #region Listener.PositionReset event
        [TestMethod]
        public void BaseStationAircraftList_PositionReset_Resets_Aircraft_Coordinate_List()
        {
            _AircraftList.Start();

            var aircraftMock = TestUtilities.CreateMockImplementation<IAircraft>();

            _BaseStationMessage.Icao24 = "ABC123";
            _BaseStationMessage.Latitude = 1.0;
            _BaseStationMessage.Longitude = 2.0;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _Port30003Listener.Raise(r => r.PositionReset += null, new EventArgs<string>("ABC123"));

            aircraftMock.Verify(r => r.ResetCoordinates(), Times.Once());
        }

        [TestMethod]
        public void BaseStationAircraftList_PositionReset_Ignores_Resets_On_Aircraft_Not_Being_Tracked()
        {
            _AircraftList.Start();

            var aircraftMock = TestUtilities.CreateMockImplementation<IAircraft>();

            _BaseStationMessage.Icao24 = "ABC123";
            _BaseStationMessage.Latitude = 1.0;
            _BaseStationMessage.Longitude = 2.0;
            _Port30003Listener.Raise(m => m.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _Port30003Listener.Raise(r => r.PositionReset += null, new EventArgs<string>("123456"));

            aircraftMock.Verify(r => r.ResetCoordinates(), Times.Never());
        }
        #endregion

        #region StandingDataManager.LoadCompleted event
        [TestMethod]
        public void BaseStationAircraftList_StandingDataManager_LoadCompleted_Reloads_Missing_Route()
        {
            _AircraftList.Start();

            _BaseStationMessage.Callsign = "VIR1";
            _Port30003Listener.Raise(r => r.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            _StandingDataManager.Verify(r => r.FindRoute("VIR1"), Times.Once());

            IAircraft aircraft;
            long unused1, unused2;

            aircraft = _AircraftList.TakeSnapshot(out unused1, out unused2)[0];
            Assert.AreEqual("", aircraft.Origin);
            Assert.AreEqual("", aircraft.Destination);
            Assert.AreEqual(0, aircraft.Stopovers.Count);

            _StandingDataManager.Setup(r => r.FindRoute("VIR1")).Returns(_Route);
            _StandingDataManager.Raise(r => r.LoadCompleted += null, EventArgs.Empty);

            _StandingDataManager.Verify(r => r.FindRoute("VIR1"), Times.Exactly(2));
            aircraft = _AircraftList.TakeSnapshot(out unused1, out unused2)[0];
            Assert.AreNotEqual("", aircraft.Origin);
            Assert.AreNotEqual("", aircraft.Destination);
            Assert.AreEqual(1, aircraft.Stopovers.Count);
        }

        [TestMethod]
        public void BaseStationAircraftList_StandingDataManager_LoadCompleted_Does_Not_Refresh_Existing_Routes()
        {
            _AircraftList.Start();

            _StandingDataManager.Setup(r => r.FindRoute("VIR1")).Returns(_Route);
            _BaseStationMessage.Callsign = "VIR1";
            _Port30003Listener.Raise(r => r.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _StandingDataManager.Raise(r => r.LoadCompleted += null, EventArgs.Empty);
            _StandingDataManager.Verify(r => r.FindRoute("VIR1"), Times.Once());
        }

        [TestMethod]
        public void BaseStationAircraftList_StandingDataManager_LoadCompleted_Does_Not_Refresh_Routes_On_Aircraft_With_No_Callsign()
        {
            _AircraftList.Start();

            _Port30003Listener.Raise(r => r.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _StandingDataManager.Raise(r => r.LoadCompleted += null, EventArgs.Empty);
            _StandingDataManager.Verify(r => r.FindRoute(It.IsAny<string>()), Times.Never());
        }

        [TestMethod]
        public void BaseStationAircraftList_StandingDataManager_LoadCompleted_Reloads_ModeS_Country_And_IsMilitary()
        {
            _AircraftList.Start();

            _Port30003Listener.Raise(r => r.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            _StandingDataManager.Verify(r => r.FindCodeBlock("4008F6"), Times.Once());

            IAircraft aircraft;
            long unused1, unused2;

            aircraft = _AircraftList.TakeSnapshot(out unused1, out unused2)[0];
            Assert.AreEqual(null, aircraft.Icao24Country);
            Assert.AreEqual(false, aircraft.IsMilitary);

            _StandingDataManager.Setup(r => r.FindCodeBlock("4008F6")).Returns(new CodeBlock() { Country = "UK", IsMilitary = true });
            _StandingDataManager.Raise(r => r.LoadCompleted += null, EventArgs.Empty);

            _StandingDataManager.Verify(r => r.FindCodeBlock("4008F6"), Times.Exactly(2));
            aircraft = _AircraftList.TakeSnapshot(out unused1, out unused2)[0];
            Assert.AreEqual("UK", aircraft.Icao24Country);
            Assert.AreEqual(true, aircraft.IsMilitary);
        }

        [TestMethod]
        public void BaseStationAircraftList_StandingDataManager_LoadCompleted_Reloads_AircraftType_Information()
        {
            _AircraftList.Start();

            _BaseStationAircraft.ICAOTypeCode = "A380";
            _Port30003Listener.Raise(r => r.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            _StandingDataManager.Verify(r => r.FindAircraftType("A380"), Times.Once());

            IAircraft aircraft;
            long unused1, unused2;

            aircraft = _AircraftList.TakeSnapshot(out unused1, out unused2)[0];
            Assert.AreEqual(null, aircraft.NumberOfEngines);
            Assert.AreEqual(EngineType.None, aircraft.EngineType);
            Assert.AreEqual(Species.None, aircraft.Species);
            Assert.AreEqual(WakeTurbulenceCategory.None, aircraft.WakeTurbulenceCategory);

            _StandingDataManager.Setup(r => r.FindAircraftType("A380")).Returns(new AircraftType() {
                Engines = "4",
                EngineType = EngineType.Jet,
                Manufacturers = { "AIRBUS" },
                Models = { "A-380" },
                Species = Species.Landplane,
                Type = "A380",
                WakeTurbulenceCategory = WakeTurbulenceCategory.Heavy,
            });
            _StandingDataManager.Raise(r => r.LoadCompleted += null, EventArgs.Empty);

            _StandingDataManager.Verify(r => r.FindAircraftType("A380"), Times.Exactly(2));
            aircraft = _AircraftList.TakeSnapshot(out unused1, out unused2)[0];
            Assert.AreEqual("4", aircraft.NumberOfEngines);
            Assert.AreEqual(EngineType.Jet, aircraft.EngineType);
            Assert.AreEqual(Species.Landplane, aircraft.Species);
            Assert.AreEqual(WakeTurbulenceCategory.Heavy, aircraft.WakeTurbulenceCategory);
            Assert.AreEqual(null, aircraft.Manufacturer);
            Assert.AreEqual(null, aircraft.Model);
        }

        [TestMethod]
        public void BaseStationAircraftList_StandingDataManager_LoadCompleted_Does_Not_Reload_AircraftType_Information_For_Aircraft_With_No_Type()
        {
            _AircraftList.Start();

            _BaseStationAircraft.ICAOTypeCode = "A380";
            _StandingDataManager.Setup(r => r.FindAircraftType("A380")).Returns(new AircraftType() {
                Engines = "4",
                EngineType = EngineType.Jet,
                Manufacturers = { "AIRBUS" },
                Models = { "A-380" },
                Species = Species.Landplane,
                Type = "A380",
                WakeTurbulenceCategory = WakeTurbulenceCategory.Heavy,
            });
            _Port30003Listener.Raise(r => r.Port30003MessageReceived += null, _BaseStationMessageEventArgs);
            _StandingDataManager.Verify(r => r.FindAircraftType("A380"), Times.Once());

            _StandingDataManager.Raise(r => r.LoadCompleted += null, EventArgs.Empty);
            _StandingDataManager.Verify(r => r.FindAircraftType("A380"), Times.Once());
        }

        [TestMethod]
        public void BaseStationAircraftList_StandingDataManager_LoadCompleted_Does_Not_Reload_AircraftType_Information_If_Aircraft_Already_Have_Type_Information()
        {
            _AircraftList.Start();

            _Port30003Listener.Raise(r => r.Port30003MessageReceived += null, _BaseStationMessageEventArgs);

            _StandingDataManager.Raise(r => r.LoadCompleted += null, EventArgs.Empty);
            _StandingDataManager.Verify(r => r.FindAircraftType(It.IsAny<string>()), Times.Never());
        }
        #endregion
    }
}
