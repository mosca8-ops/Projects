using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using TXT.WEAVR.Procedure;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    [AddComponentMenu("WEAVR/Procedures/Procedure Execution Time")]
    public class ProcedureExecutionTime : MonoBehaviour
    {
        [Draggable]
        private ProcedureRunner m_runner;

        public UnityEventFloat OnTick;
        public UnityEventFloat OnBestChanged;

        private float m_currentTime;
        private float m_currentBest;
        private bool m_started;

        public bool Started {
            get { return m_started; }
            set {
                if (m_started != value)
                {
                    m_started = value;
                    if (m_started)
                    {
                        m_currentTime = 0;
                        ExecutionModeChanged(m_runner.CurrentProcedure?.DefaultExecutionMode);
                    }
                }
            }
        }

        protected float CurrentBest {
            get { return m_currentBest; }
            set {
                if (m_currentBest != value)
                {
                    m_currentBest = value;
                    OnBestChanged.Invoke(m_currentBest);
                }
            }
        }

        public void StartTime()
        {
            m_started = false;
            Started = true;
        }

        public void StopTime()
        {
            Started = false;
        }

        public void ResetCurrentBest()
        {
            PlayerPrefs.SetFloat(m_runner.CurrentProcedure?.DefaultExecutionMode.ModeName, 0);
            PlayerPrefs.Save();
            CurrentBest = 0;
        }

        public void SaveNewBest()
        {
            if (Mathf.Approximately(m_currentBest, 0) || m_currentTime < m_currentBest)
            {
                PlayerPrefs.SetFloat(m_runner.CurrentProcedure?.DefaultExecutionMode.ModeName, m_currentTime);
                PlayerPrefs.Save();
                CurrentBest = m_currentTime;
            }
        }

        // Use this for initialization
        void Start()
        {
            m_runner = ProcedureRunner.Current;
            m_runner.CurrentProcedure.Configuration.ExecutionModeChanged -= ExecutionModeChanged;
            m_runner.CurrentProcedure.Configuration.ExecutionModeChanged += ExecutionModeChanged;

            ExecutionModeChanged(m_runner.CurrentProcedure?.DefaultExecutionMode);
        }

        private void OnEnable()
        {
            if (m_runner == null)
            {
                m_runner = ProcedureRunner.Current;
            }
            m_runner.CurrentProcedure.Configuration.ExecutionModeChanged -= ExecutionModeChanged;
            m_runner.CurrentProcedure.Configuration.ExecutionModeChanged += ExecutionModeChanged;
        }

        private void OnDisable()
        {
            if (m_runner != null)
            {
                m_runner.CurrentProcedure.Configuration.ExecutionModeChanged -= ExecutionModeChanged;
            }
        }

        private void OnDestroy()
        {
            if (m_runner != null)
            {
                m_runner.CurrentProcedure.Configuration.ExecutionModeChanged -= ExecutionModeChanged;
            }
        }

        private void ExecutionModeChanged(ExecutionMode mode)
        {
            CurrentBest = PlayerPrefs.GetFloat(mode.ModeName, 0);
        }

        // Update is called once per frame
        void Update()
        {
            if (m_started)
            {
                m_currentTime += Time.deltaTime;
                OnTick.Invoke(m_currentTime);
            }
        }
    }
}