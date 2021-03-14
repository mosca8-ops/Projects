using TXT.WEAVR;
using TXT.WEAVR.Procedure;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.UI
{
    [RequireComponent(typeof(Button))]
    [AddComponentMenu("")]
    public class ExecutionModeButton : MonoBehaviour
    {

        [Header("Components")]
        [Draggable]
        public Text bigLabel;
        [Draggable]
        public Text smallLabel;
        [Draggable]
        public Button button;

        [SerializeField]
        [Draggable]
        private ExecutionMode _executionMode;


        private void Start()
        {
            button = GetComponent<Button>();
        }

        public ExecutionMode Mode {
            get {
                return _executionMode;
            }
            set {
                if (value)
                {
                    _executionMode = value;
                    if (smallLabel != null)
                    {
                        smallLabel.text = value.ToString();
                    }
                    if (bigLabel != null)
                    {
                        bigLabel.text = value.ModeShortName;
                    }
                }
            }
        }
    }
}