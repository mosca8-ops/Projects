using System;

namespace TXT.WEAVR.Cockpit
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;

    [ModifierState("Rotary Modifier")]
    public class RotaryModifier : BaseModifier
    {
        protected override float Tolerance => 0.001f;

        public override bool OnPointerDrag(PointerEventData eventData)
        {
            if (eventData.dragging)
            {
                float maxDelta = _useDeltaX ? eventData.delta.x : eventData.delta.y;
                float nextRotation = ModifiedValue - maxDelta;
                if (HasLimits)
                {
                    nextRotation = Mathf.Clamp(nextRotation, MinLimit, MaxLimit);
                }

                float deltaRotation = nextRotation - ModifiedValue;
                if (Math.Abs(deltaRotation) > Tolerance)
                {
                    ModifiedValue += deltaRotation;
                }
                return true;
            }
            return false;
        }


        protected override void UpdateScene()
        {
            switch (m_ModifierAxis)
            {
                case ModifierAxis.X:
                    //Owner.transform.RotateAround(Owner.transform.position, Owner.transform.right, ModifiedValue - Owner.transform.eulerAngles.x);
                    Owner.transform.eulerAngles = new Vector3(ModifiedValue, Owner.transform.eulerAngles.y, Owner.transform.eulerAngles.z);
                    break;
                case ModifierAxis.Y:
                    //Owner.transform.RotateAround(Owner.transform.position, Owner.transform.up, ModifiedValue - Owner.transform.eulerAngles.y);
                    Owner.transform.eulerAngles = new Vector3(Owner.transform.eulerAngles.x, ModifiedValue, Owner.transform.eulerAngles.z);
                    break;
                case ModifierAxis.Z:
                    //Owner.transform.RotateAround(Owner.transform.position, Owner.transform.forward, ModifiedValue - Owner.transform.eulerAngles.z);
                    Owner.transform.eulerAngles = new Vector3(Owner.transform.eulerAngles.x, Owner.transform.eulerAngles.y, ModifiedValue);
                    break;
            }
        }

    }
}