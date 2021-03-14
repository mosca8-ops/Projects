namespace TXT.WEAVR.Common
{
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Core;
    using UnityEngine;

    [DoNotExpose]
    [AddComponentMenu("")]
    public class GeneratedObject : MonoBehaviour
    {
        [SerializeField]
        private Component m_generator;
        public Component Generator {
            get {
                return m_generator;
            }
            set {
                if(m_generator == null) {
                    m_generator = value;
                }
            }
        }

        #region [  EDITOR LOGIC  ]

        private HashSet<object> m_references;
        private HashSet<object> References
        {
            get
            {
                if (m_references == null)
                {
                    m_references = new HashSet<object>();
                }
                return m_references;
            }
        }

        public int Users => References.Count;

        public void RegisterUser(object user)
        {
            References.Add(user);
        }

        public void UnregisterUser(object user)
        {
            References.Remove(user);
        }

        #endregion
    }
}