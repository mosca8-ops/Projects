using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{

    public abstract class AnimationBlock<T, V> : BaseAnimationBlock, ITargetingObject where T : Object
    {
        [SerializeField]
        [Draggable]
        protected T m_target;
        [SerializeField]
        protected V m_startValue;
        [SerializeField]
        protected V m_endValue;

        private bool TargetIsComponent => typeof(Component).IsAssignableFrom(typeof(T));
        private bool TargetIsGameObject => typeof(GameObject) == typeof(T);

        public virtual Object Target
        {
            get => m_target;
            set
            {
                if (value is T t)
                {
                    m_target = t;
                }
                else if (value is Component c)
                {
                    if (TargetIsComponent)
                    {
                        m_target = c.GetComponent<T>();
                    }
                    else if (TargetIsGameObject)
                    {
                        m_target = c.gameObject as T;
                    }
                }
                else if (value is GameObject go)
                {
                    if (TargetIsComponent)
                    {
                        m_target = go.GetComponent<T>();
                    }
                    else if (TargetIsGameObject)
                    {
                        m_target = go as T;
                    }
                }
            }
        }

        public string TargetFieldName => nameof(m_target);

        public V StartValue
        {
            get => m_startValue;
            set
            {
                if((m_startValue != null && !m_startValue.Equals(value)) || (value != null && !value.Equals(m_startValue)))
                {
                    BeginChange();
                    m_startValue = value;
                    OnStartValueChanged();
                    PropertyChanged(nameof(StartValue));
                }
            }
        }
        
        public V EndValue
        {
            get => m_endValue;
            set
            {
                if ((m_endValue != null && !m_endValue.Equals(value)) || (value != null && !value.Equals(m_endValue)))
                {
                    BeginChange();
                    m_endValue = value;
                    OnEndValueChanged();
                    PropertyChanged(nameof(EndValue));
                }
            }
        }

        public override bool CanProvide<TB>()
        {
            return (m_startValue != null && m_startValue is TB) || typeof(TB).IsAssignableFrom(typeof(V));
        }

        public override VB Provide<VB>()
        {
            if(m_endValue is VB vb)
            {
                return vb;
            }
            return ProvideData<VB>();
        }

        protected virtual VB ProvideData<VB>()
        {
            return default;
        }

        protected virtual void OnStartValueChanged()
        {
            
        }

        protected virtual void OnEndValueChanged()
        {

        }
    }
    
    public abstract class ComponentAnimation<T> : BaseAnimationBlock, ITargetingObject, IPreviewAnimation where T : Component
    {
        [SerializeField]
        [Tooltip("The target component to animate")]
        [Draggable]
        protected T m_target;
        public Object Target {
            get => m_target;
            set => m_target = value is T t ? t 
                : value is Component c ? c.GetComponent<T>() 
                : value is GameObject go ? go.GetComponent<T>() 
                : null;
        }

        public string TargetFieldName => nameof(m_target);

        public virtual void ApplyPreview(GameObject previewGameObject)
        {
            if (CanPreview())
            {
                T previewComp = previewGameObject.GetComponent<T>();
                if (!previewComp) { return; }

                T prevT = m_target;
                try
                {
                    m_target = previewComp;
                    OnStart();
                    Animate(1, 1);
                    m_target = prevT;
                }
                catch
                {
                    m_target = prevT;
                }
            }
        }

        public virtual bool CanPreview() => true;
    }

    public abstract class GameObjectAnimation : BaseAnimationBlock, ITargetingObject, IPreviewAnimation
    {
        [SerializeField]
        [Tooltip("The target object to animate")]
        [Draggable]
        protected GameObject m_target;
        public Object Target {
            get => m_target;
            set => m_target = value is Component c ? c.gameObject 
                : value is GameObject go ? go 
                : null;
        }

        public string TargetFieldName => nameof(m_target);

        public void ApplyPreview(GameObject previewGameObject)
        {
            if (CanPreview() && previewGameObject)
            {
                GameObject prevGO = m_target;
                try
                {
                    m_target = previewGameObject;
                    OnStart();
                    Animate(1, 1);
                    m_target = prevGO;
                }
                catch
                {
                    m_target = prevGO;
                }
            }
        }

        public virtual bool CanPreview() => true;
    }

    public abstract class AnimationBlock<T> : BaseAnimationBlock, ITargetingObject where T : Object
    {
        [SerializeField]
        [Tooltip("The target to animate")]
        [Draggable]
        protected T m_target;

        private bool TargetIsComponent => typeof(Component).IsAssignableFrom(typeof(T));
        private bool TargetIsGameObject => typeof(GameObject) == typeof(T);

        public virtual Object Target
        {
            get => m_target;
            set
            {
                if (value is T t)
                {
                    m_target = t;
                }
                else if (value is Component c)
                {
                    if (TargetIsComponent)
                    {
                        m_target = c.GetComponent<T>();
                    }
                    else if (TargetIsGameObject)
                    {
                        m_target = c.gameObject as T;
                    }
                }
                else if (value is GameObject go)
                {
                    if (TargetIsComponent)
                    {
                        m_target = go.GetComponent<T>();
                    }
                    else if (TargetIsGameObject)
                    {
                        m_target = go as T;
                    }
                }
            }
        }

        public string TargetFieldName => nameof(m_target);
    }
}