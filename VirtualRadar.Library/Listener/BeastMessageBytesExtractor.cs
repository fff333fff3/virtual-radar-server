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
using VirtualRadar.Interface.Listener;

namespace VirtualRadar.Library.Listener
{
    /// <summary>
    /// The default implementation of <see cref="IBeastMessageBytesExtractor"/>.
    /// </summary>
    class BeastMessageBytesExtractor : IBeastMessageBytesExtractor
    {
        /// <summary>
        /// The buffer of unprocessed bytes from the last read.
        /// </summary>
        private byte[] _ReadBuffer;

        /// <summary>
        /// The number of unprocessed bytes within the read buffer.
        /// </summary>
        private int _ReadBufferLength;

        /// <summary>
        /// The buffer that we use (and reuse) when extracting the unstuffed packet content from the stream.
        /// </summary>
        private byte[] _Payload;

        /// <summary>
        /// True once the first packet has been received.
        /// </summary>
        private bool _SeenFirstPacket;

        /// <summary>
        /// True if the incoming bytes are in binary format.
        /// </summary>
        private bool _IsBinaryFormat;

        /// <summary>
        /// True if the incoming AVR format bytes have parity applied.
        /// </summary>
        private bool _HasParity;

        /// <summary>
        /// True if the incoming AVR format bytes have MLAT prefixes.
        /// </summary>
        private bool _HasMlatPrefix;

        /// <summary>
        /// The byte that starts the AVR messages.
        /// </summary>
        private byte _AvrMessageStartIndicator;

        /// <summary>
        /// True if we know what format the stream is in.
        /// </summary>
        private bool _StreamFormatEstablished;

        /// <summary>
        /// The object that we return extracted bytes in.
        /// </summary>
        private ExtractedBytes _ExtractedBytes = new ExtractedBytes() { Format = ExtractedBytesFormat.ModeS };

        /// <summary>
        /// See interface docs.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <param name="bytesRead"></param>
        /// <returns></returns>
        public IEnumerable<ExtractedBytes> ExtractMessageBytes(byte[] bytes, int offset, int bytesRead)
        {
            var length = _ReadBufferLength + bytesRead;
            if(_ReadBuffer == null || length > _ReadBuffer.Length) {
                var newReadBuffer = new byte[length];
                if(_ReadBuffer != null) _ReadBuffer.CopyTo(newReadBuffer, 0);
                _ReadBuffer = newReadBuffer;
            }
            Array.ConstrainedCopy(bytes, 0, _ReadBuffer, _ReadBufferLength, bytesRead);
            _ReadBufferLength = length;

            if(EstablishStreamFormat()) {
                var startOfPacket = FindStartIndex(0);

                if(!_SeenFirstPacket && startOfPacket == 1) startOfPacket = FindStartIndex(startOfPacket);

                int firstByteAfterLastValidPacket = -1;
                while(startOfPacket != -1 && startOfPacket < _ReadBufferLength) {
                    int endOfPacket, dataLength = 0;
                    if(!_IsBinaryFormat) endOfPacket = Array.IndexOf<byte>(_ReadBuffer, 0x3B, startOfPacket, _ReadBufferLength - startOfPacket);
                    else                 endOfPacket = ExtractBinaryPayload(ref startOfPacket, ref dataLength);
                    if(endOfPacket == -1) break;

                    _SeenFirstPacket = true;
                    firstByteAfterLastValidPacket = _IsBinaryFormat ? endOfPacket : endOfPacket + 1;

                    if(!_IsBinaryFormat) dataLength = ExtractAvrPayload(startOfPacket, endOfPacket, dataLength);

                    if(dataLength == 7 || dataLength == 14) {
                        _ExtractedBytes.Bytes = _Payload;
                        _ExtractedBytes.Offset = 0;
                        _ExtractedBytes.Length = dataLength;
                        _ExtractedBytes.HasParity = _HasParity || _IsBinaryFormat;

                        yield return _ExtractedBytes;
                    }

                    startOfPacket = FindStartIndex(firstByteAfterLastValidPacket);
                }

                if(firstByteAfterLastValidPacket != -1) {
                    var unusedBytesCount = _ReadBufferLength - firstByteAfterLastValidPacket;
                    if(unusedBytesCount > 0) {
                        if(unusedBytesCount > 1024) {
                            // We don't want the read buffer growing out of control when reading a source that doesn't contain
                            // anything that looks like valid messages
                            unusedBytesCount = 0;
                        } else {
                            for(int si = firstByteAfterLastValidPacket, di = 0;di < unusedBytesCount;++si, ++di) {
                                _ReadBuffer[di] = _ReadBuffer[si];
                            }
                        }
                    }
                    _ReadBufferLength = unusedBytesCount;
                }
            }
        }

