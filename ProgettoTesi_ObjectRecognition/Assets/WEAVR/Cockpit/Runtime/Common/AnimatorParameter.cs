namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public struct AnimatorParameter
    {
        public AnimatorControllerParameterType type;
        public int hashId;
        public string name;
        public float numericValue;
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

        public void Update(AnimatorControllerParameter parameter) {
            hashId = parameter.nameHash;
            name = parameter.name;
            type = parameter.type;
        }

        public void UpdateValue(bool value) {
            boolValue = value;
        }

        public void UpdateValue(float value) {
            numericValue = value;
        }

        public void SetValue(Animator animator) {
            switch (type) {
                case AnimatorControllerParameterType.Bool:
                    animator.SetBool(hashId, boolValue);
                    break;
                case AnimatorControllerParameterType.Float:
                    animator.SetFloat(hashId, numericValue);
                    break;
                case AnimatorControllerParameterType.Int:
                    animator.SetInteger(hashId, (int)numericValue);
                    break;
                case AnimatorControllerParameterType.Trigger:
                    animator.SetTrigger(hashId);
                    break;
            }
        }

        #region [  FOR LEGACY COMPATIBILITY  ]

        public AnimatorParameter(Common.AnimatorParameter legacyParameter) {
            hashId = legacyParameter.hashId;
            name = legacyParameter.name;
            type = legacyParameter.type;

            boolValue = legacyParameter.boolValue;
            numericValue = legacyParameter.numericValue;
        }

        public void CopyFromLegacy(Common.AnimatorParameter legacyParameter) {
            hashId = legacyParameter.hashId;
            name = legacyParameter.name;
            type = legacyParameter.type;

            boolValue = legacyParameter.boolValue;
            numericValue = legacyParameter.numericValue;
        }

        #endregion
    }
}
