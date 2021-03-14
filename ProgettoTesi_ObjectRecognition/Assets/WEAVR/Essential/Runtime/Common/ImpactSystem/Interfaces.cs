using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.ImpactSystem
{

    public interface IHitReceiver
    {
        float MinHitForce { get; }
        void AbsorbHit(object source, float force, ContactPoint point);
    }

    public interface IImpactMaterialProvider
    {
        bool HasMaterial(ImpactMaterial material);
        IEnumerable<ImpactMaterial> Materials { get; }
    }

    public interface IImpactEffect
    {
        void PerformEffect(Collision collision, ContactPoint point, Collider otherCollider);
    }
}
