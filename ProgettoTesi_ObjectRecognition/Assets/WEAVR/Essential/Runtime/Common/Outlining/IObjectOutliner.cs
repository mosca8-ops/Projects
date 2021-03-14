using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.VR;
using TXT.WEAVR.Common;
using UnityEngine.Events;
using System;

namespace TXT.WEAVR.Common
{
    public enum OutlineMode { OneRenderer, FirstChildren, AllChildren }

    public interface IObjectOutliner
    {
        bool Active { get; }
        void Outline(GameObject go, Color color);
        void RemoveOutline(GameObject go, Color color);
    }
}