#if  WEAVR_EXTENSIONS_MRTK && TO_TEST 
namespace TXT.WEAVR.InteractionUI
{
    using UnityEngine;


    public class SwitchButtonChangeStatus : StateMachineBehaviour
    {
        public SwitchButton switchButton;
        public bool statusON;

        private int _animatorStatus = Animator.StringToHash("Status");
        private int _animatorIsChanging = Animator.StringToHash("IsChanging");

        // OnStateEnter is called before OnStateEnter is called on any state inside this state machine
        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.SetBool(_animatorIsChanging, false);
            animator.SetBool(_animatorStatus, statusON);
            switchButton.IsON = statusON;
        }
    }
}
#endif
