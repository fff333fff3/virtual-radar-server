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
using VirtualRadar.Interface.WebServer;
using System.Net;
using System.IO;

namespace VirtualRadar.WebServer
{
    /// <summary>
    /// A wrapper around HttpListenerResponse.
    /// </summary>
    class Response : IResponse
    {
        /// <summary>
        /// The response object that we are wrapping.
        /// </summary>
        private HttpListenerResponse _Response;

        /// <summary>
        /// See interface docs.
        /// </summary>
        public long ContentLength
        {
            get { return _Response.ContentLength64; }
            set { _Response.ContentLength64 = value; }
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public string MimeType
        {
            get { return _Response.ContentType; }
            set { _Response.ContentType = value; }
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public Stream OutputStream
        {
            get { return _Response.OutputStream; }
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public HttpStatusCode StatusCode
        {
            get { return (HttpStatusCode)_Response.StatusCode; }
            set { _Response.StatusCode = (int)value; }
        }

        /// <summary>
        /// Creates a new object.
        /// </summary>
        /// <param name="response"></param>
        public Response(HttpListenerResponse response)
        {
            _Response = response;
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void AddHeader(string name, string value)
        {
            _Response.AddHeader(name, value);
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        /// <param name="url"></param>
        public void Redirect(string url)
        {
            _Response.Redirect(url);
        }
    }
}
