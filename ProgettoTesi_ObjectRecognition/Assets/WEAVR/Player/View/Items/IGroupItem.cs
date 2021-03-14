using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Player.Views
{
    public interface IGroupItem : ISelectItem
    {
        string Description { get; set; }
        bool IsNew { get; set; }
    }
}

