using System;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using TXT.WEAVR.Maintenance;

#if WEAVR_VR
using Valve.VR.InteractionSystem;
using System.Collections;
#endif

namespace TXT.WEAVR.Interaction
{
    [AddComponentMenu("WEAVR/VR/Interactions/Knob")]
    [RequireComponent(typeof(InteractionController))]
    public class VR_Knob : AbstractVR_Knob
    {
        [Space]
        public UnityEventFloat onValueChanged;
        public UnityEventInt onCheckpointReached;

        protected override void HandleCheckpointReached(int iCheckpointIdx)
        {
            onCheckpointReached.Invoke(iCheckpointIdx);
        }

        protected override void HandleValueChanged(float iValue)
        {
            onValueChanged.Invoke(iValue);
        }
    }
}

