using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Components/Minimap")]
    public class Minimap : MonoBehaviour
    {
        [Header("World")]
        public Transform target;
        public Transform minPoint;
        public Transform maxPoint;

        [Header("Minimap")]
        public Transform minimapTarget;
        public Transform minimapMinPoint;
        public Transform minimapMaxPoint;
        
        void Update()
        {
            var world = minPoint.InverseTransformPoint(maxPoint.position);
            var minimap = minimapMinPoint.InverseTransformPoint(minimapMaxPoint.position);

            var scale = new Vector3(
                world.x != 0 ? minimap.x / world.x : 0,
                world.y != 0 ? minimap.y / world.y : 0,
                world.z != 0 ? minimap.z / world.z : 0
                );

            minimapTarget.position = minimapMinPoint.TransformPoint(Vector3.Scale(minPoint.InverseTransformPoint(target.position), scale));
        }
    }
}
