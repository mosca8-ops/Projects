using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Animation
{

    public class GenericAnimationPool : IAnimationPool
    {
        private Stack<IAnimation> m_animations;
        private List<IAnimation> m_createdAnimations;
        private Func<IAnimationPool, IAnimation> m_fullBuilder;

        public GenericAnimationPool(Func<IPooledAnimation> builder) {
            m_animations = new Stack<IAnimation>();
            m_createdAnimations = new List<IAnimation>();
            m_fullBuilder = p => {
                IPooledAnimation anim = builder();
                anim.SetPool(this);
                return anim;
            };
        }

        public GenericAnimationPool(Func<IAnimationPool, IAnimation> builder) {
            m_animations = new Stack<IAnimation>();
            m_createdAnimations = new List<IAnimation>();
            m_fullBuilder = builder;
        }

        public void Clear() {

        }

        public IAnimation GetAnimation() {
            IAnimation animation = null;
            if (m_animations.Count > 0) {
                animation = m_animations.Pop();
            }
            else {
                animation = m_fullBuilder(this);
            }
            animation.CurrentState = AnimationState.NotStarted;
            return animation;
        }

        public IAnimation Get() {
            return GetAnimation();
        }

        public void Reclaim(IAnimation animation) {
            animation.AnimationEndCallback = null;
            m_animations.Push(animation);
        }
    }
}
