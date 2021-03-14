using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Components/Billboard Element")]
    [RequireComponent(typeof(RectTransform))]
    public class BillboardElement : MonoBehaviour
    {
        [SerializeField]
        [ShowAsReadOnly]
        private int m_id = 0;

        public int ID
        {
            get => m_id;
            set
            {
                if(m_id != value)
                {
                    m_id = value;
                }
            }
        }

        private void OnValidate()
        {
            var billboard = GetComponentInParent<Billboard>();
            if (!billboard)
            {
                var parent = transform.parent;
                while (parent && !billboard)
                {
                    billboard = parent.GetComponent<Billboard>();
                    parent = parent.parent;
                }
            }
            if (billboard)
            {
                billboard.RefreshElements();
            }
        }
    }
}
