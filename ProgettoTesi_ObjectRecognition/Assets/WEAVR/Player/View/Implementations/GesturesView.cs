
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TXT.WEAVR.Common;
using TXT.WEAVR.InteractionUI;

namespace TXT.WEAVR.Player.Views
{
    public class GesturesView : MonoBehaviour, IGesturesView
    {
        [Header("Components")]
        [Draggable]
        [Type(typeof(IInteractablePanel))]
        public Component m_interactablePanel;

        public bool IsVisible { 
            get => m_interactablePanel is IInteractablePanel panel ? panel.Active : false;
            set
            {
                if(m_interactablePanel is IInteractablePanel panel)
                {
                    panel.Active = value;
                }
                if (value) OnShow?.Invoke(this);
                else OnHide?.Invoke(this);
            }
        }

        public event ViewDelegate OnShow;
        public event ViewDelegate OnHide;
        public event ViewDelegate OnBack;

        public IInteractablePanel GetPanel() => m_interactablePanel as IInteractablePanel;

        public void Hide() => IsVisible = false;

        public void Show()
        {
            IsVisible = true;
            GetPanel().ResetInput();
        }
    }
}

