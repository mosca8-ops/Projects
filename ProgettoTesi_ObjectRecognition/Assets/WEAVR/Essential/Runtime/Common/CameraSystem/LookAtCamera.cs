using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [ExecuteAlways]
    [AddComponentMenu("WEAVR/Camera System/Look At Camera")]
    public class LookAtCamera : MonoBehaviour
    {
        [Draggable]
        [InfoBox(InfoBoxAttribute.InfoIconType.Information, "If left empty it will use the current WEAVR Camera")]
        public Camera cameraToFace;
        [Draggable]
        public Transform rotatingTransform;
        [SerializeField]
        private bool m_keepVectorUp = true;
        [SerializeField]
        private OptionalFloat m_rotationTime;
        [SerializeField]
        private bool m_invertCanvases = true;

        private bool m_useWeavrCamera;
        private Transform m_cameraTarget;

        private Transform CameraTarget
        {
            get
            {
                if (!m_cameraTarget)
                {
                    m_cameraTarget = new GameObject(name + "_CameraTarget").transform;
                    m_cameraTarget.gameObject.hideFlags = HideFlags.HideAndDontSave;
                }
                return m_cameraTarget;
            }
        }

        private void Reset()
        {
            m_rotationTime = 1;
            m_rotationTime.enabled = false;
        }

        private void Awake()
        {
            m_useWeavrCamera = !cameraToFace || cameraToFace == WeavrCamera.CurrentCamera;
            if (!rotatingTransform)
            {
                rotatingTransform = transform;
            }
        }

        private void OnValidate()
        {
            if(m_rotationTime.value < 0)
            {
                m_rotationTime.value = 0;
            }
        }

        private void OnEnable()
        {
            if (m_useWeavrCamera)
            {
                if (WeavrCamera.CurrentCamera)
                {
                    cameraToFace = WeavrCamera.CurrentCamera;
                }
                WeavrCamera.CameraChanged -= CameraChanged;
                WeavrCamera.CameraChanged += CameraChanged;
            }
        }

        private void OnDisable()
        {
            if (m_useWeavrCamera)
            {
                WeavrCamera.CameraChanged -= CameraChanged;
            }
            CheckAndDestroyTempObjects();
        }

        private void CameraChanged(Camera newCamera)
        {
            if (newCamera)
            {
                cameraToFace = newCamera;
            }
        }

        void Update() 
        {
            if (m_rotationTime.enabled && m_rotationTime.value > 0)
            {
                var cameraTarget = CameraTarget;
                cameraTarget.SetPositionAndRotation(rotatingTransform.position, rotatingTransform.rotation);
                if (m_keepVectorUp)
                {
                    cameraTarget.LookAt(cameraToFace.transform, Vector3.up);
                }
                else
                {
                    cameraTarget.LookAt(cameraToFace.transform);
                }
                if(m_invertCanvases && rotatingTransform is RectTransform)
                {
                    cameraTarget.Rotate(Vector3.up, 180, Space.Self);
                }
                rotatingTransform.rotation = Quaternion.Slerp(rotatingTransform.rotation, cameraTarget.rotation, Time.deltaTime / m_rotationTime.value);
            }
            else
            {
                if (m_keepVectorUp)
                {
                    rotatingTransform.LookAt(cameraToFace.transform, Vector3.up);
                }
                else
                {
                    rotatingTransform.LookAt(cameraToFace.transform);
                }
                if (m_invertCanvases && rotatingTransform is RectTransform)
                {
                    rotatingTransform.Rotate(Vector3.up, 180, Space.Self);
                }
                CheckAndDestroyTempObjects();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckAndDestroyTempObjects()
        {
            if (m_cameraTarget)
            {
                if (Application.isPlaying)
                {
                    Destroy(m_cameraTarget.gameObject);
                }
                else
                {
                    DestroyImmediate(m_cameraTarget.gameObject);
                }
            }
        }
    }
}