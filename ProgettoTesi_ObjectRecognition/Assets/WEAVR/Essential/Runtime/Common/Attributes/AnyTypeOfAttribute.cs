using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class AnyTypeOfAttribute : WeavrAttribute
    {
        public Type BaseType { get; private set; }
        public string InstantiationMethod { get; private set; }
        
        /// <summary>
        /// Accepts any type of object
        /// </summary>
        /// <param name="baseType">The valid type of the object</param>
        /// <param name="instantiationMethod">[Optional] The name of the method to instantiate the object. 
        /// Should return an object of the same specified type</param>
        public AnyTypeOfAttribute(Type baseType, string instantiationMethod = null)
        {
            BaseType = baseType;
            InstantiationMethod = instantiationMethod;
        }
    }
}