        private bool EstablishStreamFormat()
        {
            if(!_StreamFormatEstablished) {
                _IsBinaryFormat = Array.IndexOf<byte>(_ReadBuffer, 0x1a, 0, _ReadBufferLength) != -1;

                if(_IsBinaryFormat) _StreamFormatEstablished = true;
                else if(_ReadBufferLength > 22) {
                    foreach(var ch in _ReadBuffer) {
                        switch(ch) {
                            case 0x2a:  // * format AVR: parity needs stripping, no mlat prefix
                                _StreamFormatEstablished = true;
                                _HasParity = true;
                                _AvrMessageStartIndicator = ch;
                                break;
                            case 0x3a:  // : format AVR: parity stripped, no mlat prefix
                                _StreamFormatEstablished = true;
                                _AvrMessageStartIndicator = ch;
                                break;
                            case 0x40:  // @ format AVR: parity needs stripping, has mlat prefix
                                _StreamFormatEstablished = true;
                                _HasParity = _HasMlatPrefix = true;
                                _AvrMessageStartIndicator = ch;
                                break;
                        }
                        if(_StreamFormatEstablished) break;
                    }

                    if(!_StreamFormatEstablished && _ReadBufferLength > 100) {
                        // Stop the read buffer from growing too large while it's filled with trash that obviously isn't coming from a radio
                        _ReadBufferLength = 0;
                    }
                }
            }

            return _StreamFormatEstablished;
        }

        private int FindStartIndex(int start)
        {
            int result = -1;

            if(!_IsBinaryFormat) {
                result = Array.IndexOf<byte>(_ReadBuffer, _AvrMessageStartIndicator, start, _ReadBufferLength - start);
                if(result != -1) ++result;
            } else {
                for(var i = start;i < _ReadBufferLength;++i) {
                    var ch = _ReadBuffer[i];
                    if(ch == 0x1a) {
                        if(++i < _ReadBufferLength) {
                            if(_ReadBuffer[i] != 0x1a) {
                                result = i;
                                break;
                            }
                        }
                    }
                }
            }

            return result;
        }

        private int ExtractBinaryPayload(ref int startOfPacket, ref int dataLength)
        {
            int endOfPacket;
            dataLength = 0;
            switch(_ReadBuffer[startOfPacket++]) {
                case 0x31: dataLength = 4; break;
                case 0x32: dataLength = 7; break;
                case 0x33: dataLength = 14; break;
            }

            if(_Payload == null || _Payload.Length < dataLength) _Payload = new byte[dataLength];
            int si = startOfPacket, di = 0;

            for(;si < _ReadBufferLength && di < 7;++si, ++di) {
                var ch = _ReadBuffer[si];
                if(ch == 0x1a && ++si >= _ReadBufferLength) break;
            }

            for(di = 0;si < _ReadBufferLength && di < dataLength;++si) {
                var ch = _ReadBuffer[si];
                if(ch == 0x1a) {
                    if(++si >= _ReadBufferLength) break;
                    ch = _ReadBuffer[si];
                }
                _Payload[di++] = ch;
            }

            endOfPacket = di != dataLength ? -1 : si;

            return endOfPacket;
        }

        private int ExtractAvrPayload(int startOfPacket, int endOfPacket, int dataLength)
        {
            if(_HasMlatPrefix) {
                var actualStartOfPacket = startOfPacket + 12;
                if(actualStartOfPacket >= _ReadBufferLength) actualStartOfPacket = endOfPacket;
                else if(!ConvertAsciiHexDigits(null, startOfPacket, actualStartOfPacket)) actualStartOfPacket = endOfPacket;
                startOfPacket = actualStartOfPacket;
            }

            dataLength = endOfPacket - startOfPacket;
            dataLength = (dataLength & 1) == 1 ? -1 : dataLength / 2;

            if(dataLength > 0 && dataLength < 15) {
                if(_Payload == null || _Payload.Length < dataLength) _Payload = new byte[dataLength];
                if(!ConvertAsciiHexDigits(_Payload, startOfPacket, endOfPacket)) dataLength = -1;
            }

            return dataLength;
        }

        private bool ConvertAsciiHexDigits(byte[] buffer, int start, int end)
        {
            bool result = true;

            for(int di = 0, si = start;si < end;++di, ++si) {
                var highNibble = ExtractHexDigitValue(_ReadBuffer[si]);
                var lowNibble = ExtractHexDigitValue(_ReadBuffer[++si]);

                if(highNibble == 0xff || lowNibble == 0xff) {
                    result = false;
                    break;
                }

                if(buffer != null) buffer[di] = (byte)((highNibble << 4) | lowNibble);
            }

            return result;
        }

        private byte ExtractHexDigitValue(byte b)
        {
            if(b >= 0x30 && b <= 0x39) return (byte)(b - 0x30);
            else if(b >= 0x41 && b <= 0x46) return (byte)(b - 0x37);
            else if(b >= 0x61 && b <= 0x66) return (byte)(b - 0x57);
            else return 0xff;
        }
    }
}
