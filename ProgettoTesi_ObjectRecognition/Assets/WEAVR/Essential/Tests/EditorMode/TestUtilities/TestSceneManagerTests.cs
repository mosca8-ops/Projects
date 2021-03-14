using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TXT.WEAVR.TestsUtility
{

    public class TestSceneManagerTests
    {
        [TearDown]
        public void TearDown()
        {
            TestSceneManager.CleanUp();
        }

        [Test]
        public void TestOpenScene_Create_Single_Passes()
        {
            // Setup
            string sceneName = "__TEST_SCENE__";

            // Test
            var scene = TestSceneManager.CreateOrGetTestScene(sceneName, additive: false);

            try
            {
                // Assert
                Assert.IsTrue(scene.IsValid(), $"Scene {sceneName} is not Valid");
                Assert.IsTrue(scene.isLoaded, $"Scene {sceneName} is not Loaded");
                Assert.AreEqual(sceneName, scene.name, "Scene names do not match");
            }
            finally
            {
                // TearDown
                TestSceneManager.DeleteTestScene(sceneName);
            }
        }

        [Test]
        public void TestOpenScene_Delete_Single_Passes()
        {
            // Setup
            string sceneName = "__TEST_SCENE__";

            // Test
            var scene = TestSceneManager.CreateOrGetTestScene(sceneName, additive: false);

            try
            {
                // Assert
                Assert.IsTrue(scene.IsValid(), $"Scene {sceneName} is not Valid");
            }
            finally
            {
                // TearDown
                TestSceneManager.DeleteTestScene(sceneName);
            }

            Assert.IsFalse(scene.IsValid(), $"Scene {sceneName} is valid");
        }

        [Test]
        public void TestOpenScene_Create_Additive_Passes()
        {
            // Setup
            string sceneName = "__TEST_SCENE__";
            int currentScenes = EditorSceneManager.loadedSceneCount;

            // Test
            var scene = TestSceneManager.CreateOrGetTestScene(sceneName, additive: true);

            try
            {

                // Assert
                Assert.IsTrue(scene.IsValid(), $"Scene {sceneName} is not Valid");
                Assert.IsTrue(scene.isLoaded, $"Scene {sceneName} is not Loaded");
                Assert.AreEqual(currentScenes + 1, EditorSceneManager.loadedSceneCount);
                Assert.AreEqual(sceneName, scene.name, "Scene names do not match");
            }

            finally
            {
                // TearDown
                TestSceneManager.DeleteTestScene(sceneName);
            }
        }

        [Test]
        public void TestOpenScene_CreateTwoWithSameName_Additive_Fails()
        {
            // Setup
            string sceneName = "__TEST_SCENE__";
            int currentScenes = EditorSceneManager.loadedSceneCount;

            // Test
            var scene = TestSceneManager.CreateOrGetTestScene(sceneName, additive: true);
            var scene2 = TestSceneManager.CreateOrGetTestScene(sceneName, additive: true);

            try
            {

                // Assert
                Assert.IsTrue(scene.IsValid(), $"Scene {sceneName} is not Valid");
                Assert.IsTrue(scene.isLoaded, $"Scene {sceneName} is not Loaded");
                Assert.AreEqual(currentScenes + 1, EditorSceneManager.loadedSceneCount);
                Assert.AreEqual(scene, scene2, "Scenes do not match");
            }

            finally
            {
                // TearDown
                TestSceneManager.CleanUp();
            }
        }

        [Test]
        public void TestOpenScene_Delete_Additive_Passes()
        {
            // Setup
            string sceneName = "__TEST_SCENE__";
            int currentScenes = EditorSceneManager.loadedSceneCount;

            // Test
            var scene = TestSceneManager.CreateOrGetTestScene(sceneName, additive: true);

            try
            {
                // Assert
                Assert.IsTrue(scene.IsValid(), $"Scene {sceneName} is not Valid");
            }
            finally
            {
                // TearDown
                TestSceneManager.DeleteTestScene(sceneName);
            }

            Assert.IsFalse(scene.IsValid(), $"Scene {sceneName} is valid");
            Assert.AreEqual(currentScenes, EditorSceneManager.loadedSceneCount);

            TestSceneManager.CleanUp();
        }

        [Test]
        public void TestOpenScene_CleanUp_Passes()
        {
            // Setup
            string sceneName = "__TEST_SCENE__";
            string sceneName2 = "__TEST_SCENE_2__";
            string sceneName3 = "__TEST_SCENE_3__";

            // Since UNITY creates a new empty scene for testing, it breaks our testing pipeline
            // because on Cleanup even that scene will be deleted
            int currentScenes = GetPersistentLoadedScenesCount();

            // Test
            var scene = TestSceneManager.CreateOrGetTestScene(sceneName, additive: true);
            var scene2 = TestSceneManager.CreateOrGetTestScene(sceneName2, additive: true);
            var scene3 = TestSceneManager.CreateOrGetTestScene(sceneName3, additive: true);

            TestSceneManager.CleanUp();

            Assert.IsFalse(scene.IsValid(), $"Scene {sceneName} is not Valid");
            Assert.IsFalse(scene2.IsValid(), $"Scene {sceneName2} is not Valid");
            Assert.IsFalse(scene3.IsValid(), $"Scene {sceneName3} is not Valid");
            Assert.AreEqual(currentScenes, EditorSceneManager.loadedSceneCount);
        }

        [Test]
        public void TestOpenScene_PrepareScenes_Additive_Passes()
        {
            // Setup
            string sceneName = "__TEST_SCENE__";
            string sceneName2 = "__TEST_SCENE_2__";
            string sceneName3 = "__TEST_SCENE_3__";

            int currentScenes = EditorSceneManager.loadedSceneCount;

            // Test
            var scenes = TestSceneManager.PrepareScenes(true, sceneName, sceneName2, sceneName3);

            // Assert
            Assert.AreEqual(currentScenes + 3, EditorSceneManager.loadedSceneCount);
            Assert.IsTrue(scenes[0].IsValid(), $"Scene {sceneName} is not Valid");
            Assert.IsTrue(scenes[1].IsValid(), $"Scene {sceneName2} is not Valid");
            Assert.IsTrue(scenes[2].IsValid(), $"Scene {sceneName3} is not Valid");
            Assert.AreEqual(scenes[0].name, sceneName);
            Assert.AreEqual(scenes[1].name, sceneName2);
            Assert.AreEqual(scenes[2].name, sceneName3);
        }

        private static int GetPersistentLoadedScenesCount()
        {
            int currentScenes = 0;
            for (int i = 0; i < EditorSceneManager.loadedSceneCount; i++)
            {
                if (!string.IsNullOrEmpty(EditorSceneManager.GetSceneAt(i).name))
                {
                    currentScenes++;
                }
            }

            return currentScenes;
        }
    }
}
