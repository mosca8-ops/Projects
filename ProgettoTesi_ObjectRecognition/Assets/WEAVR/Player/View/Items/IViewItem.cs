using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Player.Views
{
    public interface IViewItem
    {
        Guid Id { get; set; }
        string Label { get; set; }
        Color Color { get; set; }
        Texture2D Image { get; set; }
        bool Enabled { get; set; }
        bool IsVisible { get; set; }
        void Clear();
    }
}

