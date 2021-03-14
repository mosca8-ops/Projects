using System;
using TXT.WEAVR.Common;
using UnityEngine;

#if WEAVR_VR
using Valve.VR.InteractionSystem;
#endif

namespace TXT.WEAVR.Interaction
{
    [Obsolete("Use VR_RotatorSimple instead")]
    [AddComponentMenu("")]
    public class VR_Rotator : VR_Manipulator
    {

        public float initialValue;

        [Tooltip("The axis around which the circular drive will rotate in local space")]
        public Vector3 axisOfRotation = Vector3.up;

        public Span defaultLimits = new Span(0, 1);

        public float valueScale = 1;

        //public LinearMapping linearMapping;
        [Tooltip("If true, the transform of the Manipulate will be rotated accordingly")]
        public bool rotateGameObject = true;
        [HiddenBy(nameof(rotateGameObject))]
        [Draggable]
        public Transform manipulate;
        [HiddenBy(nameof(rotateGameObject))]
        [Draggable]
        public Transform rotationPoint;

        [Space]
        public bool relativeRotation = true;
        [Space]

        [Header("Limited Rotation")]
        [Tooltip("If true, the rotation will be limited to [minAngle, maxAngle], if false, the rotation is unlimited")]
        public bool limited = false;
        [Tooltip("If limited is true, this specifies the lower limit and upper limits, otherwise value is unused")]
        public Span angleLimits = new Span(-45, 45);
        public Vector2 frozenDistanceMinMaxThreshold = new Vector2(0.1f, 0.2f);

        [Tooltip("If limited, set whether drive will freeze its angle when the min angle is reached")]
        public bool freezeOnMin = false;

        [Tooltip("If limited, set whether drive will freeze its angle when the max angle is reached")]
        public bool freezeOnMax = false;

        [Tooltip("If limited is true, this forces the starting angle to be startAngle, clamped to [minAngle, maxAngle]")]
        public bool forceStart = false;

        [Space]
        public Vector3 offset;
        [Tooltip("The output angle value of the drive in degrees, unlimited will increase or decrease without bound, take the 360 modulus to find number of rotations")]
        public float outAngle;

        [Space]
        public float signedAngle;

        [Space]
        [SerializeField]
        private bool m_debug;
        [SerializeField]
        [HiddenBy(nameof(m_debug))]
        private float m_debugPointSize = 0.05f;
        [SerializeField]
        [HiddenBy(nameof(m_debug))]
        private Color m_debugPointColor = Color.red;

        [Space]
        [SerializeField]
        public UnityEventFloat OnChange;

        private float m_lastOutAngle;

        private Quaternion start;

        private Vector3 worldPlaneNormal = new Vector3(1.0f, 0.0f, 0.0f);
        private Vector3 localPlaneNormal = new Vector3(1.0f, 0.0f, 0.0f);

        private Vector3 lastHandProjected;
        private Vector3 lastHandForward;

        private Quaternion m_startRotation;

        public float Value;

        private Span currentLimits;

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
            return value is float || (value != null && float.TryParse(value.ToString(), out Value));
        }

        public override void UpdateValue(float value)
        {
            Value = value;
        }

        //-------------------------------------------------
        void Start()
        {
            initialValue = defaultLimits.min;
            currentLimits = defaultLimits;
            if (manipulate == null)
            {
                manipulate = transform;
            }

            if (rotationPoint == null)
            {
                rotationPoint = manipulate;
            }

            worldPlaneNormal = axisOfRotation;

            localPlaneNormal = worldPlaneNormal;

            if (transform.parent)
            {
                worldPlaneNormal = transform.parent.localToWorldMatrix.MultiplyVector(worldPlaneNormal).normalized;
            }

            if (limited)
            {
                start = Quaternion.identity;
                outAngle = Vector3.Dot(transform.localEulerAngles, axisOfRotation);

                if (forceStart)
                {
                    outAngle = Mathf.Clamp(initialValue, angleLimits.min, angleLimits.max);
                }
            }
            else
            {
                start = Quaternion.AngleAxis(Vector3.Dot(rotationPoint.localEulerAngles, axisOfRotation), localPlaneNormal);
                outAngle = 0.0f;
            }

            UpdateAll();

            m_startRotation = manipulate.localRotation;
        }

