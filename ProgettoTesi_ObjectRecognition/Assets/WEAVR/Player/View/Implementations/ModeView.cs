using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using TXT.WEAVR.Localization;
using System;
using TXT.WEAVR.Common;
using System.Linq;

namespace TXT.WEAVR.Player.Views
{
    public class ModeView : BaseView, IModeView
    {
        [Header("Mode Page Items")]
        [Tooltip("Procedure Name - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI procedureName;
        [Tooltip("Description - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI description;
        [Tooltip("Button opens language list - TMP dropdown")]
        [Draggable]
        public TMP_Dropdown languageDropdown;
        [Tooltip("Start Button")]
        [Draggable]
        public Button modeStartButton;
        [Tooltip("Back Button")]
        [Draggable]
        public Button modeBackButton;

        private List<IExecutionModeItem> m_executionModeItems;
        private List<IExecutionModeViewModel> m_modeViewModels;
        private List<Language> m_languages;
        private IExecutionModeViewModel m_selectedViewModel;

        public string ProcedureName { get => procedureName.text; set => procedureName.text = value; }
        public Language Language 
        { 
            get => m_languages[languageDropdown.value]; 
            set => languageDropdown.value = m_languages.IndexOf(value); 
        }

        public IExecutionModeViewModel SelectedMode 
        { 
            get => m_selectedViewModel;
            set
            {
                if(m_selectedViewModel != value)
                {
                    m_selectedViewModel = value;
                    foreach(var item in m_executionModeItems)
                    {
                        item.IsSelected = item.ModeId == value?.Name;
                    }
                }
            } 
        }

        public event ViewDelegate<IModeView> OnCancel;

        public event ViewDelegate<IModeView> OnStart;

        protected override void Start()
        {
            base.Start();
            m_executionModeItems = new List<IExecutionModeItem>(view.GetComponentsInChildren<IExecutionModeItem>());
            modeStartButton.onClick.AddListener(StartClicked);
            modeBackButton.onClick.AddListener(CancelClicked);

            foreach(var mode in m_executionModeItems)
            {
                mode.OnSelected -= Mode_OnSelected;
                mode.OnSelected += Mode_OnSelected;
            }
        }

        private void Mode_OnSelected(ISelectItem item)
        {
            if (item is IExecutionModeItem modeItem)
            {
                var mode = modeItem.ModeId;
                m_selectedViewModel = m_modeViewModels.FirstOrDefault(i => i.Name == mode);
            }
        }

        private void CancelClicked()
        {
            OnCancel?.Invoke(this);
        }

        private void StartClicked()
        {
            OnStart?.Invoke(this);
        }

        public void SetAvailableLanguages(IEnumerable<Language> languages)
        {
            m_languages = new List<Language>(languages);
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(languages.Select(l => l.TwoLettersISOName).ToList());
        }

        public void SetExecutionModes(IEnumerable<IExecutionModeViewModel> executionModes)
        {
            // TODO: Perform the translation here of the execution modes
            foreach(var mode in m_executionModeItems)
            {
                (mode as Component).gameObject.SetActive(executionModes.Any(e => e.Name == mode.ModeId));
            }
            m_modeViewModels = new List<IExecutionModeViewModel>(executionModes);

            SelectedMode = executionModes.FirstOrDefault();
        }
    }
}
