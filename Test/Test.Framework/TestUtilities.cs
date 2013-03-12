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
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.Serialization;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using Moq;
using System.Linq.Expressions;
using InterfaceFactory;
using VirtualRadar.Interface;

namespace Test.Framework
{
    /// <summary>
    /// A collection of static methods that cover common aspects of testing objects.
    /// </summary>
    public static class TestUtilities
    {
        #region AssertIsAttribute
        /// <summary>
        /// Throws an assertion if the type passed across is not sealed and is not based on Attribute.
        /// </summary>
        /// <param name="attributeType"></param>
        public static void AssertIsAttribute(Type attributeType)
        {
            Assert.IsNotNull(attributeType);
            Assert.IsTrue(typeof(Attribute).IsAssignableFrom(attributeType), "Attribute does not inherit from System.Attribute");
            Assert.IsTrue(attributeType.IsSealed, "Attribute is not sealed");
            Assert.AreNotEqual(0, attributeType.GetCustomAttributes(typeof(AttributeUsageAttribute), false).Length);
        }
        #endregion

        #region AssertIsException
        /// <summary>
        /// Throws an assertion if the type passed across is not an exception type, does not have the
        /// correct constructors or is not serialisable.
        /// </summary>
        /// <param name="exceptionType"></param>
        public static void AssertIsException(Type exceptionType)
        {
            Assert.IsNotNull(exceptionType);
            Assert.IsTrue(typeof(Exception).IsAssignableFrom(exceptionType), "Exception does not inherit from System.Exception");
            #if PocketPC || WindowsCE || NETCF
            #else
                if(exceptionType.GetCustomAttributes(typeof(SerializableAttribute), false) == null) Assert.Fail("The exception is not serializable");
                ConstructorInfo serializeCtor = exceptionType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] {typeof(SerializationInfo), typeof(StreamingContext)}, new ParameterModifier[]{});
                Assert.IsNotNull(serializeCtor);
            #endif

            ConstructorInfo defaultCtor = exceptionType.GetConstructor(new Type[] {});
            ConstructorInfo stringCtor = exceptionType.GetConstructor(new Type[] {typeof(String)});
            ConstructorInfo innerExceptionCtor = exceptionType.GetConstructor(new Type[] {typeof(String), typeof(Exception)});

            Assert.IsNotNull(defaultCtor == null);
            Assert.IsNotNull(stringCtor == null);
            Assert.IsNotNull(innerExceptionCtor == null);

            Exception defaultException = (Exception)Activator.CreateInstance(exceptionType);
            Assert.IsNull(defaultException.InnerException);

            string expectedText = "My Message TeXT";
            Exception stringException = (Exception)stringCtor.Invoke(new object[]{expectedText});
            Assert.IsTrue(stringException.Message.IndexOf(expectedText) > -1);
            Assert.IsNull(stringException.InnerException);

