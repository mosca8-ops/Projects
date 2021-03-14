using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TXT.WEAVR.TestsUtility;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TXT.WEAVR
{
    public class GuidManagerTest
    {
        List<(Guid guid, GameObject go)> m_testGameObjects = new List<(Guid, GameObject)>();

        [SetUp]
        public void SetUp()
        {
            //CreateTestGameObjects();
            GuidManager.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            UnregisterCreatedTestGameObjects();

            TestSceneManager.CleanUp();
        }

        private void CreateTestGameObjects(int amount = 2)
        {
            m_testGameObjects.Clear();
            for (int i = 0; i < amount; i++)
            {
                GameObject go = new GameObject("__CREATED_TEST_GO_" + i);
                Guid guid = Guid.NewGuid();

                m_testGameObjects.Add((guid, go));
            }
        }

        private void RegisterCreatedTestGameObjects()
        {
            foreach(var (guid, go) in m_testGameObjects)
            {
                GuidManager.Register(guid, go);
            }
        }

        private void UnregisterCreatedTestGameObjects()
        {
            foreach (var (guid, _) in m_testGameObjects)
            {
                GuidManager.Unregister(guid);
            }
            m_testGameObjects.Clear();
        }

        [Test]
        public void GuidManager_GuidExists_Passes()
        {
            // Setup
            GameObject newGO = new GameObject("__TEST__") { hideFlags = HideFlags.HideAndDontSave };
            Guid guid = Guid.NewGuid();

            // Test
            GuidManager.Register(guid, newGO);
            bool guidExists = GuidManager.ContainsGuid(guid);

            // Assert
            Assert.IsTrue(guidExists);
        }

        [Test]
        public void GuidManager_RegisterGenericGuid_Passes()
        {
            // Setup
            Vector3 objToSave = Vector3.one;
            Guid guid = Guid.NewGuid();
            int registeredObjects = GuidManager.Objects.Count;

            // Test
            bool outcome = GuidManager.Register(guid, objToSave);
            int newRegisterCount = GuidManager.Objects.Count;

            // Assert
            Assert.IsTrue(outcome, "Register outcome");
            Assert.AreEqual(registeredObjects + 1, newRegisterCount);
        }

        [Test]
        public void GuidManager_RegisterComponentGuid_Passes()
        {
            // Setup
            GameObject newGO = new GameObject("__TEST__") { hideFlags = HideFlags.HideAndDontSave };
            Guid guid = Guid.NewGuid();
            var component = newGO.AddComponent<BoxCollider>();
            int registeredObjects = GuidManager.Objects.Count;

            // Test
            var outcome = GuidManager.Register(guid, component);
            int newRegisterCount = GuidManager.Objects.Count;

            // Assert
            Assert.IsTrue(outcome, "Register outcome");
            Assert.AreEqual(registeredObjects + 1, newRegisterCount);
        }

        [Test]
        public void GuidManager_GetGameObjectFromGenericGuid_Passes()
        {
            // Setup
            GameObject newGO = new GameObject("__TEST__") { hideFlags = HideFlags.HideAndDontSave };
            Guid guid = Guid.NewGuid();
            var component = newGO.AddComponent<BoxCollider>();

            // Test
            bool outcome = GuidManager.Register(guid, component);
            var savedGo = GuidManager.GetGameObject(guid);

            // Assert
            Assert.IsTrue(outcome, "Register outcome");
            Assert.AreEqual(newGO, savedGo);
        }

        [Test]
        public void GuidManager_GetGameObjectFromGenericGuid_Fails()
        {
            // Setup
            Vector3 objToSave = Vector3.one;
            Guid guid = Guid.NewGuid();

            // Test
            GuidManager.Register(guid, objToSave);
            var savedGO = GuidManager.GetGameObject(guid);

            // Assert
            Assert.IsNull(savedGO);
        }

        [Test]
        public void GuidManager_UnregisterGenericGuid_Passes()
        {
            // Setup
            GameObject newGO = new GameObject("__TEST__") { hideFlags = HideFlags.HideAndDontSave };
            Guid guid = Guid.NewGuid();

            // Test
            GuidManager.Register(guid, newGO);
            int registeredObjects = GuidManager.Objects.Count;
            GuidManager.Unregister(guid);
            int newRegisterCount = GuidManager.Objects.Count;

            // Assert
            Assert.AreEqual(registeredObjects - 1, newRegisterCount);
        }

        [Test]
        public void GuidManager_GetGenericGuid_Passes()
        {
            // Setup
            Vector3 objToSave = Vector3.one;
            Guid guid = Guid.NewGuid();

            // Test
            GuidManager.Register(guid, objToSave);
            var objSaved = GuidManager.GetObject(guid);

            // Assert
            Assert.AreEqual(objToSave, objSaved);
        }

        [Test]
        public void GuidManager_RegisterGameObject_Passes()
        {
            // Setup
            int gameobjectsCount = GuidManager.GameObjects.Count;
            GameObject newGO = new GameObject("__TEST__") { hideFlags = HideFlags.HideAndDontSave };
            Guid guid = Guid.NewGuid();

            // Test
            var outcome = GuidManager.Register(guid, newGO);
            int newCount = GuidManager.GameObjects.Count;

            // Assert
            Assert.IsTrue(outcome, "Register outcome");
            Assert.AreEqual(gameobjectsCount + 1, newCount);
        }

        [Test]
        public void GuidManager_RegisterGameObjectSecondTime_Fails()
        {
            // Setup
            int gameobjectsCount = GuidManager.GameObjects.Count;
            GameObject newGO = new GameObject("__TEST__") { hideFlags = HideFlags.HideAndDontSave };
            Guid guid = Guid.NewGuid();

            // Test
            var outcome1 = GuidManager.Register(guid, newGO);
            var outcome2 = GuidManager.Register(guid, newGO);
            int newCount = GuidManager.GameObjects.Count;

            // Assert
            Assert.IsTrue(outcome1, "Register outcome 1");
            Assert.IsFalse(outcome2, "Register outcome 2");
            Assert.AreEqual(gameobjectsCount + 1, newCount);
        }

        [Test]
        public void GuidManager_GetGameObject_Passes()
        {
            // Setup
            GameObject newGO = new GameObject("__TEST__") { hideFlags = HideFlags.HideAndDontSave };
            Guid guid = Guid.NewGuid();

            // Test
            GuidManager.Register(guid, newGO);
            GameObject registeredGameObject = GuidManager.GetGameObject(guid);

            // Assert
            Assert.AreEqual(newGO, registeredGameObject);
        }

        [Test]
        public void GuidManager_RegisterGameObject_EmptyGuid_Fails()
        {
            // Setup
            int gameobjectsCount = GuidManager.GameObjects.Count;
            GameObject newGO = new GameObject("__TEST__") { hideFlags = HideFlags.HideAndDontSave };
            Guid guid = Guid.Empty;

            // Test
            var outcome = GuidManager.Register(guid, newGO);
            int newCount = GuidManager.GameObjects.Count;
            GameObject registeredGameObject = GuidManager.GetGameObject(guid);

            // Assert
            LogAssert.Expect(LogType.Error, new Regex(".*[Cc]annot register [Ee]mpty [Gg]uid.*"));
            Assert.IsFalse(outcome, "Register outcome");
            Assert.AreEqual(gameobjectsCount, newCount);
            Assert.IsNull(registeredGameObject);
        }

        [Test]
        public void GuidManager_RegisterGameObject_ExistingGuid_Fails()
        {
            // Setup
            int gameobjectsCount = GuidManager.GameObjects.Count;
            GameObject newGO = new GameObject("__TEST__") { hideFlags = HideFlags.HideAndDontSave };
            GameObject newGO2 = new GameObject("__TEST_2__") { hideFlags = HideFlags.HideAndDontSave };
            Guid guid = Guid.NewGuid();

            // Test
            var outcome1 = GuidManager.Register(guid, newGO);
            var outcome2 = GuidManager.Register(guid, newGO2);
            int newCount = GuidManager.GameObjects.Count;
            GameObject registeredGameObject = GuidManager.GetGameObject(guid);

            // Assert
            LogAssert.Expect(LogType.Error, new Regex(".*[Tt]here is already a [Gg]ame[Oo]bject '.*' registered with the [Gg]uid.*"));
            Assert.IsTrue(outcome1, "Register outcome 1");
            Assert.IsFalse(outcome2, "Register outcome 2");
            Assert.AreEqual(gameobjectsCount + 1, newCount);
            Assert.AreEqual(newGO, registeredGameObject);
        }

        [Test]
        public void GuidManager_RegisterGameObject_NullGameObject_Fails()
        {
            // Setup
            int gameobjectsCount = GuidManager.GameObjects.Count;
            GameObject newGO = null;
            Guid guid = Guid.NewGuid();

            // Test
            var outcome = GuidManager.Register(guid, newGO);
            int newCount = GuidManager.GameObjects.Count;
            GameObject registeredGameObject = GuidManager.GetGameObject(guid);

            // Assert
            LogAssert.Expect(LogType.Error, new Regex(".*[Cc]annot register [Nn]ull [Gg]ame[Oo]bject.*"));
            Assert.IsFalse(outcome, "Register outcome");
            Assert.AreEqual(gameobjectsCount, newCount);
            Assert.IsNull(registeredGameObject);
        }

        [Test]
        public void GuidManager_UnregisterGameObject_Passes()
        {
            // Setup
            CreateTestGameObjects();
            RegisterCreatedTestGameObjects();
            int gameobjectsCount = GuidManager.GameObjects.Count;

            // Test
            GuidManager.Unregister(m_testGameObjects[0].guid);
            int newCount = GuidManager.GameObjects.Count;
            GameObject registeredGameObject = GuidManager.GetGameObject(m_testGameObjects[0].guid);

            // Assert
            Assert.AreEqual(gameobjectsCount - 1, newCount);
            Assert.IsNull(registeredGameObject);
        }

        [Test]
        public void GuidManager_UnregisterGameObject_Fails()
        {
            // Setup
            CreateTestGameObjects();
            RegisterCreatedTestGameObjects();
            int gameobjectsCount = GuidManager.GameObjects.Count;
            Guid guid = Guid.NewGuid();

            // Test
            GuidManager.Unregister(guid);
            int newCount = GuidManager.GameObjects.Count;
            GameObject registeredGameObject = GuidManager.GetGameObject(guid);

            // Assert
            Assert.AreEqual(gameobjectsCount, newCount);
            Assert.IsNull(registeredGameObject);
        }

        [Test]
        public void GuidManager_RegisterWithMultipleScenes_Passes()
        {
            // Setup
            // Prepare scenes
            var scene1 = "__TEST_SCENE__1";
            var scene2 = "__TEST_SCENE__2";
            var scenes = TestSceneManager.PrepareScenes(true, scene1, scene2);

            // Set first scene active
            SceneManager.SetActiveScene(scenes[0]);
            CreateTestGameObjects();
            RegisterCreatedTestGameObjects();

            // Set as currently active
            SceneManager.SetActiveScene(scenes[1]);
            // Register new element
            int gameobjectsCount = GuidManager.GameObjects.Count;
            GameObject newGO = new GameObject("__TEST__");
            Guid guid = Guid.NewGuid();

            // Test
            GuidManager.Register(guid, newGO);
            int newCount = GuidManager.GameObjects.Count;
            GameObject registeredGo = GuidManager.GetGameObject(guid);
            GameObject registeredScene1Go = GuidManager.GetGameObject(m_testGameObjects[0].guid);

            // Asserts
            Assert.AreEqual(gameobjectsCount + 1, newCount);
            Assert.AreEqual(newGO, registeredGo);
            Assert.AreEqual(scenes[1].name, registeredGo.scene.name);
            Assert.AreEqual(m_testGameObjects[0].go, registeredScene1Go);
            Assert.AreEqual(scenes[0].name, registeredScene1Go.scene.name);
        }

        [Test]
        public void GuidManager_GuidInScene_Passes()
        {
            // Setup
            // Prepare scenes
            var scene1 = "__TEST_SCENE__1";
            var scene2 = "__TEST_SCENE__2";
            var scenes = TestSceneManager.PrepareScenes(true, scene1, scene2);

            // Set first scene active
            SceneManager.SetActiveScene(scenes[0]);
            CreateTestGameObjects();
            RegisterCreatedTestGameObjects();

            // Set as currently active
            SceneManager.SetActiveScene(scenes[1]);
            // Register new element
            int gameobjectsCount = GuidManager.GameObjects.Count;
            GameObject newGO = new GameObject("__TEST__");
            Guid guid = Guid.NewGuid();

            // Test
            GuidManager.Register(guid, newGO);
            var sceneName1 = GuidManager.GetSceneOfGuid(m_testGameObjects[0].guid).name;
            var sceneName2 = GuidManager.GetSceneOfGuid(guid).name;

            // Asserts
            Assert.AreEqual(scenes[0].name, sceneName1);
            Assert.AreEqual(scenes[1].name, sceneName2);
        }

        [Test]
        public void GuidManager_GuidInNotActiveScene_Fails()
        {
            // Setup
            // Prepare scenes
            var scene1 = "__TEST_SCENE__1";
            var scene2 = "__TEST_SCENE__2";
            var scenes = TestSceneManager.PrepareScenes(true, scene1, scene2);

            // Set first scene active
            SceneManager.SetActiveScene(scenes[0]);
            CreateTestGameObjects();
            RegisterCreatedTestGameObjects();

            // Set as currently active
            SceneManager.SetActiveScene(scenes[1]);
            // Then back again to the first one
            SceneManager.SetActiveScene(scenes[0]);
            // Register new element
            int gameobjectsCount = GuidManager.GameObjects.Count;
            GameObject newGO = new GameObject("__TEST__");
            Guid guid = Guid.NewGuid();

            // Test
            GuidManager.Register(guid, newGO);
            var sceneName1 = GuidManager.GetSceneOfGuid(m_testGameObjects[0].guid).name;
            var sceneName2 = GuidManager.GetSceneOfGuid(guid).name;

            // Asserts
            Assert.AreEqual(scenes[0].name, sceneName1);
            Assert.AreEqual(scenes[0].name, sceneName2);
        }
    }
}
