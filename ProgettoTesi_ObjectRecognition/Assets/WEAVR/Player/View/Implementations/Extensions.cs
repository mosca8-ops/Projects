using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TXT.WEAVR.Player.Views
{
    public static partial class ViewExtensions
    {
        public static Sprite CreateSprite(this Texture2D tex)
        {
            return tex ? SpriteCache.Instance.Get(tex) : null;
        }
    }
}

