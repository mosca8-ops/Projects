using System;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Maintenance
{
    [Serializable]
    public class SwitchState
    {
        [SerializeField]
        [Tooltip("When stable, the Down state is kept stable")]
        public bool isStable = false;
        [SerializeField]
        public bool isContinuous = false;
        [SerializeField]
        [Button(nameof(Snapshot), "Save")]
        public Vector3 deltaPosition;
        [SerializeField]
        public Vector3 deltaEuler;

        [SerializeField]
        public string displayName;
        [SerializeField]
        public Color displayColor = Color.green;

        private Vector3 m_localPosition;
        private Quaternion m_localRotation;

        public Vector3 LocalPosition => m_localPosition;
        public Quaternion LocalRotation => m_localRotation;

        private AbstractSwitch m_switch;
        public AbstractSwitch Switch {
            get { return m_switch; }
            set {
                if (m_switch != value)
                {
                    m_switch = value;
                }
            }
        }

        public SwitchState()
        {

        }

        public SwitchState(bool isStable, bool isContinuous)
        {
            this.isStable = isStable;
            this.isContinuous = isContinuous;
        }

        public virtual void Initialize()
        {
            m_localPosition = deltaPosition + m_switch.DefaultLocalPosition;
            m_localRotation = Quaternion.Euler(deltaEuler + m_switch.DefaultLocalEuler);

        }

        public virtual void Initialize(AbstractSwitch aswitch)
        {
            Switch = aswitch;
            Initialize();
        }

        private Vector3 AdjustAngles(Vector3 euler)
        {
            return new Vector3(AdjustAngle(euler.x), AdjustAngle(euler.y), AdjustAngle(euler.z));
        }

        private float AdjustAngle(float angle)
        {
            return angle > 180 ? 180 - angle : angle < -180 ? 180 + angle : angle;
        }

        public virtual void Snapshot()
        {
            if (m_switch != null)
            {
                deltaPosition = m_switch.transform.localPosition - m_switch.DefaultLocalPosition;
                deltaEuler = m_switch.transform.localEulerAngles - m_switch.DefaultLocalEuler;
            }
        }

        public virtual void Restore()
        {
            if (m_switch != null)
            {
                m_switch.transform.localPosition = m_switch.DefaultLocalPosition + deltaPosition;
                m_switch.transform.localEulerAngles = m_switch.DefaultLocalEuler + deltaEuler;
            }
        }

        public SwitchState Clone(AbstractSwitch abstractSwitch)
        {
            SwitchState state = this.MemberwiseClone() as SwitchState;
            m_switch = abstractSwitch;
            return state;
        }
    }
}
