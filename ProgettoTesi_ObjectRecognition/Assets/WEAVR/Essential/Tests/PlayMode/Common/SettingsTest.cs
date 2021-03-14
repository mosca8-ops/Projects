using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using TXT.WEAVR.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TXT.WEAVR.Common
{
    public class SettingsTest
    {
        string defaultPath = Path.Combine(Application.streamingAssetsPath, "testSettings.json");
        SettingsHandler settings;

        [SetUp]
        public void SetupSettings()
        {
            settings = new SettingsHandler(defaultPath, true);
        }

        [TearDown]
        public void TeardownSettings()
        {
            settings = null;
            if (File.Exists(defaultPath))
            {
                File.Delete(defaultPath);
            }
        }

        // A Test behaves as an ordinary method
        //[Test]
        public void SettingsTest_MergeGroupPasses()
        {
            // Use the Assert class to test conditions
            throw new NotImplementedException();
        }

        // A Test behaves as an ordinary method
        [Test]
        public void SettingsTest_SerializationPasses()
        {
            // Setup
            string path = defaultPath;
            var group = new SettingsGroup()
            {
                group = "Group_Test",
                flags = SettingsFlags.Editable | SettingsFlags.Runtime | SettingsFlags.Visible,
            };

            var line = new Setting()
            {
                name = "Test_Property",
                flags = SettingsFlags.Editable | SettingsFlags.Runtime | SettingsFlags.Visible,
                Value = true,
            };

            var colorLine = new Setting()
            {
                name = "Test_Color",
                flags = SettingsFlags.EditableInPlayer,
                Value = Color.red,
            };

            group.settings.Add(line);
            group.settings.Add(colorLine);

            // Perform actions
            File.WriteAllText(path, JsonConvert.SerializeObject(group, Formatting.Indented));
            var newGroup = JsonConvert.DeserializeObject<SettingsGroup>(File.ReadAllText(path));

            // Teardown
            File.Delete(path);

            // Assert results
            Assert.IsNotNull(newGroup, "Serialization failed");
            Assert.AreEqual(newGroup.group, group.group);
            CollectionAssert.AllItemsAreInstancesOfType(newGroup.settings, typeof(Setting));
            Assert.AreEqual(newGroup.settings.ElementAt(0).name, group.settings.ElementAt(0).name);
            Assert.AreEqual(newGroup.settings.ElementAt(1).name, group.settings.ElementAt(1).name);
            Assert.AreEqual(newGroup.settings.ElementAt(0).Value, group.settings.ElementAt(0).Value);
            Assert.AreEqual(newGroup.settings.ElementAt(1).Value, group.settings.ElementAt(1).Value);
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        //[UnityTest]
        public IEnumerator SettingsTestWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
            throw new NotImplementedException();
        }
    }
}
