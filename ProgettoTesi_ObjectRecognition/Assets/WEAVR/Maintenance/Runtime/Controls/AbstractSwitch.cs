using System.Collections.Generic;
using TXT.WEAVR.Interaction;
using UnityEngine;

namespace TXT.WEAVR.Maintenance
{
    public abstract class AbstractSwitch : AbstractInteractiveBehaviour
    {
        [SerializeField]
        protected Vector3 m_defaultLocalPosition;
        [SerializeField]
        protected Vector3 m_defaultLocalEuler;

        public Vector3 DefaultLocalPosition => m_defaultLocalPosition;
        public Vector3 DefaultLocalEuler => m_defaultLocalEuler;

        public abstract IReadOnlyList<SwitchState> States { get; }

        protected override void Reset()
        {
            base.Reset();
            SaveDefaults();
        }

        public void SaveDefaults()
        {
            m_defaultLocalPosition = transform.localPosition;
            m_defaultLocalEuler = transform.localEulerAngles;
        }

        public void RestoreDefaults()
        {
            transform.localPosition = m_defaultLocalPosition;
            transform.localEulerAngles = m_defaultLocalEuler;
        }
    }
}
