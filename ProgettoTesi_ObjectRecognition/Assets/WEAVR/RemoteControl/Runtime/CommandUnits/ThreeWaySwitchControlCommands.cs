using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TXT.WEAVR.Maintenance;

namespace TXT.WEAVR.RemoteControl
{
    [AddComponentMenu("WEAVR/Remote Control/Commands/3-Way Switch Controls")]
    public class ThreeWaySwitchControlCommands : BaseCommandUnit
    {
        [RemoteEvent]
        public event Action<string> OnSwitchDown;
        [RemoteEvent]
        public event Action<string> OnSwitchMiddle;
        [RemoteEvent]
        public event Action<string> OnSwitchUp;

        private AbstractThreeWaySwitch m_lastThreeWaySwitch;
        public AbstractThreeWaySwitch LastThreeWaySwitch
        {
            get => m_lastThreeWaySwitch;
            set
            {
                if (m_lastThreeWaySwitch != value)
                {
                    if (m_lastThreeWaySwitch)
                    {
                        m_lastThreeWaySwitch.OnDown.RemoveListener(InvokeSwitchDown);
                        m_lastThreeWaySwitch.OnMiddle.RemoveListener(InvokeSwitchMiddle);
                        m_lastThreeWaySwitch.OnUp.RemoveListener(InvokeSwitchUp);
                    }
                    m_lastThreeWaySwitch = value;
                    if (m_lastThreeWaySwitch)
                    {
                        m_lastThreeWaySwitch.OnDown.RemoveListener(InvokeSwitchDown);
                        m_lastThreeWaySwitch.OnDown.AddListener(InvokeSwitchDown);
                        m_lastThreeWaySwitch.OnMiddle.RemoveListener(InvokeSwitchMiddle);
                        m_lastThreeWaySwitch.OnMiddle.AddListener(InvokeSwitchMiddle);
                        m_lastThreeWaySwitch.OnUp.RemoveListener(InvokeSwitchUp);
                        m_lastThreeWaySwitch.OnUp.AddListener(InvokeSwitchUp);
                    }
                }
            }
        }

        [RemotelyCalled]
        public void SwitchDown(Guid guid)
        {
            var threeWaySwitch = Query.GetComponentByGuid<AbstractThreeWaySwitch>(guid);
            if (threeWaySwitch)
            {
                LastThreeWaySwitch = threeWaySwitch;
                threeWaySwitch.CurrentState = Switch3WayState.Down;
            }
        }

        [RemotelyCalled]
        public void SwitchDown(string path)
        {
            var threeWaySwitch = Query.Find<AbstractThreeWaySwitch>(QuerySearchType.Scene, path).First();
            if (threeWaySwitch)
            {
                LastThreeWaySwitch = threeWaySwitch;
                threeWaySwitch.CurrentState = Switch3WayState.Down;
            }
        }

        [RemotelyCalled]
        public void SwitchMiddle(Guid guid)
        {
            var threeWaySwitch = Query.GetComponentByGuid<AbstractThreeWaySwitch>(guid);
            if (threeWaySwitch)
            {
                LastThreeWaySwitch = threeWaySwitch;
                threeWaySwitch.CurrentState = Switch3WayState.Middle;
            }
        }

        [RemotelyCalled]
        public void SwitchMiddle(string path)
        {
            var threeWaySwitch = Query.Find<AbstractThreeWaySwitch>(QuerySearchType.Scene, path).First();
            if (threeWaySwitch)
            {
                LastThreeWaySwitch = threeWaySwitch;
                threeWaySwitch.CurrentState = Switch3WayState.Middle;
            }
        }

        [RemotelyCalled]
        public void SwitchUp(Guid guid)
        {
            var twoWaySwitch = Query.GetComponentByGuid<AbstractThreeWaySwitch>(guid);
            if (twoWaySwitch)
            {
                LastThreeWaySwitch = twoWaySwitch;
                twoWaySwitch.CurrentState = Switch3WayState.Up;
            }
        }

        [RemotelyCalled]
        public void SwitchUp(string path)
        {
            var threeWaySwitch = Query.Find<AbstractThreeWaySwitch>(QuerySearchType.Scene, path).First();
            if (threeWaySwitch)
            {
                LastThreeWaySwitch = threeWaySwitch;
                threeWaySwitch.CurrentState = Switch3WayState.Up;
            }
        }

        public void InvokeSwitchDown(string path)
        {
            OnSwitchDown?.Invoke(path);
        }

        public void InvokeSwitchMiddle(string path)
        {
            OnSwitchMiddle?.Invoke(path);
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

        public void InvokeSwitchMiddle(GameObject gameObject)
        {
            if (gameObject == null) return;

            var path = SceneTools.GetGameObjectPath(gameObject);
            InvokeSwitchMiddle(path);
        }

        public void InvokeSwitchUp(GameObject gameObject)
        {
            if (gameObject == null) return;

            var path = SceneTools.GetGameObjectPath(gameObject);
            InvokeSwitchUp(path);
        }

        private void InvokeSwitchDown()
        {
            InvokeSwitchDown(LastThreeWaySwitch.GetHierarchyPath());
        }

        private void InvokeSwitchMiddle()
        {
            InvokeSwitchMiddle(LastThreeWaySwitch.GetHierarchyPath());
        }

        private void InvokeSwitchUp()
        {
            InvokeSwitchUp(LastThreeWaySwitch.GetHierarchyPath());
        }
    }

}