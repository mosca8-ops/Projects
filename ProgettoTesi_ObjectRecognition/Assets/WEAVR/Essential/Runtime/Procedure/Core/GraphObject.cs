using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Localization;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class GraphObject : ProcedureObject
    {
        [SerializeField]
        [AutoFill]
        protected LocalizedString m_title;
        [SerializeField]
        private Vector2 m_uiPosition;
        [SerializeField]
        private bool m_uiCollapsed;
        [SerializeField]
        private bool m_uiSuperCollapsed;

        public virtual string Title
        {
            get => m_title;
            set
            {
                if(m_title != value)
                {
                    m_title = value;
                    PropertyChanged(nameof(Title));
                }
            }
        }

        public Vector2 UI_Position
        {
            get { return m_uiPosition; }
            set
            {
                if(m_uiPosition != value)
                {
                    m_uiPosition = value;
                    PropertyChanged(nameof(UI_Position));
                }
            }
        }

        public bool UI_Expanded
        {
            get { return m_uiCollapsed; }
            set
            {
                if(m_uiCollapsed != value)
                {
                    m_uiCollapsed = value;
                    PropertyChanged(nameof(UI_Expanded));
                }
            }
        }

        public bool UI_SuperCollapsed
        {
            get { return m_uiSuperCollapsed; }
            set
            {
                if(m_uiSuperCollapsed != value)
                {
                    m_uiSuperCollapsed = value;
                    PropertyChanged(nameof(UI_SuperCollapsed));
                }
            }
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Title) ? name : Title;
        }
    }
}