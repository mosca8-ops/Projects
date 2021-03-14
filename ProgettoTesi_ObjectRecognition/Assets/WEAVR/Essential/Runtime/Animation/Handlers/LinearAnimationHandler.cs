using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Animation
{
    public class LinearAnimationHandler : BaseAnimationHandler, IAnimationHandler
    {
        protected const float k_sqrConnectEpsilon = 0.0001f;
        protected const float k_connectRotationEpsilon = 0.03f * Mathf.Rad2Deg;

        protected Rigidbody m_rigidBody;
        protected bool m_rotationEnabled = false;

        protected float m_moveSpeed;
        protected float m_rotateSpeed;

        protected bool m_destinationIsTemporary;
        protected bool m_rigidBodyWasKinematic;

        protected GameObject m_gameObject;

        public virtual Transform Transform { get; protected set; }
        public virtual Transform Destination { get; protected set; }

        public override GameObject GameObject {
            get { return m_gameObject; }
            set {
                if(value != null) {
                    m_gameObject = value;
                    Transform = m_gameObject.transform;
                }
            }
        }

        public override Type[] HandledTypes {
            get { return new Type[] { }; }
        }

        public virtual void ChangeDestination(Transform destination, Transform offset) {
            if (offset == null) {
                Destination = destination;

                m_destinationIsTemporary = false;
                CurrentState = AnimationState.NotStarted;
                return;
            }
            s_instantTargetTransform.SetPositionAndRotation(Transform.position, Transform.rotation);
            s_instantTargetTransform.SetParent(offset, true);

            var lastPosition = offset.position;
            var lastRotation = offset.rotation;

            offset.SetPositionAndRotation(destination.position, destination.rotation);

            Destination = CreateTemporaryTransform(destination, s_instantTargetTransform.position, s_instantTargetTransform.rotation);

            s_instantTargetTransform.SetParent(null);
            offset.position = lastPosition;
            offset.rotation = lastRotation;

            CurrentState = AnimationState.NotStarted;
            m_destinationIsTemporary = true;
        }

        public virtual void ChangeDestination(Vector3 destination) {
            m_destinationIsTemporary = true;
            Destination = CreateTemporaryTransform(null, destination, Transform.rotation);
            CurrentState = AnimationState.NotStarted;
        }

        public override void Animate(float dt) {
            if (m_rigidBody != null) {
                m_rigidBody.isKinematic = true;
                if (m_rotationEnabled) { PhysicsRotation(dt); }
                PhysicsMovement(dt);
            }
            else {
                if (m_rotationEnabled) { SimpleRotation(dt); }
                SimpleMovement(dt);
            }

            if ((Transform.position - Destination.position).sqrMagnitude < k_sqrConnectEpsilon
                && (!m_rotationEnabled || Quaternion.Angle(Transform.rotation, Destination.rotation) < k_connectRotationEpsilon)) {
                // Destination Reached !!!
                CurrentState = AnimationState.Finished;
            }
        }

        protected override void DataChanged() {
            base.DataChanged();
            m_rigidBody = m_gameObject.GetComponent<Rigidbody>();
            m_rigidBodyWasKinematic = m_rigidBody != null ? m_rigidBody.isKinematic : false;

            if(m_data == null) { return; }
            
        }

        protected override void StateChanged() {
            base.StateChanged();
            if(m_state == AnimationState.Stopped || m_state == AnimationState.Finished) {
                if (m_rigidBody != null) {
                    m_rigidBody.isKinematic = m_rigidBodyWasKinematic;
                }
                if (m_destinationIsTemporary) { Destroy(Destination.gameObject); }
            }
        }

        #region [  MOVEMENT LOGIC  ]

        private void SimpleRotation(float dt) {
            if (m_rotateSpeed == 0) {
                Transform.rotation = Destination.rotation;
            }
            else {
                Transform.rotation = Quaternion.RotateTowards(Transform.rotation, Destination.rotation, m_rotateSpeed * dt);
            }
        }

        private void SimpleMovement(float dt) {
            if (m_moveSpeed == 0) {
                Transform.position = Destination.position;
            }
            else {
                Transform.position = Vector3.MoveTowards(Transform.position, Destination.position, dt * m_moveSpeed);
            }
        }

        private void PhysicsRotation(float dt) {
            if (m_rotateSpeed == 0) {
                m_rigidBody.MoveRotation(Destination.rotation);
            }
            else {
                m_rigidBody.MoveRotation(Quaternion.RotateTowards(Transform.rotation, Destination.rotation, m_rotateSpeed * dt));
            }
        }

        private void PhysicsMovement(float dt) {
            if (m_moveSpeed == 0) {
                m_rigidBody.MovePosition(Destination.position);
            }
            else {
                m_rigidBody.MovePosition(Vector3.MoveTowards(Transform.position, Destination.position, dt * m_moveSpeed));
            }
        }

        #endregion
    }
}