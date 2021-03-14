using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Animation
{
    [System.Serializable]
    [DisplayName("Relative Move")]
    public class DeltaLinearAnimation : BaseAnimation
    {
        [SerializeField]
        protected Vector3 m_position;
        [SerializeField]
        protected Vector3 m_rotation;
        [SerializeField]
        protected float m_speed;

        private float m_angularSpeed;
        private bool m_positionEnabled;
        private bool m_rotationEnabled;

        private float m_movementLeft;
        private float m_rotationLeft;

        private float m_movementQuant;
        private float m_rotationQuant;

        private Vector3 m_deltaMove;
        private Vector3 m_deltaRotate;

        private Quaternion m_dRotate;

        protected Transform m_transform;

        protected Rigidbody m_rigidBody;
        protected bool m_rigidBodyWasKinematic;

        public virtual Transform Transform { get { return m_transform; } protected set { m_transform = value; } }

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
            get { return m_speed; }
            set { m_speed = value > 0 ? value : 0; }
        }

        private void UpdateDelta()
        {
            m_positionEnabled = m_position != Vector3.zero;
            m_rotationEnabled = m_rotation != Vector3.zero;

            if (m_positionEnabled)
            {
                var localPosition = Matrix4x4.TRS(Transform.position, Transform.rotation, Transform.lossyScale) * m_position;
                m_deltaMove = localPosition * m_speed;
                m_movementLeft = localPosition.magnitude;
                m_movementQuant = m_deltaMove.magnitude * 0.01f;
            }
            else
            {
                m_movementLeft = 0;
            }

            if (m_rotationEnabled)
            {
                m_angularSpeed = m_speed;
                m_deltaRotate = m_rotation * m_angularSpeed;
                m_rotationLeft = m_rotation.magnitude;
                m_rotationQuant = m_deltaRotate.magnitude * 0.01f;
            }
            else
            {
                m_rotationLeft = 0;
            }
        }

        public virtual void SetDelta(Vector3 position, Vector3 rotation)
        {
            m_position = position;
            m_rotation = rotation;
            if (m_state == AnimationState.Playing || m_state == AnimationState.Paused)
            {
                UpdateDelta();
            }
        }

        protected override object[] SerializeDataInternal()
        {
            return new object[] { m_position, m_rotation, m_speed };
        }

        protected override bool DeserializeDataInternal(object[] data)
        {
            if (data == null || data.Length < 3)
            {
                return false;
            }
            m_position = PropertyConvert.ToVector3(data[0]);
            m_rotation = PropertyConvert.ToVector3(data[1]);
            return PropertyConvert.TryParse(data[2]?.ToString(), out m_speed);
        }

        public override void OnStart()
        {
            base.OnStart();
            UpdateDelta();
            if (LinearSpeed == 0 && m_angularSpeed == 0)
            {
                CurrentState = AnimationState.Finished;
            }
        }

        public override void Animate(float dt)
        {
            if (m_rigidBody != null)
            {
                m_rigidBody.isKinematic = true;
                if (m_rotationEnabled) { PhysicsRotation(dt); }
                if (m_positionEnabled) { PhysicsMovement(dt); }
            }
            else
            {
                if (m_rotationEnabled) { SimpleRotation(dt); }
                if (m_positionEnabled) { SimpleMovement(dt); }
            }

            if (m_movementLeft <= m_movementQuant && m_rotationLeft <= m_rotationQuant)
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
            }
        }

        #region [  MOVEMENT LOGIC  ]

        private void SimpleRotation(float dt)
        {
            if (m_angularSpeed == 0)
            {
                m_rotationLeft = 0;
            }
            else
            {
                var delta = m_deltaRotate * dt;
                m_rotationLeft -= delta.magnitude;
                Transform.Rotate(delta);
            }
        }

        private void SimpleMovement(float dt)
        {
            if (m_speed == 0)
            {
                m_movementLeft = 0;
            }
            else
            {
                var delta = m_deltaMove * dt;
                m_movementLeft -= delta.magnitude;
                Transform.Translate(delta, Space.World);
            }
        }

        private void PhysicsRotation(float dt)
        {
            if (m_angularSpeed == 0)
            {
                m_rotationLeft = 0;
            }
            else
            {
                var delta = m_deltaRotate * dt;
                m_rotationLeft -= delta.magnitude;
                m_rigidBody.MoveRotation(Transform.localRotation * Quaternion.Euler(delta));
            }
        }

        private void PhysicsMovement(float dt)
        {
            if (m_speed == 0)
            {
                m_movementLeft = 0;
            }
            else
            {
                var delta = m_deltaMove * dt;
                m_movementLeft -= delta.magnitude;
                m_rigidBody.MovePosition(Transform.position + delta);
            }
        }

        #endregion
    }
}
