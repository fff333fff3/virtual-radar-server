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

namespace VirtualRadar.Interface.View
{
    /// <summary>
    /// The interface for views that display information about the application and contain small support helpers for the app.
    /// </summary>
    public interface IAboutView
    {
        /// <summary>
        /// Gets or sets the view's title.
        /// </summary>
        string Caption { get; set; }

        /// <summary>
        /// Gets or sets the product's name.
        /// </summary>
        string ProductName { get; set; }

        /// <summary>
        /// Gets or sets the application's version information.
        /// </summary>
        string Version { get; set; }

        /// <summary>
        /// Gets or sets a small summary of the application's copyright information.
        /// </summary>
        string Copyright { get; set; }

        /// <summary>
        /// Gets or sets a full description of the application, licenses etc.
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// Gets or sets the full path to the configuration folder used by the application to store
        /// configuration files and data.
        /// </summary>
        string ConfigurationFolder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating that VRS is running under Mono.
        /// </summary>
        bool IsMono { get; set; }

        /// <summary>
        /// Raised when the user wants to see the content of the configuration folder.
        /// </summary>
        event EventHandler OpenConfigurationFolderClicked;

        /// <summary>
        /// Displays the contents of the configuration folder to the user.
        /// </summary>
        void ShowConfigurationFolderContents();
    }
}
