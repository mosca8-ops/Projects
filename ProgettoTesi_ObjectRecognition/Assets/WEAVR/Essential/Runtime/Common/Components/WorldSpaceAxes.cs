using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Utilities/World Space Axes")]
    [DefaultExecutionOrder(-30000)]
    public class WorldSpaceAxes : MonoBehaviour
    {
        public delegate void OverlayCameraDelegate(Camera target, Camera overlay);
        public static OverlayCameraDelegate AddCameraOverlay;
        public static OverlayCameraDelegate RemoveCameraOverlay;

        [SerializeField]
        [Button(nameof(SetCorrectLayer), "Set Layer")]
        private Camera m_axesCamera;
        [SerializeField]
        private Transform m_axes;

        public Camera Camera => m_axesCamera;
        public Transform Axes => m_axes;

        public bool AreVisible => gameObject.activeInHierarchy;

        private Quaternion m_targetRotation = Quaternion.identity;
        private Vector3 m_upVector;
        private Transform m_targetTransform;

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void SetCorrectLayer()
        {
            var layer = LayerMask.NameToLayer(Weavr.Overlay3DLayer);
            foreach (var t in gameObject.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = layer;
            }

            if (m_axesCamera)
            {
                m_axesCamera.cullingMask = 1 << layer;
            }
        }

        public void AttachToCamera(Camera camera) => AttachToCamera(camera, true);

        public virtual void AttachToCamera(Camera camera, bool setCameraAsParent)
        {
            AddCameraOverlay?.Invoke(camera, m_axesCamera);
            if (setCameraAsParent)
            {
                transform.SetParent(camera.transform, false);
            }
        }

        public virtual void DetachFromCamera(Camera camera) => DetachFromCamera(camera, true);

        public virtual void DetachFromCamera(Camera camera, bool unparent)
        {
            RemoveCameraOverlay?.Invoke(camera, m_axesCamera);
            if(unparent && transform.parent == camera.transform)
            {
                transform.SetParent(null, false);
            }
        }

        public void SetOrientation(Transform other)
        {
            m_targetTransform = other;
        }

        public void SetOrientation(Vector3 forward, Vector3 up)
        {
            m_targetTransform = null;
            m_targetRotation = Quaternion.LookRotation(forward, up);
        }

        public void ResetOrientation()
        {
            m_targetTransform = null;
            m_targetRotation = Quaternion.identity;
        }

        private void Update()
        {
            if (m_targetTransform)
            {
                m_axes.rotation = m_targetTransform.rotation;
            }
            else
            {
                m_axes.rotation = m_targetRotation;
            }
        }
    }
}
