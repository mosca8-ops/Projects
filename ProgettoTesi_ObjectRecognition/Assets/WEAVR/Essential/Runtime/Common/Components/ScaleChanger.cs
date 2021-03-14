using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Components/Scale Changer")]
    public class ScaleChanger : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The axis to apply scale changes to")]
        private Vector3 m_axis = Vector3.one;
        [SerializeField]
        [EnableIfComponentExists(typeof(Rigidbody))]
        [Tooltip("Whether to scale the mass as well or not")]
        private bool m_scaleMass = false;

        private Vector3 m_inverseAxis;
        private float m_scale = 1;
        private Rigidbody m_rigidBody;
        private float m_density;

        private void OnValidate()
        {
            m_inverseAxis = new Vector3(m_axis.x != 0 ? 1 / m_axis.x : 1f,
                                        m_axis.y != 0 ? 1 / m_axis.y : 1f,
                                        m_axis.z != 0 ? 1 / m_axis.z : 1f);
        }

        private void OnEnable()
        {
            OnValidate();
            m_rigidBody = GetComponent<Rigidbody>();
            if (m_rigidBody)
            {
                float volume = ComputeVolume();
                m_density = volume / m_rigidBody.mass;
            }
        }

        private float ComputeVolume()
        {
            var bounds = GetComponentsInChildren<Collider>().Select(c => c.bounds).ToList();
            for (int i = 0; i < bounds.Count; i++)
            {
                for (int j = i + 1; j < bounds.Count; j++)
                {
                    if (bounds[i].Intersects(bounds[j]))
                    {
                        bounds[i].Encapsulate(bounds[j]);
                        bounds.RemoveAt(j--);
                    }
                }
            }
            float volume = 0;
            for (int i = 0; i < bounds.Count; i++)
            {
                volume += bounds[i].size.x * bounds[i].size.y * bounds[i].size.z;
            }

            return volume;
        }

        public float Scale
        {
            get => m_scale;
            set
            {
                if (m_scale != value)
                {
                    m_scale = value;
                    transform.localScale += m_axis * (m_scale - 1);
                    if(m_scaleMass && m_rigidBody)
                    {
                        m_rigidBody.SetDensity(m_density);
                    }
                }
            }
        }

        public float ScaleX
        {
            get => transform.localScale.x;
            set => ApplyScale(value, 0, 0);
        }
        
        public float ScaleY
        {
            get => transform.localScale.y;
            set => ApplyScale(0, value, 0);
        }

        public float ScaleZ
        {
            get => transform.localScale.z;
            set => ApplyScale(0, 0, value);
        }

        private void ApplyScale(float deltaX, float deltaY, float deltaZ)
        {
            transform.localScale = new Vector3(transform.localScale.x + deltaX, transform.localScale.y + deltaY, transform.localScale.z + deltaZ);
            if (m_scaleMass && m_rigidBody)
            {
                m_rigidBody.SetDensity(m_density);
            }
        }

    }
}
