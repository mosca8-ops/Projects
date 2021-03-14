using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.UI
{

    public interface ITextComponent
    {
        string Text { get; set; }
        Color Color { get; set;}
        bool IsOverlay { get; set; }
    }
}