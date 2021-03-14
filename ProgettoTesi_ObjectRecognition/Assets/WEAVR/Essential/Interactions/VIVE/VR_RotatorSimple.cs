using System;
using TXT.WEAVR.Common;
using UnityEngine;

#if WEAVR_VR
using Valve.VR.InteractionSystem;
#endif

namespace TXT.WEAVR.Interaction
{
    [AddComponentMenu("WEAVR/VR/Manipulators/Rotator")]
    public class VR_RotatorSimple : VR_Manipulator
    {
        [Tooltip("The axis around which the circular drive will rotate in local space")]
        public Vector3 axisOfRotation = Vector3.up;
        public OptionalFloat initialNormalizedValue;
        [Tooltip("If limited is true, this specifies the lower limit and upper limits, otherwise value is unused")]
        public OptionalSpan angleLimits;

        [Space]
        [Tooltip("If true, the transform of the Manipulate will be rotated accordingly")]
        public bool rotateGameObject = true;
        [HiddenBy(nameof(rotateGameObject))]
        [Draggable]
        public Transform manipulate;
        [HiddenBy(nameof(rotateGameObject))]
        [Draggable]
        public Transform rotationPoint;
        public Vector3 offsetPoint;

        [Space]
        [SerializeField]
        private bool m_debug;
        [SerializeField]
        [HiddenBy(nameof(m_debug))]
        private float m_debugPointSize = 0.05f;
        [SerializeField]
        [HiddenBy(nameof(m_debug))]
        private Color m_debugPointColor = Color.red;

        [ShowAsReadOnly]
        [HiddenBy(nameof(m_debug))]
        public float m_value;
        [Tooltip("The output angle value of the drive in degrees, unlimited will increase or decrease without bound, take the 360 modulus to find number of rotations")]
        [ShowAsReadOnly]
        [HiddenBy(nameof(m_debug))]
        public float m_outAngle;
        [ShowAsReadOnly]
        [HiddenBy(nameof(m_debug))]
        public float m_signedAngle;

        [Space]
        [SerializeField]
        public UnityEventFloat OnChange;

        private float m_lastOutAngle;

        private Vector3 m_worldPlaneNormal = new Vector3(1.0f, 0.0f, 0.0f);

        private Vector3 m_lastHandProjected;
        private Quaternion m_lastHandRotation;

        private Quaternion m_startRotation;


        private Span m_limits;

        private void Reset()
        {
            angleLimits = new Span(-180, 180);
            angleLimits.enabled = false;
        }

        private void OnValidate()
        {
            
        }

#if WEAVR_VR

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

        private Vector3 m_lastForward;
        private Quaternion m_deltaRotation;

        public override bool CanHandleData(object value)
        {
            return value is float || (value != null && float.TryParse(value.ToString(), out this.m_value));
        }

        public override void UpdateValue(float value)
        {
            this.m_value = value;
        }

        //-------------------------------------------------
        void Start()
        {
            m_limits = Span.UnitSpan;
            m_lastOutAngle = 0;
            if (!manipulate)
            {
                manipulate = transform;
            }

            if (!rotationPoint)
            {
                rotationPoint = manipulate;
            }

            m_worldPlaneNormal = axisOfRotation;

            var localPlaneNormal = m_worldPlaneNormal;

            if (transform.parent)
            {
                m_worldPlaneNormal = transform.TransformDirection(axisOfRotation).normalized;
            }

            if (angleLimits.enabled)
            {
                m_outAngle = Vector3.Dot(transform.localEulerAngles, axisOfRotation);

                if (initialNormalizedValue.enabled)
                {
                    m_outAngle = angleLimits.value.Denormalize(initialNormalizedValue);
                }
            }
            else
            {
                m_outAngle = 0.0f;
            }

            m_startRotation = manipulate.localRotation;

            UpdateAll();
        }

        public override void StartManipulating(Hand hand, Interactable interactable, bool iIsKeepPressedLogic, Func<float> getter, Action<float> setter, Span? span = null)
        {
            base.StartManipulating(hand, interactable, iIsKeepPressedLogic, getter, setter, span);

            if (transform.parent)
            {
                m_worldPlaneNormal = transform.TransformDirection(axisOfRotation).normalized;
            }

            m_limits = span ?? Span.UnitSpan;

            if (m_floatGetter != null)
            {
                m_value = m_floatGetter();
                m_outAngle = angleLimits.value.Denormalize(m_limits.Normalize(m_value));
            }

            m_lastHandProjected = ComputeToTransformProjected(hand.transform);
            manipulate.localRotation = m_startRotation;
            manipulate.RotateAround(rotationPoint.position, m_worldPlaneNormal, m_outAngle);

            m_lastOutAngle = m_outAngle;
            m_lastHandRotation = hand.transform.rotation;

            SmartComputeAngle(hand);
            UpdateAll();
        }

