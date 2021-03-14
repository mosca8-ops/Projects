using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Interaction;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    public abstract class AbstractMeshDescriptorPointer : MonoBehaviour, MeshDescriptionManager.IPointer
    {

        public abstract bool Active { get; }

        public abstract bool TriggerDown { get; }

        public abstract Ray GetRay();

        protected virtual void Start()
        {
            MeshDescriptionManager.Instance?.Register(this);
        }

        protected virtual void OnDestroy()
        {
            MeshDescriptionManager.Instance?.Unregister(this);
        }

        public abstract void SetRayDistance(float maxRayDistance);
    }
}