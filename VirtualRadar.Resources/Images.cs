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
using System.Drawing;

namespace VirtualRadar.Resources
{
    /// <summary>
    /// A static class that exposes all of the images in the common resources.
    /// </summary>
    /// <remarks><para>
    /// These images are read/write properties. The application never writes to the properties but
    /// a plugin that wanted to change the graphics used by the application could do so by assigning
    /// new images to the appropriate properties of the class. Care should be taken to allow enough
    /// room on images that are rotated by the website and to replace existing images with images
    /// of the same dimensions and colour depth.
    /// </para><para>
    /// Assigning null to an image resets it back to the default image.
    /// </para><para>
    /// The body of this class was generated using this LINQPad script:
    /// </para><code>
    /// var fileNames = Directory.GetFiles(@&quot;REPLACE-ME-WITH-APPROPRIATE-PATH-TO-IMAGES&quot;);
    ///
    /// foreach(var path in fileNames.OrderBy(p =&gt; p)) {
    ///     var name = Path.GetFileNameWithoutExtension(path);
    ///     var fileName = Path.GetFileName(path);
    ///     var type = Path.GetExtension(path).ToUpper() == &quot;.ICO&quot; ? &quot;Icon&quot; : &quot;Bitmap&quot;;
	///     
    ///     Console.WriteLine(@&quot;        private static {0} _{1};&quot;, type, name);
    ///     Console.WriteLine(@&quot;        /// &lt;summary&gt;&quot;);
    ///     Console.WriteLine(@&quot;        /// Gets or sets the {0} image.&quot;, name);
    ///     Console.WriteLine(@&quot;        /// &lt;/summary&gt;&quot;);
    ///     Console.WriteLine(@&quot;        /// &lt;remarks&gt;&quot;);
    ///     Console.WriteLine(@&quot;        /// &lt;img src=&quot;&quot;../Images/{0}&quot;&quot; alt=&quot;&quot;&quot;&quot; title=&quot;&quot;{1}&quot;&quot; /&gt;&quot;, fileName, name);
    ///     Console.WriteLine(@&quot;        /// &lt;/remarks&gt;&quot;);
    ///     Console.WriteLine(@&quot;        public static {0} {1}&quot;, type, name);
    ///     Console.WriteLine(@&quot;        {&quot;);
    ///     Console.WriteLine(@&quot;            get {{ return _{0} ?? InternalResources.{0}; }}&quot;, name);
    ///     Console.WriteLine(@&quot;            set {{ _{0} = value; }}&quot;, name);
    ///     Console.WriteLine(@&quot;        }&quot;);
    ///     
    ///     Console.WriteLine();
    /// }
    /// </code>
    /// </remarks>
    public static class Images
    {
        private static Icon _ApplicationIcon;
        /// <summary>
        /// Gets or sets the ApplicationIcon image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/ApplicationIcon.ico" alt="" title="ApplicationIcon" />
        /// </remarks>
        public static Icon ApplicationIcon
        {
            get { return _ApplicationIcon ?? InternalResources.ApplicationIcon; }
            set { _ApplicationIcon = value; }
        }

        private static Bitmap _BlueBall;
        /// <summary>
        /// Gets or sets the BlueBall image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/BlueBall.png" alt="" title="BlueBall" />
        /// </remarks>
        public static Bitmap BlueBall
        {
            get { return _BlueBall ?? InternalResources.BlueBall; }
            set { _BlueBall = value; }
        }

        private static Bitmap _ChevronBlueCircle;
        /// <summary>
        /// Gets or sets the ChevronBlueCircle image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/ChevronBlueCircle.png" alt="" title="ChevronBlueCircle" />
        /// </remarks>
        public static Bitmap ChevronBlueCircle
        {
            get { return _ChevronBlueCircle ?? InternalResources.ChevronBlueCircle; }
            set { _ChevronBlueCircle = value; }
        }

