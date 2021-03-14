namespace TXT.WEAVR.Cockpit
{
    using UnityEngine;

    [StateDrawer(typeof(PositionModifier))]
    public class PositionModifierDrawer : BaseModifierDrawer
    {
        private PositionModifier _modifier;

        public override void SetTargets(params BaseState[] targets)
        {
            base.SetTargets(targets);

            _modifier = (PositionModifier) Target;
        }

    }
}