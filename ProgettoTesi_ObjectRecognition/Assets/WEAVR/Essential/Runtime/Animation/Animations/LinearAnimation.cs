using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Animation
{
    [System.Serializable]
    public class LinearAnimation : BaseAnimation
    {
        protected const float k_fullSpeed = 10000000f;
        protected const float k_sqrConnectEpsilon = 0.0000001f;
        protected const float k_connectRotationEpsilon = 0.03f * Mathf.Rad2Deg;

        protected Transform m_transform;
        protected Transform m_offset;
        [SerializeField]
        protected Transform m_destination;

        [SerializeField]
        protected bool m_rotationEnabled = false;

        [SerializeField]
        protected float m_linearSpeed;
        [SerializeField]
        [Tooltip("Whether to use angular velocity or convert the linear one")]
        [DisabledBy("m_rotationEnabled")]
        protected bool m_useAngularSpeed;
        [SerializeField]
        [DisabledBy("m_rotationEnabled;m_useAngularSpeed")]
        protected float m_angularSpeed;

        protected Rigidbody m_rigidBody;
        protected Transform m_currentDestination;
        protected bool m_destinationIsTemporary;
        protected bool m_rigidBodyWasKinematic;
        protected bool m_instantMove = false;

        public virtual Transform Transform { get { return m_transform; } protected set { m_transform = value; } }
        public virtual Transform Destination { get { return m_destination; } protected set { m_destination = value; } }
        public virtual Transform Offset { get { return m_offset; } protected set { m_offset = value; } }

        public override GameObject GameObject {
            get { return m_gameObject; }
            set {
                if (value != null)
                {
                    m_gameObject = value;
                    Transform = m_gameObject.transform;
                    m_rigidBody = m_gameObject.GetComponent<Rigidbody>();
                }
            }
        }

        public virtual float LinearSpeed {
            get { return m_linearSpeed; }
            set { m_linearSpeed = value > 0 ? value : 0; }
        }

        public virtual float AngularSpeed {
            get { return m_angularSpeed; }
            set { UseAngularSpeed = true; m_angularSpeed = value > 0 ? value : 0; }
        }

        public virtual bool UseAngularSpeed {
            get { return m_useAngularSpeed; }
            set { m_useAngularSpeed = value; }
        }

        public virtual bool IsRotationEnabled {
            get { return m_rotationEnabled; }
            set { m_rotationEnabled = value; }
        }

        public virtual void SetDestination(Transform destination, Transform offset)
        {
            Offset = offset;
            Destination = destination;
            m_destinationIsTemporary = offset != null;
            if (m_state == AnimationState.Playing || m_state == AnimationState.Paused)
            {
                UpdateDestination();
            }
        }

        private void UpdateDestination()
        {
            if (m_rigidBody != null)
            {
                m_rigidBodyWasKinematic = m_rigidBody.isKinematic;
            }
            if (m_destinationIsTemporary)
            {
                s_instantTargetTransform.SetPositionAndRotation(Transform.position, Transform.rotation);
                s_instantTargetTransform.SetParent(m_offset, true);

                var lastPosition = m_offset.position;
                var lastRotation = m_offset.rotation;

                m_offset.SetPositionAndRotation(m_destination.position, m_destination.rotation);

                m_currentDestination = CreateTemporaryTransform(m_destination, false, s_instantTargetTransform.position, s_instantTargetTransform.rotation);

                s_instantTargetTransform.SetParent(null);
                m_offset.position = lastPosition;
                m_offset.rotation = lastRotation;
            }
            else
            {
                m_currentDestination = Destination;
            }
            if (IsRotationEnabled && !UseAngularSpeed)
            {
                AngularSpeed = ConvertLinearToRadial(LinearSpeed, m_transform, m_currentDestination);
            }
        }

        public virtual void SetDestination(Vector3 destination)
        {
            m_destinationIsTemporary = true;
            Offset = null;
            IsRotationEnabled = false;
            Destination = CreateTemporaryTransform(null, false, destination, Quaternion.identity);
            if (m_state == AnimationState.Playing || m_state == AnimationState.Paused)
            {
                UpdateDestination();
            }
        }

        protected override object[] SerializeDataInternal()
        {
            return new object[] { m_destination, m_rotationEnabled, m_linearSpeed, m_useAngularSpeed, m_angularSpeed };
        }

        protected override bool DeserializeDataInternal(object[] data)
        {
            if (data[0] is Transform)
            {
                m_destination = data[0] as Transform;
                if (bool.TryParse(data[1].ToString(), out m_rotationEnabled) && PropertyConvert.TryParse(data[2]?.ToString(), out m_linearSpeed))
                {
                    bool.TryParse(data[3].ToString(), out m_useAngularSpeed);
                    PropertyConvert.TryParse(data[4]?.ToString(), out m_angularSpeed);
                    SetDestination(m_destination, null);
                    return true;
                }
            }
            return false;
        }

        public override void OnStart()
        {
            base.OnStart();
            UpdateDestination();
            if (LinearSpeed == 0 && AngularSpeed == 0)
            {
                if (m_rigidBody != null)
                {
                    if (m_rotationEnabled) { m_rigidBody.MoveRotation(m_currentDestination.rotation); }
                    m_rigidBody.MovePosition(m_currentDestination.position);
                }
                else
                {
                    if (m_rotationEnabled) { Transform.rotation = m_currentDestination.rotation; }
                    Transform.position = m_currentDestination.position;
                }
                CurrentState = AnimationState.Finished;
            }
        }

        public override void Animate(float dt)
        {
            if (m_rigidBody != null)
            {
                m_rigidBody.isKinematic = true;
                if (m_rotationEnabled) { PhysicsRotation(dt); }
                PhysicsMovement(dt);
            }
            else
            {
                if (m_rotationEnabled) { SimpleRotation(dt); }
                SimpleMovement(dt);
            }

            if ((Transform.position - m_currentDestination.position).sqrMagnitude < k_sqrConnectEpsilon
                && (!m_rotationEnabled || Quaternion.Angle(Transform.rotation, m_currentDestination.rotation) < k_connectRotationEpsilon))
            {
                // Destination Reached !!!
                CurrentState = AnimationState.Finished;
            }
        }

        protected override void StateChanged()
        {
            base.StateChanged();
            if (m_state == AnimationState.Stopped || m_state == AnimationState.Finished)
            {
                if (m_rigidBody != null)
                {
                    m_rigidBody.velocity = Vector3.zero;
                    m_rigidBody.isKinematic = m_rigidBodyWasKinematic;
                }
                if (m_destinationIsTemporary) { Destroy(m_currentDestination.gameObject); }
            }
        }

        private static float ConvertLinearToRadial(float linearVelocity, Transform source, Transform destination)
        {
            if (linearVelocity == 0) { return 0; }

            float dt = (destination.position - source.position).magnitude / linearVelocity;
            float angle = Quaternion.Angle(source.rotation, destination.rotation);

            return angle / dt;
        }

        #region [  MOVEMENT LOGIC  ]

        private void SimpleRotation(float dt)
        {
            if (m_angularSpeed == 0)
            {
                Transform.rotation = m_currentDestination.rotation;
            }
            else
            {
                Transform.rotation = Quaternion.RotateTowards(Transform.rotation, m_currentDestination.rotation, m_angularSpeed * dt);
            }
        }

        private void SimpleMovement(float dt)
        {
            if (m_linearSpeed == 0)
            {
                Transform.position = m_currentDestination.position;
            }
            else
            {
                Transform.position = Vector3.MoveTowards(Transform.position, m_currentDestination.position, dt * m_linearSpeed);
            }
        }

        private void PhysicsRotation(float dt)
        {
            if (m_angularSpeed == 0)
            {
                m_rigidBody.MoveRotation(m_currentDestination.rotation);
            }
            else
            {
                m_rigidBody.MoveRotation(Quaternion.RotateTowards(Transform.rotation, m_currentDestination.rotation, m_angularSpeed * dt));
            }
        }

        private void PhysicsMovement(float dt)
        {
            if (m_linearSpeed == 0)
            {
                m_rigidBody.MovePosition(m_currentDestination.position);
            }
            else
            {
                m_rigidBody.MovePosition(Vector3.MoveTowards(Transform.position, m_currentDestination.position, dt * m_linearSpeed));
            }
        }

        #endregion
    }
}
