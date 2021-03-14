namespace TXT.WEAVR.Interaction
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    [AddComponentMenu("WEAVR/VR/Interactions/Canvas Button")]
    public class VR_CanvasButton : VR_Object
    {
#if WEAVR_VR
        private Button m_button;
        private Animator m_animator;
        private bool m_isHoveringOld;

        protected override void Start()
        {
            base.Start();
            m_button = GetComponent<Button>();
            m_animator = m_button.GetComponent<Animator>();
            m_isHoveringOld = !isHovering;
        }

        protected override void Update()
        {
            base.Update();
            if (m_button.interactable)
            {
                if (isHovering != m_isHoveringOld)
                {
                    m_isHoveringOld = isHovering;
                    HandleHovering();
                }
            }
            else
            {
                if (m_animator != null)
                {
                    m_animator.SetTrigger(m_button.animationTriggers.disabledTrigger);
                }
            }
        }

        public override void HandleStandardInteraction()
        {
            if (m_button != null)
            {
                if (m_animator != null)
                {

                    m_animator.SetTrigger(m_button.animationTriggers.pressedTrigger);

                }
                m_button.onClick.Invoke();
            }
        }

        public override void HandleHovering()
        {
            if (m_animator != null)
            {
                if (isHovering)
                {
                    m_animator.SetTrigger(m_button.animationTriggers.highlightedTrigger);
                }
                else
                {
                    m_animator.SetTrigger(m_button.animationTriggers.normalTrigger);
                }
            }
        }
#endif

    }
}
