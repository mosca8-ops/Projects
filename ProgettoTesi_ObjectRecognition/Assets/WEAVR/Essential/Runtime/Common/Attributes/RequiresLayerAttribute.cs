using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    /// <summary>
    /// Marks this class for missing layer
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class RequireLayersAttribute : WeavrAttribute
    {
        public string[] Layers { get; private set; }

        public RequireLayersAttribute(params string[] layers) {
            Layers = layers;
        }
    }
}