        public override void StartManipulating(Hand hand, Interactable interactable, bool iIsKeepPressedLogic, Func<float> getter, Action<float> setter, Span? span = null)
        {
            base.StartManipulating(hand, interactable, iIsKeepPressedLogic, getter, setter, span);

            if (transform.parent)
            {
                worldPlaneNormal = transform.TransformDirection(axisOfRotation).normalized;
            }

            currentLimits = span ?? defaultLimits;

            if (m_floatGetter != null)
            {
                Value = m_floatGetter();
                outAngle = angleLimits.Denormalize(currentLimits.Denormalize(Value));
            }

            if (relativeRotation)
            {
                lastHandProjected = ComputeToTransformProjected(hand.hoverSphereTransform);
                manipulate.localRotation = m_startRotation;
                manipulate.RotateAround(rotationPoint.position, worldPlaneNormal, outAngle);
            }

            if (!relativeRotation && rotationPoint != manipulate)
            {
                Vector3 fromManipulate = Vector3.Project(rotationPoint.position - manipulate.position, worldPlaneNormal);
                Vector3 fromHand = Vector3.Project(rotationPoint.position - hand.hoverSphereTransform.position, worldPlaneNormal);
                manipulate.RotateAround(rotationPoint.position, worldPlaneNormal, Vector3.SignedAngle(fromManipulate, fromHand, worldPlaneNormal));
                //manipulate.RotateAround(rotationPoint.position, worldPlaneNormal, -90);
            }

            m_lastOutAngle = outAngle;

            ComputeAngle(hand);
            UpdateAll();
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

            Value = value;
            m_lastOutAngle = outAngle;
            outAngle = angleLimits.Denormalize(currentLimits.Denormalize(Value));
            UpdateAll();
        }

        protected override void UpdateManipulator(Hand hand, Interactable interactable)
        {
            ComputeAngle(hand);
            UpdateAll();
        }

        public override void StopManipulating(Hand hand, Interactable interactable)
        {
            base.StopManipulating(hand, interactable);
        }

        // If the drive is limited as is at min/max, angles greater than this are ignored 
        private float minMaxAngularThreshold = 20.0f;

        private bool frozen = false;
        private float frozenAngle = 0.0f;
        private Vector3 frozenHandWorldPos = new Vector3(0.0f, 0.0f, 0.0f);
        private Vector2 frozenSqDistanceMinMaxThreshold = new Vector2(0.0f, 0.0f);

        //-------------------------------------------------
        private void Freeze(Hand hand)
        {
            frozen = true;
            frozenAngle = outAngle;
            frozenHandWorldPos = hand.hoverSphereTransform.position;
            frozenSqDistanceMinMaxThreshold.x = frozenDistanceMinMaxThreshold.x * frozenDistanceMinMaxThreshold.x;
            frozenSqDistanceMinMaxThreshold.y = frozenDistanceMinMaxThreshold.y * frozenDistanceMinMaxThreshold.y;
        }


        //-------------------------------------------------
        private void UnFreeze()
        {
            frozen = false;
            frozenHandWorldPos.Set(0.0f, 0.0f, 0.0f);
        }

        //-------------------------------------------------
        private Vector3 ComputeToTransformProjected(Transform xForm)
        {
            //Vector3 position = xForm.position - xForm.forward * 0.1f;
            //return ComputeToTransformProjected(xForm.position - xForm.forward * 0.1f - xForm.up * 0.1f, xForm);
            return ComputeToTransformProjected(xForm.position + (xForm.forward * offset.z + xForm.right * offset.x + xForm.up * offset.y), xForm);
        }

