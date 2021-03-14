using System;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Common
{

    public abstract class AbstractCap : AbstractDoor
    {

        [Header("Cap Data")]

        [SerializeField]
        [Range(0, 90)]
        [Tooltip("Closing rotation threshold in degrees")]
        protected float m_closedThreshold = 5;
        [SerializeField]
        [Tooltip("If the door is closer than this threshold to the closing point, then it will automatically close")]
        protected float m_installThreshold = 0.1f;

        [SerializeField]
        [Draggable]
        protected Transform m_rotationPoint;
        [SerializeField]
        //[Button(nameof(AdjustRotationAxis), "Adjust")]
        protected Vector3 m_rotationAxis;

        protected float m_closedOpenDistance;

        [Space]
        [SerializeField]
        [Draggable]
        protected Transform m_installTarget;
        [SerializeField]
        //[HideInInspector]
        protected Span m_limits = new Span(0, 120);
        [SerializeField]
        [ShowAsReadOnly]
        private float m_rotationAngle;

        [Space]
        [SerializeField]
        private Events m_capEvents;

        public UnityEvent OnInstalled => m_capEvents.onInstalled;
        public UnityEvent OnRemoved => m_capEvents.onRemoved;

        [Space]
        [SerializeField]
        [ShowAsReadOnly]
        protected bool m_isInstalled;

        public bool IsInstalled {
            get {
                return m_isInstalled;
            }
            set {
                if (m_isInstalled != value)
                {
                    bool wasInstalled = m_isInstalled;
                    m_isInstalled = value && (m_installTarget == null || Vector3.Distance(m_installTarget.position, transform.position) < m_installThreshold);
                    if (m_isInstalled && !wasInstalled)
                    {
                        OnInstalled.Invoke();
                        var rb = GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.isKinematic = true;
                        }
                        AnimateDoorMovement(1);
                        CurrentOpenProgress = 1;
                    }
                    else if (!m_isInstalled && wasInstalled)
                    {
                        OnRemoved.Invoke();
                    }
                }
            }
        }

        public override bool IsClosed {
            get {
                return base.IsClosed;
            }

            set {
                base.IsClosed = value;
                if (value)
                {
                    IsInstalled = true;
                }
            }
        }

        public override bool IsFullyOpened {
            get {
                return base.IsFullyOpened;
            }

            set {
                base.IsFullyOpened = value;
                if (!value)
                {
                    IsInstalled = true;
                }
            }
        }

        protected override void Reset()
        {
            base.Reset();
            m_rotationPoint = transform;
            Controller.DefaultBehaviour = this;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (m_rigidBody == null)
            {
                m_rigidBody = GetComponent<Rigidbody>();
            }
        }

        protected override void Start()
        {
            base.Start();
            m_closedOpenDistance = Vector3.Distance(m_closedLocalPosition, m_openedLocalPosition);
            m_isInstalled = m_installTarget == null || Vector3.Distance(m_installTarget.position, transform.position) < m_installThreshold;
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
        }

        protected override void AnimateDoorMovement(float progress)
        {
            if (IsInstalled)
            {
                transform.localPosition = Vector3.MoveTowards(m_closedLocalPosition, m_openedLocalPosition, progress * m_closedOpenDistance);
                transform.localRotation = Quaternion.RotateTowards(m_closedLocalRotation, m_openedLocalRotation, m_limits.Denormalize(progress));
            }
        }

        protected override void UpdateState()
        {
            if (IsInstalled)
            {
                float closedDistance = Quaternion.Angle(transform.localRotation, m_closedLocalRotation);
                IsClosed = IsClosed ? closedDistance < m_closedThreshold * 1.2f : closedDistance < m_closedThreshold;
                IsFullyOpened = Quaternion.Angle(transform.localRotation, m_openedLocalRotation) < m_closedThreshold;
            }
        }

        private void AdjustRotationAxis()
        {
            m_rotationAxis = m_rotationPoint.TransformDirection(m_rotationAxis);
        }

        public override void SnapshotClosed()
        {
            base.SnapshotClosed();
            m_limits.min = 0;
            //m_limits.max = Quaternion.Angle(m_closedLocalRotation, m_openedLocalRotation);
            Quaternion.RotateTowards(m_closedLocalRotation, m_openedLocalRotation, 180).ToAngleAxis(out m_limits.max, out m_rotationAxis);
            m_rotationAngle = m_limits.max;
        }

        public override void SnapshotFullyOpen()
        {
            base.SnapshotFullyOpen();
            m_limits.min = 0;
            //m_limits.max = Quaternion.Angle(m_closedLocalRotation, m_openedLocalRotation);
            Quaternion.RotateTowards(m_closedLocalRotation, m_openedLocalRotation, 180).ToAngleAxis(out m_limits.max, out m_rotationAxis);
            m_rotationAngle = m_limits.max;
        }

        [Serializable]
        private struct Events
        {
            public UnityEvent onInstalled;
            public UnityEvent onRemoved;
        }
    }
}
