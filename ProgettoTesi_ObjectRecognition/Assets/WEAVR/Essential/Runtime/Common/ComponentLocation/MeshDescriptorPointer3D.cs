using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Interaction;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Component Location/Mesh Descriptor Pointer 3D")]
    public class MeshDescriptorPointer3D : AbstractMeshDescriptorPointer
    {
        [Draggable]
        public WorldPointer pointer;

        public override bool Active => pointer.GetPointerEnabled();

        public override bool TriggerDown => pointer.GetPointerDown();

        public override Ray GetRay() => new Ray(pointer.transform.position, pointer.transform.forward);

        private void OnValidate()
        {
            if (!pointer)
            {
                pointer = GetComponent<WorldPointer>();
            }
        }

        protected override void Start()
        {
            if (!pointer)
            {
                pointer = GetComponent<WorldPointer>();
            }
            base.Start();
        }

        public override void SetRayDistance(float maxRayDistance)
        {
            if (pointer)
            {
                Debug.Log("Setting Ray Distance to " + maxRayDistance);
                pointer.maxDistance = maxRayDistance;
                pointer.RayDistance = maxRayDistance;
            }
        }
    }
}
