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
using System.Drawing.Imaging;

namespace VirtualRadar.Interface.WebServer
{
    /// <summary>
    /// The interface for objects that can fill <see cref="IResponse"/> objects correctly for different
    /// types of content.
    /// </summary>
    public interface IResponder
    {
        /// <summary>
        /// Sends the audio back to the browser. The format of the audio is inferred from the MIME type.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="audio"></param>
        /// <param name="mimeType"></param>
        void SendAudio(IResponse response, byte[] audio, string mimeType);

        /// <summary>
        /// Sends the bytes back to the browser.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="binary"></param>
        /// <param name="mimeType"></param>
        void SendBinary(IResponse response, byte[] binary, string mimeType);

        /// <summary>
        /// Sends a copy of the image in the correct format back to the browser.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="image"></param>
        /// <param name="format"></param>
        /// <remarks>
        /// The supported image formats are PNG, GIF and BMP.
        /// </remarks>
        void SendImage(IResponse response, Image image, ImageFormat format);

        /// <summary>
        /// Formats the object as a JSON object and sends it via the response to the browser.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="json"></param>
        /// <param name="jsonpFunctionName"></param>
        void SendJson(IResponse response, object json, string jsonpFunctionName);

        /// <summary>
        /// Configures the response object to send text back to the browser.
        /// </summary>
        /// <param name="response">The response object to fill in - must be supplied.</param>
        /// <param name="text">The text to send back - defaults to an empty string.</param>
        /// <param name="encoding">The encoding to use when sending the text - defaults to UTF8.</param>
        /// <param name="mimeType">The <see cref="MimeType"/> to use when sending the text - defaults to MimeType.Text.</param>
        void SendText(IResponse response, string text, Encoding encoding, string mimeType);
    }
}
