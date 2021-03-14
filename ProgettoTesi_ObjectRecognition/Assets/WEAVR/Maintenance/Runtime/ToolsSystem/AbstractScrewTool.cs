using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Interaction;
using UnityEngine;

namespace TXT.WEAVR.MaintenanceTools
{

    public abstract class AbstractScrewTool : AbstractInteractiveBehaviour
    {
        [SerializeField]
        [CanBeGenerated("RotationTip")]
        private Transform m_rotationTip;

        public Transform RotationTip
        {
            get => m_rotationTip;
            set
            {
                if(m_rotationTip != value)
                {
                    m_rotationTip = value;
                }
            }
        }

        public override string GetInteractionName(ObjectsBag currentBag)
        {
            throw new System.NotImplementedException();
        }

        public override void Interact(ObjectsBag currentBag)
        {
            throw new System.NotImplementedException();
        }
    }
}
