using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Components/Ray To Scale")]
    public class RayToScale : MonoBehaviour
    {
        public Transform[] sources;
        public Span angleLimits;
        public Span scaleLimits;
        [Min(0)]
        public float maxDistance = 1000;

        void Update()
        {
            float maxScale = scaleLimits.min;

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

                    var newScale = scaleLimits.Denormalize(1 - angleLimits.Normalize(angleLimits.Clamp(Mathf.Rad2Deg * angle)));
                    if (newScale > maxScale)
                    {
                        maxScale = newScale;
                    }
                }
            }

            transform.localScale = Vector3.one * maxScale;
        }
    }
}
