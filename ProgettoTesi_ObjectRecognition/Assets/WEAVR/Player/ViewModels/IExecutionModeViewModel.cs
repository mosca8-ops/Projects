using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Player.Views
{
    public interface IExecutionModeViewModel : IViewModel
    {
        string Name { get; }
        string Description { get; }
    }
}
