using System;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Animation/Animate By Distance")]
    public class AnimateByDistance : MonoBehaviour
    {
        [Draggable]
        public AnimationClip clip;
        [Draggable]
        public Transform samplingPoint;
        public Target target;

        private float m_distance;
        private float m_normalized;

        private void OnValidate()
        {
            if (samplingPoint == null)
            {
                samplingPoint = transform;
            }
        }

        // Use this for initialization
        void Start()
        {
            OnValidate();
            if (!GetComponentInParent<Animator>())
            {
                var animator = gameObject.AddComponent<Animator>();
                animator.enabled = false;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (clip != null
                && target.TryGetNormalizedValue(samplingPoint.position, out m_distance, out m_normalized))
            {
                clip.SampleAnimation(gameObject, m_normalized * clip.length);
            }
        }

        [Serializable]
        public struct Target
        {
            [Draggable]
            public Transform target;
            public bool invert;
            public Span range;

            public bool TryGetNormalizedValue(Vector3 position, out float distance, out float normalized)
            {
                distance = Vector3.Distance(position, target.position);
                if (range.IsValid(distance))
                {
                    normalized = invert ? range.Normalize(distance) : (1 - range.Normalize(distance));
                    return true;
                }
                normalized = 0;
                return false;
            }
        }
    }
}