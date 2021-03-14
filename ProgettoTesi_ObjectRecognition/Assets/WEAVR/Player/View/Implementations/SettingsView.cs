using System.Collections.Generic;

using UnityEngine;
using System;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using TXT.WEAVR.UI;
using TXT.WEAVR.Localization;

namespace TXT.WEAVR.Player.Views
{

    public class SettingsView : BaseView, ISettingsView
    {
        // STATIC PART??

        [Space]
        [Header("User Profile Items")]
        [Tooltip("User Image")]
        [Draggable]
        public Image userImage;
        [Tooltip("User Name - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI userName;
        [Tooltip("User Department - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI userDepartment;
        [Tooltip("User Role - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI userRole;
        [Tooltip("Canvas Panel for the Player Settings")]
        [Draggable]
        public GameObject settingsListPanel; // TODO: to add examples
        [Header("Settings Page Items")]
        [Tooltip("Button opens Change Password Page")]
        [Draggable]
        public Button changePasswordButton;
        [Tooltip("Button changes Application Language")]
        [Draggable]
        public LabeledButton applicationLanguageButton;
        [Tooltip("Button changes Graphics Quality")]
        [Draggable]
        public LabeledButton graphicsQualityButton;
        [Tooltip("Slider for Automatic Procedures Updates")]
        [Draggable]
        public BooleanSlider autoProceduresUpdates;
        [Tooltip("Button opens Clear WAEVR Cache Page")]
        [Draggable]
        public Button clearCacheButton;
        [Tooltip("Button opens Reset Settings Page")]
        [Draggable]
        public Button resetSettingsButton;
        [Tooltip("Prefab for generic setting with button")]
        [Draggable]
        public LabeledButton settingsElementButtonSample; // TODO: to add in panel
        [Tooltip("Prefab for generic setting with slider")]
        [Draggable]
        public BooleanSlider settingsElementBooleanSample; // TODO: to add in panel
        [Tooltip("Prefab for generic setting with input field integer value")]
        [Draggable]
        public LabeledInputField settingsElementIntInputFieldSample; // TODO: to add in panel
        [Tooltip("Prefab for generic setting with input field integer value")]
        [Draggable]
        public LabeledInputField settingsElementFloatInputFieldSample; // TODO: to add in panel
        [Tooltip("Prefab for generic setting with input field integer value")]
        [Draggable]
        public LabeledInputField settingsElementStringInputFieldSample; // TODO: to add in panel


        private List<GameObject> m_settingsList = new List<GameObject>();

        private Language m_language;
        private string m_graphicsQuality;

        public Texture2D UserImage { get => userImage.sprite.texture; set => userImage.sprite = SpriteCache.Instance.Get(value); }

        public string UserName { get => userName.text; set => userName.text = value; }

        public string UserDepartment { get => userDepartment.text; set => userDepartment.text = value; }

        public string UserRole { get => userRole.text; set => userRole.text = value; }
        public bool AutoUpdateProcedures { get => autoProceduresUpdates.Value; set => autoProceduresUpdates.Value = value; }
        public Language CurrentLanguage
        {
            get => m_language;
            set
            {
                m_language = value;
                applicationLanguageButton.SubLabel = m_language ? m_language.DisplayName : "None";
            }
        }

        public string GraphicsQuality { 
            get => m_graphicsQuality;
            set
            {
                m_graphicsQuality = value;
                graphicsQualityButton.SubLabel = m_graphicsQuality;
            }
        }

        public event UnityAction OnChangeGraphicsQuality
        {
            add => graphicsQualityButton.onClick.AddListener(value);
            remove => graphicsQualityButton.onClick.RemoveListener(value);
        }

        public event UnityAction OnChangePassword
        {
            add => changePasswordButton.onClick.AddListener(value);
            remove => resetSettingsButton.onClick.RemoveListener(value);
        }

        public event UnityAction OnChangeApplicationLanguage
        {
            add => applicationLanguageButton.onClick.AddListener(value);
            remove => applicationLanguageButton.onClick.RemoveListener(value);
        }

        public event UnityAction<bool> OnAutoProceduresUpdates
        {
            add => autoProceduresUpdates.onValueChanged.AddListener(value);
            remove => autoProceduresUpdates.onValueChanged.RemoveListener(value);
        }

        public event UnityAction OnClearCache
        {
            add => clearCacheButton.onClick.AddListener(value);
            remove => clearCacheButton.onClick.RemoveListener(value);
        }

        public event UnityAction OnResetSettings
        {

            add => resetSettingsButton.onClick.AddListener(value);
            remove => resetSettingsButton.onClick.RemoveListener(value);
        }

        public void AddSettingLine(string key, object value, Action<object> onChanged)
        {
            // First need to understand the type of the value
            if(value is int intValue)
            {
                // Create the clone for the boolean handle
                var intClone = Instantiate(settingsElementIntInputFieldSample);
                // Assign it to the correct parent
                intClone.transform.SetParent(settingsListPanel.transform, false);
                // Set the label
                intClone.Label = key;
                // Set up the current value
                intClone.Value = intValue.ToString();
                // Hook up the event
                intClone.onValueChanged.AddListener(v => onChanged(int.Parse(v)));
                intClone.onEndEdited.AddListener(v => onChanged(int.Parse(v)));
                // Assign it to the list
                m_settingsList.Add(intClone.gameObject);
            }
            else if(value is float floatValue)
            {
                // Create the clone for the boolean handle
                var floatClone = Instantiate(settingsElementFloatInputFieldSample);
                // Assign it to the correct parent
                floatClone.transform.SetParent(settingsListPanel.transform, false);
                // Set the label
                floatClone.Label = key;
                // Set up the current value
                floatClone.Value = floatValue.ToString();
                // Hook up the event
                floatClone.onValueChanged.AddListener(v => onChanged(float.Parse(v)));
                floatClone.onEndEdited.AddListener(v => onChanged(float.Parse(v)));
                // Assign it to the list
                m_settingsList.Add(floatClone.gameObject);
            }
            else if(value is bool boolValue)
            {
                // Create the clone for the boolean handle
                var booleanClone = Instantiate(settingsElementBooleanSample);
                // Assign it to the correct parent
                booleanClone.transform.SetParent(settingsListPanel.transform, false);
                // Set the label
                booleanClone.Label = key;
                // Set up the current value
                booleanClone.Value = boolValue;
                // Hook up the event
                booleanClone.onValueChanged.AddListener(v => onChanged(v));
                // Assign it to the list
                m_settingsList.Add(booleanClone.gameObject);
            }
            else if(value is string stringValue)
            {
                // Create the clone for the boolean handle
                var stringClone = Instantiate(settingsElementIntInputFieldSample);
                // Assign it to the correct parent
                stringClone.transform.SetParent(settingsListPanel.transform, false);
                // Set the label
                stringClone.Label = key;
                // Set up the current value
                stringClone.Value = stringValue;
                // Hook up the event
                stringClone.onValueChanged.AddListener(v => onChanged(v));
                stringClone.onEndEdited.AddListener(v => onChanged(v));
                // Assign it to the list
                m_settingsList.Add(stringClone.gameObject);

            }
            else
            {
                // We have an unhandled type so we throw an exception
                throw new ArgumentException($"Unknown value type: {value?.GetType()}");
            }
        }

        public void AddSettingLine(string key, Action onSelected)
        {
            // Here we need to add a new button and assign the callback on its click event
            var buttonClone = Instantiate(settingsElementButtonSample);
            // Assign to the correct parent
            buttonClone.transform.SetParent(settingsListPanel.transform, false);
            // Set the label
            buttonClone.Label = key;
            // Assign the callback
            buttonClone.onClick.AddListener(() => onSelected());
            // Assign it to the list
            m_settingsList.Add(buttonClone.gameObject);
        }

        public void ClearSettings()
        {
            // Iterate over each registered setting and destroy its gameobject
            foreach(var settingGameObject in m_settingsList)
            {
                Destroy(settingGameObject);
            }
            // Clear the list
            m_settingsList.Clear();
        }
    }
}
