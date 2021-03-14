using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Localization;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [CreateAssetMenu(fileName = "ExecutionMode", menuName = "WEAVR/ExecutionMode")]
    [DefaultExecutionOrder(-28990)]
    public class ExecutionMode : ScriptableObject
    {
        [SerializeField]
        protected LocalizedString m_modeShortName;
        [SerializeField]
        protected LocalizedString m_modeName;
        [SerializeField]
        protected LocalizedTexture2D m_icon;

        // Additional Data
        [SerializeField]
        protected bool m_usesStepPrevNext;
        [SerializeField]
        protected bool m_usesWorldNavigation;
        [SerializeField]
        protected bool m_usesStepsPanel;
        [SerializeField]
        protected bool m_requiresNextToContinue;
        [SerializeField]
        protected bool m_canReplayHints;

        public string ModeShortName => m_modeShortName;
        public string ModeName => m_modeName;
        public Texture2D Icon => m_icon;

        // Additional Data
        public bool UsesStepPrevNext => m_usesStepPrevNext;
        public bool UsesWorldNavigation => m_usesWorldNavigation;
        public bool UsesStepsPanel => m_usesStepsPanel;
        public bool RequiresNextToContinue => m_requiresNextToContinue;
        public bool CanReplayHints => m_canReplayHints;

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(m_modeShortName) && !string.IsNullOrEmpty(name))
            {
                m_modeShortName = name.Substring(0, 1);
            }
            //if (string.IsNullOrEmpty(m_modeName))
            //{
            //    m_modeName = name;
            //}
        }
    }
}
