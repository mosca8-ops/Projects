using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.ImpactSystem
{
    [AddComponentMenu("WEAVR/Impact System/Object Material")]
    public class ObjectMaterial : AbstractObjectMaterial
    {
        [SerializeField]
        [Draggable]
        private ImpactMaterial m_material;
        [SerializeField]
        [Draggable]
        private Collider[] m_colliders;

        public ImpactMaterial Material => m_material;
        public override IEnumerable<Collider> Colliders => m_colliders;

        public bool HasCollider(Collider collider)
        {
            for (int i = 0; i < m_colliders.Length; i++)
            {
                if(m_colliders[i] == collider)
                {
                    return true;
                }
            }
            return false;
        }

        private void Reset()
        {
            OnValidate();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            m_colliders = GetComponentsInChildren<Collider>(true).Where(c => IsValid(c)).ToArray();
            if(m_material && m_material.Material)
            {
                for (int i = 0; i < m_colliders.Length; i++)
                {
                    if (Application.isPlaying)
                    {
                        m_colliders[i].material = m_material.Material;
                    }
                    else
                    {
                        m_colliders[i].sharedMaterial = m_material.Material;
                    }
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if(m_colliders == null || m_colliders.Length == 0)
            {
                m_colliders = GetComponentsInChildren<Collider>(true).Where(c => IsValid(c)).ToArray();
            }
        }

        private bool IsValid(Collider collider)
        {
            var objMat = collider.GetComponentInParent<ObjectMaterial>();
            return !objMat || objMat == this;
        }

        public override ImpactMaterial GetMaterial(Vector3 worldPoint) => Material;
    }
}
