using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.UI
{
    [AddComponentMenu("WEAVR/VR/Interactions/Hide Highlights and Hints")]
    public class VR_HideHighlightsAndHints : MonoBehaviour
    {

#if WEAVR_VR
        private bool m_showHints;
        private bool m_showHighlights;

        private void Start()
        {
            m_showHighlights = WeavrManager.ShowInteractionHighlights;
            m_showHints = WeavrManager.ShowHints;
        }

        void Update()
        {
            if(m_showHighlights != WeavrManager.ShowInteractionHighlights)
            {
                m_showHighlights = WeavrManager.ShowInteractionHighlights;
                if (m_showHighlights)
                {
                    foreach (var highlight in GetComponentsInChildren<Valve.VR.InteractionSystem.ControllerHoverHighlight>())
                    {
                        highlight.enabled = false;
                    }
                    foreach (var buttonHints in GetComponentsInChildren<Valve.VR.InteractionSystem.ControllerButtonHints>())
                    {
                        buttonHints.enabled = true;
                    }
                }
                else
                {
                    foreach (var highlight in GetComponentsInChildren<Valve.VR.InteractionSystem.ControllerHoverHighlight>())
                    {
                        highlight.HideHighlight();
                        highlight.enabled = false;
                    }
                    foreach (var hand in FindObjectsOfType<Valve.VR.InteractionSystem.Hand>())
                    {
                        Valve.VR.InteractionSystem.ControllerButtonHints.HideAllButtonHints(hand);
                        Valve.VR.InteractionSystem.ControllerButtonHints.HideAllTextHints(hand);
                    }
                    foreach (var buttonHints in GetComponentsInChildren<Valve.VR.InteractionSystem.ControllerButtonHints>())
                    {
                        buttonHints.enabled = false;
                    }
                }
            }
            if(m_showHints != WeavrManager.ShowHints)
            {
                m_showHints = WeavrManager.ShowHints;
                if (m_showHints)
                {
                    foreach (var buttonHints in GetComponentsInChildren<Valve.VR.InteractionSystem.ControllerButtonHints>())
                    {
                        buttonHints.enabled = true;
                    }
                }
                else
                {
                    foreach (var hand in FindObjectsOfType<Valve.VR.InteractionSystem.Hand>())
                    {
                        //Valve.VR.InteractionSystem.ControllerButtonHints.HideAllButtonHints(hand);
                        Valve.VR.InteractionSystem.ControllerButtonHints.HideAllTextHints(hand);
                    }
                    //foreach (var buttonHints in GetComponentsInChildren<Valve.VR.InteractionSystem.ControllerButtonHints>())
                    //{
                    //    buttonHints.enabled = false;
                    //}
                }
            }
        }
#endif
    }
}
