namespace TXT.WEAVR.InteractionUI
{
    using Common;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;

    public enum BehaviourState
    {
        Enter, 
        Update,
        Exit,
        Move
    }

    public class ChangeParametersBehaviour : StateMachineBehaviour
    {
        public BehaviourState changeOnState = BehaviourState.Exit;

        public AnimatorParametersArray boolParameters;
        public AnimatorParametersArray floatParameters;
        public AnimatorParametersArray intParameters;
        public AnimatorParametersArray triggers;

        // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            if (changeOnState == BehaviourState.Exit) {
                ApplyValues(animator);
            }
        }

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            if (changeOnState == BehaviourState.Enter) {
                ApplyValues(animator);
            }
        }

        public override void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            if (changeOnState == BehaviourState.Move) {
                ApplyValues(animator);
            }
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            if (changeOnState == BehaviourState.Update) {
                ApplyValues(animator);
            }
        }

        private void ApplyValues(Animator animator) {
            boolParameters.ApplyValues(animator);
            floatParameters.ApplyValues(animator);
            intParameters.ApplyValues(animator);
            triggers.ApplyValues(animator);
        }
    }
}
