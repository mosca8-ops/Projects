using System;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using TXT.WEAVR.Maintenance;
using UnityEngine.Serialization;

#if WEAVR_VR
using Valve.VR.InteractionSystem;
using System.Collections;
#endif

namespace TXT.WEAVR.Interaction
{
    [RequireComponent(typeof(InteractionController))]
    public abstract class AbstractVR_Knob : AbstractInteractiveBehaviour
    {

        [SerializeField]
        private string m_commandName = "Rotate";

        [Space]
        [FormerlySerializedAs("initialValue")]
        public float initialNormalizedValue;

        [Tooltip("The axis around which the circular drive will rotate in local space")]
        public Vector3 axisOfRotation = Vector3.up;


        //public LinearMapping linearMapping;
        [Tooltip("If true, the transform of the Manipulate will be rotated accordingly")]
        public bool rotateGameObject = true;
        [HiddenBy(nameof(rotateGameObject))]
        [Draggable]
        public Transform manipulate;
        [HiddenBy(nameof(rotateGameObject))]
        [Draggable]
        public Transform rotationPoint;
        [Tooltip("Whether to resume from last rotation or not")]
        public bool relativeRotation = true;

        [Space]
        [SerializeField]
        [Tooltip("If true, the rotation will be limited to [minAngle, maxAngle], if false, the rotation is unlimited")]
        private bool m_limited = false;
        [SerializeField]
        [Tooltip("If limited is true, this specifies the lower limit and upper limits, otherwise value is unused")]
        private Span m_limits = new Span(0, 90);

        [SerializeField]
        [Tooltip("If limited is true, this forces the starting angle to be startAngle, clamped to [minAngle, maxAngle]")]
        [HiddenBy(nameof(m_limited))]
        private bool m_forceStart = false;

        [Space]
        [SerializeField]
        [Tooltip("Play audio on checkpoint reach")]
        private bool m_playAudio;
        [SerializeField]
        [HiddenBy(nameof(m_playAudio))]
        [Draggable]
        private AudioSource m_audioSource;

        [Space]
        [SerializeField]
        [Tooltip("Whether to consider all checkpoints when changing values, otherwise only the closest one to the value will be considered")]
        private bool m_onlineCheckpoints = true;
        [SerializeField]
        private bool m_snapToCheckpoints = false;
        [SerializeField]
        private List<CheckPoint> m_checkpoints;

        [Space]
        [SerializeField]
        private bool m_debug = false;
        [SerializeField]
        [HiddenBy(nameof(m_debug))]
        private float m_debugPointSize = 0.05f;
        [SerializeField]
        [HiddenBy(nameof(m_debug))]
        private Color m_debugPointColor = Color.red;

        [Space]
        [Tooltip("The output angle value of the drive in degrees, unlimited will increase or decrease without bound, take the 360 modulus to find number of rotations")]
        [ShowAsReadOnly]
        public float m_outAngle;

        [ShowAsReadOnly]
        public float signedAngle;

        [SerializeField]
        [ShowAsReadOnly]
        private float m_value;

        private float m_lastOutAngle;

        private Vector3 m_worldPlaneNormal = new Vector3(1.0f, 0.0f, 0.0f);

        private Vector3 m_lastHandProjected;
        private VR_Skeleton_Poser m_SkeletonPoser = null;
        private InteractionController m_InteractionController = null;

        private CheckPoint m_lastCheckpoint;

        public string CommandName
        {
            get { return m_commandName; }
            set
            {
                m_commandName = value;
            }
        }

        protected abstract void HandleCheckpointReached(int iCheckpointIdx);
        protected abstract void HandleValueChanged(float iValue);

