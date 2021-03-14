using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core.DataTypes;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class AnimationClipBlock : GameObjectAnimation
    {
        [SerializeField]
        [Tooltip("The animation clip to play")]
        [Draggable]
        private AnimationClip m_clip;

        [NonSerialized]
        private bool m_destroyAnimator;
        [NonSerialized]
        private Animator m_animator;

        private GameObjectState m_state;

        public override bool CanProvide<T>()
        {
            return false;
        }

        public override void OnStart()
        {
            base.OnStart();
            if (m_target)
            {
                m_state = new GameObjectState(m_target);
                m_state.Snapshot();
            }
        }

        private void ValidateAnimator()
        {
            if (Application.isPlaying && !m_animator)
            {
                m_animator = m_target.GetComponentInParent<Animator>();
                if (!m_animator)
                {
                    m_animator = m_target.AddComponent<Animator>();
                    m_destroyAnimator = true;
                }
            }
        }

        protected override void Animate(float delta, float normalizedValue)
        {
            if (normalizedValue == 0)
            {
                // Restore default values
                if (m_target && m_state != null && m_state.GameObject == m_target)
                {
                    m_state.Restore();
                }
            }
            else
            if (m_clip)
            {
                ValidateAnimator();
                m_clip.SampleAnimation(m_target, normalizedValue * m_clip.length);
            }
        }

        public override void OnEnd(float normalizedDelta)
        {
            base.OnEnd(normalizedDelta);
            if (m_animator && m_destroyAnimator)
            {
                if (Application.isPlaying)
                {
                    Destroy(m_animator);
                }
                else
                {
                    DestroyImmediate(m_animator);
                }
            }
        }

        public override bool CanPreview()
        {
            return true;
        }
    }
}