        private static Bitmap _ChevronGreenCircle;
        /// <summary>
        /// Gets or sets the ChevronGreenCircle image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/ChevronGreenCircle.png" alt="" title="ChevronGreenCircle" />
        /// </remarks>
        public static Bitmap ChevronGreenCircle
        {
            get { return _ChevronGreenCircle ?? InternalResources.ChevronGreenCircle; }
            set { _ChevronGreenCircle = value; }
        }

        private static Bitmap _ChevronRedCircle;
        /// <summary>
        /// Gets or sets the ChevronRedCircle image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/ChevronRedCircle.png" alt="" title="ChevronRedCircle" />
        /// </remarks>
        public static Bitmap ChevronRedCircle
        {
            get { return _ChevronRedCircle ?? InternalResources.ChevronRedCircle; }
            set { _ChevronRedCircle = value; }
        }

        private static Bitmap _CloseSlider;
        /// <summary>
        /// Gets or sets the CloseSlider image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/CloseSlider.png" alt="" title="CloseSlider" />
        /// </remarks>
        public static Bitmap CloseSlider
        {
            get { return _CloseSlider ?? InternalResources.CloseSlider; }
            set { _CloseSlider = value; }
        }

        private static Bitmap _Collapse;
        /// <summary>
        /// Gets or sets the Collapse image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/Collapse.png" alt="" title="Collapse" />
        /// </remarks>
        public static Bitmap Collapse
        {
            get { return _Collapse ?? InternalResources.Collapse; }
            set { _Collapse = value; }
        }

        private static Bitmap _Compass;
        /// <summary>
        /// Gets or sets the Compass image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/Compass.png" alt="" title="Compass" />
        /// </remarks>
        public static Bitmap Compass
        {
            get { return _Compass ?? InternalResources.Compass; }
            set { _Compass = value; }
        }

        private static Bitmap _Corner_BottomLeft;
        /// <summary>
        /// Gets or sets the Corner_BottomLeft image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/Corner_BottomLeft.png" alt="" title="Corner_BottomLeft" />
        /// </remarks>
        public static Bitmap Corner_BottomLeft
        {
            get { return _Corner_BottomLeft ?? InternalResources.Corner_BottomLeft; }
            set { _Corner_BottomLeft = value; }
        }

        private static Bitmap _Corner_BottomRight;
        /// <summary>
        /// Gets or sets the Corner_BottomRight image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/Corner_BottomRight.png" alt="" title="Corner_BottomRight" />
        /// </remarks>
        public static Bitmap Corner_BottomRight
        {
            get { return _Corner_BottomRight ?? InternalResources.Corner_BottomRight; }
            set { _Corner_BottomRight = value; }
        }

        private static Bitmap _Corner_TopLeft;
        /// <summary>
        /// Gets or sets the Corner_TopLeft image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/Corner_TopLeft.png" alt="" title="Corner_TopLeft" />
        /// </remarks>
        public static Bitmap Corner_TopLeft
        {
            get { return _Corner_TopLeft ?? InternalResources.Corner_TopLeft; }
            set { _Corner_TopLeft = value; }
        }

        private static Bitmap _Corner_TopRight;
        /// <summary>
        /// Gets or sets the Corner_TopRight image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/Corner_TopRight.png" alt="" title="Corner_TopRight" />
        /// </remarks>
        public static Bitmap Corner_TopRight
        {
            get { return _Corner_TopRight ?? InternalResources.Corner_TopRight; }
            set { _Corner_TopRight = value; }
        }

        private static Bitmap _Crosshair;
        /// <summary>
        /// Gets or sets the Crosshair image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/Crosshair.png" alt="" title="Crosshair" />
        /// </remarks>
        public static Bitmap Crosshair
        {
            get { return _Crosshair ?? InternalResources.Crosshair; }
            set { _Crosshair = value; }
        }

        private static Bitmap _Expand;
        /// <summary>
        /// Gets or sets the Expand image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/Expand.png" alt="" title="Expand" />
        /// </remarks>
        public static Bitmap Expand
        {
            get { return _Expand ?? InternalResources.Expand; }
            set { _Expand = value; }
        }

