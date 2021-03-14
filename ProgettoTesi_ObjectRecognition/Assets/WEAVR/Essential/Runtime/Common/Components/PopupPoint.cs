using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.UI
{

    [AddComponentMenu("WEAVR/Components/Popup Point")]
    public class PopupPoint : MonoBehaviour
    {
        [Draggable]
        [CanBeGenerated("Start")]
        public Transform origin;
        [Draggable]
        [CanBeGenerated("End")]
        public Transform point;

        private void Awake()
        {
            if(origin == null)
            {
                origin = transform;
            }
            if(point == null)
            {
                point = transform;
            }
        }
    }
}
