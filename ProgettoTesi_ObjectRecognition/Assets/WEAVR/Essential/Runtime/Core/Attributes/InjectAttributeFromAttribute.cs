using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR
{

    [AttributeUsage(AttributeTargets.Field)]
    public class InjectAttributeFromAttribute : WeavrAttribute
    {
        public string SourcePath { get; private set; }

        public InjectAttributeFromAttribute(string sourcePath)
        {
            SourcePath = sourcePath;
        }
    }
}