        private static Icon _Favicon;
        /// <summary>
        /// Gets or sets the Favicon image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/Favicon.ico" alt="" title="Favicon" />
        /// </remarks>
        public static Icon Favicon
        {
            get { return _Favicon ?? InternalResources.Favicon; }
            set { _Favicon = value; }
        }

        private static Bitmap _GotoCurrentLocation;
        /// <summary>
        /// Gets or sets the GotoCurrentLocation image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/GotoCurrentLocation.png" alt="" title="GotoCurrentLocation" />
        /// </remarks>
        public static Bitmap GotoCurrentLocation
        {
            get { return _GotoCurrentLocation ?? InternalResources.GotoCurrentLocation; }
            set { _GotoCurrentLocation = value; }
        }

        private static Bitmap _HelpAbout;
        /// <summary>
        /// Gets or sets the HelpAbout image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/HelpAbout.png" alt="" title="HelpAbout" />
        /// </remarks>
        public static Bitmap HelpAbout
        {
            get { return _HelpAbout ?? InternalResources.HelpAbout; }
            set { _HelpAbout = value; }
        }

        private static Bitmap _HideList;
        /// <summary>
        /// Gets or sets the HideList image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/HideList.png" alt="" title="HideList" />
        /// </remarks>
        public static Bitmap HideList
        {
            get { return _HideList ?? InternalResources.HideList; }
            set { _HideList = value; }
        }

        private static Bitmap _IPadSplash;
        /// <summary>
        /// Gets or sets the IPadSplash image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/IPadSplash.png" alt="" title="IPadSplash" />
        /// </remarks>
        public static Bitmap IPadSplash
        {
            get { return _IPadSplash ?? InternalResources.IPadSplash; }
            set { _IPadSplash = value; }
        }

        private static Bitmap _IPhoneBackButton;
        /// <summary>
        /// Gets or sets the IPhoneBackButton image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/IPhoneBackButton.png" alt="" title="IPhoneBackButton" />
        /// </remarks>
        public static Bitmap IPhoneBackButton
        {
            get { return _IPhoneBackButton ?? InternalResources.IPhoneBackButton; }
            set { _IPhoneBackButton = value; }
        }

        private static Bitmap _IPhoneBlueButton;
        /// <summary>
        /// Gets or sets the IPhoneBlueButton image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/IPhoneBlueButton.png" alt="" title="IPhoneBlueButton" />
        /// </remarks>
        public static Bitmap IPhoneBlueButton
        {
            get { return _IPhoneBlueButton ?? InternalResources.IPhoneBlueButton; }
            set { _IPhoneBlueButton = value; }
        }

        private static Bitmap _IPhoneChevron;
        /// <summary>
        /// Gets or sets the IPhoneChevron image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/IPhoneChevron.png" alt="" title="IPhoneChevron" />
        /// </remarks>
        public static Bitmap IPhoneChevron
        {
            get { return _IPhoneChevron ?? InternalResources.IPhoneChevron; }
            set { _IPhoneChevron = value; }
        }

        private static Bitmap _IPhoneGrayButton;
        /// <summary>
        /// Gets or sets the IPhoneGrayButton image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/IPhoneGrayButton.png" alt="" title="IPhoneGrayButton" />
        /// </remarks>
        public static Bitmap IPhoneGrayButton
        {
            get { return _IPhoneGrayButton ?? InternalResources.IPhoneGrayButton; }
            set { _IPhoneGrayButton = value; }
        }

        private static Bitmap _IPhoneIcon;
        /// <summary>
        /// Gets or sets the IPhoneIcon image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/IPhoneIcon.png" alt="" title="IPhoneIcon" />
        /// </remarks>
        public static Bitmap IPhoneIcon
        {
            get { return _IPhoneIcon ?? InternalResources.IPhoneIcon; }
            set { _IPhoneIcon = value; }
        }

        private static Bitmap _IPhoneListGroup;
        /// <summary>
        /// Gets or sets the IPhoneListGroup image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/IPhoneListGroup.png" alt="" title="IPhoneListGroup" />
        /// </remarks>
        public static Bitmap IPhoneListGroup
        {
            get { return _IPhoneListGroup ?? InternalResources.IPhoneListGroup; }
            set { _IPhoneListGroup = value; }
        }

