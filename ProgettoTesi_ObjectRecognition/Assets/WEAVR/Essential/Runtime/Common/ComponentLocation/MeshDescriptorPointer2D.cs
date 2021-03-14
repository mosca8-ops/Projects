using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Component Location/Mesh Descriptor Pointer 2D")]
    public class MeshDescriptorPointer2D : AbstractMeshDescriptorPointer
    {
        [Draggable]
        public Camera rayCamera;
        public override bool Active => Input.touchCount > 0 || Input.mousePresent;

        public override bool TriggerDown => Input.touchCount > 0 ? Input.GetTouch(0).phase == TouchPhase.Began : Input.GetMouseButton(0);

        public override Ray GetRay() => Input.touchCount > 0 ? rayCamera.ScreenPointToRay(Input.GetTouch(0).position) : rayCamera.ScreenPointToRay(Input.mousePosition);

        public override void SetRayDistance(float maxRayDistance)
        {
            
        }
    }
}