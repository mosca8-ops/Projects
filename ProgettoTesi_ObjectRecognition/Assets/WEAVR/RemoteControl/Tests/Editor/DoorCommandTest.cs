using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using TXT.WEAVR.Common;
using TXT.WEAVR.Interaction;

namespace TXT.WEAVR.RemoteControl
{
    public class DoorCommandTest
    {
        private DoorControlCommands m_doorControl;
        private AbstractDoor m_door;

        [SetUp]
        public void SetUp()
        {
            m_doorControl = GameObject.FindObjectOfType<DoorControlCommands>();

            var doorObj = new GameObject("TestDoor");
            var interactionCtrl = doorObj.AddComponent<AbstractInteractionController>();
            m_door = doorObj.AddComponent<AbstractDoor>();
            interactionCtrl.DefaultBehaviour = m_door;
            m_door.SnapshotClosed();
            m_door.transform.Rotate(Vector3.up, 90f);
            m_door.SnapshotFullyOpen();
            m_door.Close();
        }

        [TearDown]
        public void Teardown()
        {
            m_doorControl = null;
            GameObject.DestroyImmediate(m_door.gameObject);
            m_door = null;
        }

        [Test]
        public void DoorControlCommands_OpenDoor_Path_Test()
        {
            var path = SceneTools.GetGameObjectPath(m_door.gameObject);
            m_doorControl.OpenDoor(path);

            //Assert
            Assert.AreEqual(1f, m_door.CurrentOpenProgress);
        }

        [Test]
        public void DoorControlCommands_CloseDoor_Path_Test()
        {
            var path = SceneTools.GetGameObjectPath(m_door.gameObject);
            m_doorControl.CloseDoor(path);

            //Assert
            Assert.AreEqual(0f, m_door.CurrentOpenProgress);
        }

        //[Test]
        //public void DoorControlCommands_OpenDoor_Guid_Test()
        //{
        //    GuidManager.Register(m_door.gameObject);
        //    m_doorControl.OpenDoor(path);
        //    //Assert
        //    Assert.AreEqual(1f, m_door.CurrentOpenProgress);
        //}
    }
}
