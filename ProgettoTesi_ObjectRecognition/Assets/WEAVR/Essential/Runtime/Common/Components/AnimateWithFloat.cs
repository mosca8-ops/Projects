using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Animation/Animate By Value")]
    public class AnimateWithFloat : MonoBehaviour
    {
        [Draggable]
        public GameObject target;
        [Draggable]
        public AnimationClip animationClip;
        public float speed = 1;
        public bool animateOnStart;
        [HiddenBy(nameof(animateOnStart))]
        [Range(0, 1)]
        public float startOn;
        public OptionalSpan animationSpan;

        private float m_progress;

        private void Start()
        {
            if (!GetComponentInParent<Animator>())
            {
                var animator = gameObject.AddComponent<Animator>();
                animator.enabled = false;
            }
            if (animateOnStart)
            {
                ResetAnimation();
            }
        }

        private void Reset()
        {
            var oldComponent = GetComponent<AnimationFloatEvent>();
            if (oldComponent)
            {
                target = oldComponent.target;
                animationClip = oldComponent.animationClip;
                if(oldComponent.StartPointPerc > 0)
                {
                    animationSpan = new Span(oldComponent.StartPointPerc, 1);
                }
                else
                {
                    animationSpan = Span.UnitSpan;
                }
            }
        }

        public void PlayAnimation(float progress)
        {
            if (animationClip)
            {
                if (!animationSpan.enabled)
                {
                    animationClip.SampleAnimation(target, animationClip.length * progress);
                }
                else if (animationSpan.value.IsValid(progress))
                {
                    animationClip.SampleAnimation(target, animationClip.length * animationSpan.value.Normalize(progress));
                }
                m_progress = progress;
            }
        }

        public void ResetAnimation()
        {
            PlayAnimation(startOn);
        }

        public void DeltaPlayAnimation(float delta)
        {
            m_progress = Mathf.Clamp01(m_progress + delta * speed);
            PlayAnimation(m_progress);
        }
    }
}