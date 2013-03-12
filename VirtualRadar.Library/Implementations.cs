// Copyright � 2010 onwards, Andrew Whewell
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
using System.Text;
using VirtualRadar.Interface;
using InterfaceFactory;

namespace VirtualRadar.Library
{
    /// <summary>
    /// Initialises the class factory with all the standard implementations in this library.
    /// </summary>
    public static class Implementations
    {
        /// <summary>
        /// Initialises the class factory with all the standard implementations in this library.
        /// </summary>
        /// <param name="factory"></param>
        public static void Register(IClassFactory factory)
        {
            factory.Register<VirtualRadar.Interface.Adsb.IAdsbTranslator, Adsb.AdsbTranslator>();
            factory.Register<VirtualRadar.Interface.Adsb.ICompactPositionReporting, Adsb.CompactPositionReporting>();
            factory.Register<VirtualRadar.Interface.BaseStation.IBaseStationAircraftList, BaseStation.BaseStationAircraftList>();
            factory.Register<VirtualRadar.Interface.BaseStation.IBaseStationMessageCompressor, BaseStation.BaseStationMessageCompressor>();
            factory.Register<VirtualRadar.Interface.BaseStation.IBaseStationMessageTranslator, BaseStation.BaseStationMessageTranslator>();
            factory.Register<VirtualRadar.Interface.BaseStation.IRawMessageTranslator, BaseStation.RawMessageTranslator>();
            factory.Register<VirtualRadar.Interface.FlightSimulatorX.IFlightSimulatorX, FlightSimulatorX.FlightSimulatorX>();
            factory.Register<VirtualRadar.Interface.Listener.IAutoConfigListener, Listener.AutoConfigListener>();
            factory.Register<VirtualRadar.Interface.Listener.IBeastMessageBytesExtractor, Listener.BeastMessageBytesExtractor>();
            factory.Register<VirtualRadar.Interface.Listener.IListener, Listener.Listener>();
            factory.Register<VirtualRadar.Interface.Listener.IPort30003MessageBytesExtractor, Listener.Port30003MessageBytesExtractor>();
            factory.Register<VirtualRadar.Interface.Listener.ISbs3MessageBytesExtractor, Listener.Sbs3MessageBytesExtractor>();
            factory.Register<VirtualRadar.Interface.Listener.ISerialListenerProvider, Listener.SerialListenerProvider>();
            factory.Register<VirtualRadar.Interface.Listener.ITcpListenerProvider, Listener.TcpListenerProvider>();
            factory.Register<VirtualRadar.Interface.ModeS.IModeSParity, ModeS.ModeSParity>();
            factory.Register<VirtualRadar.Interface.ModeS.IModeSTranslator, ModeS.ModeSTranslator>();
            factory.Register<VirtualRadar.Interface.Presenter.IAboutPresenter, Presenter.AboutPresenter>();
            factory.Register<VirtualRadar.Interface.Presenter.IConnectionClientLogPresenter, Presenter.ConnectionClientLogPresenter>();
            factory.Register<VirtualRadar.Interface.Presenter.IConnectionSessionLogPresenter, Presenter.ConnectionSessionLogPresenter>();
            factory.Register<VirtualRadar.Interface.Presenter.IDownloadDataPresenter, Presenter.DownloadDataPresenter>();
            factory.Register<VirtualRadar.Interface.Presenter.IFlightSimulatorXPresenter, Presenter.FlightSimulatorXPresenter>();
            factory.Register<VirtualRadar.Interface.Presenter.IInvalidPluginsPresenter, Presenter.InvalidPluginsPresenter>();
            factory.Register<VirtualRadar.Interface.Presenter.IMainPresenter, Presenter.MainPresenter>();
            factory.Register<VirtualRadar.Interface.Presenter.IOptionsPresenter, Presenter.OptionsPresenter>();
            factory.Register<VirtualRadar.Interface.Presenter.IPluginsPresenter, Presenter.PluginsPresenter>();
            factory.Register<VirtualRadar.Interface.Presenter.IRebroadcastOptionsPresenter, Presenter.RebroadcastOptionsPresenter>();
            factory.Register<VirtualRadar.Interface.Presenter.IReceiverLocationsPresenter, Presenter.ReceiverLocationsPresenter>();
            factory.Register<VirtualRadar.Interface.Presenter.IShutdownPresenter, Presenter.ShutdownPresenter>();
            factory.Register<VirtualRadar.Interface.Presenter.ISplashPresenter, Presenter.SplashPresenter>();
            factory.Register<VirtualRadar.Interface.Presenter.IStatisticsPresenter, Presenter.StatisticsPresenter>();
            factory.Register<VirtualRadar.Interface.Settings.IConfigurationStorage, Settings.ConfigurationStorage>();
            factory.Register<VirtualRadar.Interface.Settings.IInstallerSettingsStorage, Settings.InstallerSettingsStorage>();
            factory.Register<VirtualRadar.Interface.Settings.IPluginManifestStorage, Settings.PluginManifestStorage>();
            factory.Register<VirtualRadar.Interface.Settings.IPluginSettingsStorage, Settings.PluginSettingsStorage>();
            factory.Register<IAircraft, Aircraft>();
            factory.Register<IAircraftComparer, AircraftComparer>();
            factory.Register<IAircraftPictureManager, AircraftPictureManager>();
            factory.Register<IAudio, Audio>();
            factory.Register<IAutoConfigPictureFolderCache, AutoConfigPictureFolderCache>();
            factory.Register<IBackgroundWorker, BackgroundWorker>();
            factory.Register<IBitStream, BitStream>();
            factory.Register<IBroadcastProvider, BroadcastProvider>();
            factory.Register<IConnectionLogger, ConnectionLogger>();
            factory.Register<IDirectoryCache, DirectoryCache>();
            factory.Register<IExternalIPAddressService, ExternalIPAddressService>();
            factory.Register<IHeartbeatService, HeartbeatService>();
            factory.Register<IImageFileManager, ParallelAccessImageFileManager>();
            factory.Register<ILog, Log>();
            factory.Register<INewVersionChecker, NewVersionChecker>();
            factory.Register<IPluginManager, PluginManager>();
            factory.Register<IRebroadcastServer, RebroadcastServer>();
            factory.Register<IRebroadcastServerManager, RebroadcastServerManager>();
            factory.Register<IRuntimeEnvironment, RuntimeEnvironment>();
            factory.Register<ISimpleAircraftList, SimpleAircraftList>();
            factory.Register<IStatistics, Statistics>();

            if(Type.GetType("Mono.Runtime") == null) {
                factory.Register<ISpeechSynthesizerWrapper, DotNetSpeechSynthesizerWrapper>();
                factory.Register<VirtualRadar.Interface.FlightSimulatorX.ISimConnectWrapper, FlightSimulatorX.DotNetSimConnectWrapper>();
            } else {
                factory.Register<ISpeechSynthesizerWrapper, MonoSpeechSynthesizerWrapper>();
                factory.Register<VirtualRadar.Interface.FlightSimulatorX.ISimConnectWrapper, FlightSimulatorX.MonoSimConnectWrapper>();
            }
        }
    }
}
