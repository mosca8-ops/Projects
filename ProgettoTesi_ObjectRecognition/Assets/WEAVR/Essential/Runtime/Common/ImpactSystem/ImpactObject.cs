using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.ImpactSystem
{
    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("WEAVR/Impact System/Impact Object")]
    public class ImpactObject : MonoBehaviour
    {
        [SerializeField]
        [Draggable]
        protected Rigidbody m_rigidBody;
        [SerializeField]
        [Range(1, 16)]
        private int m_velocitySamples = 4;
        [SerializeField]
        [Draggable]
        private AbstractObjectMaterial[] m_parts;


        private Dictionary<Collider, AbstractObjectMaterial> m_partsDictionary;

        private float m_nextValidCollision;

        public IEnumerable<AbstractObjectMaterial> Parts => m_parts;
        public IReadOnlyDictionary<Collider, AbstractObjectMaterial> PartsDictionary => m_partsDictionary;

        private Vector3 m_lastPosition;
        private Quaternion m_lastRotation;
        private Vector3 m_angularVelocity;

        private int m_velocityIndex;
        private Vector3[] m_velocity;
        private Vector3 m_avgVelocity;
        public Vector3 Velocity
        {
            get => m_avgVelocity;
            set
            {
                m_velocity[m_velocityIndex] = value;
                m_avgVelocity = Vector3.zero;
                for (int i = 0; i < m_velocity.Length; i++)
                {
                    m_avgVelocity += m_velocity[i];
                }
                m_avgVelocity = m_avgVelocity / m_velocity.Length;
                m_velocityIndex = (m_velocityIndex + 1) % m_velocity.Length;
            }
        }

        private void OnValidate()
        {
            if (!m_rigidBody)
            {
                m_rigidBody = GetComponent<Rigidbody>();
            }
            m_rigidBody.interpolation = RigidbodyInterpolation.Interpolate;
            if (m_rigidBody.collisionDetectionMode == CollisionDetectionMode.Discrete)
            {
                m_rigidBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
            m_parts = GetComponentsInChildren<ObjectMaterial>(true);
        }

        // Start is called before the first frame update
        void Start()
        {
            m_velocity = new Vector3[m_velocitySamples];
            m_partsDictionary = new Dictionary<Collider, AbstractObjectMaterial>();
            for (int i = 0; i < m_parts.Length; i++)
            {
                foreach(var coll in m_parts[i].Colliders)
                {
                    m_partsDictionary[coll] = m_parts[i];
                }
            }
        }

        private void FixedUpdate()
        {
            if (m_rigidBody.isKinematic)
            {
                Velocity = (transform.position - m_lastPosition) / Time.deltaTime;
                m_angularVelocity = (transform.rotation * Quaternion.Inverse(m_lastRotation)).eulerAngles * Mathf.Deg2Rad / Time.deltaTime;

                m_rigidBody.velocity = Velocity;
                m_rigidBody.angularVelocity = m_angularVelocity;
            }
            else
            {
                Velocity = m_rigidBody.velocity;
                m_angularVelocity = m_rigidBody.angularVelocity;
            }

            m_lastPosition = transform.position;
            m_lastRotation = transform.rotation;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if(Time.time < m_nextValidCollision) { return; }

            m_nextValidCollision = Time.time + Time.fixedDeltaTime;
            var force = collision.impulse / Time.fixedDeltaTime;
            if(force == Vector3.zero)
            {
                var velocity = m_rigidBody.velocity == Vector3.zero ? Velocity : m_rigidBody.velocity;
                force = (velocity * m_rigidBody.mass) / Time.fixedDeltaTime;
            }
            for (int i = 0; i < collision.contactCount; i++)
            {
                var point = collision.GetContact(i);
                if (m_partsDictionary.TryGetValue(point.thisCollider, out AbstractObjectMaterial mat) && !mat.IsMuted)
                {
                    var otherMat = point.otherCollider.GetComponentInParent<AbstractObjectMaterial>();
                    mat.ApplyImpact(collision, point, force, otherMat);
                }
            }
        }
    }
}
