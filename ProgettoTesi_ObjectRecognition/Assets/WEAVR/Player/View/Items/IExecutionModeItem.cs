using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Player.Views
{
    public interface IExecutionModeItem : ISelectItem
    {
        string ModeId { get; }
        string Description { get; set; }
    }
}

