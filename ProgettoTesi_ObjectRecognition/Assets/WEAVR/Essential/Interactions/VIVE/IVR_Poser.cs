using UnityEngine;
using TXT.WEAVR.Interaction;


namespace TXT.WEAVR.Interaction
{
    public interface IVR_Poser
    {
#if WEAVR_VR
        VR_Skeleton_Poser GetSkeletonPoser();

        Valve.VR.SteamVR_Skeleton_JointIndexEnum GetFingerHoverIndex();

        VR_Object.HoveringMode GetHoveringMode();

#endif
    }
}