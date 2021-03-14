using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Procedure;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Camera System/Camera Movement")]
    public class CameraMovement : MonoBehaviour
    {
        public enum RotationAxes { PitchAndYaw = 0, Pitch = 1, Yaw = 2 }

        public float referenceDpi = 200;
        public float touchSensitivityFactor = 0.05f;

        [Space]
        public float movementSpeed = 1f;
        public bool fixHeight = true;

        [Space]
        public RotationAxes rotationAxis = RotationAxes.PitchAndYaw;
        public float sensitivityX = 1F;
        public float sensitivityY = 1F;
        
        public float minPitch = -60F;
        public float maxPitch = 60F;
        
        private Vector3 m_startPosition;
        private Quaternion m_startRotation;

        private Vector3 m_positionTarget;
        private Vector3 m_eulerTarget;

        private Rigidbody m_rigidBody;
        private Collider m_collider;

        private bool m_leftTouch;
        private float m_dragThresholdInPx = 10;
        private float m_maxSpeedDragRadiusInPx = 100;
        private Vector2 m_startLeftTouch;

        private bool m_rightTouch;

        private float m_dpiRatio;

        private ProcedureRunner m_runner;

        // Use this for initialization
        void Start()
        {
            if (m_runner)
            {
                m_runner.ProcedureStarted -= Runner_ProcedureStarted;
            }

            m_runner = this.TryGetSingleton<ProcedureRunner>();

            if (m_runner)
            {
                m_runner.ProcedureStarted -= Runner_ProcedureStarted;
                m_runner.ProcedureStarted += Runner_ProcedureStarted;
            }

            m_startPosition = transform.position;
            m_startRotation = transform.localRotation;
            
            m_positionTarget = transform.position;
            m_eulerTarget = transform.localEulerAngles;

            m_collider = GetComponent<Collider>();
            if(m_collider == null) {
                var sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.radius = 0.01f;
                m_collider = sphereCollider;
                m_collider.isTrigger = true;
            }

            m_rigidBody = GetComponent<Rigidbody>();
            if(m_rigidBody == null) {
                m_rigidBody = gameObject.AddComponent<Rigidbody>();
                m_rigidBody.mass = 0.1f;
                m_rigidBody.drag = 5;
                m_rigidBody.angularDrag = 10;
                m_rigidBody.useGravity = false;
                m_rigidBody.constraints = RigidbodyConstraints.FreezeRotationZ;
                if (fixHeight) {
                    m_rigidBody.constraints |= RigidbodyConstraints.FreezePositionY;
                }
            }

            m_dpiRatio = Screen.dpi < 0 ? 1 : Mathf.Sqrt(referenceDpi) / Mathf.Sqrt(Screen.dpi);
            m_dragThresholdInPx /= m_dpiRatio;
            m_maxSpeedDragRadiusInPx /= m_dpiRatio;
        }

        private void Runner_ProcedureStarted(ProcedureRunner runner, Procedure.Procedure procedure, ExecutionMode mode)
        {
            if(procedure && mode)
            {
                enabled = mode.UsesWorldNavigation;
            }
        }

        private void OnDestroy()
        {
            if (m_runner)
            {
                m_runner.ProcedureStarted -= Runner_ProcedureStarted;
            }
        }

        private void FixedUpdate() {
            ComputePositionTarget();
            ComputeRotationTarget();
            
            if(m_positionTarget.y == 0) {
                if (fixHeight) {
                    m_rigidBody.constraints |= RigidbodyConstraints.FreezePositionY;
                }
            }
            else {
                m_rigidBody.constraints &= ~RigidbodyConstraints.FreezePositionY;
                m_rigidBody.AddForce(Vector3.up * m_positionTarget.y, ForceMode.VelocityChange);
                m_positionTarget.y = 0;
            }

            m_rigidBody.AddRelativeForce(m_positionTarget, ForceMode.VelocityChange);
            m_rigidBody.AddRelativeTorque(m_eulerTarget, ForceMode.VelocityChange);

            transform.localEulerAngles = new Vector3(ClampAngle(transform.localEulerAngles.x, minPitch, maxPitch), transform.localEulerAngles.y, transform.localEulerAngles.z);
        }

        private void ComputePositionTarget() {
            m_positionTarget = Vector3.zero;
            var leftTouchDelta = Vector2.zero;
            
            if(Input.touchCount > 0) {
                foreach(var touch in Input.touches) {
                    if(touch.position.x < Screen.width / 2) {
                        if (touch.phase == TouchPhase.Began) {
                            m_startLeftTouch = touch.position;
                            leftTouchDelta = touch.deltaPosition;
                        }
                        else {
                            leftTouchDelta = touch.position - m_startLeftTouch;
                        }
                        m_leftTouch = true;
                        break;
                    }
                    m_leftTouch = false;
                }
            }
            else {
                m_leftTouch = false;
            }

            if (m_leftTouch && leftTouchDelta.magnitude > m_dragThresholdInPx) {
                float actualMovementSpeed = Mathf.MoveTowards(0, movementSpeed, leftTouchDelta.magnitude / m_maxSpeedDragRadiusInPx);
                leftTouchDelta.Normalize();
                var leftTouches = 0;
                foreach(var touch in Input.touches) {
                    if (touch.position.x < Screen.width / 2) {
                        leftTouches++;
                    }
                }
                m_positionTarget.x += leftTouchDelta.x * actualMovementSpeed;
                if (leftTouches < 2) {
                    m_positionTarget.z += leftTouchDelta.y * actualMovementSpeed;
                }
                else {
                    m_positionTarget.y += leftTouchDelta.y * actualMovementSpeed;
                }
            }
            else {
                if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) {
                    m_positionTarget += Vector3.forward * movementSpeed;
                }
                if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) {
                    m_positionTarget += Vector3.forward * -movementSpeed;
                }
                if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) {
                    m_positionTarget += Vector3.right * -movementSpeed;
                }
                if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) {
                    m_positionTarget += Vector3.right * movementSpeed;
                }
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) {
                    m_positionTarget += Vector3.down * movementSpeed;
                }
                if (Input.GetKey(KeyCode.Space)) {
                    m_positionTarget += Vector3.up * movementSpeed;
                }
                if (Input.GetKeyDown(KeyCode.R)) {
                    m_positionTarget = m_startPosition;
                    m_eulerTarget = m_startRotation.eulerAngles;
                }
            }
        }

        private void ComputeRotationTarget() {
            //m_eulerTarget = transform.localEulerAngles;
            m_eulerTarget = Vector3.zero;

            if(Input.touchCount > 0) {
                var rightTouchDelta = Vector2.zero;
                foreach (var touch in Input.touches) {
                    if (touch.position.x > Screen.width / 2) {
                        m_rightTouch = true;

                        rightTouchDelta = touch.deltaPosition;

                        break;
                    }
                    m_rightTouch = false;
                }
                if (m_rightTouch) {
                    m_eulerTarget.y = rightTouchDelta.x * touchSensitivityFactor;
                    m_eulerTarget.x = -rightTouchDelta.y * touchSensitivityFactor;
                }
            }
            else if (Input.GetMouseButton(1)) {
                m_eulerTarget.y = Input.GetAxis("Mouse X");
                m_eulerTarget.x = -Input.GetAxis("Mouse Y");
            }

            if (rotationAxis == RotationAxes.PitchAndYaw) {
                m_eulerTarget.y *= sensitivityX * m_dpiRatio;
                m_eulerTarget.x *= sensitivityY * m_dpiRatio;
            }
            else if(rotationAxis == RotationAxes.Pitch) {
                m_eulerTarget.x *= sensitivityY * m_dpiRatio;
            }
            else {
                m_eulerTarget.y *= sensitivityX * m_dpiRatio;
            }

        }

        private float ClampAngle(float angle, float from, float to) {
            if (angle > 180) angle = angle - 360;
            angle = Mathf.Clamp(angle, from, to);
            if (angle < 0) angle = 360 + angle;

            return angle;
        }

        private float WrapAngle(float angle) {
            angle = angle % 360;
            return angle < 0 ? 360 + angle : angle;
        }
    }
}

