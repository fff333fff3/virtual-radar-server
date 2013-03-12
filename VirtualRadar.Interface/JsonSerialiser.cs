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
using System.Runtime.Serialization;
using System.Reflection;
using System.IO;
using System.Collections;
using System.Threading;
using System.Globalization;

namespace VirtualRadar.Interface
{
    /// <summary>
    /// An object that can serialise (in a limited fashion) to JSON.
    /// </summary>
    /// <remarks><para>
    /// I originally used .NET's DataContractJsonSerializer to serialise to JSON, which worked just fine.
    /// However Mono's implementation of DataContractJsonSerializer has a bug - it ignores the EmitDefaultValue
    /// property on the DataMember attributes. This is bad news because I use that to indicate to the Javascript
    /// that a value hasn't changed - if the property isn't present in the JSON then it hasn't changed and the
    /// previous value should be used. This keeps the JSON sizes down, which is important when VRS is tracking
    /// 600+ aircraft.
    /// </para><para>
    /// This implementation uses the DataMember attributes to decide what to do with each property being serialised.
    /// There are big gaps in it, it only does enough to cover the serialisation cases for VRS.
    /// </para></remarks>
    public class JsonSerialiser
    {
        /// <summary>
        /// A private class describing a property in a known type.
        /// </summary>
        class PropertyDetail
        {
            public PropertyInfo PropertyInfo;
            public DataMemberAttribute DataMember;
            public string Name { get { return DataMember.Name ?? PropertyInfo.Name; } }
        }

        /// <summary>
        /// A map of object types to a dictionary of properties and their associated data members.
        /// </summary>
        private Dictionary<Type, List<PropertyDetail>> _TypePropertiesMap = new Dictionary<Type,List<PropertyDetail>>();

        /// <summary>
        /// A map of object types to their data contracts.
        /// </summary>
        private Dictionary<Type, DataContractAttribute> _TypeContractsMap = new Dictionary<Type,DataContractAttribute>();

        /// <summary>
        /// Initialises the serialiser.
        /// </summary>
        /// <param name="type"></param>
        public void Initialise(Type type)
        {
            if(type == null) throw new ArgumentNullException("type");
            MapType(type);
        }

        /// <summary>
        /// Builds up the map of properties and attributes.
        /// </summary>
        /// <param name="type"></param>
        private void MapType(Type type)
        {
            var contract = GetDataContract(type);
            if(contract == null) throw new InvalidOperationException(String.Format("{0} is not marked with the DataContract attribute", type.Name));

            _TypeContractsMap.Add(type, contract);

            var propertyList = new List<PropertyDetail>();
            foreach(var property in type.GetProperties()) {
                var memberAttribute = GetDataMember(property);
                if(memberAttribute != null) {
                    propertyList.Add(new PropertyDetail() { PropertyInfo = property, DataMember = memberAttribute });
                    if(property.PropertyType.IsClass) {
                        var propertyType = property.PropertyType;
                        if(propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>)) propertyType = propertyType.GetGenericArguments()[0];
                        if(propertyType.IsClass && propertyType != typeof(string) && !_TypeContractsMap.ContainsKey(propertyType)) MapType(propertyType);
                    }
                }
            }
            _TypePropertiesMap.Add(type, propertyList.OrderBy(p => p.Name).ToList());
        }

        /// <summary>
        /// Returns the DataContract attribute that the type is marked with or null if it's not marked.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private DataContractAttribute GetDataContract(Type type)
        {
            return (DataContractAttribute)type.GetCustomAttributes(typeof(DataContractAttribute), false).FirstOrDefault();
        }

        /// <summary>
        /// Returns the DataMember attribute that the property is marked with or null if it's not marked.
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        private DataMemberAttribute GetDataMember(PropertyInfo propertyInfo)
        {
            return (DataMemberAttribute)propertyInfo.GetCustomAttributes(typeof(DataMemberAttribute), false).FirstOrDefault();
        }

