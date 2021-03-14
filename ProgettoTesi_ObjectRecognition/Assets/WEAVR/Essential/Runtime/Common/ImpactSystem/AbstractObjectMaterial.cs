using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.ImpactSystem
{
    public abstract class AbstractObjectMaterial : MonoBehaviour
    {
        public delegate bool OnCollisionDelegate(AbstractObjectMaterial caller, Collision collision, ContactPoint point, Vector3 force);

        [SerializeField]
        protected bool m_applyOnAnySurface = true;

        public event OnCollisionDelegate OnCollision;

        private int m_mutedFrame;
        public bool IsMuted
        {
            get => m_mutedFrame == Time.frameCount;
            set
            {
                if (value)
                {
                    m_mutedFrame = Time.frameCount;
                }
            }
        }

        private void Reset()
        {
            OnValidate();
        }

        protected virtual void OnValidate()
        {

        }

        protected virtual void Awake()
        {

        }

        public abstract ImpactMaterial GetMaterial(Vector3 worldPoint);

        public void ApplyImpact(Collision collision, ContactPoint point, Vector3 force, AbstractObjectMaterial other)
        {
            if (!IsMuted)
            {
                ApplyImpactInternal(collision, point, force, other);
            }
        }

        public abstract IEnumerable<Collider> Colliders { get; }

        protected virtual void ApplyImpactInternal(Collision collision, ContactPoint point, Vector3 force, AbstractObjectMaterial other)
        {
            if (other && other != this)
            {
                other.IsMuted = true;
                //GetMaterial(point.point).ApplyImpactAt(point.point, force.magnitude, other.GetMaterial(point.point), collision.impulse, point.thisCollider);
                if (NotifyOnCollision(collision, point, force))
                {
                    GetMaterial(point.point).ApplyImpact(collision, point, force.magnitude, other.GetMaterial(point.point));
                }
            }
            else if (!other && m_applyOnAnySurface)
            {
                //GetMaterial(point.point).ApplyImpactAt(point.point, force.magnitude, null, collision.impulse, point.thisCollider);
                if (NotifyOnCollision(collision, point, force))
                {
                    GetMaterial(point.point).ApplyImpact(collision, point, force.magnitude, null);
                }
            }
        }

        protected bool NotifyOnCollision(Collision collision, ContactPoint point, Vector3 force) => OnCollision?.Invoke(this, collision, point, force) ?? true;
    }
}
