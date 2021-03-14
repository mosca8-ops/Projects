using TXT.WEAVR.Core;
using TXT.WEAVR.Procedure;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Common
{

    [System.Serializable]
    public class BooleanValueChangedEvent : UnityEvent<bool> { }

    [AddComponentMenu("WEAVR/Setup/Transparency Controller")]
    public class TransparencyController : MonoBehaviour
    {

        #region Static Part
        private static TransparencyController s_instance;

        public static TransparencyController Instance {
            get {
                if (s_instance == null)
                {
                    s_instance = FindObjectOfType<TransparencyController>();
                    if (s_instance == null)
                    {
                        // If no object is active, then create a new one
                        GameObject go = new GameObject("TransparencyController");
                        s_instance = go.AddComponent<TransparencyController>();
                    }
                    s_instance.Start();
                }
                return s_instance;
            }
        }
        #endregion

        [SerializeField]
        private bool _transparencyEnabled = false;
        public bool TransparencyEnabled {
            get {
                return _transparencyEnabled;
            }
            set {
                if (_transparencyEnabled != value)
                {
                    _transparencyEnabled = value;
                    transparencySwitched.Invoke(value);
                    if (TransparencySwitched != null)
                    {
                        TransparencySwitched(value);
                    }
                }
            }
        }

        public BooleanValueChangedEvent transparencySwitched;
        public event System.Action<bool> TransparencySwitched;

        public void Switch()
        {
            TransparencyEnabled = !TransparencyEnabled;
        }

        public void SwitchTo(bool value)
        {
            TransparencyEnabled = value;
        }

        private void Start()
        {
            ProcedureRunner.Current.StepStarted += ProcedureRunner_StepChanged;
        }

        private void ProcedureRunner_StepChanged(IProcedureStep step)
        {
            if (step != null)
            {
                transparencySwitched.Invoke(_transparencyEnabled);
                if (TransparencySwitched != null)
                {
                    TransparencySwitched(_transparencyEnabled);
                }
            }
        }
    }
}
