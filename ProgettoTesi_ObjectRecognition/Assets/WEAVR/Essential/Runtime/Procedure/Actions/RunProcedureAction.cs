using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Localization;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class RunProcedureAction : BaseAction
    {
        public delegate void BeforeLoadingProcedureDelegate(bool willLoadScene);
        public static BeforeLoadingProcedureDelegate BeforeLoadingProcedure;

        [SerializeField]
        [Draggable]
        private Procedure m_procedureToRun;
        [SerializeField]
        private ExecutionMode m_executionMode;
        [SerializeField]
        private bool m_resetScene;
        [SerializeField]
        [HideInInspector] // For future use
        private bool m_stopCurrentProcedure;

        [NonSerialized]
        private ProcedureRunner m_runner;
        [NonSerialized]
        private ExecutionMode m_modeToRun;

        public Procedure ProcedureToRun => m_procedureToRun;
        public ExecutionMode ExecutionMode => m_executionMode;

        public override void OnStart(ExecutionFlow flow, ExecutionMode executionMode)
        {
            base.OnStart(flow, executionMode);

            m_runner = flow.ExecutionEngine as ProcedureRunner;
            m_modeToRun = m_executionMode ? m_executionMode : executionMode;
        }

        public override bool Execute(float dt)
        {
            m_runner.StopCurrentProcedure();
            if (Procedure.ScenePath != m_procedureToRun.ScenePath || m_resetScene)
            {
                BeforeLoadingProcedure?.Invoke(true);
                ProcedureLauncher.LaunchProcedure(m_procedureToRun, m_modeToRun);
            }
            else
            {
                BeforeLoadingProcedure?.Invoke(false);
                m_runner.StartProcedure(m_procedureToRun, m_modeToRun);
            }
            return true;
        }

        public override string GetDescription()
        {
            return "Run: "
                + (m_procedureToRun ? m_procedureToRun.ProcedureName : "none")
                + (m_executionMode ? $" in {m_executionMode.ModeName} mode" : string.Empty);
        }
    }
}
