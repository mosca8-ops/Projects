using UnityEngine;

namespace TXT.WEAVR.Common
{

    public abstract class AbstractHingeDoor : AbstractDoor
    {
        [Header("Hinge Door Data")]
        [SerializeField]
        [Tooltip("Whether to consider the position when moving or not")]
        protected bool m_advancedMovement = false;
        [SerializeField]
        [Range(0, 90)]
        [Tooltip("Closing rotation threshold in degrees")]
        protected float m_closedThreshold = 5;

        [SerializeField]
        [Draggable]
        protected Transform m_rotationPoint;
        [SerializeField]
        //[Button(nameof(AdjustRotationAxis), "Adjust")]
        protected Vector3 m_rotationAxis;
        [SerializeField]
        [HideInInspector]
        protected Span m_limits = new Span(0, 120);
        [SerializeField]
        [Draggable]
        protected HingeJoint m_physicsHinge;
        [SerializeField]
        [ShowAsReadOnly]
        private float m_rotationAngle;

        protected float m_timeSinceLastClose;

        private Vector3 m_moveVector;

        protected override void Reset()
        {
            base.Reset();
            m_rotationPoint = transform;
            Controller.DefaultBehaviour = this;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (!m_rigidBody)
            {
                m_rigidBody = GetComponent<Rigidbody>();
            }
            if (m_rigidBody && !m_physicsHinge)
            {
                m_physicsHinge = GetComponent<HingeJoint>();
                //if (m_physicsHinge == null)
                //{
                //    m_physicsHinge = gameObject.AddComponent<HingeJoint>();
                //}
                if (m_physicsHinge)
                {
                    UpdateHingeLimits();
                }
            }
            else if (m_physicsHinge != null)
            {
                DestroyImmediate(m_physicsHinge);
                m_physicsHinge = null;
            }
            DataUpdated();
        }

        protected override void Start()
        {
            base.Start();
            m_rotationAxis.Normalize();
            InteractTrigger = Interaction.BehaviourInteractionTrigger.OnPointerDown;

            if (m_blockOnClosed)
            {
                OnClosed.AddListener(Controller.StopCurrentInteraction);
            }
            if (m_blockOnFullyOpened)
            {
                OnFullyOpened.AddListener(Controller.StopCurrentInteraction);
            }
            m_moveVector = m_openedLocalPosition - m_closedLocalPosition;
            m_currentOpening = m_limits.Normalize(Quaternion.Angle(m_closedLocalRotation, transform.localRotation));
        }

        protected override void AnimateDoorMovement(float progress)
        {
            if (m_advancedMovement)
            {
                transform.localPosition = m_closedLocalPosition + m_moveVector * progress;
            }
            if (m_rotationPoint != transform)
            {
                // TODO: Implement the correct rotation with RotateAround...
                transform.localRotation = Quaternion.RotateTowards(m_closedLocalRotation, m_openedLocalRotation, m_limits.Denormalize(progress));
            }
            else
            {
                transform.localRotation = Quaternion.RotateTowards(m_closedLocalRotation, m_openedLocalRotation, m_limits.Denormalize(progress));
            }
        }

        protected override void UpdateState()
        {
            float closedDistance = Quaternion.Angle(transform.localRotation, m_closedLocalRotation);
            IsClosed = IsClosed ? closedDistance < m_closedThreshold * 1.2f : closedDistance < m_closedThreshold;
            IsFullyOpened = Quaternion.Angle(transform.localRotation, m_openedLocalRotation) < m_closedThreshold;
        }

        private void AdjustRotationAxis()
        {
            m_rotationAxis = m_rotationPoint.TransformDirection(m_rotationAxis);
        }

        private void DataUpdated()
        {
            if (m_physicsHinge != null)
            {
                m_physicsHinge.anchor = transform.InverseTransformPoint(m_rotationPoint.position);
                m_physicsHinge.axis = m_rotationAxis;
                UpdateHingeLimits();
            }
        }

        private void UpdateHingeLimits()
        {
            if (m_physicsHinge != null)
            {
                var hingeLimits = m_physicsHinge.limits;
                hingeLimits.min = m_limits.min;
                hingeLimits.max = m_limits.max;
                m_physicsHinge.limits = hingeLimits;
            }
        }

        public override void SnapshotClosed()
        {
            base.SnapshotClosed();
            m_limits.min = 0;
            //m_limits.max = Quaternion.Angle(m_closedLocalRotation, m_openedLocalRotation);
            if (!m_rotationPoint || m_rotationPoint == transform)
            {
                Quaternion.RotateTowards(m_closedLocalRotation, m_openedLocalRotation, 180).ToAngleAxis(out m_limits.max, out m_rotationAxis);
            }
            else
            {
                var toClose = transform.TransformPoint(m_closedLocalPosition) - m_rotationPoint.position;
                var toOpen = transform.TransformPoint(m_openedLocalPosition) - m_rotationPoint.position;
                Quaternion.FromToRotation(toClose, toOpen).ToAngleAxis(out m_limits.max, out m_rotationAxis);
            }
            //m_rotationAngle = m_limits.max;
            m_rotationAngle = Quaternion.Angle(m_closedLocalRotation, m_openedLocalRotation);
            m_limits.max = m_rotationAngle;
            
            UpdateHingeLimits();
        }

        public override void SnapshotFullyOpen()
        {
            base.SnapshotFullyOpen();
            m_limits.min = 0;
            //m_limits.max = Quaternion.Angle(m_closedLocalRotation, m_openedLocalRotation);
            if (!m_rotationPoint || m_rotationPoint == transform)
            {
                Quaternion.RotateTowards(m_closedLocalRotation, m_openedLocalRotation, 180).ToAngleAxis(out m_limits.max, out m_rotationAxis);
            }
            else
            {
                var toClose = transform.TransformPoint(m_closedLocalPosition) - m_rotationPoint.position;
                var toOpen = transform.TransformPoint(m_openedLocalPosition) - m_rotationPoint.position;
                Quaternion.FromToRotation(toClose, toOpen).ToAngleAxis(out m_limits.max, out m_rotationAxis);
            }
            //m_rotationAngle = m_limits.max;
            m_rotationAngle = Quaternion.Angle(m_closedLocalRotation, m_openedLocalRotation);
            m_limits.max = m_rotationAngle;
            UpdateHingeLimits();
        }
    }
}
