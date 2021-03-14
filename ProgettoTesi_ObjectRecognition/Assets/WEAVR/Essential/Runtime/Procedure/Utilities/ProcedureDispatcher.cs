using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Procedure
{

    [AddComponentMenu("WEAVR/Procedures/Procedures Dispatcher")]
    public class ProcedureDispatcher : MonoBehaviour
    {
        [Serializable]
        public class StringUnityEvent : UnityEvent<string> { }
        [Serializable]
        public class BoolUnityEvent : UnityEvent<bool> { }

        [Draggable]
        public ProcedureRunner procedureRunner;

        [Header("Procedure")]
        public UnityEventString onProcedureStarted;
        public UnityEventString onProcedureEnded;
        public UnityEvent onRestart;

        [Header("Step Info Dispatch")]
        public StringUnityEvent onStepNumberChanged;
        public StringUnityEvent onStepTitleChanged;
        public StringUnityEvent onStepDescriptionChanged;

        [Header("Navigation Dispatch")]
        public BoolUnityEvent onPrevStepAvailabilityChanged;
        public BoolUnityEvent onNextStepAvailabilityChanged;

        // Start is called before the first frame update
        void Start()
        {
            if (!procedureRunner)
            {
                procedureRunner = this.GetSingleton<ProcedureRunner>();

                procedureRunner.ProcedureStarted -= ProcedureRunner_ProcedureStarted;
                procedureRunner.ProcedureStarted += ProcedureRunner_ProcedureStarted;

                procedureRunner.ProcedureFinished -= ProcedureRunner_ProcedureFinished;
                procedureRunner.ProcedureFinished += ProcedureRunner_ProcedureFinished;

                procedureRunner.StepStarted -= ProcedureRunner_StepStarted;
                procedureRunner.StepStarted += ProcedureRunner_StepStarted;

                procedureRunner.CanMoveNextStepChanged -= ProcedureRunner_CanMoveNextStepChanged;
                procedureRunner.CanMoveNextStepChanged += ProcedureRunner_CanMoveNextStepChanged;
            }
        }

        private void OnDisable()
        {
            procedureRunner.ProcedureStarted -= ProcedureRunner_ProcedureStarted;

            procedureRunner.ProcedureFinished -= ProcedureRunner_ProcedureFinished;

            procedureRunner.StepStarted -= ProcedureRunner_StepStarted;

            procedureRunner.CanMoveNextStepChanged -= ProcedureRunner_CanMoveNextStepChanged;
        }

        private void OnEnable()
        {
            if (procedureRunner)
            {
                procedureRunner.ProcedureStarted -= ProcedureRunner_ProcedureStarted;
                procedureRunner.ProcedureStarted += ProcedureRunner_ProcedureStarted;

                procedureRunner.ProcedureFinished -= ProcedureRunner_ProcedureFinished;
                procedureRunner.ProcedureFinished += ProcedureRunner_ProcedureFinished;

                procedureRunner.StepStarted -= ProcedureRunner_StepStarted;
                procedureRunner.StepStarted += ProcedureRunner_StepStarted;

                procedureRunner.CanMoveNextStepChanged -= ProcedureRunner_CanMoveNextStepChanged;
                procedureRunner.CanMoveNextStepChanged += ProcedureRunner_CanMoveNextStepChanged;
            }
        }

        private void ProcedureRunner_CanMoveNextStepChanged(object source, bool newValue)
        {
            onNextStepAvailabilityChanged.Invoke(newValue);
        }

        private void ProcedureRunner_ProcedureFinished(ProcedureRunner runner, Procedure procedure)
        {
            onProcedureEnded.Invoke(procedure.ProcedureName);
        }

        private void ProcedureRunner_ProcedureStarted(ProcedureRunner runner, Procedure procedure, ExecutionMode mode)
        {
            onProcedureStarted.Invoke(procedure.ProcedureName);
        }

        private void ProcedureRunner_StepStarted(IProcedureStep step)
        {
            onStepNumberChanged.Invoke(step?.Number);
            onStepTitleChanged.Invoke(step?.Title);
            onStepDescriptionChanged.Invoke(step?.Description);
        }

        public void NextStep()
        {
            procedureRunner?.MoveNextStep();
        }

        public void PreviousStep()
        {
            procedureRunner?.MovePreviousStep();
        }

        public void Restart()
        {
            onRestart.Invoke();
            this.GetSingleton<SceneLoader>().ReloadCurrentScene();
        }

        public void StartProcedure()
        {
            procedureRunner?.StartCurrentProcedure();
        }
    }
}