            Exception inner = new Exception();
            Exception innerException = (Exception)innerExceptionCtor.Invoke(new object[]{expectedText, inner});
            Assert.IsTrue(innerException.Message.IndexOf(expectedText) > -1);
            Assert.AreSame(inner, innerException.InnerException);
        }
        #endregion

        #region TestProperty
        /// <summary>
        /// Asserts that a property of an object has a given start value and can be successfuly changed to another value.
        /// </summary>
        /// <param name="obj">The object under test.</param>
        /// <param name="propertyName">The name of the property to test.</param>
        /// <param name="startValue">The expected starting value of the property.</param>
        /// <param name="newValue">The value to write to the property - to pass the property must expose this value once it has been set.</param>
        /// <param name="testForEquals">True if the equality test uses <see cref="object.Equals(object)"/>, false if it uses <see cref="Object.ReferenceEquals"/>.
        /// If either the startValue or newValue are value types then this parameter is ignored, the test is always made using <see cref="object.Equals(object)"/>.</param>
        public static void TestProperty(object obj, string propertyName, object startValue, object newValue, bool testForEquals)
        {
            Assert.IsNotNull(obj);
            PropertyInfo property = obj.GetType().GetProperty(propertyName);
            Assert.IsNotNull(property, "{0}.{1} is not a public property", obj.GetType().Name, propertyName);

            if(startValue != null && startValue.GetType().IsValueType) testForEquals = true;
            if(newValue != null && newValue.GetType().IsValueType) testForEquals = true;

            object actualStart = property.GetValue(obj, null);
            if(testForEquals) Assert.AreEqual(startValue, actualStart, "{0}.{1} expected [{2}] was [{3}]", obj.GetType().Name, propertyName, startValue == null ? "null" : startValue.ToString(), actualStart == null ? "null" : actualStart.ToString());
            else              Assert.AreSame(startValue, actualStart, "{0}.{1} expected [{2}] was [{3}]", obj.GetType().Name, propertyName, startValue == null ? "null" : startValue.ToString(), actualStart == null ? "null" : actualStart.ToString());

            if(property.CanWrite) {
                property.SetValue(obj, newValue, null);
                object actualNew = property.GetValue(obj, null);
                if(testForEquals) Assert.AreEqual(newValue, actualNew, "{0}.{1} was set to [{2}] but it returned [{3}] when read", obj.GetType().Name, propertyName, newValue == null ? "null" : newValue.ToString(), actualNew == null ? "null" : actualNew.ToString());
                else              Assert.AreSame(newValue, actualNew, "{0}.{1} was set to [{2}] but it returned [{3}] when read", obj.GetType().Name, propertyName, newValue == null ? "null" : newValue.ToString(), actualNew == null ? "null" : actualNew.ToString());
            }
        }

        /// <summary>
        /// Asserts that the property passed across starts at a given value and can be changed to another value.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propertyExpression"></param>
        /// <param name="startValue"></param>
        /// <param name="newValue"></param>
        /// <param name="testForEquals"></param>
        public static void TestProperty<T>(T obj, Expression<Func<T, object>> propertyExpression, object startValue, object newValue, bool testForEquals)
        {
            TestProperty(obj, ExtractNameOfProperty(propertyExpression), startValue, newValue, testForEquals);
        }

        /// <summary>
        /// Asserts that a property of an object has a given start value and can be successfuly changed to another value.
        /// </summary>
        /// <param name="obj">The object under test.</param>
        /// <param name="propertyName">The name of the property to test.</param>
        /// <param name="startValue">The expected starting value of the property.</param>
        /// <param name="newValue">The value to write to the property - to pass the property must expose this value once it has been set.</param>
        public static void TestProperty(object obj, string propertyName, object startValue, object newValue)
        {
            TestProperty(obj, propertyName, startValue, newValue, false);
        }

        /// <summary>
        /// Asserts that a property of an object has a given start value and can be successfuly changed to another value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="propertyExpression"></param>
        /// <param name="startValue"></param>
        /// <param name="newValue"></param>
        public static void TestProperty<T>(T obj, Expression<Func<T, object>> propertyExpression, object startValue, object newValue)
        {
            TestProperty(obj, ExtractNameOfProperty(propertyExpression), startValue, newValue, false);
        }

        /// <summary>
        /// Asserts that a bool property of an object has the given start value. Sets the property to the toggled start value and asserts that
        /// the toggled value can then be read back from the property.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propertyName"></param>
        /// <param name="startValue"></param>
        public static void TestProperty(object obj, string propertyName, bool startValue)
        {
            TestProperty(obj, propertyName, startValue, !startValue, true);
        }

        /// <summary>
        /// Asserts that a bool property of an object has the given start value. Sets the property to the toggled start value and asserts that
        /// the toggled value can then be read back from the property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="propertyExpression"></param>
        /// <param name="startValue"></param>
        public static void TestProperty<T>(T obj, Expression<Func<T, object>> propertyExpression, bool startValue)
        {
            TestProperty(obj, ExtractNameOfProperty(propertyExpression), startValue, !startValue, true);
        }

        /// <summary>
        /// Retrieves the name of a property from the lambda expression passed across. Returns null if the expression refers to anything other than a simple property.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private static string ExtractNameOfProperty(Expression expression)
        {
            var lambdaExpression = (LambdaExpression)expression;

            MemberExpression memberExpression = lambdaExpression.Body.NodeType == ExpressionType.Convert ?
                                                ((UnaryExpression)lambdaExpression.Body).Operand as MemberExpression :
                                                lambdaExpression.Body as MemberExpression;
            var propertyInfo = memberExpression == null ? null : memberExpression.Member as PropertyInfo;

            return propertyInfo == null ? null : propertyInfo.Name;
        }
        #endregion

        #region ChangeType
        /// <summary>
        /// Converts a value from one type to another, as per the standard <see cref="Convert.ChangeType(object, Type)"/>,
        /// except that this version can cope with the conversion of <see cref="Nullable{T}"/> types.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static object ChangeType(object value, Type type, IFormatProvider provider)
        {
            object result = null;

            if(!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Nullable<>)) {
                if(type.IsEnum) {
                    result = Enum.Parse(type, (string)Convert.ChangeType(value, typeof(string)));
                } else {
                    result = Convert.ChangeType(value, type, provider);
                }
            } else if(value != null) {
                var underlyingType = type.GetGenericArguments()[0];
                result = ChangeType(value, underlyingType, provider);
            }

            return result;
        }
        #endregion

        #region ConvertToBitString, ConvertBitStringToBytes
        /// <summary>
        /// Converts a value to a binary representation of a given length.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="countDigits"></param>
        /// <returns>The value, expressed as a sequence of binary digits, within a string of countDigits length.</returns>
        public static string ConvertToBitString(long value, int countDigits)
        {
            var bits = Convert.ToString(value, 2);
            if(bits.Length < countDigits) bits = String.Format("{0}{1}", new String('0', countDigits - bits.Length), bits);  // must be a better way to do this :)
            return bits;
        }

        /// <summary>
        /// Converts a value to a binary representation of a given length.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="countDigits"></param>
        /// <returns>The value, expressed as a sequence of binary digits, within a string of countDigits length.</returns>
        public static string ConvertToBitString(int value, int countDigits)
        {
            return ConvertToBitString((long)value, countDigits);
        }

        /// <summary>
        /// Converts a string of binary digits (with optional whitespace) to a collection of bytes.
        /// </summary>
        /// <param name="bits"></param>
        /// <returns></returns>
        public static List<byte> ConvertBitStringToBytes(string bits)
        {
            bits = bits.Trim().Replace(" ", "");
            if(bits.Length % 8 != 0) Assert.Fail("The number of bits must be divisible by 8");

            var result = new List<byte>();
            for(int i = 0;i < bits.Length;i += 8) {
                result.Add(Convert.ToByte(bits.Substring(i, 8), 2));
            }

            return result;
        }
        #endregion

        #region SequenceEqual
        /// <summary>
        /// Returns true if both sequences are null or if the LINQ SequenceEqual method returns true for them, otherwise returns false.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool SequenceEqual<T>(IEnumerable<T> lhs, IEnumerable<T> rhs)
        {
            return (lhs == null && rhs == null) || (lhs != null && rhs != null && lhs.SequenceEqual(rhs));
        }
        #endregion

        #region Moq helpers
        /// <summary>
        /// Creates a Moq mock object for a type that implements <see cref="ISingleton{T}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <remarks><para>
        /// The application has adopted the idea that singletons are denoted by the <see cref="ISingleton{T}"/> interface. This
        /// declares a property which returns the same singleton reference regardless of the instance that it's called from. We
        /// need to make sure that any tests will pick up the accidental use of a private instance of the class. For example we
        /// want to make sure that if the user does this:
        /// </para><code>
        /// var log = Factory.Singleton.Resolve&lt;ILog&gt;();
        /// log.DoSomething();
        /// </code><para>
        /// instead of this:
        /// </para><code>
        /// var log = Factory.Singleton.Resolve&lt;ILog&gt;().Singleton;
        /// log.DoSomething();
        /// </code><para>
        /// then the test will throw an exception. We do this by creating a strict mock that only has Singleton setup for it, so any
        /// call on it other than to read Singleton will cause the mock to throw an exception and fail the test. This strict mock is
        /// then registered with the class factory.
        /// </para><para>
        /// The Singleton property of the strict mock is then set up to return a friendly mock, and it is this mock that gets returned
        /// by the method. The test can configure this mock with the correct behaviour and rest assured that the code under test
        /// could only get a reference to this mock by asking the class factory to resolve the interface and then reading the Singleton
        /// property of the object that the class factory dished up.
        /// </para>
        /// </remarks>
        public static Mock<T> CreateMockSingleton<T>()
            where T: class
        {
            if(!typeof(ISingleton<T>).IsAssignableFrom(typeof(T))) throw new InvalidOperationException(String.Format("{0} does not implement {1}", typeof(T).Name, typeof(ISingleton<>).Name));

            Mock<T> result = new Mock<T>() { DefaultValue = DefaultValue.Mock }.SetupAllProperties();
            Mock<T> strict = new Mock<T>(MockBehavior.Strict);

            var parameter = Expression.Parameter(typeof(ISingleton<T>));
            var body = Expression.Property(parameter, "Singleton");
            var lambda = Expression.Lambda<Func<T, T>>(body, parameter);
            strict.Setup(lambda).Returns(result.Object);

            Factory.Singleton.RegisterInstance(typeof(T), strict.Object);

            return result;
        }

        /// <summary>
        /// Creates a Moq stub for an object and registers it with the class factory. The instance returned by the method
        /// is the one the code under test will always receive when it asks for an instantiation of the interface.
        /// </summary>
        /// <returns></returns>
        public static Mock<T> CreateMockImplementation<T>()
            where T: class
        {
            if(typeof(ISingleton<T>).IsAssignableFrom(typeof(T))) throw new InvalidOperationException(String.Format("{0} implements {1}, use CreateMockSingleton instead", typeof(T).Name, typeof(ISingleton<>).Name));

            Mock<T> result = new Mock<T> { DefaultValue = DefaultValue.Mock }.SetupAllProperties();
            Factory.Singleton.RegisterInstance(typeof(T), result.Object);

            return result;
        }
        #endregion
    }
}