        /// <summary>
        /// Serialises the object to the stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="obj"></param>
        public void WriteObject(Stream stream, object obj)
        {
            var currentCulture = Thread.CurrentThread.CurrentCulture;
            try {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                if(stream == null) throw new ArgumentNullException("stream");
                var body = new StringBuilder();
                body.AppendFormat("{{{0}}}", ConvertObject(obj));

                var bytes = Encoding.UTF8.GetBytes(body.ToString());
                stream.Write(bytes, 0, bytes.Length);
            } finally {
                Thread.CurrentThread.CurrentCulture = currentCulture;
            }
        }

        /// <summary>
        /// Returns the object converted into JSON without any enclosing delimiters.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private string ConvertObject(object obj)
        {
            var result = new StringBuilder();

            DataContractAttribute contract;
            var type = obj.GetType();
            if(!_TypeContractsMap.TryGetValue(type, out contract)) throw new InvalidOperationException(String.Format("{0} was not seen by Initialise", type.Name));

            var propertyList = _TypePropertiesMap[type];
            foreach(var propertyDetail in propertyList) {
                var property = propertyDetail.PropertyInfo;
                var dataMember = propertyDetail.DataMember;
                var value = property.GetValue(obj, null);

                if(dataMember.EmitDefaultValue || !IsDefaultValue(property.PropertyType, value)) {
                    if(result.Length != 0) result.Append(',');
                    result.AppendFormat(@"""{0}"":{1}", dataMember.Name ?? property.Name, ConvertValue(property.PropertyType, value));
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Returns true if the value is the default value for the type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool IsDefaultValue(Type type, object value)
        {
            bool result = value == null;
            if(!result && type.IsValueType && !(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))) {
                result = value.Equals(Activator.CreateInstance(value.GetType()));
            }

            return result;
        }

        /// <summary>
        /// Converts a value to its JSON equivalent.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private object ConvertValue(Type type, object value)
        {
            object result = value;

            if(result == null) result = "null";
            else {
                if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) type = type.GetGenericArguments()[0];
                if(type == typeof(bool)) result = (bool)value ? "true" : "false";
                else if(type == typeof(float) || type == typeof(double)) result = String.Format("{0:R}", value);
                else if(type == typeof(string)) result = String.Format(@"""{0}""", EscapeString((string)value));
                else if(type == typeof(DateTime)) {
                    var dateTime = (DateTime)value;
                    var offset = TimeZone.CurrentTimeZone.GetUtcOffset(dateTime);
                    result = String.Format(@"""\/Date({0}{1}{2:00}{3:00})\/""", JavascriptHelper.ToJavascriptTicks(dateTime.ToUniversalTime()), offset.Ticks >= 0 ? '+' : '-', Math.Abs(offset.Hours), Math.Abs(offset.Minutes));
                } else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) {
                    result = String.Format("[{0}]", FormatList(type.GetGenericArguments()[0], value));
                } else if(type.IsClass) result = String.Format("{{{0}}}", ConvertObject(value));
            }

            return result;
        }

        /// <summary>
        /// Escapes characters in the string that the JSON spec specifies must be escaped.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string EscapeString(string value)
        {
            string result = value;

            if(result.Length > 0) {
                var buffer = new StringBuilder(value.Length);
                foreach(var ch in value) {
                    switch(ch) {
                        case '\b':  buffer.Append(@"\b"); break;
                        case '\f':  buffer.Append(@"\f"); break;
                        case '\n':  buffer.Append(@"\n"); break;
                        case '\r':  buffer.Append(@"\r"); break;
                        case '\t':  buffer.Append(@"\t"); break;
                        case '"':
                        case '\\':
                        case '/':
                            buffer.Append('\\');
                            buffer.Append(ch);
                            break;
                        default:
                            buffer.Append(ch);
                            break;
                    }
                }

                result = buffer.ToString();
            }

            return result;
        }

        /// <summary>
        /// Returns a string containing the body of a list formatted as Json.
        /// </summary>
        /// <param name="elementType"></param>
        /// <param name="listObject"></param>
        /// <returns></returns>
        private string FormatList(Type elementType, object listObject)
        {
            var result = new StringBuilder();

            var list = (IList)listObject;
            foreach(var element in list) {
                if(result.Length != 0) result.Append(',');
                result.Append(ConvertValue(elementType, element));
            }

            return result.ToString();
        }
    }
}
