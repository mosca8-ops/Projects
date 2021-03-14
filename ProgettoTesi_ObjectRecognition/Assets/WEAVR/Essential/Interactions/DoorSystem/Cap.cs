using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Interaction;
using UnityEngine;
using UnityEngine.Events;

#if WEAVR_VR
using Valve.VR.InteractionSystem;
#endif

namespace TXT.WEAVR.Common
{
    [RequireComponent(typeof(InteractionController))]
    [AddComponentMenu("")]
    public class Cap : AbstractCap
    {
        [SerializeField]
        protected bool m_installOnContact = true;

        [Space]
        [SerializeField]
        private bool m_debug = false;
        [SerializeField]
        [HiddenBy(nameof(m_debug))]
        private float m_debugPointSize = 0.05f;
        [SerializeField]
        [HiddenBy(nameof(m_debug))]
        private Color m_debugPointColor = Color.red;

        public override bool CanBeDefault => true;

        
#if WEAVR_VR

        [Tooltip("The output angle value of the drive in degrees, unlimited will increase or decrease without bound, take the 360 modulus to find number of rotations")]
        [ShowAsReadOnly]
        [SerializeField]
        protected float m_outAngle;

        protected float m_lastOutAngle;

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

        private bool m_wasJustRemoved = false;

        // If the drive is limited as is at min/max, angles greater than this are ignored 
        private float m_minMaxAngularThreshold = 10.0f;

        private Hand m_handHoverLocked = null;

