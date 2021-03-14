using System;
using TXT.WEAVR;
using TXT.WEAVR.Core;
using TXT.WEAVR.InteractionUI;
using TXT.WEAVR.Procedure;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.Common
{
    public class ObjectOrbit : MonoBehaviour
    {
        [SerializeField]
        private Transform m_targetObject;
        [SerializeField]
        private bool useCurrentDistance = false;
        [SerializeField]
        [HiddenBy(nameof(useCurrentDistance), hiddenWhenTrue: true)]
        private float _distanceX = 0.0f;
        [SerializeField]
        [HiddenBy(nameof(useCurrentDistance), hiddenWhenTrue: true)]
        private float _distanceY = 0.0f;
        [SerializeField]
        [HiddenBy(nameof(useCurrentDistance), hiddenWhenTrue: true)]
        private float _distanceZ = 1f;
        [SerializeField]
        private bool isCanvas = false;
        [SerializeField]
        private float moveTime = 1f;
        [SerializeField]
        private bool useDelta = true;
        [SerializeField]
        [HiddenBy(nameof(useDelta), hiddenWhenTrue: false)]
        private float deltaRotation = 20f;
        [SerializeField]
        [HiddenBy(nameof(useDelta), hiddenWhenTrue: false)]
        private float deltaPosition = 0.20f;
        [Header("Follow Rotation Axis")]
        [SerializeField]
        private bool rotationX = true;
        [SerializeField]
        private bool rotationY = true;
        [SerializeField]
        private bool rotationZ = true;


        private Transform m_transformTarget;
        private Transform m_oldTargetObject;
        private int rotX;
        private int rotY;
        private int rotZ;

        private Transform m_targetPoint;
        public Transform TargetPoint
        {
            get
            {
                if (!m_targetPoint)
                {
                    m_targetPoint = new GameObject("Temp_ObjectOrbit_TargetPoint").transform;
                    m_targetPoint.gameObject.hideFlags = HideFlags.HideAndDontSave;
                    m_targetPoint.SetParent(m_targetObject, false);
                    m_targetPoint.localPosition = Vector3.right * (_distanceX) + Vector3.up * (_distanceY) + Vector3.forward * (_distanceZ);
                    m_targetPoint.LookAt(m_targetObject, Vector3.up);
                    if (isCanvas)
                    {
                        m_targetPoint.Rotate(Vector3.up, 180, Space.Self);
                    }
                }
                return m_targetPoint;
            }
        }

        public Transform TransformTarget
        {
            get
            {
                if (!m_transformTarget)
                {
                    m_transformTarget = new GameObject("Temp_ObjectOrbit_TransformTarget").transform;
                    m_transformTarget.gameObject.hideFlags = HideFlags.HideAndDontSave;
                }
                return m_transformTarget;
            }
            
        }

        public Transform TargetObject
        {
            get
            {
                return m_targetObject;
            }
            set
            {
                m_targetObject = value;
            }
        }

        private void Start()
        {
            if (useCurrentDistance)
            {
                _distanceX = transform.position.x - m_targetObject.position.x;
                _distanceY = transform.position.y - m_targetObject.position.y;
                if (isCanvas)
                {
                    _distanceZ = -(transform.position.z - m_targetObject.position.z);
                }
                else
                {
                    _distanceZ = transform.position.z - m_targetObject.position.z;
                }
            }

            m_oldTargetObject = new GameObject("oldTransformObject").transform;
            m_oldTargetObject.gameObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(m_oldTargetObject);
            rotX = rotationX ? 1 : 0;
            rotY = rotationY ? 1 : 0;
            rotZ = rotationZ ? 1 : 0;
        }


        void Update()
        {
            if (!m_targetObject)
            {
                return;
            }
            MovementCalculation();
            transform.position = Vector3.Lerp(transform.position, TransformTarget.position, Time.deltaTime / moveTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, TransformTarget.rotation, Time.deltaTime / moveTime);
        }

        private Transform MovementCalculation()
        {
            if (CheckDelta())
            {
                TransformTarget.SetPositionAndRotation(m_targetObject.position, TargetPoint.rotation);
                TransformTarget.rotation = Quaternion.Euler(TargetPoint.rotation.eulerAngles.x * rotX, TargetPoint.rotation.eulerAngles.y * rotY, TargetPoint.rotation.eulerAngles.z * rotZ);
                TransformTarget.Translate(new Vector3(_distanceX, _distanceY, _distanceZ));
                m_oldTargetObject.SetPositionAndRotation(m_targetObject.position, m_targetObject.rotation);
            }
            return TransformTarget;
        } 

        private bool CheckDelta()
        {
            if (useDelta)
            {
                if (Vector3.Distance(transform.position, TransformTarget.position) < 0.1)
                {
                    return (Mathf.Abs(m_targetObject.eulerAngles.y - m_oldTargetObject.eulerAngles.y) * rotY > deltaRotation || Mathf.Abs(m_targetObject.eulerAngles.x - m_oldTargetObject.eulerAngles.x) * rotX > deltaRotation
                        || Mathf.Abs(m_targetObject.eulerAngles.z - m_oldTargetObject.eulerAngles.z) * rotZ > deltaRotation || Vector3.Distance(m_targetObject.position, m_oldTargetObject.position) > deltaPosition);
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
            
        }
    }
}