        private float ValueInternal
        {
            get { return m_value; }
            set
            {
                float clamped = m_limited ? Mathf.Clamp01(value) : value;
                if (m_value != clamped)
                {
                    float lastValue = m_value;
                    m_value = clamped;
                    if (m_onlineCheckpoints)
                    {
                        if (m_value > lastValue)
                        {
                            for (int i = 0; i < m_checkpoints.Count; i++)
                            {
                                if (m_checkpoints[i].RaiseIfNeeded(lastValue, m_value))
                                {
                                    m_lastCheckpoint = m_checkpoints[i];
                                    if (m_playAudio && m_audioSource && m_audioSource.clip)
                                    {
                                        m_audioSource.Play();
                                    }
                                    HandleCheckpointReached(i);
                                }
                            }
                        }
                        else
                        {
                            for (int i = m_checkpoints.Count - 1; i >= 0; i--)
                            {
                                if (m_checkpoints[i].RaiseIfNeeded(m_value, lastValue))
                                {
                                    m_lastCheckpoint = m_checkpoints[i];
                                    if (m_playAudio && m_audioSource && m_audioSource.clip)
                                    {
                                        m_audioSource.Play();
                                    }
                                    HandleCheckpointReached(i);
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < m_checkpoints.Count; i++)
                        {
                            if (m_checkpoints[i].RaiseIfNeeded(m_value) && m_lastCheckpoint != m_checkpoints[i])
                            {
                                m_lastCheckpoint = m_checkpoints[i];
                                if (m_playAudio && m_audioSource && m_audioSource.clip)
                                {
                                    m_audioSource.Play();
                                }
                                HandleCheckpointReached(i);
                                break;
                            }
                        }
                    }

                    if (m_snapToCheckpoints && m_lastCheckpoint != null)
                    {
                        m_value = m_lastCheckpoint.EffectiveValue;
                    }

                    HandleValueChanged(m_value);
                }
            }
        }

        [IgnoreStateSerialization]
        public float Value
        {
            get { return m_value; }
            set
            {
                float clamped = m_limited ? Mathf.Clamp01(value) : value;
                if (m_value != clamped)
                {
                    ValueInternal = clamped;

                    if (m_limited)
                    {
                        m_outAngle = m_limits.Denormalize(m_value);
                    }
                    else
                    {
                        m_outAngle = m_value * 360f;
                    }
                    UpdateGameObject();
                    m_lastOutAngle = m_outAngle;
                }
            }
        }

        protected virtual void OnValidate()
        {
            if (m_checkpoints != null)
            {
                foreach (var checkPoint in m_checkpoints)
                {
                    if (checkPoint.epsilon <= 0)
                    {
                        checkPoint.epsilon = 0.0001f;
                    }
                }
            }
        }

#if WEAVR_VR

        private float m_startAngle;
        private bool m_isRotating;
        private InteractionController m_vrController;
        private InteractionController VRController
        {
            get
            {
                if (m_vrController == null)
                {
                    m_vrController = Controller as InteractionController;
                }
                return m_vrController;
            }
        }

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

        public override bool CanBeDefault => true;
        public override bool CanInteractVR(ObjectsBag bag, object hand)
        {
            return bag.Selected == null;
        }

        public override void InteractVR(ObjectsBag currentBag, object hand)
        {
            StartManipulating(hand as Hand);
        }

        public override bool CanInteract(ObjectsBag currentBag)
        {
            return true;
        }

        //-------------------------------------------------
        protected virtual void Start()
        {
            m_SkeletonPoser = transform.GetComponent<VR_Skeleton_Poser>();
            m_InteractionController = transform.GetComponent<InteractionController>();
            InteractTrigger = BehaviourInteractionTrigger.OnPointerDown;

            if (manipulate == null)
            {
                manipulate = transform;
            }

            m_worldPlaneNormal = axisOfRotation;

            if (rotationPoint == null)
            {
                rotationPoint = manipulate;
                if (transform.parent)
                {
                    m_worldPlaneNormal = transform.localToWorldMatrix.MultiplyVector(m_worldPlaneNormal).normalized;
                }
            }
            else
            {
                m_worldPlaneNormal = rotationPoint.localToWorldMatrix.MultiplyVector(m_worldPlaneNormal).normalized;
            }

            if (m_limited)
            {
                //m_outAngle = transform.localEulerAngles[(int)axisOfRotation];
                m_startAngle = Mathf.Abs(Vector3.Dot(transform.eulerAngles, m_worldPlaneNormal));

                //m_startAngle = m_outAngle;

                if (m_forceStart)
                {
                    m_outAngle = m_limits.Denormalize(initialNormalizedValue);
                }
            }
            else
            {
                m_outAngle = 0.0f;
            }

            UpdateAll();
        }

        protected override void StopInteraction(AbstractInteractiveBehaviour nextBehaviour)
        {
            StopManipulating(VRController.HoveringHand);
            base.StopInteraction(nextBehaviour);
        }

        //-------------------------------------------------
        // Called every Update() while a Hand is hovering over this object
        //-------------------------------------------------
        private void HandHoverUpdate(Hand hand)
        {
            ControllerButtonHints wControllerButtonHints = hand.gameObject.transform.GetComponentInChildren<ControllerButtonHints>();
            if (wControllerButtonHints != null)
            {
                Valve.VR.SteamVR_RenderModel wControllerRenderer = wControllerButtonHints.GetComponentInChildren<Valve.VR.SteamVR_RenderModel>();
                if (wControllerRenderer != null)
                {
                    wControllerRenderer.gameObject.SetActive(false);
                }
            }
            if (!m_isRotating || VRController.HoveringHand != hand) { return; }

            if (VR_ControllerManager.GetStandardInteractionButtonUp(hand))
            {
                StopManipulating(hand);
            }
            else
            {
                ComputeAngle(hand);
                UpdateAll();
            }
        }

        private void StopManipulating(Hand hand)
        {
            m_isRotating = false;
            hand?.HoverUnlock(GetComponent<VR_Object>());
            if (m_snapToCheckpoints && m_checkpoints.Count > 0)
            {
                StartCoroutine(AnimateValueTo(GetClosestCheckpoint(m_limited ? m_limits.Normalize(m_outAngle) : m_outAngle / 360f).EffectiveValue));
            }
        }

        private IEnumerator AnimateValueTo(float value)
        {
            value = Mathf.Clamp01(value);
            if(value == m_value)
            {
                m_outAngle = m_limited ? m_limits.Denormalize(value) : value * 360f;
                UpdateGameObject();
                m_lastOutAngle = m_outAngle;
            }
            while (value != m_value)
            {
                Value = Mathf.MoveTowards(m_value, value, Time.deltaTime * 2);
                yield return null;
            }
        }

        private CheckPoint GetClosestCheckpoint(float value)
        {
            if (m_checkpoints.Count == 0) { return null; }

            CheckPoint closestCheckpoint = m_checkpoints[0];
            float minDistance = closestCheckpoint.DistanceTo(value);
            for (int i = 1; i < m_checkpoints.Count; i++)
            {
                if (m_checkpoints[i].DistanceTo(value) < minDistance)
                {
                    closestCheckpoint = m_checkpoints[i];
                    minDistance = m_checkpoints[i].DistanceTo(value);
                }
            }
            return closestCheckpoint;
        }

        private bool HasCheckpointInRange(float from, float to, out CheckPoint closestCheckpoint)
        {
            closestCheckpoint = null;
            if (m_checkpoints.Count == 0 || to == from) {
                return false; 
            }

            if(from > to)
            {
                float temp = to;
                to = from;
                from = temp;
            }

            float minDistance = -10;
            float center = (from + to) * 0.5f;
            for (int i = 0; i < m_checkpoints.Count; i++)
            {
                if(m_checkpoints[i].IsInRange(from, to))
                {
                    float distance = m_checkpoints[i].DistanceTo(center);
                    if(distance < minDistance)
                    {
                        minDistance = distance;
                        closestCheckpoint = m_checkpoints[i];
                    }
                }
            }
            return closestCheckpoint != null;
        }

        private bool CanHandleHand(Hand iHand)
        {
            return m_InteractionController != null && 
                  (m_InteractionController.CurrentBehaviour == null || object.ReferenceEquals(m_InteractionController.CurrentBehaviour, this)) &&
                  iHand.currentAttachedObject == null;
        }
        //-------------------------------------------------
        // Called when a Hand starts hovering over this object
        //-------------------------------------------------
        private void OnHandHoverBegin(Hand iHand)
        {
            if (CanHandleHand(iHand))
            {
                if (m_SkeletonPoser != null && iHand.skeleton != null)
                {
                    iHand.skeleton.BlendToPoser(m_SkeletonPoser, 0.1f);
                    iHand.HideController();
                }
            }
        }

        /// <summary>
        /// Called when a Hand stops hovering over this object
        /// </summary>
        private void OnHandHoverEnd(Hand iHand)
        {
            if (CanHandleHand(iHand))
            {
                if (m_SkeletonPoser != null && iHand.skeleton != null)
                {
                    iHand.skeleton.BlendToSkeleton(0.2f);
                    iHand.ShowController();
                }
            }
        }

        public void StartManipulating(Hand hand)
        {
            StopAllCoroutines();

            if (relativeRotation)
            {
                m_lastHandProjected = ComputeToTransformProjected(hand.hoverSphereTransform);
            }

            if (!relativeRotation && rotationPoint != manipulate)
            {
                Vector3 fromManipulate = Vector3.Project(rotationPoint.position - manipulate.position, m_worldPlaneNormal);
                Vector3 fromHand = Vector3.Project(rotationPoint.position - hand.hoverSphereTransform.position, m_worldPlaneNormal);
                manipulate.RotateAround(rotationPoint.position, m_worldPlaneNormal, Vector3.SignedAngle(fromManipulate, fromHand, m_worldPlaneNormal));
                //manipulate.RotateAround(rotationPoint.position, worldPlaneNormal, -90);
            }

            //m_outAngle = Mathf.Abs(Vector3.Dot(manipulate.eulerAngles, m_worldPlaneNormal)) - m_startAngle;
            m_lastOutAngle = m_outAngle;

            ComputeAngle(hand);
            UpdateAll();

            m_isRotating = true;
            hand.HoverLock(GetComponent<VR_Object>());
        }

        //-------------------------------------------------
        private Vector3 ComputeToTransformProjected(Transform xForm)
        {
            //Vector3 position = xForm.position - xForm.forward * 0.1f;
            return ComputeToTransformProjected(xForm.position - xForm.forward * 0.15f - xForm.up * 0.15f, xForm);
        }

        private Vector3 ComputeToTransformProjected(Vector3 position, Transform xForm)
        {
            Vector3 toTransform = (position - rotationPoint.position).normalized;
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
            else
            {
                DebugPoint.gameObject.SetActive(false);
            }
        }



        //-------------------------------------------------
        // Updates the LinearMapping value from the angle
        //-------------------------------------------------
        private void UpdateLinearMapping()
        {
            if (m_limited)
            {
                // Map it to a [0, 1] value
                ValueInternal = m_limits.Normalize(m_outAngle);
            }
            else
            {
                // Normalize to [0, 1] based on 360 degree windings
                float flTmp = m_outAngle / 360.0f;
                //ValueInternal = flTmp - Mathf.Floor(flTmp);
                ValueInternal = flTmp;
            }
        }




        //-------------------------------------------------
        // Updates the Debug TextMesh with the linear mapping value and the angle
        //-------------------------------------------------
        private void UpdateAll()
        {
            UpdateLinearMapping();
            UpdateGameObject();

            //m_floatSetter?.Invoke(currentLimits.Denormalize(m_value) * valueScale);
            m_lastOutAngle = m_outAngle;

            var newValue = m_limited ? m_limits.Normalize(m_outAngle) : m_outAngle / 360f;
            if (m_onlineCheckpoints && HasCheckpointInRange(ValueInternal, newValue, out CheckPoint closest))
            {
                ValueInternal = closest.EffectiveValue;
            }
            else
            {
                ValueInternal = m_limited ? m_limits.Normalize(m_outAngle) : m_outAngle / 360f;
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
                signedAngle = Vector3.SignedAngle(m_lastHandProjected, toHandProjected, m_worldPlaneNormal);

                if (m_limited)
                {
                    m_outAngle = m_limits.Clamp(m_outAngle + signedAngle);
                    m_lastHandProjected = toHandProjected;
                }
                else
                {
                    m_outAngle += signedAngle;
                    m_lastHandProjected = toHandProjected;
                }
            }
        }

#endif

        //-------------------------------------------------
        // Updates the LinearMapping value from the angle
        //-------------------------------------------------
        private void UpdateGameObject()
        {
            if (rotateGameObject)
            {
                //manipulate.localRotation = start * Quaternion.AngleAxis(outAngle, localPlaneNormal);
                manipulate.RotateAround(rotationPoint.position, m_worldPlaneNormal, m_outAngle - m_lastOutAngle);
            }
        }

        public override string GetInteractionName(ObjectsBag currentBag)
        {
            return m_commandName;
        }

        public override void Interact(ObjectsBag currentBag)
        {

        }

        [Serializable]
        public class CheckPoint
        {
            public float value;
            public float epsilon;
            public OptionalSpan span;
            public UnityEventFloat onReach;

            private bool m_raised;

            public CheckPoint()
            {
                epsilon = 0.0001f;
            }

            public float EffectiveValue => span.enabled ? span.value.center : value;

            public bool RaiseIfNeeded(float value)
            {
                bool reached = false;
                if (IsValid(value))
                {
                    if (!m_raised)
                    {
                        onReach.Invoke(value);
                        reached = true;
                    }
                    m_raised = true;
                }
                else
                {
                    m_raised = false;
                }
                return reached;
            }

            public bool RaiseIfNeeded(float from, float to)
            {
                bool reached = false;
                if (IsInRange(from, to))
                {
                    if (!m_raised)
                    {
                        onReach.Invoke(value);
                        reached = true;
                    }
                    m_raised = true;
                }
                else
                {
                    m_raised = false;
                }
                return reached;
            }

            public bool IsValid(float value)
            {
                return span.enabled ? span.value.IsValid(value) : Mathf.Abs(value - this.value) < epsilon;
            }

            public float DistanceTo(float value)
            {
                return span.enabled ? Mathf.Abs(span.value.center - value) : Mathf.Abs(value - this.value);
            }

            public bool IsInRange(float from, float to)
            {
                return span.enabled ? span.value.IsValid(value) : from - epsilon < value && value < to + epsilon;
            }
        }


    }
}

