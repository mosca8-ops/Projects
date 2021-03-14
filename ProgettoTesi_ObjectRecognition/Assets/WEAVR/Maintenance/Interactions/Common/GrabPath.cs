namespace TXT.WEAVR.Maintenance
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using TXT.WEAVR.Common;
    using TXT.WEAVR.Interaction;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Animations;


#if WEAVR_VR
    using Valve.VR;
    using Valve.VR.InteractionSystem;
#endif

    [AddComponentMenu("WEAVR/Interactions/Basic/Grab Path", 0)]
    [RequireComponent(typeof(InteractionController))]
    [DefaultExecutionOrder(30000)]
    public class GrabPath : AbstractGrabbable, IVR_Poser
#if WEAVR_VR
        , IVR_Attachable
#endif
    {
        public delegate Vector3[] GetAnimationPositionDelegate(AnimationClip animationClip);
        public delegate bool AnimationAsARotationDelegate(AnimationClip animationClip);

        public static GetAnimationPositionDelegate GetAnimationPositionFunction;
        public static AnimationAsARotationDelegate AnimationAsARotationFunction;

        public override BehaviourInteractionTrigger InteractTrigger => BehaviourInteractionTrigger.OnPointerDown;

        [Space]
        [SerializeField]
        protected bool m_holdButtonForGrab = true;
        [SerializeField]
        [HiddenBy(nameof(m_holdButtonForGrab), hiddenWhenTrue: true)]
        protected bool m_hoverBlocksRelease;

        public bool throwable = true;

        [Header("VR")]
        [HideInInspector]
        public bool m_handIsFree = false;


        [Space]
        [SerializeField]
        public AnimationClip animationClip;
        [HideInInspector]
        public bool m_animationDontHaveARotation = false;
        [HiddenBy(nameof(m_animationDontHaveARotation))]
        [Tooltip("This parameter appears when you don't have any rotation in your animation, it sets the rotation that the object will have on the path all way long")]
        public Vector3 StartingRotation;
        [Tooltip("Personalize the path with the line renderer attach to the object")]
        public bool m_ShowThePath = false;
        [Tooltip("If true, a line will be displayed between the object and the entrance points of the path")]
        public bool ShowLinesWhenCloseToThePath = true;
        [HiddenBy(nameof(ShowLinesWhenCloseToThePath))]
        public string TextToDisplay = "Entrance of the path";
        [HiddenBy(nameof(ShowLinesWhenCloseToThePath))]
        [Tooltip("Select the material for the lines")]
        public Material lineMaterial;

        [Space]
        public bool m_StartOnThePath = false;
        [HiddenBy(nameof(m_StartOnThePath))]
        public float m_StartingPercentage = 0;
        public bool m_AllowEntranceAtTheBeginning = true;
        public bool m_AllowEntranceAtTheEnd = true;
        public bool m_CustomEntrancePoint;
        [HiddenBy(nameof(m_CustomEntrancePoint))]
        public GameObject m_CustomEntrance;
        [HiddenBy(nameof(m_CustomEntrancePoint))]
        public float m_entranceValue;

        [Space]
        [Tooltip("You can only go on one way, no turn back allowed.")]
        public bool m_OneWayPath = false;
        [HideInInspector]
        public bool m_ShowReverse = false;
        [HiddenBy(nameof(m_ShowReverse))]
        public bool m_reverse = false;
        public bool m_FreezeAtTheEnd = false;
        public bool m_LockTheEndOfThePath = false;
        [HiddenBy(nameof(m_LockTheEndOfThePath))]
        public bool m_LockAllPath = false;
        [Tooltip("If true, the object will stay on the path when released ")]
        public bool m_StayOnThePath = false;
        private bool m_EndOfthePath = false;


        [HideInInspector]
        public bool m_HasSkeletonPoser = false;
        [Space]
        [HiddenBy(nameof(m_HasSkeletonPoser))]
        [Tooltip("If true your pose will follow the object on the path, including the rotations. If false the pose will remain on your controller")]
        public bool PoseFollowTheObject = false;


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
                ControllerButtonHints.HideButtonHint(m_lastAttachedHand, Valve.VR.SteamVR_Input.GetAction<Valve.VR.SteamVR_Action_Boolean>("GrabGrip"));
                ControllerButtonHints.HideTextHint(m_lastAttachedHand, Valve.VR.SteamVR_Input.GetAction<Valve.VR.SteamVR_Action_Boolean>("GrabGrip"));
                m_lastAttachedHand = null;
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

        [HideInInspector]
        public List<float> m_DistanceBetweenPoints;
        [HideInInspector]
        public int m_CheckPointsCount;
        [HideInInspector]
        public List<Vector3> m_DirectionTab;
        [SerializeField]
        [HideInInspector]
        public List<Vector3> m_checkPoints;
        [HideInInspector]
        public List<float> m_Timeframe;
        [HideInInspector]
        public Collider m_Collider;

        private List<Tuple<LineRenderer, TextMesh>> helpers;
        private Hand currentHand;
        private Vector3 PreviousHandPosition;
        private AnimationClip previousClip;
        private float m_totalDistance = 0;
        private float m_distanceWithPreviousPoint = 0;
        private int m_PassedCheckPoints = 0;
        private bool m_AnimationIsPlaying = false;
        private LineRenderer line;
        private Queue<float> m_LastClampedValues;
        private float percentageOfThisPart;
        private float m_delay = 0;
        private float m_LastValueAnimation = 0;
        private float median = 0;
        private float distance;
        private bool m_oneWay;
        private Vector3 heading;
        private Vector3 direction;
        private bool medianPlusCondition;
        private bool medianLessCondition;
        private bool m_JustLeftPath = false;
        private bool m_JustEnteredPath = false;
        private float progress = 0;
        private GameObject m_entrancePoint;
        private GameObject m_exitPoint;
        private Quaternion previousRotation;
        private Vector3 previousPosition = new Vector3(10000, 10000, 10000);


        private Vector3[] GetAnimationPosition()
        {
            return GetAnimationPositionFunction != null ? GetAnimationPositionFunction(animationClip) : null;
        }
        private bool AnimationAsARotation()
        {
            return AnimationAsARotationFunction != null ? AnimationAsARotationFunction(animationClip) : false;
        }

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
            m_JustLeftPath = false;
            m_JustEnteredPath = false;
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
                var wAttachmentFlags = Hand.AttachmentFlags.DetachOthers | Hand.AttachmentFlags.DetachFromOtherHand
                                                                        | Hand.AttachmentFlags.ParentToHand | Hand.AttachmentFlags.SnapOnAttach
                                                                        | Hand.AttachmentFlags.TurnOffGravity | Hand.AttachmentFlags.TurnOnKinematic;
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

        IEnumerator PlayOnPath(Hand hand)
        {
            yield return new WaitForEndOfFrame();
            PlayOnGuidedPath(hand);
        }

        IEnumerator DisableHelp()
        {
            yield return new WaitForEndOfFrame();

            for (int i = 0; i < helpers.Count(); i++)
            {
                helpers[i].Item1.enabled = false;
                helpers[i].Item2.characterSize = 0;
            }
        }

        private void Update()
        {
            //Draw lines between object and entrance point, if close to it
            if (m_isGrabbed && !m_AnimationIsPlaying && currentHand != null)
            {
                for (int i = 0; i < helpers.Count(); i++)
                {
                    if (Vector3.Distance(currentHand.transform.position, helpers[i].Item1.GetPosition(0)) < 0.5f)
                    {
                        helpers[i].Item1.SetPosition(1, currentHand.transform.position);
                        helpers[i].Item1.enabled = true;
                        helpers[i].Item2.characterSize = 0.03f;
                    }
                }
                StartCoroutine(DisableHelp());

                if (m_entrancePoint)
                    m_entrancePoint.transform.rotation = Quaternion.LookRotation(m_entrancePoint.transform.position - Camera.main.transform.position);
                if (m_exitPoint)
                    m_exitPoint.transform.rotation = Quaternion.LookRotation(m_exitPoint.transform.position - Camera.main.transform.position);
                if (m_CustomEntrance)
                    m_CustomEntrance.transform.rotation = Quaternion.LookRotation(m_CustomEntrance.transform.position - Camera.main.transform.position);
            }

            //Rotates the text mesh to the camera
        }

        //-------------------------------------
        // Called every Update() while this GameObject is attached to the hand
        //-------------------------------------------------
        private void HandAttachedUpdate(Hand hand)
        {
            currentHand = hand;

            if (!m_JustLeftPath)
            {
                if (!m_AnimationIsPlaying && IsGrabbed)
                {
                    //Check if the objects enters in the area of the differents starting points
                    if (m_CustomEntrancePoint && m_Collider.bounds.Contains(m_CustomEntrance.transform.position))
                        InitPath(hand, m_entranceValue);

                    else if (m_AllowEntranceAtTheEnd && m_Collider.bounds.Contains(m_exitPoint.transform.position))
                        InitPath(hand, 1);

                    else if (m_AllowEntranceAtTheBeginning && m_Collider.bounds.Contains(m_entrancePoint.transform.position))
                        InitPath(hand, 0);
                }

                else if (m_StartOnThePath && IsGrabbed && m_AnimationIsPlaying)
                {
                    m_StartOnThePath = false;
                    InitPath(hand, m_StartingPercentage);

                }
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
            else
            {
                if (VR_ControllerManager.GetStandardInteractionButtonDown(hand) && base.InteractTrigger == BehaviourInteractionTrigger.OnPointerDown)
                {
                    if ((!m_hoverBlocksRelease || hand.hoveringInteractable == null))
                    {
                        StartCoroutine(LateDetach(hand));
                    }
                }
                else if (VR_ControllerManager.GetStandardInteractionButtonUp(hand) && base.InteractTrigger == BehaviourInteractionTrigger.OnPointerUp)
                {
                    if (m_canRelease && (!m_hoverBlocksRelease || hand.hoveringInteractable == null))
                    {
                        StartCoroutine(LateDetach(hand));
                    }
                    else
                    {
                        m_canRelease = true;
                    }
                }
            }

            if (IsGrabbed && m_AnimationIsPlaying)
            {
                StartCoroutine(PlayOnPath(hand));
                (hand as VR_Hand).SwitchParentTransform(transform.gameObject, false);
            }
        }

        private IEnumerator LateDetach(Hand hand)
        {
            yield return new WaitForEndOfFrame();

            if (m_StayOnThePath)
            {
                m_AnimationIsPlaying = true;
                m_StartOnThePath = true;
                m_StartingPercentage = Mathf.Lerp(m_Timeframe[m_PassedCheckPoints], m_Timeframe[m_PassedCheckPoints + 1], percentageOfThisPart - m_delay * percentageOfThisPart);
            }
            else
                m_AnimationIsPlaying = false;

            StartCoroutine(DisableHelp());

            //We wait for this coroutine to stop before disabeling all coroutines
            if (m_JustLeftPath)
                yield return new WaitForSeconds(0.1f);

            m_JustLeftPath = false;
            m_JustEnteredPath = false;
            Release();
            StopAllCoroutines();
        }

        private void OnDetachedFromHand(Hand hand)
        {
            if (!Controller.enabled) { return; }

            if (!throwable) { return; }

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
                Vector3 r = transform.TransformPoint(rigidBody.centerOfMass) - position;
                rigidBody.velocity = velocity + Vector3.Cross(angularVelocity, r);
                rigidBody.angularVelocity = angularVelocity;
            }

            // Make the object travel at the release velocity for the amount
            // of time it will take until the next fixed update, at which
            // point Unity physics will take over
            float timeUntilFixedUpdate = (Time.fixedDeltaTime + Time.fixedTime) - Time.time;
            transform.position += timeUntilFixedUpdate * velocity;
            float angle = Mathf.Rad2Deg * angularVelocity.magnitude;
            Vector3 axis = angularVelocity.normalized;
            transform.rotation *= Quaternion.AngleAxis(angle * timeUntilFixedUpdate, axis);

            //HideReleaseHint(hand, Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger, "Release");
            Release();
        }

        private void RetrieveControllerPreview()
        {
            if (m_ControllerAttachmentPoint == null)
            {
                m_ControllerAttachmentPoint = transform.Find("ControllerAttachmentPoint");
            }
            RemoveControllerPreview();
        }

        public void ShowControllerPreview()
        {
            if (m_ControllerAttachmentPoint != null)
            {
                m_ControllerAttachmentPoint.hideFlags = HideFlags.None;
                m_ControllerAttachmentPoint.gameObject.SetActive(true);
            }
        }

        public void HideControllerPreview()
        {
            if (m_ControllerAttachmentPoint != null)
            {
                m_ControllerAttachmentPoint.hideFlags = HideFlags.HideInHierarchy;
                m_ControllerAttachmentPoint.gameObject.SetActive(false);
            }
        }

        public void RemoveControllerPreview()
        {
            if (m_ControllerAttachmentPoint != null)
            {
                Destroy(m_ControllerAttachmentPoint.GetComponent<MeshRenderer>());
                Destroy(m_ControllerAttachmentPoint.GetComponent<MeshFilter>());
            }
        }

        private void InitPath(Hand hand, float startingValue)
        {
            //We switch the parent, like this the object will not follow the hand anymore
            (hand as VR_Hand).SwitchParentTransform(transform.gameObject, false);

            //We check for the pose to follow or not the object
            VR_Hand wHand = (VR_Hand)m_lastAttachedHand;
            if (PoseFollowTheObject && m_HasSkeletonPoser)
                wHand.StartAttachmentPointOverride(m_SkeletonPoser.GetAttachmentPoint(wHand.handType, true), true);
            else if (m_HasSkeletonPoser)
                wHand.StopAttachmentPointOverride();

            //We set up the position of the object on the path
            transform.rotation = Quaternion.Euler(StartingRotation);
            m_StartingPercentage = startingValue;
            StartingFromParticularPoint();

            //we calculate different parameters that will be usefull for the path's calculation
            heading = m_checkPoints[m_PassedCheckPoints + 1] - m_checkPoints[m_PassedCheckPoints];
            distance = heading.magnitude;
            direction = heading / distance;

            m_AnimationIsPlaying = true;
            StartCoroutine(EnteringPath());

        }

        IEnumerator EnteringPath()
        {
            //We do it twice to avoid bugs
            (currentHand as VR_Hand).SwitchParentTransform(transform.gameObject, false);

            //Guard in order to not left the path instantly
            m_JustEnteredPath = true;
            yield return new WaitForSeconds(0.7f);
            m_JustEnteredPath = false;
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

            //Will be usefull to calculate the median of the last movements
            m_LastClampedValues = new Queue<float>();
            for (int i = 0; i < 10; i++)
                m_LastClampedValues.Enqueue(0);


            //Preparing the lines renderers and the entrance points
            helpers = new List<Tuple<LineRenderer, TextMesh>>();
            if (m_AllowEntranceAtTheBeginning)
            {
                m_entrancePoint = new GameObject();
                m_entrancePoint.transform.position = m_checkPoints[0];

                if (ShowLinesWhenCloseToThePath)
                {
                    helpers.Add(Tuple.Create(m_entrancePoint.AddComponent<LineRenderer>(), m_entrancePoint.AddComponent<TextMesh>()));
                    helpers[helpers.Count - 1].Item1.SetPosition(0, m_checkPoints[0]);
                }
            }
            if (m_AllowEntranceAtTheEnd)
            {
                m_exitPoint = new GameObject();
                m_exitPoint.transform.position = m_checkPoints[m_CheckPointsCount - 1];

                if (ShowLinesWhenCloseToThePath)
                {
                    helpers.Add(Tuple.Create(m_exitPoint.AddComponent<LineRenderer>(), m_exitPoint.AddComponent<TextMesh>()));
                    helpers[helpers.Count - 1].Item1.SetPosition(0, m_checkPoints[m_CheckPointsCount - 1]);
                }
            }
            if (m_CustomEntrancePoint && ShowLinesWhenCloseToThePath)
            {
                helpers.Add(Tuple.Create(m_CustomEntrance.AddComponent<LineRenderer>(), m_CustomEntrance.AddComponent<TextMesh>()));
                helpers[helpers.Count - 1].Item1.SetPosition(0, m_CustomEntrance.transform.position);
            }

            //Setting up the parameters of the lines and the textMeshs
            if (ShowLinesWhenCloseToThePath)
            {
                for (int i = 0; i < helpers.Count(); i++)
                {
                    helpers[i].Item1.startWidth = 0f;
                    helpers[i].Item1.endWidth = 0.01f;
                    helpers[i].Item1.material = lineMaterial;
                    helpers[i].Item1.enabled = false;

                    helpers[i].Item2.text = TextToDisplay;
                    helpers[i].Item2.alignment = TextAlignment.Center;
                    helpers[i].Item2.anchor = TextAnchor.MiddleCenter;
                    helpers[i].Item2.characterSize = 0;
                }
            }

            if (m_OneWayPath)
            {
                if (m_reverse)
                    m_oneWay = false;
                else
                    m_oneWay = true;
            }
            else
            {
                m_oneWay = false;
                m_reverse = false;
            }

            //We destroy the line renderer during the game if he is hide, otherwise when you over the object the line renderer is also shown
            if (!m_ShowThePath)
                Destroy(GetComponent<LineRenderer>());
        }

        IEnumerator ExitingPath(float time)
        {
            //Guard in order to not go back in the past instantly
            m_JustLeftPath = true;
            yield return new WaitForSeconds(time);
            m_JustLeftPath = false;
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
                return m_ControllerAttachmentPoint;
            else
                return m_SkeletonPoser.GetAttachmentPoint(iHand.handType, HasRotationAxis());
        }

        public SteamVR_Skeleton_JointIndexEnum GetFingerHoverIndex()
        {
            return SteamVR_Skeleton_JointIndexEnum.indexTip;
        }

        public VR_Object.HoveringMode GetHoveringMode()
        {
            return VR_Object.HoveringMode.Any;
        }
        private float CalculatePercentageToNextPoint(Vector3 handPosition)
        {
            float distanceBetweenPoints = m_DistanceBetweenPoints[Mathf.Clamp(m_PassedCheckPoints, 0, m_CheckPointsCount - 2)];

            //We get the direction of the movement
            var v = handPosition - m_checkPoints[m_PassedCheckPoints];
            var d = Vector3.Dot(v, direction);

            //We calculate the projected hand on the path, then we calculate the distance with the previous and the next points,
            //and finally we recalculate the projected hand by clamping it, in order to not have a distance outside of bounds of the path 
            Vector3 projectedHand = m_checkPoints[m_PassedCheckPoints] + direction * d;
            m_distanceWithPreviousPoint = Vector3.Distance(m_checkPoints[m_PassedCheckPoints], projectedHand);

            projectedHand = Vector3.Lerp(m_checkPoints[m_PassedCheckPoints], m_checkPoints[m_PassedCheckPoints + 1], Mathf.Clamp01(m_distanceWithPreviousPoint / distanceBetweenPoints));
            m_distanceWithPreviousPoint = Vector3.Distance(m_checkPoints[m_PassedCheckPoints], projectedHand);

            //We decrement delay in order to make it smooth
            m_delay *= 0.96f;

            //We check for the angles of the hand and the different directions, if it is negative we have to go in the other direction, but only if it is going to the previous point
            if (Vector3.Dot(v, m_DirectionTab[m_PassedCheckPoints]) < 0f)
            {
                if (Mathf.Clamp01(m_distanceWithPreviousPoint / distanceBetweenPoints) * Vector3.Dot(v, m_DirectionTab[m_PassedCheckPoints]) < percentageOfThisPart)
                    return Mathf.Clamp01(m_distanceWithPreviousPoint / distanceBetweenPoints) * Math.Sign(Vector3.Dot(v, m_DirectionTab[m_PassedCheckPoints]));
                else
                    return 0;
            }

            //We return the percentage between the two checkpoints
            return Mathf.Clamp01(m_distanceWithPreviousPoint / distanceBetweenPoints);
        }
        void PlayOnGuidedPath(Hand hand)
        {
            Vector3 handPosition = hand.transform.position;
            //We suppress noise
            if (Vector3.Distance(PreviousHandPosition, handPosition) > 0.001 && !m_EndOfthePath)
            {
                PreviousHandPosition = handPosition;

                percentageOfThisPart = CalculatePercentageToNextPoint(handPosition);

                //We calculate the progress in TIME of the object on the path. We don't forget to take in consideration the delay
                progress = Mathf.Lerp(m_Timeframe[m_PassedCheckPoints], m_Timeframe[m_PassedCheckPoints + 1], percentageOfThisPart - (m_delay * percentageOfThisPart));

                //We return if we didn't move / go in the other way of what have been set
                if (percentageOfThisPart == 0 || (m_oneWay && progress < m_LastValueAnimation) || (m_reverse && progress > m_LastValueAnimation))
                    return;

                animationClip.SampleAnimation(transform.gameObject, progress);

                //We calculate the median of the last values, it allows us to not instantly uncross a checkpoint after crossing it
                if (m_LastValueAnimation != progress)
                {
                    median = 0;
                    m_LastValueAnimation = progress;

                    if (m_LastClampedValues.Count > 10)
                        m_LastClampedValues.Dequeue();

                    m_LastClampedValues.Enqueue(m_LastValueAnimation);

                    foreach (float value in m_LastClampedValues)
                        median += value;

                    median /= m_LastClampedValues.Count;

                    medianLessCondition = m_LastClampedValues.Count > 5 && median < progress;
                    medianPlusCondition = m_LastClampedValues.Count > 5 && median > progress;
                }

                //We check for the progression of the path and the direction of the movement with the help of the median conditions
                if (!m_JustLeftPath && !m_oneWay && percentageOfThisPart <= 0.01f && medianPlusCondition)
                {
                    if (m_PassedCheckPoints != 0)
                        CrossCheckPoint(-1, handPosition);
                    else if (m_PassedCheckPoints == 0 && !m_LockAllPath && !m_JustEnteredPath)
                        PathExit(hand);
                }

                else if (!m_reverse && percentageOfThisPart >= 0.99 && medianLessCondition)
                {
                    if (m_PassedCheckPoints != m_checkPoints.Count - 2)
                        CrossCheckPoint(1, handPosition);
                    else if (m_PassedCheckPoints == m_checkPoints.Count - 2 && !m_JustEnteredPath)
                    {
                        if (m_FreezeAtTheEnd)
                            m_EndOfthePath = true;
                        else if (!m_LockTheEndOfThePath)
                            PathExit(hand);
                    }
                }
            }
        }
        private void PathExit(Hand hand)
        {
            if (!m_JustLeftPath)
            {
                //We put back the object under the hand
                (hand as VR_Hand).SwitchParentTransform(transform.gameObject, true);
                StartCoroutine(ExitingPath(0.7f));

                //If pose there is, we put it back to
                if (hand.HasSkeleton())
                {
                    VR_Hand wHand = (VR_Hand)m_lastAttachedHand;
                    wHand.StopAttachmentPointOverride();
                }

                m_AnimationIsPlaying = false;
            }
        }
        private void CrossCheckPoint(int quotient, Vector3 handPosition)
        {
            m_PassedCheckPoints += quotient;

            direction = m_DirectionTab[m_PassedCheckPoints];

            m_LastClampedValues.Clear();

            //We calculate the projection of the hand to create a delay, like this the object doesn't jump to much on the path
            var v = handPosition - m_checkPoints[m_PassedCheckPoints];
            var d = Vector3.Dot(v, direction);
            Vector3 projectedHand = m_checkPoints[m_PassedCheckPoints] + direction * d;

            //The delay change regarding the checkpoint we are crossing
            if (quotient > 0)
                m_delay = (Mathf.Clamp01(Vector3.Distance(projectedHand, m_checkPoints[m_PassedCheckPoints]) / m_DistanceBetweenPoints[m_PassedCheckPoints]) * 0.90f) * quotient;
            else
                m_delay = (Mathf.Clamp01(Vector3.Distance(projectedHand, m_checkPoints[m_PassedCheckPoints + 1]) / m_DistanceBetweenPoints[m_PassedCheckPoints + 1]) * 0.90f) * quotient;
        }

        private void StartingFromParticularPoint()
        {
            m_AnimationIsPlaying = true;

            m_StartingPercentage = Mathf.Clamp(m_StartingPercentage, 0, 1);

            //We find the closestLowerCheckPoints inside the time frame, in order to find the starting check point
            int index = 0;
            if (m_StartingPercentage != 0)
            {
                var closestLowerCheckpoint = m_Timeframe.Where(n => n <= m_StartingPercentage * animationClip.length).DefaultIfEmpty().Max();
                index = m_Timeframe.IndexOf(closestLowerCheckpoint);

                //Some bugs returns -1 which makes the program to crash
                if (index < 0)
                    index = 0;
            }

            animationClip.SampleAnimation(transform.gameObject, animationClip.length * m_StartingPercentage);


            m_PassedCheckPoints = index;

            if (m_PassedCheckPoints == m_DirectionTab.Count)
                m_PassedCheckPoints--;

            if (m_reverse)
                m_LastValueAnimation = m_Timeframe[m_PassedCheckPoints + 1];
            else
                m_LastValueAnimation = m_Timeframe[m_PassedCheckPoints];
        }
        private void DrawPath()
        {
            //Draw the path of the animation with the help of the checkPoints
            line = GetComponent<LineRenderer>();
            if (line == null)
            {
                line = gameObject.AddComponent<LineRenderer>();
                line.material = new Material(Shader.Find("Transparent/Diffuse"));
                line.startWidth = 0.03f;
                line.endWidth = 0.05f;
            }

            line.positionCount = m_checkPoints.Count;
            line.SetPositions(m_checkPoints.ToArray());
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
            if (Selection.activeGameObject == null ||
                (!System.Object.ReferenceEquals(Selection.activeGameObject.transform, transform) &&
                 !Selection.activeGameObject.transform.IsChildOf(transform)))
            {
                m_showControllerPreview = false;
                HideControllerPreview();
            }
        }
        void OnValidate()
        {

            m_Collider = GetComponent<Collider>();
            if (m_Collider == null)
                throw new System.Exception("Your object needs to have a collider to works !");

            var skeleton = GetComponent<VR_Skeleton_Poser>();
            if (skeleton == null)
                m_HasSkeletonPoser = false;
            else
                m_HasSkeletonPoser = true;

            //Check if we are in the Editor, and the animationClip is getting change
            if (Application.isEditor && !Application.isPlaying && previousClip != animationClip)
            {
                //We reset all the Lists for the new animation
                if (m_checkPoints == null)
                    m_checkPoints = new List<Vector3>();
                else
                    m_checkPoints.Clear();

                if (m_Timeframe == null)
                    m_Timeframe = new List<float>();
                else
                    m_Timeframe.Clear();

                if (m_DistanceBetweenPoints == null)
                    m_DistanceBetweenPoints = new List<float>();
                else
                    m_DistanceBetweenPoints.Clear();

                if (m_DirectionTab == null)
                    m_DirectionTab = new List<Vector3>();
                else
                    m_DirectionTab.Clear();


                previousClip = animationClip;

                Vector3[] generatedPoints = GetAnimationPosition();

                m_animationDontHaveARotation = !AnimationAsARotation();

                if (generatedPoints == null)
                    throw new System.ArgumentNullException("Your animation need to change the position of your object");

                //We split the animation length in 100 parts
                for (int i = 0; i < 100; i++)
                    m_Timeframe.Add(animationClip.length * (i / 100f));

                int timeFrameIndex = 0;
                Vector3 direction = Vector3.zero;

                //First we reduce the array by taking only the directions alteration
                for (int i = 1; i < 100; i++)
                {
                    //If there is more than one axis changing we create a checkpoint, which allows us to detect nodes/direction change
                    var heading = generatedPoints[i] - generatedPoints[i - 1];
                    var distance = heading.magnitude;
                    var tempDirection = heading / distance;

                    if (direction != tempDirection)
                    {
                        m_checkPoints.Add(generatedPoints[i - 1]);
                        direction = tempDirection;
                        m_DirectionTab.Add(tempDirection);
                        m_Timeframe[timeFrameIndex] = m_Timeframe[i - 1];
                        timeFrameIndex++;
                    }
                }

                //We take all of the last elements of the tabs
                m_Timeframe[timeFrameIndex] = m_Timeframe[m_Timeframe.Count - 1];
                m_Timeframe.RemoveRange(timeFrameIndex + 1, m_Timeframe.Count - timeFrameIndex - 1);
                m_checkPoints.Add(generatedPoints[99]);

                if (generatedPoints.Length == 0)
                    throw new System.ArgumentNullException("You need to give an animation with at least a translation");

                m_CheckPointsCount = m_checkPoints.Count;

                //Removing points that are to close
                for (int i = m_CheckPointsCount - 1; i > 0; i--)
                {
                    var distanceBetweenPoints = Vector3.Distance(m_checkPoints[i - 1], m_checkPoints[i]);
                    m_totalDistance += distanceBetweenPoints;

                    if (distanceBetweenPoints < 0.2f)
                    {
                        m_checkPoints.RemoveAt(i - 1);
                        m_Timeframe.RemoveAt(i - 1);
                        m_DirectionTab.RemoveAt(i - 1);
                    }
                    else
                        m_DistanceBetweenPoints.Add(distanceBetweenPoints);
                }
                //Debug purposes

                //for (int i = 0; i < m_DirectionTab.Count; i++)
                //    Debug.Log(m_DirectionTab[i]);

                //for (int i = 0; i < m_Timeframe.Count; i++)
                //    Debug.Log("Time frame" + m_Timeframe[i]);

                //for (int i = 0; i < m_DistanceBetweenPoints.Count; i++)
                //    Debug.Log("Distance between points" + m_DistanceBetweenPoints[i]);

                m_DistanceBetweenPoints.Reverse();
                m_CheckPointsCount = m_checkPoints.Count;

                DrawPath();
            }

            if (m_ShowThePath)
            {
                if (line == null && m_checkPoints.Count > 0)
                    DrawPath();
                else
                    line.enabled = true;
            }
            else if (line != null)
                line.enabled = false;

            //We save the last know rotation in order to 
            if (m_StartOnThePath)
            {
                if (previousPosition == new Vector3(10000, 10000, 10000))
                {
                    previousPosition = transform.position;
                    previousRotation = transform.rotation;
                }

                transform.rotation = Quaternion.Euler(StartingRotation);
                StartingFromParticularPoint();
            }
            else if (previousPosition != new Vector3(10000, 10000, 10000) && previousPosition != transform.position)
            {
                transform.position = previousPosition;
                transform.rotation = previousRotation;

                previousPosition = new Vector3(10000, 10000, 10000);
            }

            if (m_OneWayPath)
                m_ShowReverse = true;
            else
                m_ShowReverse = false;
        }

#endif //UNITY_EDITOR


#endif


    }
}