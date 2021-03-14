using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ArrayElementAttribute : WeavrAttribute
    {
        public string ArrayPath { get; private set; }
        public string NamePath { get; private set; }
        public bool NotNull { get; private set; }

        /// <summary>
        /// Show this element as a popup with values from the array with specified <paramref name="arrayPath"/>
        /// </summary>
        /// <param name="arrayPath">The relative path to get the array at</param>
        /// <param name="nameRelativePath">The name relative path to display in popup</param>
        public ArrayElementAttribute(string arrayPath, bool notNull = false) {
            ArrayPath = arrayPath;
            NotNull = notNull;
        }

        /// <summary>
        /// Show this element as a popup with values from the array with specified <paramref name="arrayPath"/>
        /// </summary>
        /// <param name="arrayPath">The relative path to get the array at</param>
        /// <param name="nameRelativePath">The name relative path to display in popup</param>
        /// <param name="notNull">Whether this element is allowed to not have a value or not</param>
        public ArrayElementAttribute(string arrayPath, string nameRelativePath, bool notNull = false)
        {
            ArrayPath = arrayPath;
            NotNull = notNull;
            NamePath = nameRelativePath;
        }
    }
}