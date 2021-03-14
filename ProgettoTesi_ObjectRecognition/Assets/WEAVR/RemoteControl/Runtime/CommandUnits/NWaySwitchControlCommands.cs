using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TXT.WEAVR.Maintenance;

namespace TXT.WEAVR.RemoteControl
{
    [AddComponentMenu("WEAVR/Remote Control/Commands/N-Way Switch Controls")]
    public class NWaySwitchControlCommands : BaseCommandUnit
    {
        [RemoteEvent]
        public event Action<string, int> OnSwitchStateChanged;

        private AbstractNWaySwitch m_lastNWaySwitch;
        public AbstractNWaySwitch LastNWaySwitch
        {
            get => m_lastNWaySwitch;
            set
            {
                if (m_lastNWaySwitch != value)
                {
                    if (m_lastNWaySwitch)
                    {
                        m_lastNWaySwitch.OnStateChanged.RemoveListener(SwitchStateChanged);
                    }
                    m_lastNWaySwitch = value;
                    if (m_lastNWaySwitch)
                    {
                        m_lastNWaySwitch.OnStateChanged.RemoveListener(SwitchStateChanged);
                        m_lastNWaySwitch.OnStateChanged.AddListener(SwitchStateChanged);
                    }
                }
            }
        }

        [RemotelyCalled]
        public void SwitchToState(Guid guid, int stateIndex)
        {
            var nWaySwitch = Query.GetComponentByGuid<AbstractNWaySwitch>(guid);
            if (nWaySwitch)
            {
                LastNWaySwitch = nWaySwitch;
                nWaySwitch.CurrentStateIndex = stateIndex;
            }
        }

        [RemotelyCalled]
        public void SwitchToState(string path, int stateIndex)
        {
            var nWaySwitch = Query.Find<AbstractNWaySwitch>(QuerySearchType.Scene, path).First();
            if (nWaySwitch)
            {
                LastNWaySwitch = nWaySwitch;
                nWaySwitch.CurrentStateIndex = stateIndex;
            }
        }

        private void SwitchStateChanged(int stateIndex)
        {
            OnSwitchStateChanged?.Invoke(LastNWaySwitch.GetHierarchyPath(), stateIndex);
        }
    } 
}
