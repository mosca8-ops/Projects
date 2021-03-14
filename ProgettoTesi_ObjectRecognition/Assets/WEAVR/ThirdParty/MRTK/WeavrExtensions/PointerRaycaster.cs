#if  WEAVR_EXTENSIONS_MRTK && TO_TEST 
namespace TXT.WEAVR.InteractionUI
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;

    public class PointerRaycaster : MonoBehaviour
    {
        public delegate void OnRaycastSuccess(PointedData pointedData);
        public delegate void OnFullRaycastSuccess(PointedDataAll allPointedData);

        public enum PointedDataType
        {
            None = 0,
            World3D = 1,
            World2D = 2,
            World3D2D = 3,
            Canvas = 4,
            All = 7
        }

        public Camera pointerCamera;
        public bool isRaycasting;
        public bool orderByDistance;

        [Header("Cursor Related")]
        public Transform pointer;
        public bool is3dPointer = true;

        [Range(0, 100)]
        public float maxPointerDepth;
        [Range(0, 100)]
        public float maxHitDistance;

        public event OnRaycastSuccess Pointed3DObject;
        public event OnRaycastSuccess Pointed2DObject;
        public event OnRaycastSuccess PointedWorldObject;
        public event OnRaycastSuccess PointedCanvasObject;
        public event OnFullRaycastSuccess PointedObject;

        protected Transform _cameraTransform;

        protected PointerEventData _pointerData;

        private List<RaycastResult> _raycastResults;

        protected PointedDataAll _pointedData;

        private bool _isDefaultCursor;

        private void OnValidate()
        {
            if (pointerCamera == null)
            {
                pointerCamera = Camera.main;
            }
            if (pointer == null)
            {
                Debug.LogWarning("Cursor is not set for PointerRaycaster, the default mouse input will be used instead.");
                Debug.Log("Cursor is not set for PointerRaycaster, the default mouse input will be used instead.");
            }
        }

        // Use this for initialization
        void Start()
        {
            _raycastResults = new List<RaycastResult>();
            _pointerData = new PointerEventData(EventSystem.current);
            _cameraTransform = pointerCamera.transform;
            _isDefaultCursor = pointer == null;
            _pointedData = new PointedDataAll()
            {
                pointed2D = new PointedData()
                {
                    type = PointedDataType.World2D,
                    pointingCamera = pointerCamera
                },
                pointed3D = new PointedData()
                {
                    type = PointedDataType.World3D,
                    pointingCamera = pointerCamera
                },
                pointedCanvas = new PointedData()
                {
                    type = PointedDataType.Canvas,
                    pointingCamera = pointerCamera
                }
            };
        }

        // Update is called once per frame
        void Update()
        {
            if (!isRaycasting)
            {
                return;
            }
            _pointedData.Invalidate();
            bool requiresFullRaycasting = PointedObject != null;
            bool requiresWorldRaycasting = PointedWorldObject != null || requiresFullRaycasting;
            if ((requiresFullRaycasting || PointedCanvasObject != null) && RaycastCanvas())
            {
                if (PointedCanvasObject != null) { PointedCanvasObject(_pointedData.pointedCanvas); }
            }
            if ((requiresWorldRaycasting || Pointed3DObject != null) && RaycastWorld3D())
            {
                if (Pointed3DObject != null) { Pointed3DObject(_pointedData.pointed3D); }
                if (PointedWorldObject != null) { PointedWorldObject(_pointedData.pointed3D); }
            }
            if ((requiresWorldRaycasting || Pointed2DObject != null) && RaycastWorld2D())
            {
                if (Pointed2DObject != null) { Pointed2DObject(_pointedData.pointed2D); }
                if (PointedWorldObject != null) { PointedWorldObject(_pointedData.pointed2D); }
            }

            if (requiresFullRaycasting)
            {
                _pointedData.Readjust(orderByDistance);
                PointedObject(_pointedData);
            }
        }

        protected virtual bool RaycastWorld3D()
        {
            RaycastHit hitInfo;
            // Raycast 3d
            if (Physics.Raycast(_cameraTransform.position, (pointer.position - _cameraTransform.position) * maxHitDistance,
                    out hitInfo, maxHitDistance, Physics.AllLayers))
            {
                _pointedData.pointed3D.pointedObject = hitInfo.collider.gameObject;
                _pointedData.pointed3D.pointedPosition = hitInfo.point;
                _pointedData.pointed3D.isValid = true;
                _pointedData.pointed3D.sqrDistanceToCamera = (_cameraTransform.position - hitInfo.point).sqrMagnitude;
                return true;
            }
            return false;
        }

        protected virtual bool RaycastWorld2D()
        {
            // Raycast 2d
            var raycast2d = Physics2D.Raycast(_cameraTransform.position, (pointer.position - _cameraTransform.position));
            if (raycast2d.transform != null)
            {
                _pointedData.pointed2D.pointedObject = raycast2d.transform.gameObject;
                _pointedData.pointed2D.pointedPosition = raycast2d.point;
                _pointedData.pointed2D.pointedPosition.z = raycast2d.transform.position.z;
                _pointedData.pointed2D.isValid = true;
                _pointedData.pointed2D.sqrDistanceToCamera = (_cameraTransform.position - _pointedData.pointed2D.pointedPosition).sqrMagnitude;
                return true;
            }
            return false;
        }

        protected virtual bool RaycastCanvas()
        {
            var cameraToPointer = pointer.position - _cameraTransform.position;
            bool isBehindCamera = Vector3.Dot(cameraToPointer, _cameraTransform.forward) < 0;
            var cursorPosition = _isDefaultCursor ? GetDefaultCursorPosition() :
                                 pointerCamera.WorldToScreenPoint(isBehindCamera ? (_cameraTransform.position - cameraToPointer) : pointer.position);
            _pointerData.Reset();
            _pointerData.position = cursorPosition;
            _raycastResults.Clear();
            EventSystem.current.RaycastAll(_pointerData, _raycastResults);
            //RaycastResult minResult = new RaycastResult();
            //float minDistanceToCamera = float.MaxValue;
            //var cameraPosition = _cameraTransform.position;
            foreach (var result in _raycastResults)
            {
                if (result.isValid)
                {
                    //var distanceToCamera = (cameraPosition - result.gameObject.transform.position).sqrMagnitude;
                    //if(distanceToCamera <= minDistanceToCamera) {
                    //    minDistanceToCamera = distanceToCamera;
                    //    minResult = result;
                    //}
                    _pointedData.pointedCanvas.pointedObject = result.gameObject;
                    _pointedData.pointedCanvas.pointedPosition = result.gameObject.transform.position;
                    _pointedData.pointedCanvas.isValid = true;
                    _pointedData.pointedCanvas.sqrDistanceToCamera = (_cameraTransform.position - result.gameObject.transform.position).sqrMagnitude;
                    return true;
                }
            }
            //if (minResult.isValid) {
            //    _pointedData.pointedCanvas.pointedObject = minResult.gameObject;
            //    _pointedData.pointedCanvas.pointedPosition = minResult.gameObject.transform.position;
            //    _pointedData.pointedCanvas.isValid = true;
            //    _pointedData.pointedCanvas.sqrDistanceToCamera = minDistanceToCamera;
            //    return true;
            //}
            return false;
        }

        protected virtual Vector3 GetDefaultCursorPosition()
        {
            return Input.mousePosition;
        }

        public struct PointedData
        {
            public bool isValid;
            public PointedDataType type;
            public GameObject pointedObject;
            public Vector3 pointedPosition;
            public Camera pointingCamera;
            public float sqrDistanceToCamera;
        }

        public struct PointedDataAll
        {
            public bool isValid;
            public PointedDataType type;
            public Camera pointingCamera;
            public GameObject pointedObject;
            public Vector3 pointedPosition;
            public PointedData pointed3D;
            public PointedData pointed2D;
            public PointedData pointedCanvas;

            internal void Readjust(bool orderByDistance)
            {
                isValid = pointed2D.isValid || pointed3D.isValid || pointedCanvas.isValid;
                type = pointed2D.isValid && pointed3D.isValid && pointedCanvas.isValid ? PointedDataType.All :
                       pointedCanvas.isValid ? PointedDataType.Canvas :
                       pointed2D.isValid && pointed3D.isValid ? PointedDataType.World3D2D :
                       pointed2D.isValid ? PointedDataType.World2D :
                       pointed3D.isValid ? PointedDataType.World3D :
                       PointedDataType.None;
                switch (type)
                {
                    case PointedDataType.Canvas:
                        pointedObject = pointedCanvas.pointedObject;
                        break;
                    case PointedDataType.World2D:
                        pointedObject = pointed2D.pointedObject;
                        break;
                    case PointedDataType.World3D2D:
                        pointedObject = orderByDistance && pointed2D.sqrDistanceToCamera <= pointed3D.sqrDistanceToCamera ?
                                        pointed2D.pointedObject : pointed3D.pointedObject;
                        break;
                    case PointedDataType.All:
                        if (!orderByDistance)
                        {
                            pointedObject = pointedCanvas.isValid ? pointedCanvas.pointedObject :
                                            pointed3D.isValid ? pointed3D.pointedObject :
                                            pointed2D.isValid ? pointed2D.pointedObject : null;
                            break;
                        }
                        if (pointedCanvas.sqrDistanceToCamera <= pointed2D.sqrDistanceToCamera)
                        {
                            if (pointedCanvas.sqrDistanceToCamera <= pointed3D.sqrDistanceToCamera)
                            {
                                pointedObject = pointedCanvas.pointedObject;
                            }
                            else
                            {
                                pointedObject = pointed3D.pointedObject;
                            }
                        }
                        else if (pointed2D.sqrDistanceToCamera <= pointed3D.sqrDistanceToCamera)
                        {
                            pointedObject = pointed2D.pointedObject;
                        }
                        else if (pointed3D.sqrDistanceToCamera <= pointedCanvas.sqrDistanceToCamera)
                        {
                            pointedObject = pointed3D.pointedObject;
                        }
                        break;
                    case PointedDataType.World3D:
                        pointedObject = pointed3D.pointedObject;
                        break;
                    default:
                        pointedObject = null;
                        break;
                }
                //pointedObject = pointedCanvas.isValid ? pointedCanvas.pointedObject :
                //                    pointed3D.isValid ? pointed3D.pointedObject :
                //                    pointed2D.isValid ? pointed2D.pointedObject : 
                //                    null;
            }

            internal void Invalidate()
            {
                pointed2D.isValid = pointed3D.isValid = pointedCanvas.isValid = false;
            }
        }
    }
}
#endif
