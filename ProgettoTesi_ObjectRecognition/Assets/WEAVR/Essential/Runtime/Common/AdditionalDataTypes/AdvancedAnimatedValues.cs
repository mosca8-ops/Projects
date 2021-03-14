using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR
{
    [Serializable]
    public class AnimatedMaterial : AnimatedValue<Material>
    {
        private Material m_sample;

        protected override void OnStart()
        {
            base.OnStart();
            m_sample = new Material(m_startValue);
        }

        protected override Material Interpolate(Material a, Material b, float ratio) {
            m_sample.Lerp(a, b, ratio);
            return m_sample;
        }

        public static implicit operator AnimatedMaterial(Material value) {
            return new AnimatedMaterial() { m_target = value };
        }
    }
}
