using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Components/Ray To Transparency")]
    public class RayToTransparency : MonoBehaviour
    {
        public Transform[] sources;
        public Graphic[] targetGraphics;
        public bool invertValue;
        public Span angleLimits;
        public Span transparencyLimits;
        [Min(0)]
        public float maxDistance = 1000;

        private void OnValidate()
        {
            if (transparencyLimits.min < 0)
            {
                transparencyLimits.min = 0;
            }
            if (transparencyLimits.max > 1)
            {
                transparencyLimits.max = 1;
            }
            if (transparencyLimits.min > transparencyLimits.max)
            {
                transparencyLimits.Resize(transparencyLimits.max, transparencyLimits.min);
            }
        }

        void Update()
        {
            float maxValue = transparencyLimits.min;

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
                    var newValue = transparencyLimits.Denormalize(invertValue ? normalizedValue : 1 - normalizedValue);
                    if (newValue > maxValue)
                    {
                        maxValue = newValue;
                    }
                }
            }

            for (int i = 0; i < targetGraphics.Length; i++)
            {
                if (targetGraphics[i])
                {
                    var color = targetGraphics[i].color;
                    color.a = maxValue;
                    targetGraphics[i].color = color;
                }
            }
        }
    }
}
