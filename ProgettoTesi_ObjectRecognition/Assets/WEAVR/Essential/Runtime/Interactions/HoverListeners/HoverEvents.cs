using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Interaction
{

    [AddComponentMenu("WEAVR/Interactions/Hover Events")]
    public class HoverEvents : MonoBehaviour, IInteractionHoverListener
    {
        private UnityEventGameObject m_onHover;
        private UnityEventGameObject m_onUnhover;

        public void OnHoverEnter(AbstractInteractionController controller)
        {
            m_onHover?.Invoke(controller.gameObject);
        }

        public void OnHoverExit(AbstractInteractionController controller)
        {
            m_onUnhover?.Invoke(controller.gameObject);
        }
    }

}