        private static Bitmap _IPhoneOnOff;
        /// <summary>
        /// Gets or sets the IPhoneOnOff image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/IPhoneOnOff.png" alt="" title="IPhoneOnOff" />
        /// </remarks>
        public static Bitmap IPhoneOnOff
        {
            get { return _IPhoneOnOff ?? InternalResources.IPhoneOnOff; }
            set { _IPhoneOnOff = value; }
        }

        private static Bitmap _IPhonePinstripes;
        /// <summary>
        /// Gets or sets the IPhonePinstripes image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/IPhonePinstripes.png" alt="" title="IPhonePinstripes" />
        /// </remarks>
        public static Bitmap IPhonePinstripes
        {
            get { return _IPhonePinstripes ?? InternalResources.IPhonePinstripes; }
            set { _IPhonePinstripes = value; }
        }

        private static Bitmap _IPhoneSelectedTick;
        /// <summary>
        /// Gets or sets the IPhoneSelectedTick image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/IPhoneSelectedTick.png" alt="" title="IPhoneSelectedTick" />
        /// </remarks>
        public static Bitmap IPhoneSelectedTick
        {
            get { return _IPhoneSelectedTick ?? InternalResources.IPhoneSelectedTick; }
            set { _IPhoneSelectedTick = value; }
        }

        private static Bitmap _IPhoneSelection;
        /// <summary>
        /// Gets or sets the IPhoneSelection image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/IPhoneSelection.png" alt="" title="IPhoneSelection" />
        /// </remarks>
        public static Bitmap IPhoneSelection
        {
            get { return _IPhoneSelection ?? InternalResources.IPhoneSelection; }
            set { _IPhoneSelection = value; }
        }

        private static Bitmap _IPhoneSplash;
        /// <summary>
        /// Gets or sets the IPhoneSplash image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/IPhoneSplash.png" alt="" title="IPhoneSplash" />
        /// </remarks>
        public static Bitmap IPhoneSplash
        {
            get { return _IPhoneSplash ?? InternalResources.IPhoneSplash; }
            set { _IPhoneSplash = value; }
        }

        private static Bitmap _IPhoneToolbar;
        /// <summary>
        /// Gets or sets the IPhoneToolbar image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/IPhoneToolbar.png" alt="" title="IPhoneToolbar" />
        /// </remarks>
        public static Bitmap IPhoneToolbar
        {
            get { return _IPhoneToolbar ?? InternalResources.IPhoneToolbar; }
            set { _IPhoneToolbar = value; }
        }

        private static Bitmap _IPhoneToolButton;
        /// <summary>
        /// Gets or sets the IPhoneToolButton image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/IPhoneToolButton.png" alt="" title="IPhoneToolButton" />
        /// </remarks>
        public static Bitmap IPhoneToolButton
        {
            get { return _IPhoneToolButton ?? InternalResources.IPhoneToolButton; }
            set { _IPhoneToolButton = value; }
        }

        private static Bitmap _IPhoneWhiteButton;
        /// <summary>
        /// Gets or sets the IPhoneWhiteButton image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/IPhoneWhiteButton.png" alt="" title="IPhoneWhiteButton" />
        /// </remarks>
        public static Bitmap IPhoneWhiteButton
        {
            get { return _IPhoneWhiteButton ?? InternalResources.IPhoneWhiteButton; }
            set { _IPhoneWhiteButton = value; }
        }

        private static Bitmap _Logo128x128;
        /// <summary>
        /// Gets or sets the Logo128x128 image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/Logo128x128.png" alt="" title="Logo128x128" />
        /// </remarks>
        public static Bitmap Logo128x128
        {
            get { return _Logo128x128 ?? InternalResources.Logo128x128; }
            set { _Logo128x128 = value; }
        }

