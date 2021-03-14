namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;

    [Serializable]
    [DiscreteState("Material State")]
    public class MaterialState : BaseDiscreteState
    {

        public bool materialEnabled;
        public Material material;


        private bool _valueUpdated = false;
        private MeshRenderer meshRenderer;

        public override bool UseOwnerEvents
        {
            get
            {
                return false;
            }
        }


        public override void Awake()
        {
            base.Awake();
            meshRenderer = Owner.GetComponent<MeshRenderer>();
        }

        public override void OnStateEnter(BaseDiscreteState fromState)
        {
            _valueUpdated = false;

            if (UseAnimator && AnimatorParameter.name != null)
            {
                ApplyValueUpdate();
                AnimatorParameter.SetValue(Owner.animator);
            }
            else
            {
                if (materialEnabled)
                {
                    if (meshRenderer != null)
                    {
                        meshRenderer.material = material;
                    }
                }
            }
        }

        public override void OnStateExit(BaseDiscreteState toState)
        {

        }

        protected override void ApplyValueUpdate()
        {
            if (!_valueUpdated)
            {
                _valueUpdated = true;
                base.ApplyValueUpdate();
            }
        }


    }
}