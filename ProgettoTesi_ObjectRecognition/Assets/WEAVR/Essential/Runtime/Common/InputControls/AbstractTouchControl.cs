using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TXT.WEAVR.InputControls
{
    [AddComponentMenu("")]
	public abstract class AbstractTouchControl : MonoBehaviour, IEventSystemHandler
	{
        [SerializeField]
        [HideInInspector]
        protected Animator m_animator;


        protected virtual void OnValidate()
        {
            if (!m_animator) { m_animator = GetComponent<Animator>(); }
        }

        public virtual void Hide()
        {
            if (m_animator) { m_animator.SetInteger("State", 0); }
        }

        public virtual void FaintReveal()
        {
            if (m_animator) { m_animator.SetInteger("State", 1); }
        }

        public virtual void FullReveal()
        {
            if (m_animator) { m_animator.SetInteger("State", 2); }
        }

        protected virtual void Awake()
        {
            if (!m_animator) { m_animator = GetComponent<Animator>(); }
        }

        public abstract bool CanHandlePointerDown(PointerEventData data);
    }
}