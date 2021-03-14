using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using System;

namespace TXT.WEAVR.Player.Views
{
    public class ChangePasswordView : BaseView, IChangePasswordView
    {
        // STATIC PART??

        [Header("Change Password Page Items")]
        [Tooltip("New Password - TMP input field")]
        [Draggable]
        public TMP_InputField oldPasswordField; //TODO
        [Tooltip("New Password - TMP input field")] 
        [Draggable]
        public TMP_InputField newPasswordField; //TODO
        [Tooltip("Confirm Password - TMP input field")]
        [Draggable]
        public TMP_InputField confirmPasswordField; //TODO
        [Tooltip("Error Label")]
        [Draggable]
        public TMP_Text errorLabel;
        [Tooltip("Save button")]
        [Draggable]
        public Button savePasswordButton;
        [Tooltip("Back Button")]
        [Draggable]
        public Button changePasswordBackButton;

        public string CurrentPassword => oldPasswordField.text;

        public string NewPassword => newPasswordField.text;

        public string Error { 
            get => errorLabel.text;
            set
            {
                if(errorLabel.text != value)
                {
                    errorLabel.text = value;
                    errorLabel.gameObject.SetActive(!string.IsNullOrEmpty(value));
                }
            }
        }

        protected override void Start()
        {
            base.Start();
            RemoveEvents();
            AddEvents();
        }

        private void AddEvents()
        {
            changePasswordBackButton.onClick.AddListener(Hide);
            savePasswordButton.onClick.AddListener(ChangePassword);
            newPasswordField.onValueChanged.AddListener(NewPasswordValueChanged);
            confirmPasswordField.onValueChanged.AddListener(ConfirmPasswordValueChanged);
        }

        private void RemoveEvents()
        {
            changePasswordBackButton.onClick.RemoveListener(Hide);
            savePasswordButton.onClick.RemoveListener(ChangePassword);
            newPasswordField.onValueChanged.RemoveListener(NewPasswordValueChanged);
            confirmPasswordField.onValueChanged.RemoveListener(ConfirmPasswordValueChanged);
        }

        public override void Show()
        {
            base.Show();
            confirmPasswordField.text = string.Empty;
            newPasswordField.text = string.Empty;
            oldPasswordField.text = string.Empty;
            savePasswordButton.interactable = false;
        }

        private void ConfirmPasswordValueChanged(string value)
        {
            ValidateConfirmButton();
        }

        private void NewPasswordValueChanged(string value)
        {
            ValidateConfirmButton();
        }

        private void ValidateConfirmButton()
        {
            if (newPasswordField.text != confirmPasswordField.text && !string.IsNullOrEmpty(confirmPasswordField.text))
            {
                Error = "Passwords do not match";
            }
            else if (!string.IsNullOrEmpty(newPasswordField.text))
            {
                Error = string.Empty;
            }

            Refresh();
        }

        public void Refresh()
        {
            savePasswordButton.interactable = string.IsNullOrEmpty(Error) && newPasswordField.text == confirmPasswordField.text && !string.IsNullOrEmpty(newPasswordField.text);
        }

        private void ChangePassword()
        {
            OnPasswordChange?.Invoke(CurrentPassword, NewPassword);
        }

        public event Action<string, string> OnPasswordChange;
    }
}
