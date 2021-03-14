using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Procedure;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Setup/Player Rig")]
    public class WeavrPlayerRig : MonoBehaviour, IWeavrSingleton
    {
        public bool AutoRestoreActivations = true;
        [SerializeField]
        [Draggable]
        private GameObject[] m_objectsVT;
        [SerializeField]
        [Draggable]
        private GameObject[] m_objectsOPS;

        private ProcedureRunner m_runner;

        private Dictionary<GameObject, bool> m_defaultActivations;

        private void Start()
        {
            if(m_defaultActivations == null)
            {
                m_defaultActivations = new Dictionary<GameObject, bool>();
                foreach (var go in m_objectsVT)
                {
                    m_defaultActivations[go] = go.activeSelf;
                }
                foreach (var go in m_objectsOPS)
                {
                    m_defaultActivations[go] = go.activeSelf;
                }
            }
            if (!m_runner)
            {
                m_runner = this.TryGetSingleton<ProcedureRunner>();
            }
            if (m_runner)
            {
                m_runner.ProcedureStarted -= Runner_ProcedureStarted;
                m_runner.ProcedureStarted += Runner_ProcedureStarted;

                m_runner.ProcedureFinished -= Runner_ProcedureFinished;
                m_runner.ProcedureFinished += Runner_ProcedureFinished;

                m_runner.ProcedureStopped -= Runner_ProcedureStopped;
                m_runner.ProcedureStopped += Runner_ProcedureStopped;

                if (m_runner.RunningProcedure)
                {
                    Runner_ProcedureStarted(m_runner, m_runner.RunningProcedure, m_runner.ExecutionMode);
                }
            }
        }

        private void Runner_ProcedureStopped(ProcedureRunner runner, Procedure.Procedure procedure)
        {
            if (AutoRestoreActivations)
            {
                RestoreDefaultActivations();
            }
        }

        private void Runner_ProcedureFinished(ProcedureRunner runner, Procedure.Procedure procedure)
        {
            if (AutoRestoreActivations)
            {
                RestoreDefaultActivations();
            }
        }

        private void Runner_ProcedureStarted(ProcedureRunner runner, Procedure.Procedure procedure, ExecutionMode mode)
        {
            bool useVTobjects = mode.UsesWorldNavigation;
            foreach (var go in m_objectsVT)
            {
                go.SetActive(useVTobjects);
            }
            foreach (var go in m_objectsOPS)
            {
                go.SetActive(!useVTobjects);
            }
        }

        private void RestoreDefaultActivations()
        {
            foreach (var pair in m_defaultActivations)
            {
                pair.Key.SetActive(pair.Value);
            }
        }

        private void OnDisable()
        {
            if (m_runner)
            {
                m_runner.ProcedureStarted -= Runner_ProcedureStarted;
                m_runner.ProcedureFinished -= Runner_ProcedureFinished;
                m_runner.ProcedureStopped -= Runner_ProcedureStopped;
            }
            Weavr.UnregisterSingleton(this);
        }
    }
}
