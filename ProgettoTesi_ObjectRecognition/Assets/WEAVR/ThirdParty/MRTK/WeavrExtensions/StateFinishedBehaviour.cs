#if  WEAVR_EXTENSIONS_MRTK && TO_TEST 
namespace TXT.WEAVR.InteractionUI
{
    using UnityEngine;

    public delegate void EndStateAction();

    public class StateFinishedBehaviour : StateMachineBehaviour
    {
        public EndStateAction actionToPerform;

        // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (actionToPerform != null)
            {
                actionToPerform();
            }
        }
    }
}
#endif
