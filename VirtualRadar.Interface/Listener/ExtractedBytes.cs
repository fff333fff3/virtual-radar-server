﻿// Copyright © 2012 onwards, Andrew Whewell
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

namespace VirtualRadar.Interface.Listener
{
    #pragma warning disable 0659 // Overrides Equals but not GetHashCode - the object is mutable and cannot be safely used as a key, no requirement to implement GetHashCode
    /// <summary>
    /// The object returned by message extractors that describes the bytes corresponding to one message.
    /// </summary>
    public class ExtractedBytes : ICloneable
    {
        /// <summary>
        /// Gets or sets the array of bytes extracted from the source that describes one complete message.
        /// </summary>
        public byte[] Bytes { get; set; }

        /// <summary>
        /// Gets or sets the start of the payload within <see cref="Bytes"/>.
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Gets or sets the length of the payload within <see cref="Bytes"/>.
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// Gets or sets the format that the content of <see cref="Bytes"/> is in.
        /// </summary>
        public ExtractedBytesFormat Format { get; set; }

        /// <summary>
        /// Gets or sets a value indicating that the checksum on the extracted bytes was invalid (when applicable).
        /// </summary>
        public bool ChecksumFailed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating that the bytes have parity applied to them (when applicable).
        /// </summary>
        public bool HasParity { get; set; }

        /// <summary>
        /// Returns a deep copy of the object.
        /// </summary>
        /// <returns></returns>
        public virtual object Clone()
        {
            var newBytes = Bytes == null ? null : new byte[Bytes.Length];
            if(newBytes != null) Array.Copy(Bytes, newBytes, Bytes.Length);

            var result = (ExtractedBytes)Activator.CreateInstance(GetType());
            result.Bytes = newBytes;
            result.Offset = Offset;
            result.Length = Length;
            result.Format = Format;
            result.ChecksumFailed = ChecksumFailed;
            result.HasParity = HasParity;

            return result;
        }

        /// <summary>
        /// See base docs.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        /// <remarks>
        /// Note that Equals is overridden to detect identical content - it is NOT implemented to allow the class to be used
        /// as a key. The class is mutable. It must not be used as a key in dictionaries, hash tables etc.
        /// </remarks>
        public override bool Equals(object obj)
        {
            bool result = Object.ReferenceEquals(this, obj);
            if(!result) {
                var other = obj as ExtractedBytes;
                result = (other.Bytes == null && Bytes == null) ||
                         (other.Bytes != null && Bytes != null && Bytes.SequenceEqual(other.Bytes));
                if(result) result = other.ChecksumFailed == ChecksumFailed &&
                                    other.Format == Format &&
                                    other.HasParity == HasParity &&
                                    other.Length == Length &&
                                    other.Offset == Offset;
            }

            return result;
        }
    }
    #pragma warning restore 0659
}
