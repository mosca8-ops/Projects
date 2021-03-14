using System;
using TXT.WEAVR;
using TXT.WEAVR.Core;
using TXT.WEAVR.Procedure;
using UnityEngine;

namespace TXT.WEAVR.UI
{
    [AddComponentMenu("")]
    public class ExecutionModeMenu : MonoBehaviour
    {
        [Draggable]
        public ProcedureRunner runner;
        [Draggable]
        public ExecutionModeButton buttonSample;
        [Draggable]
        public Transform buttonsList;

        // Use this for initialization
        void Start()
        {
            if (buttonsList == null)
            {
                buttonsList = transform;
            }

            foreach (ExecutionMode mode in Enum.GetValues(typeof(ExecutionMode)))
            {
                if (!mode)
                {
                    continue;
                }
                var newButton = Instantiate<ExecutionModeButton>(buttonSample);
                if (newButton != null)
                {
                    newButton.gameObject.SetActive(true);
                    newButton.transform.SetParent(buttonsList, false);
                    newButton.Mode = mode;
                    newButton.button.onClick.AddListener(() => StartProcedure(mode));
                }
            }
        }

        private void StartProcedure(ExecutionMode mode)
        {
            runner.CurrentProcedure.DefaultExecutionMode = mode;
            runner.StartCurrentProcedure();
        }
    }
}