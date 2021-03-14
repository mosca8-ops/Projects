using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Animation
{
    [System.Serializable]
    [DisplayName("Spiral Animation")]
    public class SpiralAnimation : BaseAnimation
    {
        public enum RotationDirection { Clockwise, CounterClockwise };
        [SerializeField]
        protected RotationDirection m_rotationDirection;
        [SerializeField]
        protected Vector3 m_finalPoint;
        [SerializeField]
        protected float m_stepSize;
        [SerializeField]
        protected float m_speed;

        protected Vector3 m_rotation;

        private float m_movementLeft;
        private float m_movementEpsilon;

        private Vector3 m_deltaMove;
        private Vector3 m_deltaRotate;

        protected Transform m_transform;

        protected Rigidbody m_rigidBody;
        protected bool m_rigidBodyWasKinematic;

        public virtual Transform Transform { get { return m_transform; } protected set { m_transform = value; } }

        public override GameObject GameObject {
            get { return m_gameObject; }
            set {
                if (value != null) {
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

        private void UpdateDelta() {
            var localPosition = Matrix4x4.TRS(Transform.position, Transform.rotation, Vector3.one) * m_finalPoint;
            m_deltaMove = localPosition.normalized * m_speed;
            m_movementLeft = localPosition.magnitude;

            if (m_stepSize <= 0) {
                m_rotation = Vector3.zero;
                m_deltaRotate = Vector3.zero;
            }
            else { 
                float numberOfSteps = localPosition.magnitude / m_stepSize;
                float totalRotation = 360f * numberOfSteps;
                m_rotation = m_finalPoint.normalized * totalRotation * (m_rotationDirection == RotationDirection.Clockwise ? -1 : 1);

                float angularSpeed = m_speed / m_stepSize * 360;
                m_deltaRotate = m_rotation.normalized * angularSpeed;
            }
            m_movementEpsilon = m_deltaMove.magnitude * 0.01f;
        }

        public virtual void SetValues(Vector3 finalPoint, float stepSize, float speed = 0) {
            m_finalPoint = finalPoint;
            m_stepSize = stepSize;
            if(speed != 0) {
                m_speed = speed;
            }
            if (m_state == AnimationState.Playing || m_state == AnimationState.Paused) {
                UpdateDelta();
            }
        }

        protected override object[] SerializeDataInternal() {
            return new object[] { m_rotationDirection, m_finalPoint, m_stepSize, m_speed };
        }

        protected override bool DeserializeDataInternal(object[] data) {
            if (data == null || data.Length < 4) {
                return false;
            }
            m_rotationDirection = (RotationDirection)System.Enum.Parse(typeof(RotationDirection), data[0].ToString());
            m_finalPoint = PropertyConvert.ToVector3(data[1]);
            return PropertyConvert.TryParse(data[2].ToString(), out m_stepSize) 
                && PropertyConvert.TryParse(data[3].ToString(), out m_speed);
        }

        public override void OnStart() {
            base.OnStart();
            UpdateDelta();
            if (LinearSpeed == 0) {
                CurrentState = AnimationState.Finished;
            }
        }

        public override void Animate(float dt) {
            if (m_rigidBody != null) {
                m_rigidBody.isKinematic = true;
                PhysicsRotation(dt);
                PhysicsMovement(dt);
            }
            else {
                SimpleRotation(dt);
                SimpleMovement(dt);
            }

            if (m_movementLeft < m_movementEpsilon) {
                // Destination Reached !!!
                CurrentState = AnimationState.Finished;
            }
        }

        protected override void StateChanged() {
            base.StateChanged();
            if (m_state == AnimationState.Stopped || m_state == AnimationState.Finished) {
                if (m_rigidBody != null) {
                    m_rigidBody.velocity = Vector3.zero;
                    m_rigidBody.isKinematic = m_rigidBodyWasKinematic;
                }
            }
        }

        #region [  MOVEMENT LOGIC  ]

        private void SimpleRotation(float dt) {
            Transform.Rotate(m_deltaRotate * dt);
        }

        private void SimpleMovement(float dt) {
            if (m_speed == 0) {
                m_movementLeft = 0;
            }
            else {
                var delta = m_deltaMove * dt;
                m_movementLeft -= delta.magnitude;
                Transform.position += delta;
            }
        }

        private void PhysicsRotation(float dt) {
            m_rigidBody.MoveRotation(Transform.localRotation * Quaternion.Euler(m_deltaRotate * dt));
        }

        private void PhysicsMovement(float dt) {
            if (m_speed == 0) {
                m_movementLeft = 0;
            }
            else {
                var delta = m_deltaMove * dt;
                m_movementLeft -= delta.magnitude;
                m_rigidBody.MovePosition(Transform.position + delta);
            }
        }

        #endregion
    }
}
