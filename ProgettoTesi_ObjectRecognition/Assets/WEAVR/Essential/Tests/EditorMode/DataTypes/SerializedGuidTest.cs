using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TXT.WEAVR
{
    public class SerializedGuidTest
    {
        string m_testFilePath = Path.Combine(Application.streamingAssetsPath, "test_serialized_guid.json");

        [Test]
        public void SerializedGuid_ConvertFromGuid_Pass()
        {
            // Setup
            Guid guid = Guid.NewGuid();

            // Test
            SerializedGuid convertedGuid = guid;

            // Assert
            Assert.AreEqual(guid, convertedGuid.Guid);
        }

        [Test]
        public void SerializedGuid_ConvertToGuid_Pass()
        {
            // Setup
            Guid guid = Guid.NewGuid();
            SerializedGuid sGuid = new SerializedGuid(guid);

            // Test
            Guid convertedGuid = sGuid;

            // Assert
            Assert.AreEqual(guid, convertedGuid);
        }

        [Test]
        public void SerializedGuid_AreEqual_Pass()
        {
            // Setup
            Guid guid = Guid.NewGuid();

            // Test
            SerializedGuid sGuid = new SerializedGuid(guid);
            SerializedGuid sGuid2 = guid;

            // Assert
            Assert.AreEqual(sGuid, sGuid2);
        }

        [Test]
        public void SerializedGuid_Serialization_Pass()
        {
            // Setup
            SerializedGuid sGuid = Guid.NewGuid();
            m_testFilePath = Path.Combine(Application.streamingAssetsPath, "test_serialized_guid.json");

            // Test
            File.WriteAllText(m_testFilePath, JsonUtility.ToJson(sGuid));
            SerializedGuid deserializedGuid = JsonUtility.FromJson<SerializedGuid>(File.ReadAllText(m_testFilePath));

            // Assert
            Assert.AreEqual(sGuid.Guid, deserializedGuid.Guid);
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(m_testFilePath))
            {
                File.Delete(m_testFilePath);
            }
        }
    }
}
