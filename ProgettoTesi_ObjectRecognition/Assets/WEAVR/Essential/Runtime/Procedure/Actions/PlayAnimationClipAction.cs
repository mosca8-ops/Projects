using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class PlayAnimationClipAction : BaseReversibleProgressAction, ITargetingObject, ISerializedNetworkProcedureObject
    {
        [Serializable]
        private class ValueProxyAnimationClip : ValueProxyObject<AnimationClip> { }

        [SerializeField]
        [Tooltip("The object to be animated by the animation")]
        [Draggable]
        private ValueProxyGameObject m_target;
        [SerializeField]
        [Tooltip("The animation to play")]
        [Draggable]
        private ValueProxyAnimationClip m_animationClip;
        [SerializeField]
        [Tooltip("The speed of the animation, a negative speed will play the animation backwards")]
        private float m_speed = 1;
        [SerializeField]
        [Tooltip("Whether to loop the animation or not")]
        [ShowIf(nameof(CanLoop))]
        private bool m_loop = false;
        [SerializeField]
        [Tooltip("Whether to alternate the animation or not")]
        [ShowIf(nameof(ShowAlternate))]
        private bool m_alternate = true;

        [SerializeField]
        private bool m_isGlobal = true;

        private float m_time;
        private float m_currentSpeed;
        private bool m_disableAnimator;

        public UnityEngine.Object Target {
            get => m_target;
            set => m_target.Value = value is GameObject go ? go : value is Component c ? c.gameObject : value == null ? null : m_target.Value; }

        public string TargetFieldName => nameof(m_target);

        public string IsGlobalFieldName => nameof(m_isGlobal);

        public bool IsGlobal => m_isGlobal;

        public override void OnStart(ExecutionFlow flow, ExecutionMode executionMode)
        {
            base.OnStart(flow, executionMode);
            var animator = m_target.Value.GetComponent<Animator>();
            if (animator && animator.enabled)
            {
                animator.enabled = false;
                m_disableAnimator = true;
            }
            else
            {
                m_disableAnimator = false;
            }
            m_currentSpeed = m_speed == 0 ? 0 : m_speed;
            m_time = m_currentSpeed > 0 ? 0 : m_animationClip.Value.length;
        }

        private bool CanLoop()
        {
            return AsyncThread != 0;
        }

        private bool ShowAlternate()
        {
            return AsyncThread != 0 && m_loop;
        }

        public override void OnValidate()
        {
            base.OnValidate();
            m_loop &= AsyncThread != 0;
        }

        public override bool Execute(float dt)
        {
            var clip = m_animationClip.Value;
            clip.SampleAnimation(m_target, m_time);

            if(m_currentSpeed > 0)
            {
                if (m_time >= clip.length) {
                    if (m_loop)
                    {
                        if (m_alternate)
                        {
                            m_currentSpeed = -m_currentSpeed;
                        }
                        else
                        {
                            m_time = 0;
                        }
                        return false;
                    }
                    return true;
                }
                m_time = Mathf.Min(m_time + dt * m_currentSpeed, clip.length);
                Progress = clip.length > 0 ? m_time / clip.length : 1;
            }
            else
            {
                if (m_time <= 0) {
                    if (m_loop)
                    {
                        if (m_alternate)
                        {
                            m_currentSpeed = -m_currentSpeed;
                        }
                        else
                        {
                            m_time = clip.length;
                        }
                        return false;
                    }
                    return true;
                }
                m_time = Mathf.Max(m_time + dt * m_currentSpeed, 0);
                Progress = 1 - (clip.length > 0 ? m_time / clip.length : 1);
            }

            return false;
        }

        public override void FastForward()
        {
            base.FastForward();
            m_animationClip.Value.SampleAnimation(m_target, m_animationClip.Value.length);
            m_time = 0;
        }

        public override void OnContextExit(ExecutionFlow flow)
        {
            if (RevertOnExit)
            {
                //m_time = m_animationClip.length;
                if (AsyncThread == 0)
                {
                    flow.EnqueueCoroutine(AnimateBackwards(), true);
                }
                else
                {
                    flow.StartCoroutine(AnimateBackwards());
                }
            }
        }

        public override void OnStop()
        {
            base.OnStop();
            if(m_disableAnimator && RevertOnExit)
            {
                ReenableAnimator();
            }
        }

        private void ReenableAnimator()
        {
            if (m_disableAnimator)
            {
                var animator = m_target.Value.GetComponent<Animator>();
                if (animator) { animator.enabled = true; }
            }
            m_disableAnimator = false;
        }

        private IEnumerator AnimateBackwards()
        {
            float speed = -m_speed;
            //float time = speed > 0 ? 0 : m_animationClip.length;
            float time = m_time;
            var clip = m_animationClip.Value;
            if (speed > 0)
            {
                clip.SampleAnimation(m_target, time);
                while (time < clip.length)
                {
                    time = Mathf.Min(time + Time.deltaTime * speed, clip.length);
                    yield return null;
                    clip.SampleAnimation(m_target, time);
                }
            }
            else
            {
                clip.SampleAnimation(m_target, time);
                while (time > 0)
                {
                    time = Mathf.Max(time + Time.deltaTime * speed, 0);
                    yield return null;
                    clip.SampleAnimation(m_target, time);
                }
            }
            ReenableAnimator();
        }

        public override string GetDescription()
        {
            var animationClip = m_animationClip.ToString();
            var speed = m_speed != 1 ? $" with speed {m_speed}" : "";
            return $"{m_target}.Animation = {animationClip} {speed}";
        }
    }
}