        private Transform m_debugPoint;
        public Transform DebugPoint
        {
            get
            {
                if (m_debugPoint == null)
                {
                    m_debugPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
                    m_debugPoint.GetComponent<Renderer>().material.color = m_debugPointColor;
                    m_debugPoint.hideFlags = HideFlags.DontSave;
                    m_debugPoint.localScale = Vector3.one * m_debugPointSize;
                }
                return m_debugPoint;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            m_mappingChangeSamples = new float[m_numMappingChangeSamples];
        }

        private Valve.VR.ISteamVR_Action_In_Source m_trigger = Valve.VR.SteamVR_Input.GetAction<Valve.VR.SteamVR_Action_Boolean>("GrabGrip");

        private bool m_hintsAreShown;
        private InteractionController m_vrController;
        private InteractionController VRController => m_vrController;

        protected override void Start()
        {
            base.Start();

            OnRemoved.AddListener(() => m_wasJustRemoved = true);

            InteractTrigger = BehaviourInteractionTrigger.OnPointerDown;

            m_vrController = Controller as InteractionController;

            m_worldPlaneNormal = m_rotationAxis;

            m_localPlaneNormal = m_worldPlaneNormal;

            if (m_rotationPoint == null || m_rotationPoint == transform)
            {
                m_rotationPoint = transform;
                if (transform.parent)
                {
                    m_worldPlaneNormal = transform.localToWorldMatrix.MultiplyVector(m_worldPlaneNormal).normalized;
                }
            }
            else
            {
                m_worldPlaneNormal = m_rotationPoint.localToWorldMatrix.MultiplyVector(m_worldPlaneNormal).normalized;
            }

            m_outAngle = m_limits.Clamp(Vector3.Dot(transform.localEulerAngles, m_rotationAxis));

            UpdateAll();
        }

        protected override void UpdateState()
        {
            if (VRController.AttachedHand != null && !IsInstalled)
            {
                CanInstallWhileInHand(VRController.AttachedHand);
            }
            else
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
        }

        private bool CanInstallWhileInHand(Hand hand)
        {
            bool canInstall = Vector3.Distance(transform.position, m_installTarget.position) < m_installThreshold;
            if (!canInstall)
            {
                m_wasJustRemoved = false;
            }
            else if (m_wasJustRemoved)
            {
                canInstall = false;
            }
            if(canInstall && m_installOnContact)
            {
                if (m_hintsAreShown)
                {
                    ControllerButtonHints.HideTextHint(hand, m_trigger);
                    ControllerButtonHints.HideButtonHint(hand, m_trigger);

                    m_hintsAreShown = false;
                }
                if(VRController.CurrentBehaviour != this)
                {
                    VRController.StopCurrentInteraction();
                    IsInstalled = true;
                    InteractVR(VRController.bagHolder.Bag, hand);
                }
                return false;
            }
            else if (canInstall && !ControllerButtonHints.IsButtonHintActive(hand, m_trigger))
            {
                m_hintsAreShown = true;
                ControllerButtonHints.ShowTextHint(hand, m_trigger, "Install");

                return true;
            }
            else if (canInstall)
            {
                return true;
            }
            else if (!canInstall && m_hintsAreShown)
            {
                ControllerButtonHints.HideTextHint(hand, m_trigger);
                ControllerButtonHints.HideButtonHint(hand, m_trigger);

                m_hintsAreShown = false;
            }
            return false;
        }



        //-------------------------------------------------
        // Called when this GameObject becomes attached to the hand
        //-------------------------------------------------
        private void OnAttachedToHand(Hand hand)
        {
            if (!m_installOnContact && !IsInstalled)
            {
                IsClosed = false;
            }
        }


        //-------------------------------------------------
        // Called when this GameObject is detached from the hand
        //-------------------------------------------------
        private void OnDetachedFromHand(Hand hand)
        {
            if (CanInstallWhileInHand(hand))
            {
                IsInstalled = true;
                base.UpdateState();
            }
        }

        public override bool CanInteractVR(ObjectsBag bag, object hand)
        {
            return base.CanInteractVR(bag, hand) && !IsLocked && IsInstalled;
        }

        public override void InteractVR(ObjectsBag currentBag, object hand)
        {
            //StartSliding(hand);
            if (IsInstalled)
            {
                StopDoorMove();
                StartDriving(hand as Hand);
            }
        }

        protected override void StopInteractionVR(AbstractInteractiveBehaviour nextBehaviour)
        {
            //base.StopInteractionVR(nextBehaviour);
            //if (m_lockedHand != null)
            //{
            //    StopSliding(m_lockedHand);
            //}
            StopDriving(m_handHoverLocked);
        }

        //-------------------------------------------------
        void OnDisable()
        {
            if (m_handHoverLocked)
            {
                StopDriving(m_handHoverLocked);
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
            ControllerButtonHints.HideButtonHint(hand, m_trigger);

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

        protected override void StopInteraction(AbstractInteractiveBehaviour nextBehaviour)
        {
            base.StopInteraction(nextBehaviour);
            if(m_handHoverLocked != null)
            {
                m_handHoverLocked.HoverUnlock(GetComponent<Interactable>());
                m_handHoverLocked = null;
            }
        }

        private void StopDriving(Hand hand)
        {
            m_driving = false;

            if (hand == null) return;

            if (m_handHoverLocked == hand)
            {
                hand.HoverUnlock(GetComponent<Interactable>());
                m_handHoverLocked = null;
            }

            if (IsInstalled)
            {
                CalculateMappingChangeRate();
            }
        }

        private void StartDriving(Hand hand)
        {
            if (hand == null) return;

            if (IsInstalled)
            {
                m_lastHandProjected = ComputeToTransformProjected(hand.hoverSphereTransform);

                hand.HoverLock(GetComponent<Interactable>());
                m_handHoverLocked = hand;

                m_driving = true;

                ComputeAngle(hand);
                UpdateAll();

                m_lastOutAngle = m_outAngle;
            }
            else
            {
                m_handHoverLocked = null;
            }

            HideHints(hand);
        }

        //-------------------------------------------------
        private Vector3 ComputeToTransformProjected(Transform xForm)
        {
            return ComputeToTransformProjected(xForm.position - xForm.forward * 0.15f - xForm.up * 0.15f, xForm);
        }

        private Vector3 ComputeToTransformProjected(Vector3 position, Transform xForm)
        {
            Vector3 toTransform = (position - m_rotationPoint.position).normalized;
            Vector3 toTransformProjected = new Vector3(0.0f, 0.0f, 0.0f);

            // Need a non-zero distance from the hand to the center of the CircularDrive
            if (toTransform.sqrMagnitude > 0.0f)
            {
                toTransformProjected = Vector3.ProjectOnPlane(toTransform, m_worldPlaneNormal).normalized;
            }
            else
            {
                return ComputeToTransformProjected(position - xForm.forward * 0.1f - xForm.up * 0.1f, xForm);
                //Debug.LogFormat("The collider needs to be a minimum distance away from the CircularDrive GameObject {0}", gameObject.ToString());
                //Debug.Assert(false, string.Format("The collider needs to be a minimum distance away from the CircularDrive GameObject {0}", gameObject.ToString()));
            }

            ShowDebugPoint(position);

            return toTransformProjected;
        }

        private void ShowDebugPoint(Vector3 toTransform)
        {
            if (m_debug)
            {
                DebugPoint.gameObject.SetActive(true);
                DebugPoint.position = toTransform;
            }
            else if(m_debugPoint != null)
            {
                DebugPoint.gameObject.SetActive(false);
            }
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
            if (IsInstalled)
            {
                UpdateLinearMapping();
                AnimateDoorMovement(m_currentOpening);
            }
        }

        //-------------------------------------------------
        // Computes the angle to rotate the game object based on the change in the transform
        //-------------------------------------------------
        private void ComputeAngle(Hand hand)
        {
            Vector3 toHandProjected = ComputeToTransformProjected(hand.hoverSphereTransform);

            if (!toHandProjected.Equals(m_lastHandProjected))
            {
                float signedAngle = Vector3.SignedAngle(m_lastHandProjected, toHandProjected, m_worldPlaneNormal);

                m_outAngle = m_limits.Clamp(m_outAngle + signedAngle);
                m_lastHandProjected = toHandProjected;
            }
        }
#endif
    }
}
