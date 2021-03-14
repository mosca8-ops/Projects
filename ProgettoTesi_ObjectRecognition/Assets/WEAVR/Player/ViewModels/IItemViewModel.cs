using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Player.Views
{
    public interface IItemViewModel : IViewModel
    {
        Guid Id { get; }
        string Name { get; }
        Color Color { get; }
        Texture2D Image { get; }
        bool Enabled { get; }
    }
}
