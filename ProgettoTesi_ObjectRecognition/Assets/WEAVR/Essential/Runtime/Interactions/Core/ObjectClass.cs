namespace TXT.WEAVR.Interaction
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public struct ObjectClass
    {
        public string type;
    }

    [Serializable]
    public struct InputObjectClass
    {
        public string validType;
    }

    [Serializable]
    public class InputObjectClassArray
    {
        [SerializeField]
        private InputObjectClass[] _inputClasses;

        public InputObjectClassArray() {
            _inputClasses = new InputObjectClass[0];
        }

        public InputObjectClassArray(params InputObjectClass[] collection) {
            _inputClasses = collection;
        }

        private void AddClass(string @class)
        {
            for (int i = 0; i < _inputClasses.Length; i++)
            {
                //if(_inputClasses[i].validType == )
            }
        }

        public bool HasInputClass(string classType) {
            for (int i = 0; i < _inputClasses.Length; i++) {
                if(_inputClasses[i].validType == classType) {
                    return true;
                }
            }
            return false;
        }

        public bool HasInputClass(InputObjectClass @class) {
            return HasInputClass(@class.validType);
        }

        public bool HasInputClass(ObjectClass @class) {
            return HasInputClass(@class.type);
        }
    }
}