        private static Bitmap _Marker_Airplane;
        /// <summary>
        /// Gets or sets the Marker_Airplane image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/Marker_Airplane.png" alt="" title="Marker_Airplane" />
        /// </remarks>
        public static Bitmap Marker_Airplane
        {
            get { return _Marker_Airplane ?? InternalResources.Marker_Airplane; }
            set { _Marker_Airplane = value; }
        }

        private static Bitmap _Marker_AirplaneSelected;
        /// <summary>
        /// Gets or sets the Marker_AirplaneSelected image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/Marker_AirplaneSelected.png" alt="" title="Marker_AirplaneSelected" />
        /// </remarks>
        public static Bitmap Marker_AirplaneSelected
        {
            get { return _Marker_AirplaneSelected ?? InternalResources.Marker_AirplaneSelected; }
            set { _Marker_AirplaneSelected = value; }
        }

        private static Bitmap _MovingMapChecked;
        /// <summary>
        /// Gets or sets the MovingMapChecked image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/MovingMapChecked.png" alt="" title="MovingMapChecked" />
        /// </remarks>
        public static Bitmap MovingMapChecked
        {
            get { return _MovingMapChecked ?? InternalResources.MovingMapChecked; }
            set { _MovingMapChecked = value; }
        }

        private static Bitmap _MovingMapUnchecked;
        /// <summary>
        /// Gets or sets the MovingMapUnchecked image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/MovingMapUnchecked.png" alt="" title="MovingMapUnchecked" />
        /// </remarks>
        public static Bitmap MovingMapUnchecked
        {
            get { return _MovingMapUnchecked ?? InternalResources.MovingMapUnchecked; }
            set { _MovingMapUnchecked = value; }
        }

        private static Bitmap _OpenSlider;
        /// <summary>
        /// Gets or sets the OpenSlider image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/OpenSlider.png" alt="" title="OpenSlider" />
        /// </remarks>
        public static Bitmap OpenSlider
        {
            get { return _OpenSlider ?? InternalResources.OpenSlider; }
            set { _OpenSlider = value; }
        }

        private static Bitmap _RowHeader;
        /// <summary>
        /// Gets or sets the RowHeader image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/RowHeader.png" alt="" title="RowHeader" />
        /// </remarks>
        public static Bitmap RowHeader
        {
            get { return _RowHeader ?? InternalResources.RowHeader; }
            set { _RowHeader = value; }
        }

        private static Bitmap _RowHeaderSelected;
        /// <summary>
        /// Gets or sets the RowHeaderSelected image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/RowHeaderSelected.png" alt="" title="RowHeaderSelected" />
        /// </remarks>
        public static Bitmap RowHeaderSelected
        {
            get { return _RowHeaderSelected ?? InternalResources.RowHeaderSelected; }
            set { _RowHeaderSelected = value; }
        }

        private static Bitmap _ShowList;
        /// <summary>
        /// Gets or sets the ShowList image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/ShowList.png" alt="" title="ShowList" />
        /// </remarks>
        public static Bitmap ShowList
        {
            get { return _ShowList ?? InternalResources.ShowList; }
            set { _ShowList = value; }
        }

        private static Bitmap _SmallPlaneNorth;
        /// <summary>
        /// Gets or sets the SmallPlaneNorth image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/SmallPlaneNorth.png" alt="" title="SmallPlaneNorth" />
        /// </remarks>
        public static Bitmap SmallPlaneNorth
        {
            get { return _SmallPlaneNorth ?? InternalResources.SmallPlaneNorth; }
            set { _SmallPlaneNorth = value; }
        }

        private static Bitmap _TestSquare;
        /// <summary>
        /// Gets or sets the TestSquare image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/TestSquare.png" alt="" title="TestSquare" />
        /// </remarks>
        public static Bitmap TestSquare
        {
            get { return _TestSquare ?? InternalResources.TestSquare; }
            set { _TestSquare = value; }
        }

        private static Bitmap _Transparent_25;
        /// <summary>
        /// Gets or sets the Transparent_25 image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/Transparent_25.png" alt="" title="Transparent_25" />
        /// </remarks>
        public static Bitmap Transparent_25
        {
            get { return _Transparent_25 ?? InternalResources.Transparent_25; }
            set { _Transparent_25 = value; }
        }

