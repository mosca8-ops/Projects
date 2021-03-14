using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using TXT.WEAVR.UI;

namespace TXT.WEAVR.Player.Views
{
    public class LoginView : BaseView, ILoginView
    {
        // STATIC PART??

        [Space]
        [Header("Login Page Items")]
        [Tooltip("Username field - button")]
        [Draggable]
        public TMP_InputField usernameField; //TODO
        [Tooltip("Password field - button")]
        [Draggable]
        public TMP_InputField passwordField; //TODO
        [Tooltip("Remember Me Setting - Slider element")]
        [Draggable]
        public BooleanSlider rememberMeSlider;
        [Tooltip("Forgot password button")]
        [Draggable]
        public Button forgotPasswordButton;
        [Tooltip("Login button")]
        [Draggable]
        public Button loginButton;

        public string Username { get => usernameField.text; set => usernameField.text = value; }
        public string Password { get => passwordField.text; set => passwordField.text = value; }
        public bool RememberMe { get => rememberMeSlider.Value; set => rememberMeSlider.Value = value; }

        public event UnityAction OnForgotPassword
        {
            add => forgotPasswordButton.onClick.AddListener(value);
            remove => forgotPasswordButton.onClick.RemoveListener(value);
        }

        public event UnityAction OnSubmitLogin
        {
            add => loginButton.onClick.AddListener(value);
            remove => loginButton.onClick.RemoveListener(value);
        }
    }
}
