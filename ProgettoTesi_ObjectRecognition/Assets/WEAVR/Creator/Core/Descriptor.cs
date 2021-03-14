using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Localization;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public abstract class Descriptor : ScriptableObject
    {
        [SerializeField]
        protected string m_name;
        [SerializeField]
        protected string m_fullPath;
        [SerializeField]
        [HideInInspector]
        protected int m_depth;
        [SerializeField]
        protected Color m_color;
        [SerializeField]
        private Texture2D m_icon;

        public virtual Color Color
        {
            get => m_color;
            set
            {
                if (m_color != value)
                {
                    m_color = value;
                }
            }
        }


        public Texture2D Icon
        {
            get => m_icon;
            set
            {
                if (m_icon != value)
                {
                    m_icon = value;
                }
            }
        }

        public int Depth
        {
            get => m_depth;
            set
            {
                if(m_depth != value)
                {
                    m_depth = value;
                }
            }
        }

        public virtual string Name
        {
            get => m_name;
            set
            {
                if (m_name != value)
                {
                    if (m_name != null && m_fullPath != null && m_fullPath.Contains(m_name))
                    {
                        m_fullPath = m_fullPath.Remove(m_fullPath.LastIndexOf(m_name), m_name.Length);
                    }
                    m_name = value.Replace('/', ' ');
                    FullPath += m_name;
                }
            }
        }

        public virtual string FullPath
        {
            get => m_fullPath;
            set
            {
                if(m_fullPath != value)
                {
                    m_fullPath = value;
                }
            }
        }

        protected virtual void OnEnable()
        {
            if(m_name == null)
            {
                m_name = "descriptor";
            }
        }

        public virtual void Clear()
        {

        }
    }
}