        private Vector3 ComputeToTransformProjected(Vector3 position, Transform xForm)
        {
            Vector3 toTransform = (position - rotationPoint.position).normalized;
            Vector3 toTransformProjected = new Vector3(0.0f, 0.0f, 0.0f);

            // Need a non-zero distance from the hand to the center of the CircularDrive
            if (toTransform.sqrMagnitude > 0.0f)
            {
                toTransformProjected = Vector3.ProjectOnPlane(toTransform, worldPlaneNormal).normalized;
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
            if (limited)
            {
                // Map it to a [0, 1] value
                Value = angleLimits.Normalize(outAngle);
            }
            else
            {
                // Normalize to [0, 1] based on 360 degree windings
                float flTmp = outAngle / 360.0f;
                Value = flTmp - Mathf.Floor(flTmp);
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
                manipulate.RotateAround(rotationPoint.position, worldPlaneNormal, outAngle - m_lastOutAngle);
            }
        }

        //-------------------------------------------------
        // Updates the Debug TextMesh with the linear mapping value and the angle
        //-------------------------------------------------
        private void UpdateAll()
        {
            UpdateLinearMapping();
            UpdateGameObject();

            m_floatSetter?.Invoke(currentLimits.Denormalize(Value) * valueScale);
            m_lastOutAngle = outAngle;

            OnChange.Invoke(Value);
        }


        //-------------------------------------------------
        // Computes the angle to rotate the game object based on the change in the transform
        //-------------------------------------------------
        private void ComputeAngle(Hand hand)
        {
            Vector3 toHandProjected = ComputeToTransformProjected(hand.hoverSphereTransform);

            if (!toHandProjected.Equals(lastHandProjected))
            {
                float absAngleDelta = Vector3.Angle(lastHandProjected, toHandProjected);
                signedAngle = Vector3.SignedAngle(lastHandProjected, toHandProjected, worldPlaneNormal);



                if (signedAngle < 0)
                {
                    absAngleDelta = Mathf.Abs(signedAngle);
                }

                //if(absAngleDelta > 180)
                //{
                //    absAngleDelta = 180 - absAngleDelta;
                //}

                if (absAngleDelta > 0.0f)
                {
                    if (frozen)
                    {
                        float frozenSqDist = (hand.hoverSphereTransform.position - frozenHandWorldPos).sqrMagnitude;
                        if (frozenSqDist > frozenSqDistanceMinMaxThreshold.x)
                        {
                            outAngle = frozenAngle + UnityEngine.Random.Range(-1.0f, 1.0f);

                            float magnitude = Util.RemapNumberClamped(frozenSqDist, frozenSqDistanceMinMaxThreshold.x, frozenSqDistanceMinMaxThreshold.y, 0.0f, 1.0f);
                        }
                    }
                    else
                    {
                        Vector3 cross = Vector3.Cross(lastHandProjected, toHandProjected).normalized;
                        float dot = Vector3.Dot(worldPlaneNormal, cross);

                        float signedAngleDelta = signedAngle; // absAngleDelta;

                        //if (dot < 0.0f)
                        //{
                        //    signedAngleDelta = -signedAngleDelta;
                        //}

                        if (limited)
                        {
                            float angleTmp = Mathf.Clamp(outAngle + signedAngleDelta, angleLimits.min, angleLimits.max);

                            if (outAngle == angleLimits.min)
                            {
                                if (angleTmp > angleLimits.min && absAngleDelta < minMaxAngularThreshold)
                                {
                                    outAngle = angleTmp;
                                    lastHandProjected = toHandProjected;
                                }
                            }
                            else if (outAngle == angleLimits.max)
                            {
                                if (angleTmp < angleLimits.max && absAngleDelta < minMaxAngularThreshold)
                                {
                                    outAngle = angleTmp;
                                    lastHandProjected = toHandProjected;
                                }
                            }
                            else if (angleTmp == angleLimits.min)
                            {
                                outAngle = angleTmp;
                                lastHandProjected = toHandProjected;
                                if (freezeOnMin)
                                {
                                    Freeze(hand);
                                }
                            }
                            else if (angleTmp == angleLimits.max)
                            {
                                outAngle = angleTmp;
                                lastHandProjected = toHandProjected;
                                if (freezeOnMax)
                                {
                                    Freeze(hand);
                                }
                            }
                            else
                            {
                                outAngle = angleTmp;
                                lastHandProjected = toHandProjected;
                            }
                        }
                        else
                        {
                            outAngle += signedAngleDelta;
                            lastHandProjected = toHandProjected;
                        }
                    }
                }
            }
        }







#endif

    }
}

