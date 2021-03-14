using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Player.Views
{
    public interface IChangePasswordView : IView
    {
        string Error { get; set; }
        string CurrentPassword { get; }
        string NewPassword { get; }

        event Action<string, string> OnPasswordChange;
        void Refresh();
    }
}
