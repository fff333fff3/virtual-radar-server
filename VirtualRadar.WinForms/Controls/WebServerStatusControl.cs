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
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using VirtualRadar.Interface;
using VirtualRadar.Localisation;
using InterfaceFactory;
using System.Drawing;

namespace VirtualRadar.WinForms.Controls
{
    /// <summary>
    /// A user control that shows the state of the web server and lets the user change it.
    /// </summary>
    public partial class WebServerStatusControl : UserControl
    {
        #region Properties
        private MonoAutoScaleMode _MonoAutoScaleMode;
        /// <summary>
        /// Gets or sets the AutoScaleMode.
        /// </summary>
        /// <remarks>Works around Mono's weirdness over AutoScaleMode and anchoring / docking - see the comments against MonoAutoScaleMode.</remarks>
        public new AutoScaleMode AutoScaleMode
        {
            get { return _MonoAutoScaleMode.AutoScaleMode; }
            set { _MonoAutoScaleMode.AutoScaleMode = value; }
        }

        private string _LocalAddress;
        /// <summary>
        /// Gets or sets the root URL to display when the user views the local loopback address of the server.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string LocalAddress
        {
            get { return _LocalAddress; }
            set { if(_LocalAddress != value) { _LocalAddress = value; ShowCorrectAddress(); } }
        }

        private string _NetworkAddress;
        /// <summary>
        /// Gets or sets the root URL to display when the user views the LAN address of the server.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string NetworkAddress
        {
            get { return _NetworkAddress; }
            set { if(_NetworkAddress != value) { _NetworkAddress = value; ShowCorrectAddress(); } }
        }

        private string _InternetAddress;
        /// <summary>
        /// Gets or sets the root URL to display when the user views the WAN address of the server.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string InternetAddress
        {
            get { return _InternetAddress; }
            set { if(_InternetAddress != value) { _InternetAddress = value; ShowCorrectAddress(); } }
        }

