#if  WEAVR_EXTENSIONS_MRTK && TO_TEST 
namespace TXT.WEAVR.InteractionUI
{
    using UnityEngine;

    public class PointerFollower : MonoBehaviour
    {
        [Tooltip("Should be set if pointer manager is null")]
        public Transform pointerToFollow;
        public PointerManager pointerManager;
        public Transform cameraTransform;
        public Transform pointerToTranslate;

        [Header("Configuration")]
        public bool followPointer = false;
        public bool includeChildren = false;
        public bool keepSameDistance = false;
        public bool flipCanvas = false;
        public float followSpeed = 10f;
        public float relativeDistanceFromPointer = 0.1f;
        public float maxDistanceFromCamera = 1f;
        public float minDistanceFromCamera = 0.2f;
        [Tooltip("If pointer is within this radius from the object center, the object will not follow the pointer")]
        public float keepStableRadius = 999f;

        private bool _isPointedByPointer;
        private bool _pointerAtCameraCenter;
        private Transform _thisTransform;
        private Transform _lookAtTransform;

        private float _distanceToCamera;

        private void OnValidate()
        {
            if (pointerManager != null)
            {
                //if (pointerToFollow == null)
                //{
                //    pointerToFollow = pointerManager.pointer;
                //    Debug.LogWarning("Cursor set to pointer manager cursor");
                //}
                if (cameraTransform == null && pointerManager.pointerRaycaster != null && pointerManager.pointerRaycaster.pointerCamera != null)
                {
                    cameraTransform = pointerManager.pointerRaycaster.pointerCamera.transform;
                }
            }
            if (cameraTransform != null)
            {
                var cameraComponent = cameraTransform.GetComponentInChildren<Camera>();
                var canvas = GetComponentInChildren<Canvas>();
                canvas.worldCamera = cameraComponent;
            }
        }

        // Use this for initialization
        void Start()
        {
            if (pointerManager != null)
            {
                pointerManager.PointingObjectChanged += PointerManager_PointingObjectChanged;
                //pointerManager.pointerRaycaster.PointedObject += PointerRaycaster_PointedObject;
            }
            else
            {
                _pointerAtCameraCenter = pointerToFollow == null;
            }
            if (cameraTransform == null)
            {
                cameraTransform = Camera.main.transform;
            }
            _thisTransform = transform;
            _lookAtTransform = new GameObject().transform;
            _lookAtTransform.position = _thisTransform.position;
            _lookAtTransform.rotation = _thisTransform.rotation;
            _distanceToCamera = (cameraTransform.position - _thisTransform.position).magnitude;
        }

        private void PointerRaycaster_PointedObject(PointerRaycaster.PointedDataAll allPointedData)
        {
            if (allPointedData.pointedObject != null)
            {
                var currentTransform = allPointedData.pointedObject.transform;
                while (includeChildren && currentTransform != _thisTransform)
                {
                    currentTransform = currentTransform.parent;
                }
                _isPointedByPointer = currentTransform != _thisTransform;
            }
            else
            {
                _isPointedByPointer = false;
            }
        }

        private void PointerManager_PointingObjectChanged(GameObject previous, GameObject current)
        {
            if (current != null)
            {
                var currentTransform = current.transform;
                while (includeChildren && currentTransform != null && currentTransform != _thisTransform)
                {
                    currentTransform = currentTransform.parent;
                }
                _isPointedByPointer = currentTransform == _thisTransform;
            }
            else
            {
                _isPointedByPointer = false;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (followPointer)
            {
                float fraction = followSpeed * Time.deltaTime;
                if (!_isPointedByPointer)
                {
                    var pointer = pointerToFollow != null ? pointerToFollow : pointerManager.pointer;
                    var pointerPosition = pointerToFollow == null ?
                                          cameraTransform.position + cameraTransform.forward * _distanceToCamera :
                                          pointer.position - pointer.forward * relativeDistanceFromPointer;
                    _lookAtTransform.position = pointerPosition;
                }
                if (keepSameDistance)
                {
                    _distanceToCamera = Mathf.Clamp((cameraTransform.position - _thisTransform.position).magnitude, minDistanceFromCamera, maxDistanceFromCamera);
                    _lookAtTransform.position = cameraTransform.position + (_lookAtTransform.position - cameraTransform.position).normalized * _distanceToCamera;
                }

                //if (keepSameDistance) {
                //    // Set the correct distance from camera

                //}
                _thisTransform.position = Vector3.Lerp(_thisTransform.position, _lookAtTransform.position, fraction);
                _lookAtTransform.LookAt(cameraTransform.transform);
                if (flipCanvas)
                {
                    _lookAtTransform.Rotate(_lookAtTransform.up, 180f, Space.World);
                }
                _thisTransform.rotation = Quaternion.Lerp(_thisTransform.rotation, _lookAtTransform.rotation, fraction);
            }
            else if (keepSameDistance)
            {
                _distanceToCamera = (cameraTransform.position - _thisTransform.position).magnitude;
            }
        }

        private void Instance_FocusEntered(GameObject focusedObject)
        {
            throw new System.NotImplementedException();
        }

        private bool IsPointerOutsideRadius(Vector3 pointerPosition)
        {
            return false;
        }

        private Vector3 GetPointerPosition()
        {
            return Vector3.zero;
        }
    }
}
#endif
