using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TXT.WEAVR.RemoteControl
{
    public class SceneQueryUnitTest
    {
        SceneQueryUnit m_queryUnit;
        List<Transform> m_transforms;

        Transform root;
        Transform child1;
        Transform child2;
        Transform child3;
        Transform grandChild1;
        Transform grandChild2;
        Transform grandChild3;
        Transform trickyChild1;
        Transform trickyGrandChild1;
        Transform grandGrandChild1;

        Scene testScene;

        [SetUp]
        public void Initialize()
        {
            m_queryUnit = new SceneQueryUnit(SceneManager.GetActiveScene());
        }

        private void CreateDummyHierarchy(bool useTrickyChildren = false)
        {
            root = CreateTransform("root");
            child1 = CreateTransform("Child_1");
            child2 = CreateTransform("Child_2");
            child3 = CreateTransform("Child_2"); // Same name as 2
            grandChild1 = CreateTransform("G/Child_1");
            grandChild2 = CreateTransform("G'-!@#$%%^&()_(!_+=|;.<");
            grandChild3 = CreateTransform(@"ccc    ☼}╤á");
            grandGrandChild1 = CreateTransform("G\\G/Child_1");

            if (useTrickyChildren)
            {
                trickyChild1 = CreateTransform("G");
                trickyGrandChild1 = CreateTransform("Child_1");
            }

            // Level 1
            child1.SetParent(root);
            child2.SetParent(root);
            child3.SetParent(root);

            // Level 2
            grandChild1.SetParent(child1);
            grandChild2.SetParent(child2);
            grandChild3.SetParent(child2);

            if (useTrickyChildren)
            {
                trickyChild1.SetParent(child1);
            }

            // Level 3
            grandGrandChild1.SetParent(grandChild1);

            if (useTrickyChildren)
            {
                trickyGrandChild1.SetParent(trickyChild1);
            }
        }

        private void InitializeObjectTypes()
        {
            InitializeObjectTypesList(
                CreateTransform("Test_T1"),
                CreateTransform("Test_T2"),
                CreateTransform("Test_T3"),
                CreateTransform("Test_T4"),
                CreateTransform("Test_T5"),
                CreateTransform("Test_T6"));
        }

        private static Transform CreateTransform(string name)
        {
            return new GameObject(name) { hideFlags = HideFlags.DontSave }.transform;
        }

        private static void Destroy(Component c)
        {
            if (!c) { return; }
            if (Application.isPlaying)
            {
                Object.Destroy(c.gameObject);
            }
            else
            {
                Object.DestroyImmediate(c.gameObject);
            }
        }

        private static Transform[] CreateTransforms(params string[] names)
        {
            return names.Select(n => CreateTransform(n)).ToArray();
        }

        private void InitializeObjectTypesList(params Transform[] transforms)
        {
            m_transforms = new List<Transform>(transforms);
        }

        private void InitializeObjectTypesList(params string[] transformNames) => InitializeObjectTypesList(CreateTransforms(transformNames));

        private SimpleQuery<T> InitializeValueTypes<T>(params T[] values) where T : struct
        {
            HashSet<T> vectors = new HashSet<T>(values);

            return new SimpleQuery<T>(
                null,  // Need to mock here (Try NSubstitue..) 
                vectors
                );
        }

        private string CreatePathString(params Component[] components)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < components.Length; i++)
            {
                sb.Append(components[i].gameObject.name).Append('/');
            }

            if(sb.Length > 0)
            {
                sb.Length--;
            }

            return sb.ToString();
        }

        [TearDown]
        public void Terminate()
        {
            Destroy(root);

            TerminateObjectTypes();

            if (testScene.IsValid())
            {
                EditorSceneManager.CloseScene(testScene, true);
            }
        }

        private void TerminateObjectTypes()
        {
            if (m_transforms?.Count > 0)
            {
                foreach (var t in m_transforms)
                {
                    if (Application.isPlaying) { Object.Destroy(t.gameObject); }
                    else { Object.DestroyImmediate(t.gameObject); }
                }
                m_transforms.Clear();
            }
        }

        // A Test behaves as an ordinary method
        [Test]
        public void SceneQuery_CanHandleOnlySceneSearch()
        {
            // Assert
            Assert.IsTrue(m_queryUnit.CanHandleSearchType(QuerySearchType.Scene));
            Assert.IsFalse(m_queryUnit.CanHandleSearchType(QuerySearchType.Generic));
            Assert.IsFalse(m_queryUnit.CanHandleSearchType(QuerySearchType.Procedure));
            Assert.IsFalse(m_queryUnit.CanHandleSearchType(QuerySearchType.Interaction));
            Assert.IsFalse(m_queryUnit.CanHandleSearchType(QuerySearchType.None));
        }

        [Test]
        public void SceneQuery_GetTransform_Trivial_Pass()
        {
            // Setup
            CreateDummyHierarchy();

            var path = CreatePathString(root);
            var found_root = m_queryUnit.Query<Transform>(QuerySearchType.Scene, path).First();

            // Assert the values
            Assert.AreEqual(root, found_root, "Path: " + path);
        }

        [Test]
        public void SceneQuery_GetTransform_Trivial_Fail()
        {
            // Setup
            CreateDummyHierarchy();

            // Test
            var found_root = m_queryUnit.Query<Transform>(QuerySearchType.Scene, "root2").First();

            // Assert the values
            Assert.AreEqual(null, found_root);
        }

        [Test]
        public void SceneQuery_GetTransform_TrivialDeeper_Pass()
        {
            // Setup
            CreateDummyHierarchy();

            // Perform Test
            var path = CreatePathString(root, child2);
            var found = m_queryUnit.Query<Transform>(QuerySearchType.Scene, path).First();

            // Assert the values
            Assert.AreEqual(child2, found, "Path: " + path);
        }

        [Test]
        public void SceneQuery_GetTransform_TwoWithSameName_Pass()
        {
            // Setup
            CreateDummyHierarchy();

            // Perform Test
            var path = CreatePathString(root, child2);
            var foundList = m_queryUnit.Query<Transform>(QuerySearchType.Scene, path).ToList();

            // Assert the values
            Assert.IsTrue(foundList.Count == 2, "Child2 list does not have TWO elements. Path: " + path);
            CollectionAssert.Contains(foundList, child2);
            CollectionAssert.Contains(foundList, child3);
        }

        [Test]
        public void SceneQuery_GetTransform_SeparatorsInName_Pass()
        {
            // Setup
            CreateDummyHierarchy();

            // Perform Test
            var path = CreatePathString(root, child1, grandChild1);
            var found = m_queryUnit.Query<Transform>(QuerySearchType.Scene, path).First();

            // Assert the values
            Assert.AreEqual(grandChild1, found, "Path: " + path);
        }

        [Test]
        public void SceneQuery_GetTransform_SeparatorsInNameDeeper_Pass()
        {
            // Setup
            CreateDummyHierarchy();

            // Perform Test
            var path = CreatePathString(root, child1, grandChild1, grandGrandChild1);
            var found = m_queryUnit.Query<Transform>(QuerySearchType.Scene, path).First();

            // Assert the values
            Assert.AreEqual(grandGrandChild1, found, "Path: " + path);
        }

        [Test]
        public void SceneQuery_GetTransform_StandardSymbolsInName_Pass()
        {
            // Setup
            CreateDummyHierarchy();

            // Perform Test
            var path = CreatePathString(root, child2, grandChild2);
            var found = m_queryUnit.Query<Transform>(QuerySearchType.Scene, path).First();

            // Assert the values
            Assert.AreEqual(grandChild2, found, "Path: " + path);
        }

        [Test]
        public void SceneQuery_GetTransform_UnicodeSymbolsInName_Pass()
        {
            // Setup
            CreateDummyHierarchy();

            // Perform Test
            var path = CreatePathString(root, child2, grandChild3);
            var found = m_queryUnit.Query<Transform>(QuerySearchType.Scene, path).First();

            // Assert the values
            Assert.AreEqual(grandChild3, found, "Path: " + path);
        }

        [Test]
        public void SceneQuery_GetTransform_ByFilter_Pass()
        {
            // Setup
            CreateDummyHierarchy();
            child3.position = Vector3.up * 10000f;

            // Perform Test
            var path = CreatePathString(root, child2);
            var found = m_queryUnit.Query<Transform>(QuerySearchType.Scene, t => t.position.y > 1000f).First();

            // Assert the values
            Assert.AreEqual(child3, found, "Path: " + path);
        }

        [Test]
        public void SceneQuery_GetRigidBody_TwoWithSameName_Pass()
        {
            // Setup
            CreateDummyHierarchy();
            var rigidBody = child3.gameObject.AddComponent<Rigidbody>();

            // Perform Test
            var path = CreatePathString(root, child2);
            var found = m_queryUnit.Query<Rigidbody>(QuerySearchType.Scene, path).First();

            // Assert the values
            Assert.AreEqual(rigidBody, found, "Path: " + path);
        }

        [Test]
        public void SceneQuery_GetGameObject_Trivial_Pass()
        {
            // Setup
            CreateDummyHierarchy();

            // Perform Test
            var path = CreatePathString(root, child1);
            var found = m_queryUnit.Query<GameObject>(QuerySearchType.Scene, path).First();

            // Assert the values
            Assert.AreEqual(child1.gameObject, found, "Path: " + path);
        }

        [Test]
        public void SceneQuery_GetNotUnityObject_Trivial_Fail()
        {
            // Setup
            CreateDummyHierarchy();

            // Perform Test
            var path = CreatePathString(root, child1);
            var found = m_queryUnit.Query<string>(QuerySearchType.Scene, path).First();

            // Assert the values
            Assert.IsNull(found, "Path: " + path);
        }

        private void ListBlocks()
        {
            // Make the test
            var found_root_list = m_queryUnit.Query<Transform>(QuerySearchType.Scene, "root").ToList();
            var found_child1_list = m_queryUnit.Query<Transform>(QuerySearchType.Scene, "Child_1").ToList();
            var found_child2_list = m_queryUnit.Query<Transform>(QuerySearchType.Scene, "Child_2").ToList();
            var found_child3_list = m_queryUnit.Query<Transform>(QuerySearchType.Scene, "Child_2").ToList();
            var found_grandChild1_list = m_queryUnit.Query<Transform>(QuerySearchType.Scene, "G/Child_1").ToList();
            var found_grandChild2_list = m_queryUnit.Query<Transform>(QuerySearchType.Scene, "G'-!@#$%%^&()_(!_+=|;.<").ToList();
            var found_grandChild3_list = m_queryUnit.Query<Transform>(QuerySearchType.Scene, @"ccc    ☼}╤á").ToList();
            var found_grandGrandChild1_list = m_queryUnit.Query<Transform>(QuerySearchType.Scene, "G\\G/Child_1").ToList();

            // Assert the lists first
            Assert.IsTrue(found_root_list.Count == 1, "Root list does not have ONE element");
            Assert.IsTrue(found_child1_list.Count == 1, "Child1 list does not have ONE element");
            Assert.IsTrue(found_child2_list.Count == 2, "Child2 list does not have TWO elements");
            Assert.IsTrue(found_grandChild1_list.Count == 1, "GrandChild1 list does not have ONE element");
            Assert.IsTrue(found_grandChild2_list.Count == 1, "GrandChild2 list does not have ONE element");
            Assert.IsTrue(found_grandChild3_list.Count == 1, "GrandChild3 list does not have ONE element");
            Assert.IsTrue(found_grandGrandChild1_list.Count == 1, "GrandGrandChild1 list does not have ONE element");
            CollectionAssert.AreEqual(found_child2_list, found_child3_list);
        }
    }
}
