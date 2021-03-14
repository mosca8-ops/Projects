using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Animation
{
    [SerializeField]
    public class ScaleAnimation : BaseAnimation
    {
        [SerializeField]
        protected Vector3 m_scaleFactor;
        [SerializeField]
        protected float m_speed;
        [SerializeField]
        protected bool m_scaleMass;

        private float m_scaleLeft;

        private Vector3 m_deltaScale;
        private float m_deltaMass;

        protected Transform m_transform;

        protected Rigidbody m_rigidBody;

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

        public virtual float ScaleSpeed {
            get { return m_speed; }
            set { m_speed = value > 0 ? value : 0; }
        }

        private void UpdateDelta()
        {
            var newScale = GetNewScale(m_scaleFactor, Transform.localScale);
            m_deltaScale = newScale * m_speed;
            m_scaleLeft = newScale.magnitude;

            m_scaleMass &= m_rigidBody != null;
            if (m_scaleMass)
            {
                m_deltaMass = m_scaleLeft * m_speed;
                if (m_scaleFactor.sqrMagnitude < 1)
                {
                    m_deltaMass = -m_deltaMass;
                }
            }
        }

        private Vector3 GetNewScale(Vector3 scaleFactor, Vector3 localScale)
        {
            return Vector3.Scale(scaleFactor - Vector3.one, localScale);
        }

        public virtual void SetDesiredScale(Vector3 newScale, bool scaleMass = false)
        {
            m_scaleFactor = newScale;
            m_scaleMass = scaleMass;
            if (m_state == AnimationState.Playing || m_state == AnimationState.Paused)
            {
                UpdateDelta();
            }
        }

        protected override object[] SerializeDataInternal()
        {
            return new object[] { m_scaleFactor, m_speed, m_scaleMass };
        }

        protected override bool DeserializeDataInternal(object[] data)
        {
            if (data == null || data.Length < 3)
            {
                return false;
            }
            m_scaleFactor = PropertyConvert.ToVector3(data[0]);
            return PropertyConvert.TryParse(data[1]?.ToString(), out m_speed) && bool.TryParse(data[2].ToString(), out m_scaleMass);
        }

        public override void OnStart()
        {
            base.OnStart();
            UpdateDelta();
        }

        public override void Animate(float dt)
        {
            var delta = m_deltaScale * dt;
            m_scaleLeft -= delta.magnitude;
            Transform.localScale += delta;

            if (m_scaleMass)
            {
                m_rigidBody.mass += m_deltaMass * dt;
            }

            if (m_scaleLeft < Mathf.Epsilon || m_speed == 0)
            {
                // Destination Reached !!!
                CurrentState = AnimationState.Finished;
            }
        }
    }
}
