namespace TXT.WEAVR.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public class AnimatorParameter
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

        public AnimatorParameter(AnimatorControllerParameter parameter) {
            hashId = parameter.nameHash;
            name = parameter.name;
            type = parameter.type;

            boolValue = type == AnimatorControllerParameterType.Bool ? parameter.defaultBool : false;
            numericValue = type == AnimatorControllerParameterType.Float || type == AnimatorControllerParameterType.Int ?
                           parameter.defaultFloat : 0;
        }

        public AnimatorParameter(AnimatorControllerParameter parameter, float value) {
            hashId = parameter.nameHash;
            name = parameter.name;
            type = parameter.type;

            boolValue = type == AnimatorControllerParameterType.Bool ? value != 0 : false;
            numericValue = type == AnimatorControllerParameterType.Float || type == AnimatorControllerParameterType.Int ?
                           value : 0;
        }

        public AnimatorParameter(AnimatorControllerParameter parameter, bool value) {
            hashId = parameter.nameHash;
            name = parameter.name;
            type = parameter.type;

            boolValue = type == AnimatorControllerParameterType.Bool ? value : false;
            numericValue = type == AnimatorControllerParameterType.Float || type == AnimatorControllerParameterType.Int ?
                           (value ? 1 : 0) : 0;
        }

        public AnimatorParameter Update(AnimatorControllerParameter parameter) {
            hashId = parameter.nameHash;
            name = parameter.name;
            type = parameter.type;

            return this;
        }
    }
}