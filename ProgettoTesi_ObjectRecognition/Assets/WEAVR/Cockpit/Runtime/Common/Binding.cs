namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Core;
    using UnityEngine;

    public enum BindingMode { Read, Write, Both }

    [Serializable]
    [HideInternals]
    public class Binding : ScriptableObject
    {
        public string id;
        public BindingMode mode;
        public GameObject dataSource;

        public Type type;

        [DoNotExpose]
        [PropertyPath]
        public string propertyPath;

        private Property _property;
        [DoNotExpose]
        public Property Property {
            get {
                if (_property == null && dataSource != null)
                {
                    _property = Property.Get(dataSource, propertyPath, "Simulation"); // TODO WeavrModules.Simulation);
                }
                return _property;
            }
        }


    }
}