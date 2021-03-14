using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TXT.WEAVR.Common
{

    public abstract class AbstractPanelDoor : AbstractDoor
    {
        [Header("Panel Data")]
        [SerializeField]
        [Tooltip("If the door is closer than this threshold to the closing point, then it will automatically close")]
        protected float m_closeThreshold = 0.1f;
        [SerializeField]
        [Draggable]
        protected AnimationClip m_animation;

        [SerializeField]
        [HideInInspector]
        protected float m_localDistance;
        [SerializeField]
        [HideInInspector]
        protected float m_localAngle;
        [SerializeField]
        [HideInInspector]
        protected float m_actualCloseThreshold;

        protected override void OnValidate()
        {
            base.OnValidate();
            ComputeDistances();
        }

        protected override void UpdateState()
        {
            bool wasClosed = IsClosed;
            IsClosed = Vector3.Distance(transform.localPosition, m_closedLocalPosition) < m_actualCloseThreshold;
            if (!wasClosed && IsClosed)
            {
                transform.localPosition = m_closedLocalPosition;
                transform.localRotation = m_closedLocalRotation;
            }
        }

        protected override void AnimateDoorMovement(float progress)
        {
            if(m_animation != null)
            {
                m_animation.SampleAnimation(gameObject, m_animation.length * progress);
            }
            else if(m_rigidBody != null)
            {
                bool wasKinematic = m_rigidBody.isKinematic;
                m_rigidBody.isKinematic = true;
                transform.localPosition = Vector3.MoveTowards(m_closedLocalPosition, m_openedLocalPosition, m_localDistance * progress);
                transform.localRotation = Quaternion.RotateTowards(m_closedLocalRotation, m_openedLocalRotation, m_localAngle * progress);
                m_rigidBody.isKinematic = wasKinematic;
            }
            else
            {
                transform.localPosition = Vector3.MoveTowards(m_closedLocalPosition, m_openedLocalPosition, m_localDistance * progress);
                transform.localRotation = Quaternion.RotateTowards(m_closedLocalRotation, m_openedLocalRotation, m_localAngle * progress);
            }
        }

        public override void SnapshotClosed()
        {
            base.SnapshotClosed();
            ComputeDistances();
        }

        public override void SnapshotFullyOpen()
        {
            base.SnapshotFullyOpen();
            ComputeDistances();
        }

        private void ComputeDistances()
        {
            m_localDistance = Vector3.Distance(m_closedLocalPosition, m_openedLocalPosition);
            m_localAngle = Quaternion.Angle(m_closedLocalRotation, m_openedLocalRotation);
            m_actualCloseThreshold = m_closeThreshold / transform.lossyScale.magnitude;
        }

        protected override void Start()
        {
            base.Start();
            ComputeDistances();
        }
    }
}
