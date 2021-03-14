#if WEAVR_VR
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace TXT.WEAVR.Interaction
{
    public interface IVR_Attachable
    {

        bool HasRotationAxis();
        bool IsHandParent();
        Transform GetAttachmentPoint(Hand iHand);

    }
}
#endif