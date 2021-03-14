namespace TXT.WEAVR.Maintenance
{
    using System;
    using System.Collections;
    using TXT.WEAVR.Common;
    using TXT.WEAVR.Core;
    using TXT.WEAVR.Interaction;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Animations;


#if WEAVR_VR
    using Valve.VR;
    using Valve.VR.InteractionSystem;
#endif

    [Stateless]
    [AddComponentMenu("WEAVR/Interactions/Basic/Grabbable", 0)]
    [RequireComponent(typeof(InteractionController))]
    public class Grabbable : AbstractGrabbable, IVR_Poser
#if WEAVR_VR
        , IVR_Attachable
#endif
    {

        [Flags]
        public enum AttachmentFlags
        {
            SnapOnAttach = 1 << 0, // The object should snap to the position of the specified attachment point on the hand.
            DetachOthers = 1 << 1, // Other objects attached to this hand will be detached.
            DetachFromOtherHand = 1 << 2, // This object will be detached from the other hand.
            ParentToHand = 1 << 3, // The object will be parented to the hand.
            VelocityMovement = 1 << 4, // The object will attempt to move to match the position and rotation of the hand.
            TurnOnKinematic = 1 << 5, // The object will not respond to external physics.
            TurnOffGravity = 1 << 6, // The object will not respond to external physics.
            AllowSidegrade = 1 << 7, // The object is able to switch from a pinch grab to a grip grab. Decreases likelyhood of a good throw but also decreases likelyhood of accidental drop
        };

        public override BehaviourInteractionTrigger InteractTrigger => BehaviourInteractionTrigger.OnPointerDown;

        [Header("VR")]
        [SerializeField]
        protected bool m_holdButtonForGrab = true;
        [SerializeField]
        [HiddenBy(nameof(m_holdButtonForGrab), hiddenWhenTrue: true)]
        protected bool m_hoverBlocksRelease;

        public bool throwable = true;

        [SerializeField]
        protected bool m_snapOnAttach = true;
        //[EnumFlags]
        //[SerializeField]
        protected AttachmentFlags m_attachFlags = AttachmentFlags.DetachOthers
                                                | AttachmentFlags.DetachFromOtherHand
                                                | AttachmentFlags.ParentToHand
                                                | AttachmentFlags.SnapOnAttach
                                                | AttachmentFlags.TurnOffGravity
                                                | AttachmentFlags.TurnOnKinematic;
        [HideInInspector]
        public bool m_handIsFree = false;

        public event Func<bool> IsHoveringOtherObjects;

        protected override void OnReleaseInternal()
        {
#if WEAVR_VR
            if (m_lastAttachedHand != null)
            {
                m_lastAttachedHand.DetachObject(gameObject, true);
                if (m_lastAttachedHand.GetType() == typeof(VR_Hand))
                {
                    VR_Hand wHand = (VR_Hand)m_lastAttachedHand;
                    wHand.StopAttachmentPointOverride();
                }
                if (throwable)
                {
                    var rb = GetComponent<Rigidbody>();
                    if (rb)
                    {
                        rb.useGravity = true;
                    }
                }

                ControllerButtonHints.HideButtonHint(m_lastAttachedHand, Valve.VR.SteamVR_Input.GetAction<Valve.VR.SteamVR_Action_Boolean>("GrabGrip"));
                ControllerButtonHints.HideTextHint(m_lastAttachedHand, Valve.VR.SteamVR_Input.GetAction<Valve.VR.SteamVR_Action_Boolean>("GrabGrip"));
                m_lastAttachedHand = null;

                //if(m_parentWhileAttached != transform.parent)
                //{
                //    transform.SetParent(m_parentWhileAttached, true);
                //}
            }
#endif
        }

#if WEAVR_VR


        //private Hand.AttachmentFlags attachmentFlags = Hand.defaultAttachmentFlags | (Hand.AttachmentFlags.SnapOnAttach) & (~Hand.AttachmentFlags.DetachOthers);
        private Coroutine m_hintCoroutine;
        private Hand m_lastAttachedHand;
        private FixedJoint m_fixedJoint;
        private bool m_grabDataReady;
        private Transform m_tempHoldTransform;

        protected float m_attachTime;
        protected Vector3 m_attachPosition;
        protected Quaternion m_attachRotation;
        protected Transform m_attachEaseInTransform;
        protected bool m_snapAttachEaseInCompleted = false;

        private bool m_canRelease;
        public VR_Skeleton_Poser m_SkeletonPoser = null;
        private PositionConstraint m_positionConstraint = null;
        private VR_Object m_VRObject = null;

        [HideInInspector]
        public Vector3 m_rotationAxis = new Vector3(0.0f, 1.0f, 0.0f);
        private bool m_swapToOtherHands;

        [HideInInspector]
        public Transform m_ControllerAttachmentPoint = null;
        [HideInInspector]
        public bool m_showControllerPreview = false;
        private const string c_ControllerAttachmentPointName = "ControllerAttachmentPoint";

        private Transform m_parentWhileAttached;

        public override bool CanInteractVR(ObjectsBag bag, object hand)
        {
            return CanInteractVR(bag, hand as Hand);
        }

        protected bool CanInteractVR(ObjectsBag bag, Hand hand)
        {
            return hand != null && hand.currentAttachedObject?.GetComponent<AbstractInteractionController>() == null;
        }

        public override bool UseStandardVRInteraction(ObjectsBag bag, object hand)
        {
            return true;
        }

        public override void Grab(bool highlight)
        {
            base.Grab(false);
            var controller = GetComponent<InteractionController>();
            if (controller.HoveringHand != null && controller.AttachedHand == null && CanInteractVR(controller.bagHolder.Bag, controller.HoveringHand))
            {
                InteractVR(controller.bagHolder.Bag, controller.HoveringHand);
            }
        }

        public override void InteractVR(ObjectsBag bag, object handObject)
        {
            base.InteractVR(bag, handObject);
            base.Grab(false);
            m_currentBag = bag;
            var hand = handObject as VR_Hand;
            if (hand != null)
            {
                if (hand.currentAttachedObject != gameObject)
                {
                    m_swapToOtherHands = IsGrabbed && m_lastAttachedHand != null && !ReferenceEquals(hand, m_lastAttachedHand);
                    StopAllCoroutines();
                    // Attach this object to the hand
                    m_lastAttachedHand = hand;
                    AttachToHand(hand);
                    m_canRelease = m_holdButtonForGrab;
                }
                else
                {
                    Release(bag);
                }
            }
        }

        private void AttachToHand(VR_Hand iHand)
        {
            bool wHasPose = m_SkeletonPoser != null;
            if (m_positionConstraint == null)
            {
                m_VRObject?.SetupObjectToHandInteraction(wHasPose);
                if (m_snapOnAttach)
                {
                    m_attachFlags = m_attachFlags | AttachmentFlags.SnapOnAttach;
                }
                else
                {
                    m_attachFlags = m_attachFlags & ~AttachmentFlags.SnapOnAttach;
                }
                var wAttachmentFlags = (Hand.AttachmentFlags)m_attachFlags;
                if (wHasPose)
                {
                    iHand.StartAttachmentPointOverride(m_SkeletonPoser.GetAttachmentPoint(iHand.handType, true), true);
                    iHand.AttachObject(gameObject, iHand.GetGrabStarting(), wAttachmentFlags);
                }
                else
                {
                    iHand.AttachObject(gameObject, iHand.GetGrabStarting(), wAttachmentFlags, m_ControllerAttachmentPoint);
                }
            }
            else
            {
                m_VRObject?.SetupHandToObjectInteraction(wHasPose);
                if (wHasPose)
                {
                    if (m_handIsFree)
                    {
                        iHand.StartAttachmentPointOverride(m_SkeletonPoser.GetAttachmentPoint(iHand.handType, false), false, m_rotationAxis);
                    }
                    else
                    {
                        iHand.StartAttachmentPointOverride(m_SkeletonPoser.GetAttachmentPoint(iHand.handType, false), false);
                    }
                }
                iHand.AttachObject(gameObject, iHand.GetGrabStarting(), Hand.AttachmentFlags.DetachOthers);
            }
        }


        //-------------------------------------------------
        // Called every Update() while this GameObject is attached to the hand
        //-------------------------------------------------
        private void HandAttachedUpdate(Hand hand)
        {
            //if (!Controller.enabled) { return; }
            //if (hand.GetStandardInteractionButtonUp() || ((hand.controller != null) && hand.controller.GetPressUp(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger)))
            if (!transform.IsChildOf(hand.transform))
            {
                m_parentWhileAttached = transform.parent;
            }
            if (m_holdButtonForGrab)
            {
                if (!VR_ControllerManager.GetStandardInteractionButton(hand))
                {
                    // Detach ourselves late in the frame.
                    // This is so that any vehicles the player is attached to
                    // have a chance to finish updating themselves.
                    // If we detach now, our position could be behind what it
                    // will be at the end of the frame, and the object may appear
                    // to teleport behind the hand when the player releases it.
                    StartCoroutine(LateDetach(hand));
                }
            }
            else if (enabled)
            {
                if (VR_ControllerManager.GetStandardInteractionButtonDown(hand) && base.InteractTrigger == BehaviourInteractionTrigger.OnPointerDown)
                {
                    if (!m_hoverBlocksRelease || !IsHoveringOthers(hand))
                    {
                        StartCoroutine(LateDetach(hand));
                    }
                }
                else if (VR_ControllerManager.GetStandardInteractionButtonUp(hand) && base.InteractTrigger == BehaviourInteractionTrigger.OnPointerUp)
                {
                    if (m_canRelease && (!m_hoverBlocksRelease || !IsHoveringOthers(hand)))
                    {
                        StartCoroutine(LateDetach(hand));
                    }
                    else
                    {
                        m_canRelease = true;
                    }
                }
            }
        }

        private bool IsHoveringOthers(Hand hand)
        {
            if (hand.hoveringInteractable)
            {
                var controller = hand.hoveringInteractable.GetComponent<InteractionController>();
                return !controller || controller.IsVRInteractable(hand);
            }
            return IsHoveringOtherObjects != null && IsHoveringOtherObjects();
        }

        private IEnumerator LateDetach(Hand hand)
        {
            yield return new WaitForEndOfFrame();
            Release();
            StopAllCoroutines();
        }

        private void OnDetachedFromHand(Hand hand)
        {
            if (!Controller.enabled) { return; }

            if (!throwable)
            {
                Release();
                return;
            }

            if (m_swapToOtherHands)
            {
                m_swapToOtherHands = false;
                return;
            }

            Rigidbody rigidBody = GetComponent<Rigidbody>();
            if (rigidBody != null)
            {
                rigidBody.isKinematic = false;
                rigidBody.interpolation = RigidbodyInterpolation.Interpolate;
            }

            Vector3 position = Vector3.zero;
            Vector3 velocity = Vector3.zero;
            Vector3 angularVelocity = Vector3.zero;
            if (hand == null)
            {
                FinishEstimatingVelocity();
                velocity = GetVelocityEstimate();
                angularVelocity = GetAngularVelocityEstimate();
                position = transform.position;
            }
            else
            {
                hand.GetEstimatedPeakVelocities(out velocity, out angularVelocity);
                position = hand.transform.position;
            }

            if (rigidBody != null)
            {
                //Vector3 r = transform.TransformPoint(rigidBody.centerOfMass) - position;
                //rigidBody.velocity = velocity + Vector3.Cross(angularVelocity, r);
                //rigidBody.angularVelocity = angularVelocity;

                rigidBody.velocity = velocity;
                rigidBody.angularVelocity = angularVelocity;
            }

            // Make the object travel at the release velocity for the amount
            // of time it will take until the next fixed update, at which
            // point Unity physics will take over

            ////float timeUntilFixedUpdate = (Time.fixedDeltaTime + Time.fixedTime) - Time.time;
            ////transform.position += timeUntilFixedUpdate * velocity;
            ////float angle = Mathf.Rad2Deg * angularVelocity.magnitude;
            ////Vector3 axis = angularVelocity.normalized;
            ////transform.rotation *= Quaternion.AngleAxis(angle * timeUntilFixedUpdate, axis);

            //HideReleaseHint(hand, Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger, "Release");
            Release();
        }


        private void RetrieveControllerPreview()
        {
            if (!m_ControllerAttachmentPoint)
            {
                m_ControllerAttachmentPoint = transform.Find("ControllerAttachmentPoint");
            }
            RemoveControllerPreview();
        }

        public void ShowControllerPreview()
        {
            if (m_ControllerAttachmentPoint)
            {
                m_ControllerAttachmentPoint.hideFlags = HideFlags.None;
                m_ControllerAttachmentPoint.gameObject.SetActive(true);
            }
        }

        public void HideControllerPreview()
        {
            if (m_ControllerAttachmentPoint)
            {
                m_ControllerAttachmentPoint.hideFlags = HideFlags.HideInHierarchy;
                m_ControllerAttachmentPoint.gameObject.SetActive(false);
            }
        }

        public void RemoveControllerPreview()
        {
            if (m_ControllerAttachmentPoint)
            {
                Destroy(m_ControllerAttachmentPoint.GetComponent<MeshRenderer>());
                Destroy(m_ControllerAttachmentPoint.GetComponent<MeshFilter>());
            }
        }

        protected override void Start()
        {
            base.Start();
            m_VRObject = transform.GetComponent<VR_Object>();
            if (m_SkeletonPoser == null)
            {
                m_SkeletonPoser = transform.GetComponent<VR_Skeleton_Poser>();
            }
            else if (m_VRObject != null)
            {
                m_VRObject.skeletonPoser = m_SkeletonPoser;
            }
            m_positionConstraint = transform.GetComponent<PositionConstraint>();
            RetrieveControllerPreview();
        }

        public bool HasRotationAxis()
        {
            return m_handIsFree;
        }
        public bool IsHandParent()
        {
            return m_positionConstraint == null;
        }
        public VR_Skeleton_Poser GetSkeletonPoser()
        {
            return m_SkeletonPoser;
        }

        public Transform GetAttachmentPoint(Hand iHand)
        {
            if (GetSkeletonPoser() == null)
            {
                return m_ControllerAttachmentPoint;
            }
            else
            {
                return m_SkeletonPoser.GetAttachmentPoint(iHand.handType, HasRotationAxis());
            }

        }

        public SteamVR_Skeleton_JointIndexEnum GetFingerHoverIndex()
        {
            return SteamVR_Skeleton_JointIndexEnum.indexTip;
        }

        public VR_Object.HoveringMode GetHoveringMode()
        {
            return VR_Object.HoveringMode.Any;
        }


#if UNITY_EDITOR
        protected override void OnDestroy()
        {
            base.OnDestroy();
            Selection.selectionChanged -= this.HandleSelectionChanged;
            HideControllerPreview();
        }

        public void HandleSelectionChanged()
        {
            if (!this || !transform || !Selection.activeGameObject || !Selection.activeGameObject.transform ||
                (!System.Object.ReferenceEquals(Selection.activeGameObject.transform, transform) &&
                 !Selection.activeGameObject.transform.IsChildOf(transform)))
            {
                m_showControllerPreview = false;
                HideControllerPreview();
            }
        }
#endif //UNITY_EDITOR


#endif


    }
}
