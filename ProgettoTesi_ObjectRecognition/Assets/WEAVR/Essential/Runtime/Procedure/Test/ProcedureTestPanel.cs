using System.Linq;
using TXT.WEAVR.Common;
using TXT.WEAVR.InteractionUI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TXT.WEAVR.Procedure
{
    [AddComponentMenu("")]
    public class ProcedureTestPanel : MonoBehaviour, IWeavrSingleton
    {
        private static ProcedureTestPanel s_instance;
        public static ProcedureTestPanel SceneInstance {
            get {
                if (!s_instance)
                {
                    foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                    {
                        s_instance = root.GetComponentInChildren<ProcedureTestPanel>(true);
                        if (s_instance) { break; }
                    }
                }
                return s_instance;
            }
        }

        public static void DestroySceneInstance()
        {
            if (s_instance)
            {
                if (Application.isPlaying)
                {
                    Destroy(s_instance.gameObject);
                }
                else
                {
                    DestroyImmediate(s_instance.gameObject);
                }
                s_instance = null;
            }
        }

        public static void DisableSceneInstance()
        {
            if (s_instance)
            {
                s_instance.gameObject.SetActive(false);
            }
        }

        public GameObject controlsPanel;
        [Type(typeof(IInteractablePanel))]
        public Component gesturesPanel;

        [Header("Step Data")]
        public Text stepTitle;
        public Text stepNumber;
        public Text stepDescription;

        [Header("Buttons")]
        public GameObject buttonsPanel;
        public Button prevButton;
        public Button nextButton;

        [Header("Extra Buttons")]
        public GameObject extraButtonsPanel;
        public ProcedureTestButton buttonSample;

        private ProcedureRunner m_runner;

        private void Awake()
        {
            gameObject.tag = "EditorOnly";
            //gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
        }

        private void Start()
        {
            if (!m_runner)
            {
                OnEnable();
            }
        }

        private void OnEnable()
        {
            OnDisable();

            m_runner = this.TryGetSingleton<ProcedureRunner>();

            if (m_runner)
            {
                m_runner.ProcedureStarted += Runner_ProcedureStarted;
                m_runner.StepStarted += Runner_StepStarted;
                m_runner.RequiresNextToContinue += Runner_RequiresNextToContinue;
            }
        }

        private void Runner_RequiresNextToContinue(object source, bool newValue)
        {
            if (nextButton && newValue) { nextButton.interactable = true; }
        }

        private void Runner_StepStarted(IProcedureStep step)
        {
            if (stepTitle) { stepTitle.text = step.Title; }
            if (stepNumber) { stepNumber.text = step.Number; }
            if (stepDescription) { stepDescription.text = step.Description; }

            if (nextButton) { nextButton.interactable = m_runner.CanMoveNext || m_runner.MoveNextOverride != null; }
        }

        private void Runner_ProcedureStarted(ProcedureRunner runner, Procedure procedure, ExecutionMode mode)
        {
            if (!mode || !controlsPanel)
            {
                OnDisable();
                return;
            }

            if (gesturesPanel)
            {
                if (!mode.UsesWorldNavigation && gesturesPanel is IInteractablePanel panel)
                {
                    gesturesPanel.gameObject.SetActive(true);
                    CameraOrbit.Instance.GesturesPanel = panel;
                }
                else
                {
                    gesturesPanel.gameObject.SetActive(false);
                }
            }

            controlsPanel.SetActive(true);
            if (buttonsPanel)
            {
                buttonsPanel.SetActive(mode.UsesStepPrevNext);
                if (prevButton)
                {
                    prevButton.onClick.RemoveListener(m_runner.MovePreviousStep);
                    prevButton.onClick.AddListener(m_runner.MovePreviousStep);
                }
                if (nextButton)
                {
                    nextButton.onClick.RemoveListener(m_runner.MoveNextStep);
                    nextButton.onClick.AddListener(m_runner.MoveNextStep);
                }
            }
        }

        private void OnDisable()
        {
            if (m_runner)
            {
                m_runner.ProcedureStarted -= Runner_ProcedureStarted;
                m_runner.StepStarted -= Runner_StepStarted;
                m_runner.RequiresNextToContinue -= Runner_RequiresNextToContinue;

                if (controlsPanel)
                {
                    controlsPanel.SetActive(false);
                }
            }
        }
    }
}
