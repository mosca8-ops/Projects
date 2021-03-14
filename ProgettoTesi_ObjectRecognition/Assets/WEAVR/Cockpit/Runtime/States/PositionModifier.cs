using System;

namespace TXT.WEAVR.Cockpit
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;

    [ModifierState("Position Modifier")]
    public class PositionModifier : BaseModifier
    {
        protected override float Tolerance => 0.001f;

        public override bool OnPointerDrag(PointerEventData eventData)
        {
            if (eventData.dragging)
            {
                
                float maxDelta = _useDeltaX ? eventData.delta.x : eventData.delta.y;
                float nextValue = ModifiedValue + maxDelta;
                float wStep = (MaxLimit - MinLimit) / 20.0f;
                nextValue = Mathf.Clamp(nextValue, - wStep, + wStep);
                if (Math.Abs(nextValue) > Tolerance)
                { 
                    ModifiedValue += nextValue;
                }
                return true;
            }
            return false;
        }

        protected override void UpdateScene()
        {
            float wDelta = ModifiedValue - PreviousModifiedValue;
            switch (m_ModifierAxis)
            {
                case ModifierAxis.X:
                    //Owner.transform.Translate(ModifiedValue - Owner.transform.localPosition.x, 0, 0);
                    Owner.transform.localPosition = new Vector3(ModifiedValue, Owner.transform.localPosition.y, Owner.transform.localPosition.z);
                    break;
                case ModifierAxis.Y:
                    //Owner.transform.Translate(0, ModifiedValue - Owner.transform.localPosition.y, 0);
                    Owner.transform.localPosition = new Vector3(Owner.transform.localPosition.x, ModifiedValue, Owner.transform.localPosition.z);
                    break;
                case ModifierAxis.Z:
                    //Owner.transform.Translate(0, 0, ModifiedValue - Owner.transform.localPosition.z);
                    Owner.transform.localPosition = new Vector3(Owner.transform.localPosition.x, Owner.transform.localPosition.y, ModifiedValue);
                    break;
            }
        }

    }
}