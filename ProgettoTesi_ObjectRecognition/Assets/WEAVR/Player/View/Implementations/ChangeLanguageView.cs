using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using TXT.WEAVR.Localization;
using System;

namespace TXT.WEAVR.Player.Views
{
    public class ChangeLanguageView : BaseView, IChangeLanguageView
    {
        // STATIC PART??

        [Header("Applicaton Language Page Items")]
        [Tooltip("Button open list dropdown - OPS player")]
        [Draggable]
        public TMP_Dropdown playerLanguagesDropdown; // OPS only
        [Tooltip("Scrollbar for the Active Collaborations List")]
        [Draggable]
        public Scrollbar playerLanguagesScrollbar; // TODO: Control movement by Arrows(VR only)
        [Tooltip("Button up arrow of scrollbar for the Active Collaborations List")]
        [Draggable]
        public Button upArrowPlayerLanguagesScrollbar; // TODO: Control Scrollbar movement (VR only)
        [Tooltip("Button down arrow of scrollbar for the Active Collaborations List")]
        [Draggable]
        public Button downArrowPlayerLanguagesScrollbar; // TODO: Control Scrollbar movement (VR only)
        [Tooltip("Back Button")]
        [Draggable]
        public Button playerLanguagesBackButton;
        [Tooltip("Apply Button")]
        [Draggable]
        public Button playerLanguagesApplyButton; // OPS only

        private List<Language> m_languages;

        protected override void Start()
        {
            base.Start();
            playerLanguagesBackButton.onClick.RemoveListener(Hide);
            playerLanguagesBackButton.onClick.AddListener(Hide);
            playerLanguagesApplyButton.onClick.RemoveListener(ApplySelectedLanguage);
            playerLanguagesApplyButton.onClick.AddListener(ApplySelectedLanguage);
        }

        private void ApplySelectedLanguage()
        {
            if(0 <= playerLanguagesDropdown.value && playerLanguagesDropdown.value < m_languages?.Count)
            {
                OnLanguageSelected?.Invoke(m_languages[playerLanguagesDropdown.value]);
            }
            Hide();
        }

        public List<Language> Languages { 
            get => m_languages;
            set
            {
                if(m_languages != value)
                {
                    m_languages = value;
                    playerLanguagesDropdown.ClearOptions();
                    if(m_languages != null)
                    {
                        var options = playerLanguagesDropdown.options;
                        foreach(var lang in m_languages)
                        {
                            if (lang.Icon)
                            {
                                options.Add(new TMP_Dropdown.OptionData(lang.DisplayName, SpriteCache.Instance.Get(lang.Icon)));
                            }
                            else
                            {
                                options.Add(new TMP_Dropdown.OptionData(lang.DisplayName));
                            }
                        }

                        playerLanguagesDropdown.options = options;
                    }
                }
            }
        }

        public Language SelectedLanguage { 
            get => 0 <= playerLanguagesDropdown.value && playerLanguagesDropdown.value < m_languages?.Count ? m_languages[playerLanguagesDropdown.value] : null;
            set => playerLanguagesDropdown.value = m_languages.IndexOf(value);
        }

        public event Action<Language> OnLanguageSelected;
    }
}
