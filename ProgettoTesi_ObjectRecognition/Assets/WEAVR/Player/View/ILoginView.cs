using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Player.Views
{
    public interface ILoginView : IView
    {
        string Username { get; set; }
        string Password { get; set; }
        bool RememberMe { get; set; }

        event UnityAction OnForgotPassword;

        event UnityAction OnSubmitLogin;
    }
}

