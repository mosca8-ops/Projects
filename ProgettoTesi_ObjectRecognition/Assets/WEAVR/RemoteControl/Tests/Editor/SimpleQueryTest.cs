using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TXT.WEAVR.RemoteControl
{
    public class SimpleQueryTest
    {
        SimpleQuery<Vector3> m_valueTypeQuery;
        SimpleQuery<Transform> m_objectTypeQuery;
        List<Transform> m_transforms;

        [SetUp]
        public void Initialize()
        {
            //m_valueTypeQuery = InitializeValueTypes(Vector3.zero, Vector3.one, Vector3.back, Vector3.up, Vector3.left, Vector3.right);

            //InitializeObjectTypes();
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
            return new GameObject(name) { hideFlags = HideFlags.HideAndDontSave }.transform;
        }

        private static Transform[] CreateTransforms(params string[] names)
        {
            return names.Select(n => CreateTransform(n)).ToArray();
        }

        private void InitializeObjectTypesList(params Transform[] transforms)
        {
            m_transforms = new List<Transform>(transforms);

            m_objectTypeQuery = new SimpleQuery<Transform>(
                null,// Need to mock here (Try NSubstitue..) 
                m_transforms
                );
        }

        private void InitializeObjectTypesList(params string[] transformNames) => InitializeObjectTypesList(CreateTransforms(transformNames));

        private IQuery<T> InitializeValueTypes<T>(params T[] values) where T : struct
        {
            HashSet<T> vectors = new HashSet<T>(values);

            return new SimpleQuery<T>(
                null,  // Need to mock here (Try NSubstitue..) 
                vectors
                );
        }

        [TearDown]
        public void Terminate()
        {
            TerminateObjectTypes();
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
        public void SimpleQuery_ValueType_GetFirst_Passes()
        {
            // Setup
            var simpleQuery = InitializeValueTypes(Vector3.one, Vector3.zero, Vector3.up);

            // Make the test
            var first = simpleQuery.First();

            // Assert
            Assert.AreEqual(Vector3.one, first);
        }

        [Test]
        public void SimpleQuery_ValueType_GetFirstFiltered_Passes()
        {
            // Setup
            var simpleQuery = InitializeValueTypes(Vector3.one, Vector3.zero, Vector3.up, new Vector3(1, 1, 0));

            // Make the test
            var first = simpleQuery.First(v => v.y > 0 && v.z == 0);

            // Assert
            Assert.AreEqual(Vector3.up, first);
        }

        [Test]
        public void SimpleQuery_ValueType_GetFirstFiltered_Fails()
        {
            // Setup
            var simpleQuery = InitializeValueTypes(Vector3.one, Vector3.zero, Vector3.up, new Vector3(1, 1, 0));

            // Make the test
            var first = simpleQuery.First(v => v.y > 0 && v.z == 0);

            // Assert
            Assert.AreNotEqual(Vector3.one, first);
        }

        [Test]
        public void SimpleQuery_ValueType_GetFirst_Fails()
        {
            // Setup
            var simpleQuery = InitializeValueTypes<Vector3>();

            // Make the test
            var first = simpleQuery.First();

            // Assert
            Assert.AreEqual(default(Vector3), first);
        }

        [Test]
        public void SimpleQuery_ValueType_GetLast_Passes()
        {
            // Setup
            var simpleQuery = InitializeValueTypes(Vector3.one, Vector3.zero, Vector3.up);
            var oneItemQuery = InitializeValueTypes(Vector3.one);

            // Make the test
            var last = simpleQuery.Last();

            // Assert
            Assert.AreEqual(Vector3.up, last);
            Assert.AreEqual(Vector3.one, oneItemQuery.Last());
        }

        [Test]
        public void SimpleQuery_ValueType_GetLastFiltered_Passes()
        {
            // Setup
            var simpleQuery = InitializeValueTypes(Vector3.one, Vector3.zero, new Vector3(1, 1, 0), Vector3.up, Vector3.forward);

            // Make the test
            var last = simpleQuery.Last(v => v.y > 0 && v.z == 0);

            // Assert
            Assert.AreEqual(Vector3.up, last);
        }

        [Test]
        public void SimpleQuery_ValueType_GetLastFiltered_Fails()
        {
            // Setup
            var simpleQuery = InitializeValueTypes(Vector3.one, Vector3.zero, new Vector3(1, 1, 0), Vector3.up, Vector3.forward);

            // Make the test
            var last = simpleQuery.Last(v => v.y < 0 && v.z == 0);

            // Assert
            Assert.AreEqual(default(Vector3), last);
        }

        [Test]
        public void SimpleQuery_ToList_Passes()
        {
            // Setup
            List<Vector3> source = new List<Vector3>() { Vector3.one, Vector3.zero, Vector3.up };
            var simpleQuery = InitializeValueTypes(source.ToArray());

            // Make the test
            var list = simpleQuery.ToList();

            // Assert
            CollectionAssert.AreEqual(source, list);
        }

        [Test]
        public void SimpleQuery_ToList_Fails()
        {
            // Setup
            List<Vector3> source = new List<Vector3>() { Vector3.one, Vector3.zero, Vector3.up };
            var simpleQuery = InitializeValueTypes(source.ToArray());
            source.RemoveAt(0);

            // Make the test
            var list = simpleQuery.ToList();

            // Assert
            CollectionAssert.AreNotEqual(source, list);
        }

        [Test]
        public void SimpleQuery_GetEnumerator_IsDifferentFromInternal()
        {
            // Setup
            List<Vector3> source = new List<Vector3>() { Vector3.one, Vector3.zero, Vector3.up };
            var simpleQuery = InitializeValueTypes(source.ToArray());

            // Make the test
            var firstEnumerator = simpleQuery.GetEnumerator();
            firstEnumerator.MoveNext();
            var firstElement = firstEnumerator.Current;

            // Asserts
            Assert.AreEqual(simpleQuery.First(), firstElement);
            Assert.AreNotEqual(firstEnumerator, simpleQuery.GetEnumerator());
        }

        [Test]
        public void SimpleQuery_ValueType_GetElementAt_FromStart_Passes()
        {
            // Setup
            var simpleQuery = InitializeValueTypes(Vector3.one, Vector3.zero, Vector3.up, new Vector3(0.1f, 0.1f, 0.1f));

            // Make the test
            // Should return the second element from the start
            var found = simpleQuery.GetElementAt(2);

            // Assert
            Assert.AreEqual(Vector3.up, found);
        }

        [Test]
        public void SimpleQuery_ValueType_GetElementAt_FromEnd_Passes()
        {
            // Setup
            var simpleQuery = InitializeValueTypes(Vector3.one, Vector3.zero, Vector3.up, new Vector3(0.1f, 0.1f, 0.1f));

            // Make the test
            // Should return the second element from the end
            var found = simpleQuery.GetElementAt(-2);

            // Assert
            Assert.AreEqual(Vector3.up, found);
        }

        [Test]
        public void SimpleQuery_ReferenceType_GetFirst_Passes()
        {
            // Setup
            InitializeObjectTypes();

            // Make the test
            var first = m_objectTypeQuery.First();

            // Assert
            Assert.AreEqual(m_transforms[0], first);
        }

        [Test]
        public void SimpleQuery_ReferenceType_GetFirstFiltered_Passes()
        {
            // Setup
            InitializeObjectTypesList("First", "Second", "Third");

            // Make the test
            var first = m_objectTypeQuery.First(v => v.gameObject.name == "Second");

            // Assert
            Assert.AreEqual(m_transforms[1], first);
        }

        [Test]
        public void SimpleQuery_ReferenceType_GetFirstFiltered_Fails()
        {
            // Setup
            InitializeObjectTypesList("Zero", "First x", "First y", "Second");

            // Make the test
            var first = m_objectTypeQuery.First(v => v.gameObject.name == "First");

            // Assert
            Assert.AreEqual(null, first);
        }

        [Test]
        public void SimpleQuery_ReferenceType_GetFirst_Fails()
        {
            // Setup
            InitializeObjectTypesList(new string[0]);

            // Make the test
            var first = m_objectTypeQuery.First();

            // Assert
            Assert.AreEqual(null, first);
        }
    }
}
