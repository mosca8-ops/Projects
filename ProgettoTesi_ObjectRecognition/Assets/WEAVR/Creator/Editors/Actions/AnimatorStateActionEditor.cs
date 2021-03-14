using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;
using System.Linq;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(SetAnimatorStateAction), true)]
    class AnimatorStateActionEditor : ActionEditor
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            var action = target as SetAnimatorStateAction;
            if (action)
            {
                action.GetAllStatesCallback = GetStates;
                //var animator = action.Target as Animator;
                //if (animator && !Application.isPlaying)
                //{
                //    var runtimeController = animator.runtimeAnimatorController;
                //    if (runtimeController)
                //    {
                //        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(runtimeController));
                //        if (controller && runtimeController.name == controller.name)
                //        {
                //            //action.ParametersNames = controller.parameters;
                //            //Debug.Log($"[Updating controller] {controller.name} vs {runtimeController.name}");
                //            //animator.runtimeAnimatorController = controller;
                //            //action.OnValidate();
                //            //action.ParametersNames = controller.parameters.Select(p => p.name).ToArray();
                //        }
                //    }
                //}
            }
        }

        private string[][] GetStates(Animator animator)
        {
            var animatorController = GetAnimatorController(animator);
            if (!animatorController)
            {
                return new string[0][];
            }

            var layers = animatorController.layers;
            string[][] value = new string[layers.Length][];
            
            for (int i = 0; i < layers.Length; i++)
            {
                var layer = layers[i];
                value[i] = layer.stateMachine.states.Select(s => s.state.name).ToArray();
            }
            //var layer = animatorController.layers.Where(l => l.name == m_layerName).FirstOrDefault();

            //m_stateIndex = layer.syncedLayerIndex;

            //List<string> statesNames = new List<string>();
            //foreach (var childAnimatorState in layer.stateMachine.states)
            //{
            //    statesNames.Add(childAnimatorState.state.name);
            //}
            return value;
        }

        private AnimatorController GetAnimatorController(Animator animator)
        {
            var runtimeController = animator.runtimeAnimatorController;
            if (runtimeController)
            {
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(runtimeController));
                if (controller && runtimeController.name == controller.name)
                {
                    return controller;
                }
            }
            return null;
        }
    }
}
