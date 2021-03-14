using System.Collections;
using TXT.WEAVR.Interaction;
using UnityEngine;


#if WEAVR_VR
using Valve.VR;
using Valve.VR.InteractionSystem;
#endif

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Interactions/Doors/Hinge Door")]
    [RequireComponent(typeof(InteractionController))]
    public class HingeDoor : 
        AbstractHingeDoor
#if WEAVR_VR
        ,IVR_Poser, IVR_Attachable
#endif
    {

        public override bool CanBeDefault => true;

#if WEAVR_VR

        [Tooltip("The output angle value of the drive in degrees, unlimited will increase or decrease without bound, take the 360 modulus to find number of rotations")]
        [ShowAsReadOnly]
        [SerializeField]
        protected float m_outAngle;

        [Space]
        [SerializeField]
        protected bool m_maintainMomemntum = true;
        [SerializeField]
        [HiddenBy(nameof(m_maintainMomemntum))]
        protected float m_momemtumDampenRate = 5.0f;

        protected int m_numMappingChangeSamples = 5;
        protected float[] m_mappingChangeSamples;
        protected float m_mappingChangeRate;
        protected int m_sampleCount = 0;

        private Vector3 m_worldPlaneNormal = new Vector3(1.0f, 0.0f, 0.0f);
        private Vector3 m_localPlaneNormal = new Vector3(1.0f, 0.0f, 0.0f);

        private Vector3 m_lastHandProjected;

        private bool m_driving = false;

        // If the drive is limited as is at min/max, angles greater than this are ignored 
        private float m_minMaxAngularThreshold = 10.0f;
        private Hand m_handHoverLocked = null;
        public VR_Skeleton_Poser m_SkeletonPoser = null;
        public bool m_handIsFree = false;
        private VR_Object m_VRObject;

        protected override void Awake()
        {
            base.Awake();
            m_mappingChangeSamples = new float[m_numMappingChangeSamples];
        }

        public override bool CanInteractVR(ObjectsBag bag, object hand)
        {
            return base.CanInteractVR(bag, hand) && !IsLocked;
        }

        public override void InteractVR(ObjectsBag currentBag, object hand)
        {
            StopDoorMove();
            StartDriving(hand as Hand);
        }

        protected override void StopInteractionVR(AbstractInteractiveBehaviour nextBehaviour)
        {
            StopDriving(m_handHoverLocked);
        }

        //-------------------------------------------------
        protected override void Start()
        {
            base.Start();

            m_worldPlaneNormal = m_rotationAxis;

            m_localPlaneNormal = m_worldPlaneNormal;

            if (transform.parent)
            {
                m_worldPlaneNormal = transform.parent.localToWorldMatrix.MultiplyVector(m_worldPlaneNormal).normalized;
            }

            m_outAngle = m_limits.Denormalize(m_currentOpening);

            m_VRObject = GetComponent<VR_Object>();
            if (m_SkeletonPoser == null)
            {
                m_SkeletonPoser = transform.GetComponent<VR_Skeleton_Poser>();
            }
            else if (m_VRObject != null)
            {
                m_VRObject.skeletonPoser = m_SkeletonPoser;
            }
            UpdateAll();
        }


        //-------------------------------------------------
        void OnDisable()
        {
            if (m_handHoverLocked)
            {
                StopDriving(m_handHoverLocked);
            }
        }

        protected override void UpdateState()
        {
            base.UpdateState();
            if (IsClosed)
            {
                m_mappingChangeRate = 0;
            }
            else if (m_maintainMomemntum && m_mappingChangeRate != 0.0f)
            {
                //Dampen the mapping change rate and apply it to the mapping
                m_mappingChangeRate = Mathf.Lerp(m_mappingChangeRate, 0.0f, m_momemtumDampenRate * Time.deltaTime);
                CurrentOpenProgress = Mathf.Clamp01(m_currentOpening + (m_mappingChangeRate * Time.deltaTime));

                AnimateDoorMovement(m_currentOpening);
            }
        }

        protected override void AnimateDoorMovement(float progress)
        {
            base.AnimateDoorMovement(progress);
            m_outAngle = m_limits.Denormalize(progress);
        }

        //-------------------------------------------------
        private IEnumerator HapticPulses(Hand iHand, float flMagnitude, int nCount)
        {
            if (iHand != null)
            {
                int nRangeMax = (int)Util.RemapNumberClamped(flMagnitude, 0.0f, 1.0f, 100.0f, 900.0f);
                nCount = Mathf.Clamp(nCount, 1, 10);

                for (ushort i = 0; i < nCount; ++i)
                {
                    ushort duration = (ushort)Random.Range(100, nRangeMax);
                    iHand.TriggerHapticPulse(duration);
                    yield return new WaitForSeconds(.01f);
                }
            }
        }

        protected void CalculateMappingChangeRate()
        {
            //Compute the mapping change rate
            m_mappingChangeRate = 0.0f;
            int mappingSamplesCount = Mathf.Min(m_sampleCount, m_mappingChangeSamples.Length);
            if (mappingSamplesCount != 0)
            {
                for (int i = 0; i < mappingSamplesCount; ++i)
                {
                    m_mappingChangeRate += m_mappingChangeSamples[i];
                }
                m_mappingChangeRate /= mappingSamplesCount;
            }
        }


        private void HideHints(Hand hand)
        {
            ControllerButtonHints.HideButtonHint(hand, hand.uiInteractAction);
            ControllerButtonHints.HideTextHint(hand, hand.uiInteractAction);
            ControllerButtonHints.HideTextHint(hand, hand.grabGripAction);
            ControllerButtonHints.HideButtonHint(hand, hand.grabGripAction);
            ControllerButtonHints.HideTextHint(hand, hand.grabPinchAction);
            ControllerButtonHints.HideButtonHint(hand, hand.grabPinchAction);
        }

        //-------------------------------------------------
        private void OnHandHoverEnd(Hand hand)
        {
            ControllerButtonHints.HideTextHint(hand, hand.grabGripAction);

            if (m_driving && VR_ControllerManager.GetStandardInteractionButton(hand))
            {
                StartCoroutine(HapticPulses(hand, 1.0f, 10));
            }

            m_driving = false;
            m_handHoverLocked = null;
        }


        //-------------------------------------------------
        private void HandHoverUpdate(Hand hand)
        {

            if (!enabled || !gameObject.activeInHierarchy)
            {
                m_mappingChangeRate = 0;
                StopDriving(hand);
                return;
            }

            if (VR_ControllerManager.GetStandardInteractionButtonUp(hand))
            {
                // Trigger was just released
                StopDriving(hand);
            }
            else if (m_driving && VR_ControllerManager.GetStandardInteractionButton(hand))
            {
                //Debug.Log($"Driving = {m_driving}");
                ComputeAngle(hand);
                UpdateAll();
            }
        }

        private void StopDriving(Hand hand)
        {
            m_driving = false;

            if (hand == null) return;

            RegisterAction(DoorAction.EndInteraction);

            if (m_handHoverLocked == hand)
            {
                hand.HoverUnlock(GetComponent<Interactable>());
                m_handHoverLocked = null;
            }

            hand.DetachObject(gameObject);
            if (hand.GetType() == typeof(VR_Hand))
            {
                VR_Hand wHand = (VR_Hand)hand;
                wHand.StopAttachmentPointOverride();
                //wHand.RestoreControllerPose();

            }
            CalculateMappingChangeRate();
        }

        private void StartDriving(Hand hand)
        {
            if (hand == null) return;

            RegisterAction(DoorAction.StartInteraction);

            if (transform.parent)
            {
                m_worldPlaneNormal = transform.TransformDirection(m_rotationAxis).normalized;
            }

            m_lastHandProjected = ComputeToTransformProjected(hand.hoverSphereTransform);

            hand.HoverLock(GetComponent<Interactable>());
            m_handHoverLocked = hand;
            m_driving = true;
            if (hand.GetType() == typeof(VR_Hand) && m_SkeletonPoser != null)
            {
                VR_Hand wHand = (VR_Hand) hand;
                if (m_handIsFree)
                {
                    wHand.StartAttachmentPointOverride(m_SkeletonPoser.GetAttachmentPoint(hand.handType, false), false, m_rotationAxis);
                }
                else
                {
                    wHand.StartAttachmentPointOverride(m_SkeletonPoser.GetAttachmentPoint(hand.handType, false), false);
                }
            }

            m_VRObject?.SetupHandToObjectInteraction(m_SkeletonPoser != null);
            hand.AttachObject(gameObject, hand.GetGrabStarting(), Hand.AttachmentFlags.DetachOthers);
            HideHints(hand);

            ComputeAngle(hand);
            UpdateAll();
        }

        //-------------------------------------------------
        private Vector3 ComputeToTransformProjected(Transform xForm)
        {
            Vector3 toTransform = (xForm.position - transform.position).normalized;
            Vector3 toTransformProjected = new Vector3(0.0f, 0.0f, 0.0f);

            // Need a non-zero distance from the hand to the center of the CircularDrive
            if (toTransform.sqrMagnitude > 0.0f)
            {
                toTransformProjected = Vector3.ProjectOnPlane(toTransform, m_worldPlaneNormal).normalized;
            }
            else
            {
                Debug.LogFormat("The collider needs to be a minimum distance away from the CircularDrive GameObject {0}", gameObject.ToString());
                Debug.Assert(false, string.Format("The collider needs to be a minimum distance away from the CircularDrive GameObject {0}", gameObject.ToString()));
            }

            return toTransformProjected;
        }

        //-------------------------------------------------
        // Updates the LinearMapping value from the angle
        //-------------------------------------------------
        private void UpdateLinearMapping()
        {
            // Map it to a [0, 1] value
            float prevMapping = m_currentOpening;
            CurrentOpenProgress = m_limits.Normalize(m_outAngle);

            m_mappingChangeSamples[m_sampleCount % m_mappingChangeSamples.Length] = (1.0f / Time.deltaTime) * (m_currentOpening - prevMapping);
            m_sampleCount++;
        }


        //-------------------------------------------------
        // Updates the Debug TextMesh with the linear mapping value and the angle
        //-------------------------------------------------
        private void UpdateAll()
        {
            UpdateLinearMapping();
            AnimateDoorMovement(m_currentOpening);
        }

        //-------------------------------------------------
        // Computes the angle to rotate the game object based on the change in the transform
        //-------------------------------------------------
        private void ComputeAngle(Hand hand)
        {
            Vector3 toHandProjected = ComputeToTransformProjected(hand.hoverSphereTransform);

            if (!toHandProjected.Equals(m_lastHandProjected))
            {
                float absAngleDelta = Vector3.Angle(m_lastHandProjected, toHandProjected);

                if (absAngleDelta > 0.0f)
                {
                    Vector3 cross = Vector3.Cross(m_lastHandProjected, toHandProjected).normalized;
                    float dot = Vector3.Dot(m_worldPlaneNormal, cross);

                    float signedAngleDelta = absAngleDelta;

                    if (dot < 0.0f)
                    {
                        signedAngleDelta = -signedAngleDelta;
                    }

                    float angleTmp = m_limits.Clamp(m_outAngle + signedAngleDelta);

                    if (m_outAngle == m_limits.min)
                    {
                        if (angleTmp > m_limits.min && absAngleDelta < m_minMaxAngularThreshold)
                        {
                            m_outAngle = angleTmp;
                            m_lastHandProjected = toHandProjected;
                        }
                    }
                    else if (m_outAngle == m_limits.max)
                    {
                        if (angleTmp < m_limits.max && absAngleDelta < m_minMaxAngularThreshold)
                        {
                            m_outAngle = angleTmp;
                            m_lastHandProjected = toHandProjected;
                        }
                    }
                    else if (angleTmp == m_limits.min)
                    {
                        m_outAngle = angleTmp;
                        m_lastHandProjected = toHandProjected;
                    }
                    else if (angleTmp == m_limits.max)
                    {
                        m_outAngle = angleTmp;
                        m_lastHandProjected = toHandProjected;
                    }
                    else
                    {
                        m_outAngle = angleTmp;
                        m_lastHandProjected = toHandProjected;
                    }
                }
            }
        }

        public bool HasRotationAxis()
        {
            return m_handIsFree;
        }
        public bool IsHandParent()
        {
            return false;
        }
        public VR_Skeleton_Poser GetSkeletonPoser()
        {
            return m_SkeletonPoser;
        }

        public SteamVR_Skeleton_JointIndexEnum GetFingerHoverIndex()
        {
            return SteamVR_Skeleton_JointIndexEnum.indexTip;
        }

        public VR_Object.HoveringMode GetHoveringMode()
        {
            return VR_Object.HoveringMode.Any;
        }

        public Transform GetAttachmentPoint(Hand iHand)
        {
            return m_SkeletonPoser?.GetAttachmentPoint(iHand.handType, HasRotationAxis());
        }
#endif
    }
}
