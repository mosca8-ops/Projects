using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Player.Views
{
    public delegate void OnChangeDelegate(IViewModel viewModel);

    public interface IViewModel
    {
        event OnChangeDelegate Changed;
    }
}
