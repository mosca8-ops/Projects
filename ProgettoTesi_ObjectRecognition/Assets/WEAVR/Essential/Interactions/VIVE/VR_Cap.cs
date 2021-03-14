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
    [AddComponentMenu("WEAVR/VR/Interactions/Cap")]
    [RequireComponent(typeof(Grabbable))]
    public class VR_Cap : AbstractVR_Knob
    {
#if WEAVR_VR
        private Grabbable mGrabbable = null;

        protected override void HandleCheckpointReached(int iCheckpointIdx)
        {
            if (mGrabbable)
            {
                mGrabbable.Grab(false);
            }
        }

        protected override void HandleValueChanged(float iValue)
        {
            //Nothing to do
        }


        protected override void Start()
        {
            base.Start();
            mGrabbable = transform.GetComponent<Grabbable>();
        }
#else
        protected override void HandleCheckpointReached(int iCheckpointIdx)
        {

        }

        protected override void HandleValueChanged(float iValue)
        {
            //Nothing to do
        }
#endif
    }
}