        private void SmartComputeAngle(Hand hand)
        {
            //Vector3 toTransformProjected = Vector3.ProjectOnPlane(hand.hoverSphereTransform.position - rotationPoint.position, m_worldPlaneNormal);
            //if (toTransformProjected.sqrMagnitude > 0.001f)
            //{
                ComputeAngle(hand);
            //    m_lastHandRotation = hand.hoverSphereTransform.rotation;
            //}
            //else
            //{
            //    var delta = hand.hoverSphereTransform.rotation * Quaternion.Inverse(m_lastHandRotation);
            //    delta.ToAngleAxis(out float angle, out Vector3 axis);
            //    angle = angle > 180 ? 360 - angle : angle;
            //    m_signedAngle += Vector3.Dot(axis, m_worldPlaneNormal) < 0 ? -angle : angle;

            //    if (angleLimits.enabled)
            //    {
            //        m_outAngle = angleLimits.value.Clamp(m_outAngle + m_signedAngle);
            //    }
            //    else
            //    {
            //        m_outAngle += m_signedAngle;
            //    }

            //    m_lastHandProjected = toTransformProjected.normalized;
            //    m_lastHandRotation = hand.hoverSphereTransform.rotation;
            //}
        }

        public override void SetInitialValue(float value)
        {
            base.SetInitialValue(value);

            if (manipulate == null)
            {
                manipulate = transform;
            }

            if (rotationPoint == null)
            {
                rotationPoint = manipulate;
            }

            m_value = value;
            m_lastOutAngle = m_outAngle;
            m_outAngle = angleLimits.value.Denormalize(m_limits.Denormalize(this.m_value));
            UpdateAll();
        }

        protected override void UpdateManipulator(Hand hand, Interactable interactable)
        {
            SmartComputeAngle(hand);
            UpdateAll();
        }

        public override void StopManipulating(Hand hand, Interactable interactable)
        {
            base.StopManipulating(hand, interactable);
        }

        //-------------------------------------------------
        private Vector3 ComputeToTransformProjected(Transform xForm)
        {
            //Vector3 position = xForm.position - xForm.forward * 0.1f;
            //return ComputeToTransformProjected(xForm.position - xForm.forward * 0.1f - xForm.up * 0.1f, xForm);
            return ComputeToTransformProjected(xForm.position + (xForm.forward * offsetPoint.z + xForm.right * offsetPoint.x + xForm.up * offsetPoint.y), xForm);
        }

        private Vector3 ComputeToTransformProjected(Vector3 position, Transform xForm)
        {
            Vector3 toTransform = (position - rotationPoint.position).normalized;
            Vector3 toTransformProjected;
            // Need a non-zero distance from the hand to the center of the CircularDrive
            if (toTransform.sqrMagnitude > 0.0001f)
            {
                toTransformProjected = Vector3.ProjectOnPlane(toTransform, m_worldPlaneNormal).normalized;
            }
            else
            {
                //return m_lastHandProjected;
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
            if (angleLimits.enabled)
            {
                // Map it to a [0, 1] value
                m_value = angleLimits.value.Normalize(m_outAngle);
            }
            else
            {
                m_value = m_outAngle / 360.0f;
            }
        }


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

        //-------------------------------------------------
        // Updates the Debug TextMesh with the linear mapping value and the angle
        //-------------------------------------------------
        private void UpdateAll()
        {
            UpdateLinearMapping();
            UpdateGameObject();

            m_floatSetter?.Invoke(m_limits.Denormalize(m_value));
            m_lastOutAngle = m_outAngle;

            OnChange.Invoke(m_value);
        }


        //-------------------------------------------------
        // Computes the angle to rotate the game object based on the change in the transform
        //-------------------------------------------------
        private void ComputeAngle(Hand hand)
        {
            Vector3 toHandProjected = ComputeToTransformProjected(hand.transform);

            if (!toHandProjected.Equals(m_lastHandProjected))
            {
                m_signedAngle = Vector3.SignedAngle(m_lastHandProjected, toHandProjected, m_worldPlaneNormal);

                if (angleLimits.enabled)
                {
                    m_outAngle = angleLimits.value.Clamp(m_outAngle + m_signedAngle);
                    m_lastHandProjected = toHandProjected;
                }
                else
                {
                    m_outAngle += m_signedAngle;
                    m_lastHandProjected = toHandProjected;
                }
            }
        }

#endif

    }
}

