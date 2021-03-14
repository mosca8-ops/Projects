using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [CreateAssetMenu(fileName = "ProcedureConfig", menuName = "WEAVR/Procedure Config")]
    [DefaultExecutionOrder(-28480)]
    public class ProcedureConfig : ScriptableObject
    {
        [SerializeField]
        private string m_shortName;
        [SerializeField]
        private string m_template;
        [SerializeField]
        private bool m_canSkipSteps;

        [SerializeField]
        [LongText]
        private string m_templateDescription;

        [Space]
        [SerializeField]
        [ArrayElement(nameof(m_executionModes))]
        private ExecutionMode m_defaultExecutionMode;
        [SerializeField]
        [ArrayElement(nameof(m_executionModes))]
        private ExecutionMode m_hintsReplayExecutionMode;
        [SerializeField]
        private List<ExecutionMode> m_executionModes;

        public string ShortName => m_shortName;

        public string Template => m_template;

        public bool CanSkipSteps => m_canSkipSteps;

        public string TemplateDescription => m_templateDescription;

        public event Action<ExecutionMode> ExecutionModeChanged;

        public ExecutionMode DefaultExecutionMode
        {
            get
            {
                if (m_defaultExecutionMode == null && m_executionModes.Count > 0)
                {
                    m_defaultExecutionMode = m_executionModes[0];
                    ExecutionModeChanged?.Invoke(m_defaultExecutionMode);
                }
                return m_defaultExecutionMode;
            }
            set
            {
                if (m_defaultExecutionMode != value && value && m_executionModes.Contains(value))
                {
                    m_defaultExecutionMode = value;
                    ExecutionModeChanged?.Invoke(value);
                }
            }
        }

        public ExecutionMode HintsReplayExecutionMode
        {
            get
            {
                if (m_hintsReplayExecutionMode == null && m_executionModes.Count > 0)
                {
                    m_hintsReplayExecutionMode = m_executionModes.FirstOrDefault(m => m.CanReplayHints) ?? m_executionModes[0];
                }
                return m_hintsReplayExecutionMode;
            }
            set
            {
                if (m_hintsReplayExecutionMode != value && value && m_executionModes.Contains(value) && value.CanReplayHints)
                {
                    m_hintsReplayExecutionMode = value;
                }
            }
        }

        public List<ExecutionMode> ExecutionModes => m_executionModes;


        private void OnEnable()
        {
            if(m_executionModes == null)
            {
                m_executionModes = new List<ExecutionMode>();
            }

        }
    }
}
