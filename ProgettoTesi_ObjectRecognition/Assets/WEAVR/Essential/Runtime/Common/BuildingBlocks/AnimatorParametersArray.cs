namespace TXT.WEAVR.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public class AnimatorParametersArray : IEnumerable<AnimatorParametersArray.AnimatorVariable>
    {
        [SerializeField]
        private AnimatorControllerParameterType _type;

        [SerializeField]
        public AnimatorControllerParameterType ParametersType {
            get {
                return _type;
            }
            set {
                if (_type != value) {
                    _type = value;
                    Clear();
                }
            }
        }

        [SerializeField]
        private List<AnimatorVariable> _parameters;

        [SerializeField]
        private bool _hashesStored;

        [SerializeField]
        public List<AnimatorVariable> Parameters {
            get {
                return _parameters;
            }
        }
        
        public void Clear() {
            _parameters.Clear();
        }

        public AnimatorParametersArray() {
            _parameters = new List<AnimatorVariable>();
            _type = AnimatorControllerParameterType.Float;
            _hashesStored = false;
        }

        public AnimatorVariable Add() {
            var param = new AnimatorVariable();
            _parameters.Add(param);
            _hashesStored = false;
            return param;
        }

        public void Add(AnimatorControllerParameter parameter) {
            var param = new AnimatorVariable();
            _parameters.Add(param.Update(parameter));
            _hashesStored = false;
        }

        public void Remove(AnimatorVariable variable) {
            _parameters.Remove(variable);
        }

        public void RemoveAt(int i) {
            _parameters.RemoveAt(i);
        }

        public bool Contains(string parameterName) {
            foreach(var parameter in _parameters) {
                if(parameter.name == parameterName) {
                    return true;
                }
            }
            return false;
        }

        public void StoreHashes() {
            foreach(var parameter in _parameters) {
                parameter.hashId = Animator.StringToHash(parameter.name);
            }
            _hashesStored = true;
        }

        public void ApplyValues(Animator animator) {
            if (!_hashesStored) {
                StoreHashes();
            }

            switch (ParametersType) {
                case AnimatorControllerParameterType.Bool:
                    SetBoolValues(animator);
                    break;
                case AnimatorControllerParameterType.Float:
                    SetFloatValues(animator);
                    break;
                case AnimatorControllerParameterType.Int:
                    SetIntegerValues(animator);
                    break;
                case AnimatorControllerParameterType.Trigger:
                    SetTriggers(animator);
                    break;
                default:
                    break;
            }
        }

        protected virtual void SetBoolValues(Animator animator) {
            foreach (var parameter in _parameters) {
                animator.SetBool(parameter.hashId, parameter.boolValue);
            }
        }

        protected virtual void SetFloatValues(Animator animator) {
            foreach (var parameter in _parameters) {
                animator.SetFloat(parameter.hashId, parameter.numericValue);
            }
        }

        protected virtual void SetIntegerValues(Animator animator) {
            foreach (var parameter in _parameters) {
                animator.SetInteger(parameter.hashId, (int)parameter.numericValue);
            }
        }

        protected virtual void SetTriggers(Animator animator) {
            foreach (var parameter in _parameters) {
                animator.SetTrigger(parameter.hashId);
            }
        }

        public IEnumerator<AnimatorVariable> GetEnumerator() {
            return _parameters.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return _parameters.GetEnumerator();
        }

        [Serializable]
        public class AnimatorVariable
        {
            [SerializeField]
            public AnimatorControllerParameterType type;
            [SerializeField]
            public int hashId;
            [SerializeField]
            public string name;
            [SerializeField]
            public float numericValue;
            [SerializeField]
            public bool boolValue;

            public AnimatorVariable Update(AnimatorControllerParameter parameter) {
                hashId = parameter.nameHash;
                name = parameter.name;
                type = parameter.type;

                return this;
            }
        }
    }
}