        private static Bitmap _Transparent_50;
        /// <summary>
        /// Gets or sets the Transparent_50 image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/Transparent_50.png" alt="" title="Transparent_50" />
        /// </remarks>
        public static Bitmap Transparent_50
        {
            get { return _Transparent_50 ?? InternalResources.Transparent_50; }
            set { _Transparent_50 = value; }
        }

        private static Bitmap _Volume0;
        /// <summary>
        /// Gets or sets the Volume0 image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/Volume0.png" alt="" title="Volume0" />
        /// </remarks>
        public static Bitmap Volume0
        {
            get { return _Volume0 ?? InternalResources.Volume0; }
            set { _Volume0 = value; }
        }

        private static Bitmap _Volume100;
        /// <summary>
        /// Gets or sets the Volume100 image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/Volume100.png" alt="" title="Volume100" />
        /// </remarks>
        public static Bitmap Volume100
        {
            get { return _Volume100 ?? InternalResources.Volume100; }
            set { _Volume100 = value; }
        }

        private static Bitmap _Volume25;
        /// <summary>
        /// Gets or sets the Volume25 image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/Volume25.png" alt="" title="Volume25" />
        /// </remarks>
        public static Bitmap Volume25
        {
            get { return _Volume25 ?? InternalResources.Volume25; }
            set { _Volume25 = value; }
        }

        private static Bitmap _Volume50;
        /// <summary>
        /// Gets or sets the Volume50 image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/Volume50.png" alt="" title="Volume50" />
        /// </remarks>
        public static Bitmap Volume50
        {
            get { return _Volume50 ?? InternalResources.Volume50; }
            set { _Volume50 = value; }
        }

        private static Bitmap _Volume75;
        /// <summary>
        /// Gets or sets the Volume75 image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/Volume75.png" alt="" title="Volume75" />
        /// </remarks>
        public static Bitmap Volume75
        {
            get { return _Volume75 ?? InternalResources.Volume75; }
            set { _Volume75 = value; }
        }

        private static Bitmap _VolumeDown;
        /// <summary>
        /// Gets or sets the VolumeDown image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/VolumeDown.png" alt="" title="VolumeDown" />
        /// </remarks>
        public static Bitmap VolumeDown
        {
            get { return _VolumeDown ?? InternalResources.VolumeDown; }
            set { _VolumeDown = value; }
        }

        private static Bitmap _VolumeMute;
        /// <summary>
        /// Gets or sets the VolumeMute image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/VolumeMute.png" alt="" title="VolumeMute" />
        /// </remarks>
        public static Bitmap VolumeMute
        {
            get { return _VolumeMute ?? InternalResources.VolumeMute; }
            set { _VolumeMute = value; }
        }

        private static Bitmap _VolumeUp;
        /// <summary>
        /// Gets or sets the VolumeUp image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/VolumeUp.png" alt="" title="VolumeUp" />
        /// </remarks>
        public static Bitmap VolumeUp
        {
            get { return _VolumeUp ?? InternalResources.VolumeUp; }
            set { _VolumeUp = value; }
        }

        private static Bitmap _WarningBitmap;
        /// <summary>
        /// Gets or sets the WarningBitmap image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/WarningBitmap.png" alt="" title="WarningBitmap" />
        /// </remarks>
        public static Bitmap WarningBitmap
        {
            get { return _WarningBitmap ?? InternalResources.WarningBitmap; }
            set { _WarningBitmap = value; }
        }

        private static Icon _WarningIcon;
        /// <summary>
        /// Gets or sets the WarningIcon image.
        /// </summary>
        /// <remarks>
        /// <img src="../Images/WarningIcon.ico" alt="" title="WarningIcon" />
        /// </remarks>
        public static Icon WarningIcon
        {
            get { return _WarningIcon ?? InternalResources.WarningIcon; }
            set { _WarningIcon = value; }
        }
    }
}
