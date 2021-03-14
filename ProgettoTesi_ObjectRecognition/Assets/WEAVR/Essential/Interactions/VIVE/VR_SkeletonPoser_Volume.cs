using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Interaction
{
#if WEAVR_VR
    [Core.Stateless]
#endif
    [AddComponentMenu("WEAVR/VR/Interactions/Skeleton Poser Volume")]
    public class VR_SkeletonPoser_Volume : MonoBehaviour
    {
        public VR_Skeleton_Poser poser;
        public BoxCollider volume;
        public bool showController;

        private bool m_neededForCanvas;

        [ContextMenu("Make Colliders as Triggers")]
        private void MakeAllCollidersTriggers()
        {
            foreach(var coll in GetComponentsInChildren<Collider>(true))
            {
                coll.isTrigger = true;
            }
        }

#if WEAVR_VR
        private void Reset()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas && GetComponent<WorldPointerCanvas>())
            {
                poser = GetComponent<VR_Skeleton_Poser>();
                if (!poser)
                {
                    poser = gameObject.AddComponent<VR_Skeleton_Poser>();
                }

                if (!poser.skeletonMainPose)
                {
                    poser.skeletonMainPose = Resources.Load< Valve.VR.SteamVR_Skeleton_Pose>("VIVE/HandPoses/ReferencePose_PointSelect");
                }

                volume = GetComponent<BoxCollider>();
                if (!volume)
                {
                    var boxCollider = gameObject.AddComponent<BoxCollider>();
                    var rectTransform = transform as RectTransform;
                    boxCollider.center -= Vector3.forward * 0.2f / transform.localScale.z;
                    boxCollider.size = new Vector3(rectTransform.rect.width, rectTransform.rect.height, 0.4f / transform.localScale.z);
                    volume = boxCollider;
                }
                volume.isTrigger = true;
            }
        }

#endif

        private void OnValidate()
        {
            if (!volume)
            {
                volume = GetComponentInChildren<BoxCollider>();
            }
        }

#if WEAVR_VR

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(0.5f);
            if (UnityEngine.XR.XRSettings.enabled)
            {
                m_neededForCanvas = GetComponent<WorldPointerCanvas>();
                if (!poser)
                {
                    poser = GetComponent<VR_Skeleton_Poser>();
                }

                m_hands = Valve.VR.InteractionSystem.Player.instance.hands.Select(h => h as VR_Hand).ToArray();
            }
            else
            {
                volume.enabled = false;
                enabled = false;
            }
            m_restartCoroutine = null;
        }

        private RaycastHit[] m_raycastHits = new RaycastHit[8];
        private bool[] m_handsAreInside = new bool[2];
        private VR_Hand[] m_hands; // 0 - Left, 1 - Right
        private VR_Hand[] m_handsInside = new VR_Hand[2]; // 0 - Left, 1 - Right
        private VR_Hand[] m_newHandsInside = new VR_Hand[2]; // 0 - Left, 1 - Right
        private Coroutine m_restartCoroutine;

        private void Update()
        {
            if (!poser || m_hands == null || m_hands.Length < 2) { return; }
            if(!m_hands[0] && !m_hands[1])
            {
                if (m_restartCoroutine == null)
                {
                    OnDisable();
                    OnEnable();
                    m_restartCoroutine = StartCoroutine(Start());
                }
                return;
            }
            CheckHand(0);
            CheckHand(1);
        }

        private void CheckHand(int index)
        {
            if (m_handsAreInside[index] != volume.bounds.Contains(m_hands[index].transform.position))
            {
                if (m_handsAreInside[index])
                {
                    HandExited(m_hands[index]);

                }
                else
                {
                    HandEntered(m_hands[index]);
                }
                m_handsAreInside[index] = !m_handsAreInside[index];
            }
        }

        //private void FixedUpdate()
        //{
        //    if (!poser) { return; }
        //    var bounds = volume.bounds;
        //    int raycasts = Physics.BoxCastNonAlloc(bounds.center, bounds.extents, -transform.forward, m_raycastHits);

        //    VR_Hand hand = null;
        //    m_newHandsInside[0] = m_newHandsInside[1] = null;
        //    for (int i = 0; i < raycasts; i++)
        //    {
        //        hand = m_raycastHits[i].collider.GetComponentInParent<VR_Hand>();
        //        if (hand)
        //        {
        //            int handIndex = hand.handType == Valve.VR.SteamVR_Input_Sources.LeftHand ? 0 : 1;
        //            m_newHandsInside[handIndex] = hand;
        //            if(m_newHandsInside[0] && m_newHandsInside[1]) { break; }
        //        }
        //    }

        //    ChangeHandState(0);
        //    ChangeHandState(1);
        //}

        private void ChangeHandState(int index)
        {
            if(m_newHandsInside[index] == m_handsInside[index]) { return; }
            if (m_handsInside[index])
            {
                HandExited(m_handsInside[index]);
            }
            m_handsInside[index] = m_newHandsInside[index];
            if (m_handsInside[index])
            {
                HandEntered(m_handsInside[index]);
            }
        }

        private void HandEntered(VR_Hand hand)
        {
            if (!hand || hand.currentAttachedObject) { return; }
            if (hand.IsMenuHand)
                return;
            if (m_neededForCanvas)
            {
                hand.EnterMenuMode(poser);
            }
            else
            {
                if (!showController)
                {
                    hand.HideController();
                }
                hand.SetPoser(poser);
            }
        }

        private void HandExited(VR_Hand hand)
        {
            if (!hand) { return; }
            if (hand.IsMenuHand)
                return;
            if (m_neededForCanvas)
            {
                hand.ExitMenuMode();
               
            }
            else if (!hand.currentAttachedObject)
            {
                if (!showController)
                {
                    hand.ShowController();
                }
                hand.RestoreDefaultPose();
            }
        }

        private void OnEnable()
        {
            if(m_handsAreInside != null && m_handsAreInside.Length >= 2)
            {
                for (int i = 0; i < m_handsAreInside.Length; i++)
                {
                    m_handsAreInside[i] = false;
                }
            }
        }

        private void OnDisable()
        {
            if (!poser || m_hands == null || m_hands.Length < 2) { return; }
            if (m_handsAreInside[0]) HandExited(m_hands[0]);
            if (m_handsAreInside[1]) HandExited(m_hands[1]);
        }
#else
        private void Start()
        {
            volume.enabled = false;
            enabled = false;
        }
#endif
    }
}