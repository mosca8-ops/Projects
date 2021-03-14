using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [Serializable]
    public class ExecutionModesContainer
    {
        [SerializeField]
        private Procedure m_procedure;
        [SerializeField]
        private List<ExecutionMode> m_modes = new List<ExecutionMode>();

        public Procedure Procedure
        {
            get => m_procedure;
            set
            {
                if(m_procedure != value)
                {
                    m_procedure = value;
                    if (m_procedure) {
                        var oldModes = m_modes;
                        m_modes = new List<ExecutionMode>();
                        foreach (var mode in m_procedure.ExecutionModes)
                        {
                            if (mode && oldModes.Any(m => m && (m == mode || mode.ModeName == m.ModeName)))
                            {
                                m_modes.Add(mode);
                            }
                        }
                    }
                }
            }
        }

        public bool HasMode(ExecutionMode mode)
        {
            return m_modes.Contains(mode);
        }
    }

    [Serializable]
    public class OptionalExecutionModesContainer : Optional<ExecutionModesContainer>
    {

    }
}
