namespace TXT.WEAVR.Cockpit
{

    [StateDrawer(typeof(RotaryModifier))]
    public class RotaryModifierDrawer : BaseModifierDrawer
    {
        private RotaryModifier _modifier;

        public override void SetTargets(params BaseState[] targets) {
            base.SetTargets(targets);

            _modifier = (RotaryModifier)Target;
        }

    }
}