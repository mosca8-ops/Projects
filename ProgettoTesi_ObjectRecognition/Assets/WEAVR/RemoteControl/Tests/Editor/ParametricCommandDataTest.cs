using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TXT.WEAVR.RemoteControl
{
    public class ParametricCommandDataTest
    {
        private ParametricCommandData prototype;

        [Serializable]
        private class SerializedParameter
        {
            public string name;
            public string data;
        }

        [Serializable]
        private class SerializedParameterArray
        {
            public SerializedParameter[] parameters;
        }

        [SetUp]
        public void SetUp()
        {
            prototype = new ParametricCommandData();
        }

        [Test]
        public void ParametricData_PlainParameters_Passes()
        {
            // Setup
            string guid = "C5B362AB-48DB-466E-9F62-6711AB337F56";
            string intParam = 15.ToString();
            string stringParam = @"string, hello";
            string floatParam = 4.19f.ToString(CultureInfo.InvariantCulture);
            string positionalParameters = $"{intParam} \"{stringParam}\" {floatParam} {guid}";
            int paramsCount = 4;

            // Make the test
            var data = prototype.Create(0, Encoding.ASCII.GetBytes(positionalParameters));

            // Assert
            Assert.AreEqual(paramsCount, data.Parameters.Length);
            Assert.AreEqual(intParam, data.Parameters[0].Value);
            Assert.AreEqual(stringParam, data.Parameters[1].Value);
            Assert.AreEqual(floatParam, data.Parameters[2].Value);
            Assert.AreEqual(guid, data.Parameters[3].Value);
        }

        [Test]
        public void ParametricData_PositionalParameters_Passes()
        {
            // Setup
            string guid = "C5B362AB-48DB-466E-9F62-6711AB337F56";
            int intParam = 15;
            string stringParam = @"string, hello";
            float floatParam = 4.19f;
            string positionalParameters = $"({intParam}, \"{stringParam}\", {floatParam.ToString(CultureInfo.InvariantCulture)}, {guid})";
            int paramsCount = 4;

            // Make the test
            var data = prototype.Create(0, Encoding.ASCII.GetBytes(positionalParameters));

            // Assert
            Assert.AreEqual(paramsCount, data.Parameters.Length);
            Assert.AreEqual(intParam, data.Parameters[0].Value);
            Assert.AreEqual(stringParam, data.Parameters[1].Value);
            Assert.AreEqual(floatParam, data.Parameters[2].Value);
            Assert.AreEqual(guid, data.Parameters[3].Value);
        }

        [Test]
        public void ParametricData_ArrayParameters_Passes()
        {
            // Setup
            string guid = "C5B362AB-48DB-466E-9F62-6711AB337F56";
            string intParam = 15.ToString();
            string stringParam = @"string, hello";
            string floatParam = 4.19f.ToString(CultureInfo.InvariantCulture);
            string positionalParameters = $"[\"{nameof(intParam)}\": {intParam},\"{nameof(stringParam)}\": \"{stringParam}\",\"{nameof(floatParam)}\": {floatParam},\"{nameof(guid)}\": {guid}]";
            int paramsCount = 4;

            // Make the test
            var data = prototype.Create(0, Encoding.ASCII.GetBytes(positionalParameters));

            // Assert
            Assert.AreEqual(paramsCount, data.Parameters.Length);
            Assert.AreEqual(intParam, data.Parameters[0].Value);
            Assert.AreEqual(stringParam, data.Parameters[1].Value);
            Assert.AreEqual(floatParam, data.Parameters[2].Value);
            Assert.AreEqual(guid, data.Parameters[3].Value);

            Assert.IsTrue(data.TryGet(nameof(intParam), out string intData));
            Assert.AreEqual(intParam, intData);
            Assert.IsTrue(data.TryGet(nameof(stringParam), out string stringData));
            Assert.AreEqual(stringParam, stringData);
            Assert.IsTrue(data.TryGet(nameof(floatParam), out string floatData));
            Assert.AreEqual(floatParam, floatData);
            Assert.IsTrue(data.TryGet(nameof(guid), out string guidData));
            Assert.AreEqual(guid, guidData);
        }

        [Test]
        public void ParametricData_JsonParameters_Passes()
        {
            // Setup
            string guid = "C5B362AB-48DB-466E-9F62-6711AB337F56";
            string intParam = 15.ToString();
            string stringParam = @"string, hello";
            string floatParam = 4.19f.ToString(CultureInfo.InvariantCulture);

            SerializedParameterArray jsonArray = new SerializedParameterArray()
            {
                parameters = new SerializedParameter[]
                {
                    new SerializedParameter() { name = nameof(intParam), data = intParam, },
                    new SerializedParameter() { name = nameof(stringParam), data = stringParam, },
                    new SerializedParameter() { name = nameof(floatParam), data = floatParam, },
                    new SerializedParameter() { name = nameof(guid), data = guid, },
                }
            };

            int paramsCount = 4;

            // Make the test
            var data = prototype.Create(0, Encoding.ASCII.GetBytes(JsonUtility.ToJson(jsonArray)));

            // Assert
            Assert.AreEqual(paramsCount, data.Parameters.Length);
            Assert.AreEqual(intParam, data.Parameters[0].Value);
            Assert.AreEqual(stringParam, data.Parameters[1].Value);
            Assert.AreEqual(floatParam, data.Parameters[2].Value);
            Assert.AreEqual(guid, data.Parameters[3].Value);

            Assert.IsTrue(data.TryGet(nameof(intParam), out string intData));
            Assert.AreEqual(intParam, intData);
            Assert.IsTrue(data.TryGet(nameof(stringParam), out string stringData));
            Assert.AreEqual(stringParam, stringData);
            Assert.IsTrue(data.TryGet(nameof(floatParam), out string floatData));
            Assert.AreEqual(floatParam, floatData);
            Assert.IsTrue(data.TryGet(nameof(guid), out string guidData));
            Assert.AreEqual(guid, guidData);
        }

        [Test]
        public void ParametricData_ArrayParameters_Fails()
        {
            // Setup
            string guid = "C5B362AB-48DB-466E-9F62-6711AB337F56";
            string positionalParameters = $"[\"{nameof(guid)}\": {guid}]";
            int paramsCount = 1;

            // Make the test
            var data = prototype.Create(0, Encoding.ASCII.GetBytes(positionalParameters));

            // Assert
            Assert.AreEqual(paramsCount, data.Parameters.Length);
            Assert.AreEqual(guid, data.Parameters[0].Value);
            Assert.IsFalse(data.TryGet("intParam", out string intData));
        }
    }
}
