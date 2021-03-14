using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TXT.WEAVR.Common;

namespace TXT.WEAVR.RemoteControl
{

    [AddComponentMenu("WEAVR/Remote Control/Commands/Door Controls")]
    public class DoorControlCommands : BaseCommandUnit
    {
        [RemoteEvent]
        public event Action<string, float> DoorOpenProgress;

        private AbstractDoor m_lastDoor;
        public AbstractDoor LastDoor
        {
            get => m_lastDoor;
            set
            {
                if(m_lastDoor != value)
                {
                    if (m_lastDoor)
                    {
                        m_lastDoor.OnOpenProgress.RemoveListener(DoorOpening);
                    }
                    m_lastDoor = value;
                    if (m_lastDoor)
                    {
                        m_lastDoor.OnOpenProgress.RemoveListener(DoorOpening);
                        m_lastDoor.OnOpenProgress.AddListener(DoorOpening);
                    }
                }
            }
        }

        [RemotelyCalled]
        public void OpenDoor(Guid guid)
        {
            var door = Query.GetComponentByGuid<AbstractDoor>(guid);
            if (door)
            {
                LastDoor = door;
                door.Open();
            }
        }

        [RemotelyCalled]
        public void OpenDoor(string path)
        {
            var door = Query.Find<AbstractDoor>(QuerySearchType.Scene, path).First();
            if (door)
            {
                LastDoor = door;
                door.Open();
            }
        }

        [RemotelyCalled]
        public void OpenDoorAt(Guid guid, float opening)
        {
            var door = Query.GetComponentByGuid<AbstractDoor>(guid);
            if (door)
            {
                LastDoor = door;
                door.AnimatedOpenProgress = opening;
            }
        }

        [RemotelyCalled]
        public void OpenDoorAt(string path, float opening)
        {
            var door = Query.Find<AbstractDoor>(QuerySearchType.Scene, path).First();
            if (door)
            {
                LastDoor = door;
                door.AnimatedOpenProgress = opening;
            }
        }

        [RemotelyCalled]
        public void CloseDoor(Guid guid)
        {
            var door = Query.GetComponentByGuid<AbstractDoor>(guid);
            if (door)
            {
                LastDoor = door;
                door.Close();
            }
        }

        [RemotelyCalled]
        public void CloseDoor(string path)
        {
            var door = Query.Find<AbstractDoor>(QuerySearchType.Scene, path).First();
            if (door)
            {
                LastDoor = door;
                door.Close();
            }
        }

        private void DoorOpening(float opening)
        {
            DoorOpenProgress?.Invoke(LastDoor.GetHierarchyPath(), opening);
        }
    }
}
