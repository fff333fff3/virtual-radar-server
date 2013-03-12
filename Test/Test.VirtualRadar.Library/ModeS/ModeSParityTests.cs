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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualRadar.Interface.ModeS;
using InterfaceFactory;
using Test.Framework;

namespace Test.VirtualRadar.Library.ModeS
{
    [TestClass]
    public class ModeSParityTests
    {
        public TestContext TestContext { get; set; }

        private IModeSParity _ModeSParity;

        [TestInitialize]
        public void TestInitialise()
        {
            _ModeSParity = Factory.Singleton.Resolve<IModeSParity>();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ModeSParity_StripParity_Throws_If_Byte_Array_Is_Null()
        {
            _ModeSParity.StripParity(null, 0, 7);
        }

        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void ModeSParity_StripParity_Throws_If_Offset_Is_Not_Within_Bounds()
        {
            _ModeSParity.StripParity(new byte[7], 7, 1);
        }

        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void ModeSParity_StripParity_Throws_If_Offset_Is_Negative()
        {
            _ModeSParity.StripParity(new byte[7], -1, 1);
        }

        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void ModeSParity_StripParity_Throws_If_Length_Exceeds_Bytes_Available()
        {
            _ModeSParity.StripParity(new byte[6], 0, 7);
        }

        [TestMethod]
        [DataSource("Data Source='RawDecodingTests.xls';Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties='Excel 8.0'",
                    "StripParity$")]
        public void ModeSParity_StripParity_Removes_Parity_From_Last_Three_Bytes()
        {
            var worksheet = new ExcelWorksheetData(TestContext);

            var bytes = worksheet.Bytes("Bytes");
            var offset = worksheet.Int("Offset");
            var length = worksheet.Int("Length");

            _ModeSParity.StripParity(bytes, offset, length);

            Assert.IsTrue(worksheet.Bytes("Expected").SequenceEqual(bytes));
        }
    }
}
