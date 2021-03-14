using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using TXT.WEAVR.Common;
using TXT.WEAVR.UI;

namespace TXT.WEAVR.Player.Views
{
    public class FadeView : MonoBehaviour, IFadeView
    {
        [SerializeField]
        [Type(typeof(IFadeObject))]
        private Component m_fadePanel;

        public bool IsVisible 
        { 
            get => m_fadePanel.gameObject.activeInHierarchy;
            set
            {
                if (m_fadePanel.gameObject.activeInHierarchy != value)
                {
                    if (value)
                    {
                        Show();
                    }
                    else
                    {
                        Hide();
                    }
                }
            }
        }

        public event ViewDelegate OnShow;
        public event ViewDelegate OnHide;
        public event ViewDelegate OnBack;

        public IFadeObject FadePanel => m_fadePanel as IFadeObject;

        public void Hide()
        {
            FadePanel.FadeIn();
            OnHide?.Invoke(this);
        }

        public void Show()
        {
            FadePanel.FadeOut();
            OnShow?.Invoke(this);
        }
    }
}

