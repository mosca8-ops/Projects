using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Debugging
{
    public abstract class BaseModuleDebug : MonoBehaviour, IDebugBehaviour
    {
        [Range(1, 20)]
        [Tooltip("Update info each x Frames")]
        public int updateRate = 3;
        
        [SerializeField]
        [HideInInspector]
        private bool m_initialized;

        private void Reset()
        {
            m_initialized = false;
        }
        
        protected static void UpdateText(Text textComponent, string text)
        {
            if (textComponent != null)
            {
                textComponent.text = text;
            }
        }

        public abstract void UpdateInfo();
    }


    public abstract class BaseModuleDebug<T> : BaseModuleDebug where T : MonoBehaviour
    {
        public T module;
        [SerializeField]
        //[HideInInspector]
        [Draggable]
        private Text m_moduleTitle;
        [Space]
        [SerializeField]
        [HideInInspector]
        // Start from here to search for texts
        private bool m_dummy;

        protected virtual void OnValidate()
        {
            if(m_moduleTitle == null)
            {
                m_moduleTitle = GetComponentInChildren<Text>();
            }
            if(m_moduleTitle != null && module != null)
            {
                m_moduleTitle.text = module.GetType().Name;
            }
        }

        protected virtual void Start()
        {
            if (module == null)
            {
                module = InitializeManager();
            }
            if (m_moduleTitle != null && module != null)
            {
                m_moduleTitle.text = module.GetType().Name;
            }
            else if(m_moduleTitle != null)
            {
                m_moduleTitle.text = "No module set";
            }
        }

        protected abstract T InitializeManager();

        protected void Update()
        {
            if (module != null && Time.frameCount % updateRate == 0)
            {
                UpdateInfo();
            }
        }

    }
}
