using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;
using System.Linq;
using System;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(SetAnimatorValueAction), true)]
    class AnimatorParameterActionEditor : ActionEditor
    {
        private Animator m_animator;
        private AnimatorController m_controller;

        protected override void OnEnable()
        {
            base.OnEnable();
            var action = target as SetAnimatorValueAction;
            if (action)
            {
                action.GetParametersCallback = GetParameters;
                var animator = action.Target as Animator;
                m_animator = animator;
                if (animator)
                {
                    var runtimeController = animator.runtimeAnimatorController;
                    if (runtimeController)
                    {
                        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(runtimeController));
                        if (controller && runtimeController.name == controller.name)
                        {
                            m_controller = controller;
                            //action.ParametersNames = controller.parameters;
                            //Debug.Log($"[Updating controller] {controller.name} vs {runtimeController.name}");
                            //animator.runtimeAnimatorController = controller;
                            //action.OnValidate();
                            action.ParametersNames = controller.parameters.Select(p => p.name).ToArray();
                        }
                    }
                }
            }
        }

        private AnimatorControllerParameter[] GetParameters(Animator animator)
        {
            if(animator != m_animator)
            {
                m_animator = animator;
                if (m_animator && m_animator.runtimeAnimatorController)
                {
                    m_controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(m_animator.runtimeAnimatorController));
                }
                else
                {
                    m_controller = null;
                }
            }

            return m_controller ? m_controller.parameters : new AnimatorControllerParameter[0];
        }
    }
}
