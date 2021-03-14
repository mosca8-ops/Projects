using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [Serializable]
    public class ColorHolder
    {
        public string name;
        public Color color;

        public ColorHolder()
        {
            name = string.Empty;
            color = Color.clear;
        }

        public ColorHolder(string name, Color color)
        {
            this.name = name;
            this.color = color;
        }
    }
}
