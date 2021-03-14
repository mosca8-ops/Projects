using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TXT.WEAVR.Maintenance;

namespace TXT.WEAVR.RemoteControl
{
    [AddComponentMenu("WEAVR/Remote Control/Commands/2-Way Switch Controls")]
    public class TwoWaySwitchControlCommands : BaseCommandUnit
    {
        [RemoteEvent]
        public event Action<string> OnSwitchDown;
        [RemoteEvent]
        public event Action<string> OnSwitchUp;

        private AbstractTwoWaySwitch m_lastTwoWaySwitch;
        public AbstractTwoWaySwitch LastTwoWaySwitch
        {
            get => m_lastTwoWaySwitch;
            set
            {
                if (m_lastTwoWaySwitch != value)
                {
                    if (m_lastTwoWaySwitch)
                    {
                        m_lastTwoWaySwitch.OnDown.RemoveListener(InvokeSwitchDown);
                        m_lastTwoWaySwitch.OnUp.RemoveListener(InvokeSwitchUp);
                    }
                    m_lastTwoWaySwitch = value;
                    if (m_lastTwoWaySwitch)
                    {
                        m_lastTwoWaySwitch.OnDown.RemoveListener(InvokeSwitchDown);
                        m_lastTwoWaySwitch.OnDown.AddListener(InvokeSwitchDown);
                        m_lastTwoWaySwitch.OnUp.RemoveListener(InvokeSwitchUp);
                        m_lastTwoWaySwitch.OnUp.AddListener(InvokeSwitchUp);
                    }
                }
            }
        }

        [RemotelyCalled]
        public void SwitchDown(Guid guid)
        {
            var twoWaySwitch = Query.GetComponentByGuid<AbstractTwoWaySwitch>(guid);
            if (twoWaySwitch)
            {
                LastTwoWaySwitch = twoWaySwitch;
                twoWaySwitch.CurrentState = Switch2WayState.Down;
            }
        }

        [RemotelyCalled]
        public void SwitchDown(string path)
        {
            var twoWaySwitch = Query.Find<AbstractTwoWaySwitch>(QuerySearchType.Scene, path).First();
            if (twoWaySwitch)
            {
                LastTwoWaySwitch = twoWaySwitch;
                twoWaySwitch.CurrentState = Switch2WayState.Down;
            }
        }

        [RemotelyCalled]
        public void SwitchUp(Guid guid)
        {
            var twoWaySwitch = Query.GetComponentByGuid<AbstractTwoWaySwitch>(guid);
            if (twoWaySwitch)
            {
                LastTwoWaySwitch = twoWaySwitch;
                twoWaySwitch.CurrentState = Switch2WayState.Up;
            }
        }

        [RemotelyCalled]
        public void SwitchUp(string path)
        {
            var twoWaySwitch = Query.Find<AbstractTwoWaySwitch>(QuerySearchType.Scene, path).First();
            if (twoWaySwitch)
            {
                LastTwoWaySwitch = twoWaySwitch;
                twoWaySwitch.CurrentState = Switch2WayState.Up;
            }
        }

        public void InvokeSwitchDown(string path)
        {
            OnSwitchDown?.Invoke(path);
        }

        public void InvokeSwitchUp(string path)
        {
            OnSwitchUp?.Invoke(path);
        }

        public void InvokeSwitchDown(GameObject gameObject)
        {
            if (gameObject == null) return;

            var path = SceneTools.GetGameObjectPath(gameObject);
            InvokeSwitchDown(path);
        }

        public void InvokeSwitchUp(GameObject gameObject)
        {
            if (gameObject == null) return;

            var path = SceneTools.GetGameObjectPath(gameObject);
            InvokeSwitchUp(path);
        }

        private void InvokeSwitchDown()
        {
            InvokeSwitchDown(LastTwoWaySwitch.GetHierarchyPath());
        }

        private void InvokeSwitchUp()
        {
            InvokeSwitchDown(LastTwoWaySwitch.GetHierarchyPath());
        }
    }

}