using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using static TXT.WEAVR.Pose;

namespace TXT.WEAVR.Procedure
{

    public class MoveToPointBlock : ComponentAnimation<Transform>
    {
        [SerializeField]
        [Tooltip("The destination to be reached by the target")]
        [Draggable]
        private Transform m_moveTo;
        [SerializeField]
        [Tooltip("Allow the object to rotate towards the destination rotation")]
        private bool m_withRotation = true;

        private Vector3 m_deltaMove;
        private Vector3 m_startLocalPosition;
        private Quaternion m_deltaRotation;
        private Quaternion m_startRotation;
        private float m_angle;

        public Transform Destination
        {
            get => m_moveTo;
            set
            {
                if(m_moveTo != value)
                {
                    BeginChange();
                    m_moveTo = value;
                    PropertyChanged(nameof(Destination));
                }
            }
        }

        public bool WithRotation
        {
            get => m_withRotation;
            set
            {
                if(m_withRotation != value)
                {
                    BeginChange();
                    m_withRotation = value;
                    PropertyChanged(nameof(WithRotation));
                }
            }
        }

        public override bool CanProvide<T>()
        {
            return false;
        }

        public override void OnStart()
        {
            base.OnStart();
            if (m_moveTo)
            {
                m_startLocalPosition = m_target.localPosition;
                m_deltaMove = m_moveTo.position - m_target.position;
                m_deltaRotation = Quaternion.FromToRotation(m_target.forward, m_moveTo.forward);
                m_startRotation = m_target.rotation;
                m_angle = Quaternion.Angle(m_target.rotation, m_moveTo.rotation);
            }
        }

        protected override void Animate(float delta, float normalizedValue)
        {
            if (m_moveTo)
            {
                //m_target.localPosition = m_startLocalPosition + m_target.InverseTransformPoint(m_moveTo.position) * normalizedValue;
                m_target.position += m_deltaMove * delta;
                if (m_withRotation)
                {
                    m_target.rotation = Quaternion.RotateTowards(m_startRotation, m_moveTo.rotation, m_angle * normalizedValue);
                }
            }
        }

        public override bool CanPreview()
        {
            return true;
        }
    }
}
