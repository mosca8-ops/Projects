﻿using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TXT.WEAVR
{
    public class GuidComponentTest
    {
        private GameObject[] m_testObjects;

        private void CreateTestObjects(int amount = 2, HideFlags hideFlags = HideFlags.None)
        {
            m_testObjects = new GameObject[amount];
            for (int i = 0; i < amount; i++)
            {
                m_testObjects[i] = new GameObject("_Guid_TEST_" + i) { hideFlags = hideFlags };
                m_testObjects[i].AddComponent<GuidComponent>();
            }
        }

        private void ClearTestObjects()
        {
            if(m_testObjects == null) { return; }

            for (int i = 0; i < m_testObjects.Length; i++)
            {
                if (m_testObjects[i])
                {
                    DestroyGameObject(m_testObjects[i]);
                }
            }
        }

        private void DestroyGameObject(GameObject go)
        {
            if (Application.isPlaying) { Object.Destroy(go); }
            else { Object.DestroyImmediate(go); }
        }

        [SetUp]
        public void SetUp()
        {
            GuidManager.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            ClearTestObjects();
        }

        private GuidComponent CreateGuidComponent(string name)
        {
            return new GameObject(name).AddComponent<GuidComponent>();
        }

        [Test]
        public void GuidComponent_Instantiation_Passes()
        {
            // Setup

            // Test
            var guidComponent = CreateGuidComponent("__TestGuid");

            // Assert
            Assert.AreNotEqual(System.Guid.Empty, guidComponent.Guid);

            // TearDown

        }

        [UnityTest]
        public IEnumerator GuidComponent_NewSceneNoGuids_Passes()
        {
            // Setup
            int count = GuidManager.GameObjects.Count;

            // Test
            var currentScene = SceneManager.CreateScene("__TEST_NO_GUIDS_SCENE_1__");
            SceneManager.SetActiveScene(currentScene);
            var guidComponent = CreateGuidComponent("__TestGuid");
            int newCount = GuidManager.GameObjects.Count;
            var scene = SceneManager.CreateScene("__TEST_NO_GUIDS_SCENE_2__");
            SceneManager.SetActiveScene(scene);
            SceneManager.UnloadSceneAsync(currentScene);

            // Wait for the scene to unload
            yield return null;

            int newSceneCount = GuidManager.GameObjects.Count;

            // Assert
            Assert.AreEqual(count + 1, newCount);
            Assert.AreEqual(0, newSceneCount);

            // TearDown

        }

        [Test]
        public void GuidComponent_AssignGuid_Passes()
        {
            // Setup
            var guidComponent = CreateGuidComponent("__TestGuid");
            var guid = guidComponent.Guid;
            int currentRegistrations = GuidManager.GameObjects.Count;

            // Test
            var newGuid = System.Guid.NewGuid();
            GuidComponent.AssignGuid(guidComponent.gameObject, newGuid);
            int newRegistrationsCount = GuidManager.GameObjects.Count;
            var gameObjectWithGuid = GuidManager.GetGameObject(guid);
            var gameObjectWithNewGuid = GuidManager.GetGameObject(newGuid);

            // Assert
            Assert.AreEqual(currentRegistrations, newRegistrationsCount);
            Assert.AreNotEqual(guid, newGuid);
            Assert.AreNotEqual(guid, guidComponent.Guid);
            Assert.AreNotEqual(guid, newGuid);
            Assert.AreEqual(newGuid, guidComponent.Guid);
            Assert.AreNotEqual(guidComponent.gameObject, gameObjectWithGuid, "GameManager returned the previous guid object");
            Assert.IsFalse(gameObjectWithGuid, "GameManager returned the a valid object");
            Assert.AreEqual(guidComponent.gameObject, gameObjectWithNewGuid, "GameManager returned the wrong guid object");

            // TearDown
        }


        [Test]
        public void GuidComponent_Registration_OnCreation_Passes()
        {
            // Setup
            int currentRegistrations = GuidManager.GameObjects.Count;

            // Test
            var guidComponent = CreateGuidComponent("__TestGuid");
            int newRegistrationsCount = GuidManager.GameObjects.Count;
            var gameObjectWithGuid = GuidManager.GetGameObject(guidComponent.Guid);

            // Assert
            Assert.AreEqual(currentRegistrations + 1, newRegistrationsCount);
            Assert.AreEqual(guidComponent.gameObject, gameObjectWithGuid, "GameManager returned wrong guid object");

            // TearDown

        }

        [Test]
        public void GuidComponent_Registration_CreateDisabled_Passes()
        {
            // Setup
            var go = new GameObject("__Guid_Test_Inactive");
            go.SetActive(false);
            int currentRegistrations = GuidManager.GameObjects.Count;

            // Test
            var guidComponent = go.AddComponent<GuidComponent>();
            int newRegistrationsCount = GuidManager.GameObjects.Count;
            var gameObjectWithGuid = GuidManager.GetGameObject(guidComponent.Guid);

            // Assert
            Assert.AreEqual(currentRegistrations + 1, newRegistrationsCount);
            Assert.AreEqual(guidComponent.gameObject, gameObjectWithGuid, "GameManager returned wrong guid object");

            // TearDown

        }

        [Test]
        public void GuidComponent_Registration_OnActivate_Passes()
        {
            // Setup
            var go = new GameObject("__Guid_Test_Inactive");
            go.SetActive(false);
            int currentRegistrations = GuidManager.GameObjects.Count;

            // Test
            var guidComponent = go.AddComponent<GuidComponent>();
            go.SetActive(true);
            int newRegistrationsCount = GuidManager.GameObjects.Count;
            var gameObjectWithGuid = GuidManager.GetGameObject(guidComponent.Guid);

            // Assert
            Assert.AreEqual(currentRegistrations + 1, newRegistrationsCount);
            Assert.AreEqual(guidComponent.gameObject, gameObjectWithGuid, "GameManager returned wrong guid object");

            // TearDown

        }

        [Test]
        public void GuidComponent_Registration_OnEnable_Passes()
        {
            // Setup
            var go = new GameObject("__Guid_Test_Inactive");
            go.SetActive(false);
            int currentRegistrations = GuidManager.GameObjects.Count;

            // Test
            var guidComponent = go.AddComponent<GuidComponent>();
            guidComponent.enabled = false;
            go.SetActive(true);
            guidComponent.enabled = true;
            int newRegistrationsCount = GuidManager.GameObjects.Count;
            var gameObjectWithGuid = GuidManager.GetGameObject(guidComponent.Guid);

            // Assert
            Assert.AreEqual(currentRegistrations + 1, newRegistrationsCount);
            Assert.AreEqual(guidComponent.gameObject, gameObjectWithGuid, "GameManager returned wrong guid object");

            // TearDown

        }

        [Test]
        public void GuidComponent_Registration_NewEnable_Fails()
        {
            // Setup
            var guidComponent = CreateGuidComponent("__TestGuid");
            int currentRegistrations = GuidManager.GameObjects.Count;

            // Test
            guidComponent.enabled = false;
            guidComponent.enabled = true;
            int newRegistrationsCount = GuidManager.GameObjects.Count;
            var gameObjectWithGuid = GuidManager.GetGameObject(guidComponent.Guid);

            // Assert
            Assert.AreEqual(currentRegistrations, newRegistrationsCount);
            Assert.AreEqual(guidComponent.gameObject, gameObjectWithGuid, "GameManager returned wrong guid object");

            // TearDown

        }

        [Test]
        public void GuidComponent_Unregistration_OnDisable_Fails()
        {
            // Setup
            CreateTestObjects();
            int currentRegistrations = GuidManager.GameObjects.Count;

            // Test
            m_testObjects[0].SetActive(false);
            int newRegistrationsCount = GuidManager.GameObjects.Count;
            bool containsGuid = GuidManager.GetGameObject(m_testObjects[0].GetComponent<GuidComponent>().Guid) == m_testObjects[0];

            // Assert
            Assert.AreEqual(currentRegistrations, newRegistrationsCount);
            Assert.IsTrue(containsGuid, "Disabled guid was not found by the GuidManager");

            // TearDown

        }

        [UnityTest]
        public IEnumerator GuidComponent_Unregistration_OnDestroy_Passes()
        {
            // Setup
            CreateTestObjects();
            int currentRegistrations = GuidManager.GameObjects.Count;

            // Test
            DestroyGameObject(m_testObjects[0]);

            yield return null;

            int newRegistrationsCount = GuidManager.GameObjects.Count;

            // Assert
            Assert.AreEqual(currentRegistrations - 1, newRegistrationsCount);

            // TearDown

        }

        [UnityTest]
        public IEnumerator GuidComponent_Unregistration_DisabledHierarchy_Passes()
        {
            // Setup
            CreateTestObjects();
            var sonGuid = CreateGuidComponent("__TEST_SON_GUID__");
            sonGuid.transform.SetParent(m_testObjects[0].transform);
            sonGuid.gameObject.SetActive(false);
            int currentRegistrations = GuidManager.GameObjects.Count;

            // Test
            var guid = sonGuid.Guid;
            DestroyGameObject(m_testObjects[0]);
            yield return null;

            int newRegistrationsCount = GuidManager.GameObjects.Count;
            bool containsGuid = GuidManager.ContainsGuid(guid);

            // Assert
            Assert.IsFalse(sonGuid);
            Assert.IsFalse(containsGuid);
            Assert.AreEqual(currentRegistrations - 2, newRegistrationsCount);
            Assert.AreEqual(currentRegistrations - 2, newRegistrationsCount);

            // TearDown

        }

        [Test]
        public void GuidComponent_Duplicate_Passes()
        {
            // Setup

            // Test
            var guidComponent = CreateGuidComponent("__Guid_Duplicate");
            var duplicateGuidComponent = Object.Instantiate(guidComponent);

            // Assert
            LogAssert.Expect(LogType.Error, new Regex("[Gg]uid collision detected between"));
            Assert.AreNotEqual(guidComponent.Guid, duplicateGuidComponent.Guid);
        }

        [Test]
        public void GuidComponent_Duplicate_Registration_Passes()
        {
            // Setup
            CreateTestObjects();
            int currentRegistrations = GuidManager.GameObjects.Count;

            // Test
            var guidComponent = CreateGuidComponent("__Guid_Duplicate");
            var duplicateGuidComponent = Object.Instantiate(guidComponent);
            int newRegistrationsCount = GuidManager.GameObjects.Count;

            // Assert
            LogAssert.Expect(LogType.Error, new Regex("[Gg]uid collision detected between"));
            Assert.AreEqual(currentRegistrations + 2, newRegistrationsCount);
            Assert.AreNotEqual(guidComponent.Guid, duplicateGuidComponent.Guid);
        }
        
        [Test]
        public void GuidComponent_Duplicate_DisabledObject_Passes()
        {
            // Setup
            int currentRegistrations = GuidManager.GameObjects.Count;

            // Test
            var guidComponent = CreateGuidComponent("__Guid_Test_1");
            guidComponent.gameObject.SetActive(false);
            var clonedGuidComponent = Object.Instantiate(guidComponent);
            int newRegistrationsCount = GuidManager.GameObjects.Count;

            // Assert
            LogAssert.Expect(LogType.Error, new Regex("[Gg]uid collision detected between"));
            Assert.AreEqual(currentRegistrations + 2, newRegistrationsCount);
            Assert.AreNotEqual(guidComponent.Guid, clonedGuidComponent.Guid);
            Assert.AreNotEqual(System.Guid.Empty, guidComponent.Guid);
            Assert.AreNotEqual(System.Guid.Empty, clonedGuidComponent.Guid);

            // TearDown
        }

        [Test]
        public void GuidComponent_Duplicate_DeepDisabledObject_Passes()
        {
            // Setup
            CreateTestObjects();
            int currentRegistrations = GuidManager.GameObjects.Count;

            // Test
            var guidComponent = CreateGuidComponent("__Guid_Test_1");
            guidComponent.gameObject.SetActive(false);
            var intermediateParent = new GameObject("__Guid_Test_Parent").transform;
            intermediateParent.SetParent(m_testObjects[0].transform);
            guidComponent.transform.SetParent(intermediateParent);
            var clonedIntermediateParent = Object.Instantiate(intermediateParent);
            var clonedGuidComponent = clonedIntermediateParent.GetComponentInChildren<GuidComponent>(true);
            int newRegistrationsCount = GuidManager.GameObjects.Count;

            // Assert
            LogAssert.Expect(LogType.Error, new Regex("[Gg]uid collision detected between"));
            Assert.AreEqual(currentRegistrations + 2, newRegistrationsCount);
            Assert.AreNotEqual(guidComponent.Guid, clonedGuidComponent.Guid);
            Assert.AreNotEqual(System.Guid.Empty, guidComponent.Guid);
            Assert.AreNotEqual(System.Guid.Empty, clonedGuidComponent.Guid);

            // TearDown
        }
    }
}
