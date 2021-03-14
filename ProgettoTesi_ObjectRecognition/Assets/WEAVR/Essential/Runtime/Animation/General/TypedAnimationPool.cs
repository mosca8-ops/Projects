using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Animation
{

    public class TypedAnimationPool : IAnimationPool
    {
        private Stack<BaseAnimation> m_animations;
        private List<BaseAnimation> m_createdAnimations;
        private Type m_type;

        public TypedAnimationPool(Type animationType) {
            m_type = animationType;
            m_animations = new Stack<BaseAnimation>();
            m_createdAnimations = new List<BaseAnimation>();
        }

        public void Clear() {

        }

        public BaseAnimation GetAnimation() {
            BaseAnimation animation = null;
            if (m_animations.Count > 0) {
                animation = m_animations.Pop();
            }
            else {
                animation = ScriptableObject.CreateInstance(m_type) as BaseAnimation;
                animation.SetPool(this);
            }
            animation.CurrentState = AnimationState.NotStarted;
            return animation;
        }

        public IAnimation Get() {
            return GetAnimation();
        }

        public void Reclaim(IAnimation animation) {
            if (animation is BaseAnimation) {
                animation.AnimationEndCallback = null;
                m_animations.Push(animation as BaseAnimation);
            }
        }
    }
}
