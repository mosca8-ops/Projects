#if  WEAVR_VR
//======= Copyright (c) Valve Corporation, All rights reserved. ===============

using System;
using System.Collections;
using UnityEngine;
using Valve.VR;
using TXT.WEAVR.Utility;

namespace TXT.WEAVR.Interaction
{
    public static class VRSkeletonPoseExtensions
    {
        private static readonly string[] wHandNodesNames = new string[] {
            "Root",
            "wrist_r",
            "finger_thumb_0_r",
            "finger_thumb_1_r",
            "finger_thumb_2_r",
            "finger_thumb_r_end",
            "finger_index_meta_r",
            "finger_index_0_r",
            "finger_index_1_r",
            "finger_index_2_r",
            "finger_index_r_end",
            "finger_middle_meta_r",
            "finger_middle_0_r",
            "finger_middle_1_r",
            "finger_middle_2_r",
            "finger_middle_r_end",
            "finger_ring_meta_r",
            "finger_ring_0_r",
            "finger_ring_1_r",
            "finger_ring_2_r",
            "finger_ring_r_end",
            "finger_pinky_meta_r",
            "finger_pinky_0_r",
            "finger_pinky_1_r",
            "finger_pinky_2_r",
            "finger_pinky_r_end",
            "finger_thumb_r_aux",
            "finger_index_r_aux",
            "finger_middle_r_aux",
            "finger_ring_r_aux",
            "finger_pinky_r_aux"
        };

        public static bool BonesFromFbx(this SteamVR_Skeleton_Pose iPose, Transform iFbx)
        {
            iPose.leftHand.ignoreRootPoseData  = false;
            iPose.leftHand.ignoreWristPoseData = false;
            iPose.leftHand.bonePositions = new Vector3[wHandNodesNames.Length];
            iPose.leftHand.boneRotations = new Quaternion[wHandNodesNames.Length];
            iPose.leftHand.thumbFingerMovementType  = SteamVR_Skeleton_FingerExtensionTypes.Static;
            iPose.leftHand.indexFingerMovementType  = SteamVR_Skeleton_FingerExtensionTypes.Static;
            iPose.leftHand.middleFingerMovementType = SteamVR_Skeleton_FingerExtensionTypes.Static;
            iPose.leftHand.ringFingerMovementType   = SteamVR_Skeleton_FingerExtensionTypes.Static;
            iPose.leftHand.pinkyFingerMovementType  = SteamVR_Skeleton_FingerExtensionTypes.Static;

            iPose.rightHand.ignoreRootPoseData  = false;
            iPose.rightHand.ignoreWristPoseData = false;
            iPose.rightHand.bonePositions = new Vector3[wHandNodesNames.Length];
            iPose.rightHand.boneRotations = new Quaternion[wHandNodesNames.Length];
            iPose.rightHand.thumbFingerMovementType  = SteamVR_Skeleton_FingerExtensionTypes.Static;
            iPose.rightHand.indexFingerMovementType  = SteamVR_Skeleton_FingerExtensionTypes.Static;
            iPose.rightHand.middleFingerMovementType = SteamVR_Skeleton_FingerExtensionTypes.Static;
            iPose.rightHand.ringFingerMovementType   = SteamVR_Skeleton_FingerExtensionTypes.Static;
            iPose.rightHand.pinkyFingerMovementType  = SteamVR_Skeleton_FingerExtensionTypes.Static;

            for (int wIdx = 0; wIdx < wHandNodesNames.Length; ++wIdx)
            {
                Transform wCurBone = iFbx.FindRecursiveDepthFirst(wHandNodesNames[wIdx]);
                if (wCurBone != null)
                {
                    iPose.leftHand.bonePositions[wIdx] = wCurBone.localPosition;
                    iPose.leftHand.boneRotations[wIdx] = wCurBone.localRotation;
                    iPose.rightHand.bonePositions[wIdx] = wCurBone.localPosition;
                    iPose.rightHand.boneRotations[wIdx] = wCurBone.localRotation;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
    }
}
#endif
