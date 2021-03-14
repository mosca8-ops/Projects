using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Components/Override Outline")]
    public class OverrideOutline : MonoBehaviour
    {
        [Draggable]
        public Renderer[] toOutline;
        
    }
}
