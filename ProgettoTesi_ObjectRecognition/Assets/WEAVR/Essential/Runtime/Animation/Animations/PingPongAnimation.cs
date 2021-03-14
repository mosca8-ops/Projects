using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Animation
{
    [System.Serializable]
    [DisplayName("Alternating Animation")]
    public class PingPongAnimation : BaseAnimation
    {
        [SerializeField]
        protected Vector3 m_position;
        [SerializeField]
        protected float m_totalTime;
        [SerializeField]
        protected float m_speed;

        private float m_movementLeft;
        private float m_movementQuant;
        private float m_lapMovement;

        private Vector3 m_deltaMove;

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

        public virtual float Speed {
            get { return m_speed; }
            set { m_speed = value > 0 ? value : 0; }
        }

        private void UpdateDelta()
        {
            m_deltaMove = m_position * m_speed;
            m_lapMovement = m_position.magnitude;
            m_movementLeft = m_lapMovement;
            m_movementQuant = m_deltaMove.magnitude * 0.01f;

            if (m_totalTime <= 0)
            {
                m_totalTime = float.MaxValue;
            }
        }

        public virtual void SetValues(Vector3 position, int repeatCount, float speed = 0)
        {
            m_position = position;
            if (speed != 0)
            {
                m_speed = speed;
            }
            if (m_speed != 0)
            {
                m_totalTime = repeatCount * (position.magnitude / m_speed);
            }
            if (m_state == AnimationState.Playing || m_state == AnimationState.Paused)
            {
                UpdateDelta();
            }
        }

        public virtual void SetValues(Vector3 position, float time, float speed = 0)
        {
            m_position = position;
            m_totalTime = time;
            if (speed != 0)
            {
                m_speed = speed;
            }
            if (m_state == AnimationState.Playing || m_state == AnimationState.Paused)
            {
                UpdateDelta();
            }
        }

        protected override object[] SerializeDataInternal()
        {
            return new object[] { m_position, m_totalTime, m_speed };
        }

        protected override bool DeserializeDataInternal(object[] data)
        {
            if (data == null || data.Length < 3)
            {
                return false;
            }
            m_position = PropertyConvert.ToVector3(data[0]);
            return PropertyConvert.TryParse(data[1].ToString(), out m_totalTime) && PropertyConvert.TryParse(data[2]?.ToString(), out m_speed);
        }

        public override void OnStart()
        {
            base.OnStart();
            UpdateDelta();
            if (Speed == 0)
            {
                CurrentState = AnimationState.Finished;
            }
        }

        public override void Animate(float dt)
        {
            if (m_rigidBody != null)
            {
                m_rigidBody.isKinematic = true;
                PhysicsMovement(dt);
            }
            else
            {
                SimpleMovement(dt);
            }

            m_totalTime -= dt;
            if (m_totalTime < 0.01f)
            {
                CurrentState = AnimationState.Finished;
            }
            else if (m_movementLeft < m_movementQuant)
            {
                m_movementLeft = m_lapMovement;
                m_deltaMove = -m_deltaMove;
            }
        }

        protected override void StateChanged()
        {
            base.StateChanged();
            if (m_state == AnimationState.Stopped || m_state == AnimationState.Finished)
            {
                if (m_rigidBody != null)
                {
                    if (m_rigidBody.velocity != Vector3.zero)
                    {
                        m_rigidBody.velocity -= m_deltaMove;
                    }
                    m_rigidBody.isKinematic = m_rigidBodyWasKinematic;
                }
            }
        }

        #region [  MOVEMENT LOGIC  ]

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
                Transform.Translate(delta, Space.Self);
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
                delta = Transform.TransformVector(delta);
                m_movementLeft -= delta.magnitude;
                m_rigidBody.MovePosition(Transform.position + delta);
            }
        }

        #endregion
    }
}
