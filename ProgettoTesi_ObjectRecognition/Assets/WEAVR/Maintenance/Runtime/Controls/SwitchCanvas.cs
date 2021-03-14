using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Maintenance
{
    [AddComponentMenu("WEAVR/Utilities/Switch Canvas")]
    public class SwitchCanvas : MonoBehaviour
    {
        [SerializeField]
        private string m_switchName;

        [Header("Components")]
        [SerializeField]
        [Button(nameof(InflateSelf), "Apply")]
        [Draggable]
        private AbstractSwitch m_switchObject;
        [Draggable]
        public Text switchName;
        [Draggable]
        public Transform content;
        [Draggable]
        public ScrollRect scrollRect;
        [Draggable]
        public SwitchStateCanvas stateSample;

        private void OnValidate()
        {
            if (scrollRect == null)
            {
                scrollRect = GetComponentInChildren<ScrollRect>();
            }
            if (content == null && scrollRect != null)
            {
                content = scrollRect.content;
            }

            if (m_switchObject != null && string.IsNullOrEmpty(m_switchName))
            {
                m_switchName = m_switchObject.name;
            }
            SwitchName = m_switchName;
        }

        public string SwitchName {
            get { return switchName?.text; }
            set {
                if (switchName != null && switchName.text != value)
                {
                    switchName.text = value;
                }
            }
        }

        public AbstractSwitch Switch {
            get { return m_switchObject; }
            set {
                if (m_switchObject != value)
                {
                    m_switchObject = value;
                    Inflate(value);
                }
            }
        }

        [SerializeField]
        [HideInInspector]
        private List<SwitchStateCanvas> m_stateCanvases = new List<SwitchStateCanvas>();

        public float Value {
            get { return scrollRect.horizontalNormalizedPosition; }
            set { scrollRect.horizontalNormalizedPosition = value; }
        }

        // Use this for initialization
        void Start()
        {

        }

        private void InflateSelf()
        {
            Inflate(Switch);
        }

        public void Inflate(AbstractSwitch sw)
        {
            if (sw == null) { return; }
            SwitchName = string.IsNullOrEmpty(m_switchName) ? sw.name : m_switchName;
            ClearContent();
            var states = sw.States;

            for (int i = 0; i < states.Count; i++)
            {
                var newStateCanvas = Instantiate(stateSample.gameObject).GetComponent<SwitchStateCanvas>();
                newStateCanvas.gameObject.SetActive(true);
                newStateCanvas.SetState(states[i]);
                newStateCanvas.transform.SetParent(content, false);
                m_stateCanvases.Add(newStateCanvas);
            }
        }

        private void ClearContent()
        {
            for (int i = 0; i < m_stateCanvases.Count;)
            {
                if (m_stateCanvases[i] != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(m_stateCanvases[i].gameObject);
                    }
                    else
                    {
                        DestroyImmediate(m_stateCanvases[i].gameObject);
                    }
                }
                m_stateCanvases.RemoveAt(i);
            }
            m_stateCanvases.Clear();
        }
    }
}