        /// <summary>
        /// Gets or sets the full URL of the server.
        /// </summary>
        protected string Address
        {
            get { return linkLabelAddress.Text; }
            set { linkLabelAddress.Text = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating that <see cref="Address"/> should show the local loopback address.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowLocalAddress
        {
            get { return comboBoxShowAddressType.Text == Strings.ShowLocalAddress; }
            set { comboBoxShowAddressType.Text = Strings.ShowLocalAddress; }
        }

        /// <summary>
        /// Gets or sets a value indicating that <see cref="Address"/> should show the LAN address.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowNetworkAddress
        {
            get { return comboBoxShowAddressType.Text == Strings.ShowNetworkAddress; }
            set { comboBoxShowAddressType.Text = Strings.ShowNetworkAddress; }
        }

        /// <summary>
        /// Gets or sets a value indicating that <see cref="Address"/> should show the WAN address.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowInternetAddress
        {
            get { return comboBoxShowAddressType.Text == Strings.ShowInternetAddress; }
            set { comboBoxShowAddressType.Text = Strings.ShowInternetAddress; }
        }

        /// <summary>
        /// Gets or sets a value indicating that <see cref="Address"/> should show the default page.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowDefaultSite
        {
            get { return comboBoxSite.Text == Strings.DefaultVersion; }
            set { comboBoxSite.Text = Strings.DefaultVersion; }
        }

        /// <summary>
        /// Gets or sets a value indicating that <see cref="Address"/> should show the page for the desktop version of the site.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowDesktopSite
        {
            get { return comboBoxSite.Text == Strings.DesktopVersion; }
            set { comboBoxSite.Text = Strings.DesktopVersion; }
        }

        /// <summary>
        /// Gets or sets a value indicating that <see cref="Address"/> should show the page for the Flight Simulator X version of the site.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowFlightSimSite
        {
            get { return comboBoxSite.Text == Strings.FlightSimVersion; }
            set { comboBoxSite.Text = Strings.FlightSimVersion; }
        }

        /// <summary>
        /// Gets or sets a value indicating that <see cref="Address"/> should show the page for the mobile version of the site.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowMobileSite
        {
            get { return comboBoxSite.Text == Strings.MobileVersion; }
            set { comboBoxSite.Text = Strings.MobileVersion; }
        }

        /// <summary>
        /// Gets a value indicating that the <see cref="Address"/> property represents a valid address.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsAddressValid
        {
            get { return Address.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase); }
        }

        private bool _ServerIsListening;
        /// <summary>
        /// Gets or sets a value indicating that the web server is online.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ServerIsListening
        {
            get { return _ServerIsListening; }
            set
            {
                _ServerIsListening = value;
                labelServerStatus.Text = value ? Strings.WebServerOnline : Strings.WebServerOffline;
                buttonToggleServerStatus.Text = value ? Strings.TakeWebServerOffline : Strings.TakeWebServerOnline;
            }
        }

        private bool _UPnpEnabled;
        /// <summary>
        /// Gets or sets a value indicating to the user that they have enabled the UPnP features of the program.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool UPnpEnabled
        {
            get { return _UPnpEnabled; }
            set { _UPnpEnabled = value; SetUPnpStatusText(); }
        }

        private bool _UPnpRouterPresent;
        /// <summary>
        /// Gets or sets a value indicating that a UPnP router was found on the network.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool UPnpRouterPresent
        {
            get { return _UPnpRouterPresent; }
            set { _UPnpRouterPresent = value; SetUPnpStatusText(); }
        }

        private bool _UPnpIsSupported;
        /// <summary>
        /// Gets or sets a value indicating whether UPnP is supported by the operating system.
        /// </summary>
        /// <remarks>
        /// This was detected via version checking in the original implementation of the UPnP stuff. I removed
        /// the check in the rewrite as it was too coarse and I didn't replace it with anything - if you're on
        /// Windows WebServer 2003 it will just say that no UPnP router is present. However I've left this in the
        /// display properties as it's fairly trivial and I might want to put it back in the future. Just be
        /// aware that nothing might be setting it.
        /// </remarks>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool UPnpIsSupported
        {
            get { return _UPnpIsSupported; }
            set { _UPnpIsSupported = value; SetUPnpStatusText(); }
        }

        private bool _UPnpPortForwardingActive;
        /// <summary>
        /// Gets or sets a value indicating that the UPnP router is mapping requests through to the server.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool UPnpPortForwardingActive
        {
            get { return _UPnpPortForwardingActive; }
            set { _UPnpPortForwardingActive = value; SetUPnpStatusText(); }
        }
        #endregion

        #region Events exposed
        /// <summary>
        /// Raised when the user wants to toggle the web server state.
        /// </summary>
        public event EventHandler ToggleServerStatus;

        /// <summary>
        /// Raises <see cref="ToggleServerStatus"/>.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnToggleServerStatus(EventArgs args)
        {
            if(ToggleServerStatus != null) ToggleServerStatus(this, args);
        }

        /// <summary>
        /// Raised when the user wants to take the web server onto and off the Internet.
        /// </summary>
        public event EventHandler ToggleUPnpStatus;

        /// <summary>
        /// Raises <see cref="ToggleUPnpStatus"/>.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnToggleUPnpStatus(EventArgs args)
        {
            if(ToggleUPnpStatus != null) ToggleUPnpStatus(this, args);
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new object.
        /// </summary>
        public WebServerStatusControl()
        {
            _MonoAutoScaleMode = new MonoAutoScaleMode(this);
            InitializeComponent();
        }
        #endregion

        #region ShowCorrectAddress, BrowseAddress, CopyAddressToClipboard, ShowLinkLabelContextMenu
        /// <summary>
        /// Displays the selected address in the <see cref="Address"/> control.
        /// </summary>
        private void ShowCorrectAddress()
        {
            if(InvokeRequired) BeginInvoke(new MethodInvoker(() => { ShowCorrectAddress(); }));
            else {
                Address = ShowLocalAddress ? LocalAddress : 
                          ShowNetworkAddress ? NetworkAddress :
                          InternetAddress;

                if(String.IsNullOrEmpty(Address)) Address = "Not yet known";
                else {
                    string page = null;
                    if(ShowDesktopSite) page = "GoogleMap.htm";
                    else if(ShowMobileSite) page = "iPhoneMap.htm";
                    else if(ShowFlightSimSite) page = "FlightSim.htm";

                    if(page != null) Address = String.Format("{0}{1}{2}", Address, Address.EndsWith("/") ? "" : "/", page);
                }
            }
        }

        /// <summary>
        /// Opens a web browser at the page specified by the address currently on display.
        /// </summary>
        private void BrowseAddress()
        {
            if(IsAddressValid) Process.Start(Address);
        }

        /// <summary>
        /// Copies the website address to the clipboard.
        /// </summary>
        private void CopyAddressToClipboard()
        {
            if(IsAddressValid) Clipboard.SetText(Address);
        }

        /// <summary>
        /// Displays the context menu for the link label. We don't use the normal Winforms context
        /// menu facility because the link label control is huge and we only want this displayed when
        /// the user clicks on the part of it that has text.
        /// </summary>
        private void ShowLinkLabelContextMenu()
        {
            contextMenuStripWebSiteLink.Show(Cursor.Position);
        }
        #endregion

        #region ShowWebRequestHasBeenServiced
        /// <summary>
        /// Reports to the user that a web request has been serviced. This is thread safe.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="url"></param>
        /// <param name="bytesSent"></param>
        public void ShowWebRequestHasBeenServiced(string address, string url, long bytesSent)
        {
            webServerUserList.UpdateEntry(address, DateTime.Now, url, bytesSent);
        }
        #endregion

        #region SetUPnpStatusText
        private void SetUPnpStatusText()
        {
            labelUPnpStatus.Text = !UPnpIsSupported ? Strings.UPnpNotSupported :
                                   !UPnpEnabled && UPnpPortForwardingActive ? Strings.UPnpActiveWhileDisabled :
                                   !UPnpEnabled ? Strings.UPnpDisabled :
                                   !UPnpRouterPresent ? Strings.UPnPNotPresent :
                                   !UPnpPortForwardingActive ? Strings.UPnpInactive :
                                   Strings.UPnpActive;

            buttonToggleUPnpStatus.Text = UPnpPortForwardingActive ? Strings.TakeOffInternet : Strings.PutOnInternet;
            buttonToggleUPnpStatus.Enabled = UPnpIsSupported && UPnpRouterPresent && UPnpEnabled;

            labelUPnpStatus.Enabled = UPnpIsSupported && UPnpRouterPresent && UPnpEnabled;
        }
        #endregion

        #region Events consumed
        /// <summary>
        /// Called when the control has been loaded but before it is shown to the user.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if(!DesignMode) {
                Localise.Control(this);

                comboBoxShowAddressType.Items.Add(Strings.ShowLocalAddress);
                comboBoxShowAddressType.Items.Add(Strings.ShowNetworkAddress);
                comboBoxShowAddressType.Items.Add(Strings.ShowInternetAddress);
                comboBoxShowAddressType.SelectedIndex = 0;

                comboBoxSite.Items.Add(Strings.DefaultVersion);
                comboBoxSite.Items.Add(Strings.DesktopVersion);
                comboBoxSite.Items.Add(Strings.MobileVersion);
                comboBoxSite.Items.Add(Strings.FlightSimVersion);
                comboBoxSite.SelectedIndex = 0;

                if(Factory.Singleton.Resolve<IRuntimeEnvironment>().Singleton.IsMono) {
                    linkLabelAddress.TextAlign = ContentAlignment.TopLeft;
                }
            }
        }

        private void linkLabelAddress_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if(e.Button != MouseButtons.Right) BrowseAddress();
            else ShowLinkLabelContextMenu();
        }

        private void buttonToggleServerStatus_Click(object sender, EventArgs e)
        {
            OnToggleServerStatus(EventArgs.Empty);
        }

        private void buttonToggleUPnpStatus_Click(object sender, EventArgs e)
        {
            OnToggleUPnpStatus(EventArgs.Empty);
        }

        private void openInBrowserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BrowseAddress();
        }

        private void copyLinkToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyAddressToClipboard();
        }

        private void comboBoxShowAddressType_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowCorrectAddress();
        }

        private void comboBoxSite_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowCorrectAddress();
        }
        #endregion
    }
}
