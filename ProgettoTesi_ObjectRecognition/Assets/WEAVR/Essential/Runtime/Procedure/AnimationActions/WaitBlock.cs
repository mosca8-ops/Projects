using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    public class WaitBlock : BaseAnimationBlock
    {
        public override bool CanProvide<T>()
        {
            return false;
        }

        public override T Provide<T>()
        {
            return default;
        }

        protected override void Animate(float value, float normalizedValue)
        {
            
        }
    }
}