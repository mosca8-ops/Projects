using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    public enum HandleType
    {
        Position,
        Rotation,
        Scale,
        Slider,
        Slider2D
    }

    public enum HandleSpace
    {
        Auto,
        World,
        Local,
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class UseHandleAttribute : WeavrAttribute
    {
        public HandleType Type { get; private set; }
        public HandleSpace Space { get; private set; }

        public UseHandleAttribute(HandleType type, HandleSpace space = HandleSpace.Auto)
        {
            Type = type;
            Space = space;
        }
    }
}
