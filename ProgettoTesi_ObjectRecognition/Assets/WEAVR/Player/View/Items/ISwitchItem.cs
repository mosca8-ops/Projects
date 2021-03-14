using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Player.Views
{
    public interface ISwitchItem : IViewItem
    {
        bool IsOn { get; set; }
        void SetStateSilently(bool isOn);
        event OnValueChanged<bool> StateChanged;
    }
}

