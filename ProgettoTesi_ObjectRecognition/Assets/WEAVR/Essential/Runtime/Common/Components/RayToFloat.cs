using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Components/RayToFloat")]
    public class RayToFloat : MonoBehaviour
    {
        public Transform[] sources;
        public bool invertValue;
        public Span angleLimits;
        public Span limits;
        [Min(0)]
        public float maxDistance = 1000;
        public UnityEventFloat onValueChange;

        private float m_currentValue;
        public float CurrentValue
        {
            get => m_currentValue;
            set
            {
                if(m_currentValue != value)
                {
                    m_currentValue = value;
                    onValueChange?.Invoke(m_currentValue);
                }
            }
        }

        private void OnValidate()
        {
            if(limits.min < 0)
            {
                limits.min = 0;
            }
            if(limits.max > 1)
            {
                limits.max = 1;
            }
            if(limits.min > limits.max)
            {
                limits.Resize(limits.max, limits.min);
            }
        }

        void Update()
        {
            float maxValue = limits.min;

            for (int i = 0; i < sources.Length; i++)
            {
                var source = sources[i];

                if (!source || !source.gameObject.activeInHierarchy) { continue; }

                var fromSource = transform.position - source.position;
                var projectedOnSource = Vector3.Dot(fromSource, source.forward);
                if (projectedOnSource > 0 && projectedOnSource < maxDistance)
                {
                    var fromSourceMagniture = fromSource.magnitude;
                    var angle = fromSourceMagniture != 0 ? Mathf.Acos(projectedOnSource / fromSourceMagniture) : 0;

                    var normalizedValue = angleLimits.Normalize(angleLimits.Clamp(Mathf.Rad2Deg * angle));
                    var newValue = limits.Denormalize(invertValue ?  normalizedValue : 1 - normalizedValue);
                    if (newValue > maxValue)
                    {
                        maxValue = newValue;
                    }
                }
            }

            CurrentValue = maxValue;
        }
    }
}
