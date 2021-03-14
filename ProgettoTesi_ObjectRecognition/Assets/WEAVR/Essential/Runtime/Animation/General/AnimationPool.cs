using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Animation
{

    public class AnimationPool<T> : IAnimationPool where T : BaseAnimation
    {
        private Stack<T> m_animations;
        private List<T> m_createdAnimations;

        public AnimationPool() {
            m_animations = new Stack<T>();
            m_createdAnimations = new List<T>();
        }

        public void Clear() {

        }

        public T GetAnimation() {
            T animation = null;
            if(m_animations.Count > 0) {
                animation = m_animations.Pop();
            }
            else {
                animation = ScriptableObject.CreateInstance<T>();
                animation.SetPool(this);
            }
            animation.CurrentState = AnimationState.NotStarted;
            return animation;
        }

        public IAnimation Get() {
            return GetAnimation();
        }

        public void Reclaim(IAnimation animation) {
            if (animation is T) {
                animation.AnimationEndCallback = null;
                m_animations.Push(animation as T);
            }
        }
    }
}
