using UnityEngine;
using System;
using System.Collections;
using System.Threading.Tasks;
using TXT.WEAVR.Core;
using TXT.WEAVR.Common;
#if WEAVR_AR
using System.IO;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
#endif

namespace TXT.WEAVR.AR
{
    [AddComponentMenu("WEAVR/AR/AR Object Positioner")]
    public class ARObjectPositioner : MonoBehaviour, IARObjectPositioner
    {
        public bool Active { get => isActiveAndEnabled; set { gameObject.SetActive(value); enabled = true; } }
#pragma warning disable CS0067
        public event OnValueChanged<bool> MarkerAquired;
        public event OnValueChanged<bool> ObjectSetOnSurface;
#pragma warning restore CS0067

        public Camera ARCamera { get; private set; }
        public bool ShowLineToSurface { get; set; }

#if WEAVR_AR

        [SerializeField]
        private ARSession m_arSession;
        [SerializeField]
        private ARTrackedImageManager _arTrackedImageManager;
        [SerializeField]
        private ARPlaneManager _arPlaneManager;
        [SerializeField]
        private ARSessionOrigin _arSessionOrigin;
        [SerializeField]
        private ARRaycastManager _arRayCastManager;
        [SerializeField]
        private ARAnchorManager _arAnchorManager;

        [SerializeField]
        [Space]
        private GameObject m_placedObject;
        [SerializeField]
        private DottedLine m_lineToSurface;
        [SerializeField]
        private float m_lineVisibilityDuration = 1f;
        [SerializeField]
        private WorldSpaceAxes m_axes;

        private ARTrackedImage _arTrackedImage;
        private ARAnchor _arAnchor;

        private Vector3 _positionOffset = Vector3.zero;
        private Vector3 _rotationOffset = Vector3.zero;
        private float _scale = 1;

        private Vector3 m_lastIntersectionPoint;
        private int m_lastIntersectionFrame;

        private ARTargetMode _currentMode = ARTargetMode.Marker;
        private SavedData savedData;

        private bool? m_isArSupported = null;

        private Vector3 m_placedObjectOriginalPosition;
        private Quaternion m_placedObjectOriginalRotation;
        private Vector3 m_placedObjectOriginalScale;
        private Transform m_placedObjectOriginalParent;

        public string SaveDataPath => $"{Application.persistentDataPath}/trackedInfo/object_parameters_{gameObject.scene.name}_{(m_placedObject ? m_placedObject.name : "")}_{CurrentARTargetMode}.json";

        private Coroutine m_surfaceInfoCoroutine;
        private Coroutine m_axisToLocalCoroutine;

        [Serializable]
        public struct SavedData
        {
            public Vector3 positionOffset;
            public Vector3 rotationOffset;
            public float scale;
        }

        public Vector3 PositionOffset {
            get => _positionOffset;
            set {
                if (_positionOffset != value)
                {
                    _positionOffset = value;
                    SetAxisToWorld();
                    AdjustTransform();
                }
            }
        }

        public Vector3 RotationOffset {
            get => _rotationOffset;
            set {
                if (_rotationOffset != value)
                {
                    _rotationOffset = value;
                    SetAxisToLocal();
                    AdjustTransform();
                }
            }
        }
        
        public float Scale {
            get => _scale;
            set {
                if (_scale != value)
                {
                    _scale = value;
                    AdjustTransform();
                }
            }
        }

        public ARTargetMode CurrentARTargetMode {
            get {
                return _currentMode;
            }
            set {
                _currentMode = value;
                ResumeTracking();
            }
        }

        public GameObject ARTarget 
        { 
            get => m_placedObject;
            set
            {
                if(m_placedObject != value)
                {
                    Transform currentParent = null;
                    if (m_placedObject)
                    {
                        currentParent = m_placedObject.transform.parent;
                        RestorePlacedObjectState();
                    }
                    m_placedObject = value;
                    if (m_placedObject)
                    {
                        SavePlacedObjectState();
                        if (currentParent && (currentParent == _arAnchor || currentParent == _arTrackedImage))
                        {
                            m_placedObject.transform.SetParent(currentParent, false);
                            //JsonDeseralize();
                            //ResetTranformValues();
                            RotationOffset = Vector3.zero;
                            PositionOffset = Vector3.zero;
                            Scale = 1;
                        }
                    }
                }
            }
        }


        public bool ShowWorldAxes
        {
            get => m_axes && m_axes.AreVisible;
            set
            {
                if (m_axes)
                {
                    if(value) { m_axes.Show(); }
                    else { m_axes.Hide(); }
                };
            }
        }

        public Gradient LineToSurfaceGradient { 
            get => m_lineToSurface ? m_lineToSurface.LineGradient : null;
            set => m_lineToSurface.LineGradient = value; 
        }

        private void StopPlaneTracking()
        {
            _arPlaneManager.requestedDetectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.None;

            EnableSurfacePlanes(false);
        }

        private void EnableSurfacePlanes(bool enable)
        {
            foreach (var plane in _arPlaneManager.trackables)
            {
                //Destroy(plane.gameObject);
                if (plane)
                {
                    plane.gameObject.SetActive(enable);
                }
            }
        }

        private void StopImageTracking()
        {
            foreach (var plane in _arTrackedImageManager.trackables)
            {
                //Destroy(plane.gameObject);
                plane.gameObject.SetActive(false);
            }
        }

        private void Awake()
        {
            if (!m_arSession) m_arSession = FindObjectOfType<ARSession>();
            if (!_arTrackedImageManager) _arTrackedImageManager = FindObjectOfType<ARTrackedImageManager>();
            if (!_arSessionOrigin) _arSessionOrigin = FindObjectOfType<ARSessionOrigin>();
            if (!_arRayCastManager) _arRayCastManager = FindObjectOfType<ARRaycastManager>();
            if (!_arPlaneManager) _arPlaneManager = FindObjectOfType<ARPlaneManager>();
            if (!_arAnchorManager) _arAnchorManager = FindObjectOfType<ARAnchorManager>();

            if (_arSessionOrigin)
            {
                ARCamera = _arSessionOrigin.camera;
            }

            if (ARCamera && m_axes)
            {
                m_axes.AttachToCamera(ARCamera);
            }

            if (!Directory.Exists(Application.persistentDataPath + "/trackedInfo"))
            {
                Directory.CreateDirectory(Application.persistentDataPath + "/trackedInfo");
            }

            _arPlaneManager.requestedDetectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.None;
        }

        IEnumerator Start()
        {
            Debug.Log("Initial ARSessione.state == " + ARSession.state);
            if (ARSession.state == ARSessionState.None || ARSession.state == ARSessionState.CheckingAvailability)
            {
                yield return ARSession.CheckAvailability();
            }
            Debug.Log("ARSessione.state == " + ARSession.state);

            if (ARSession.state == ARSessionState.Unsupported)
            {
                m_isArSupported = false;
            }
            else if(ARSession.state == ARSessionState.NeedsInstall)
            {
                yield return ARSession.Install();
            }
            else
            {
                m_isArSupported = true;
            }

            JsonDeseralize();
        }

        public void OnEnable()
        {
            SavePlacedObjectState();
            try
            {
                _arTrackedImageManager.trackedImagesChanged -= OnImageChanged;
                _arTrackedImageManager.trackedImagesChanged += OnImageChanged;
            }
            catch(Exception ex)
            {
                WeavrDebug.LogException(this, ex);
            }
        }

        private void SavePlacedObjectState()
        {
            if (m_placedObject)
            {
                m_placedObjectOriginalPosition = m_placedObject.transform.localPosition;
                m_placedObjectOriginalRotation = m_placedObject.transform.localRotation;
                m_placedObjectOriginalScale = m_placedObject.transform.localScale;
                m_placedObjectOriginalParent = m_placedObject.transform.parent;
            }
        }

        public void OnDisable()
        {
            if (_arAnchor) { Destroy(_arAnchor); }

            RestorePlacedObjectState();

            _arTrackedImageManager.trackedImagesChanged -= OnImageChanged;
            m_arSession.Reset();
        }

        private void RestorePlacedObjectState()
        {
            if (m_placedObject)
            {
                m_placedObject.transform.SetParent(m_placedObjectOriginalParent, false);
                m_placedObject.transform.localPosition = m_placedObjectOriginalPosition;
                m_placedObject.transform.localRotation = m_placedObjectOriginalRotation;
                m_placedObject.transform.localScale = m_placedObjectOriginalScale;
            }
        }

        private void OnImageChanged(ARTrackedImagesChangedEventArgs args)
        {
            if (_currentMode == ARTargetMode.Marker)
            {
                foreach (var trackedImage in args.removed)
                {
                    if (_arTrackedImage == trackedImage)
                    {
                        MarkerAquired?.Invoke(false);
                        break;
                    }
                }
                foreach (var trackedImage in args.added)
                {
                    PlaceOnAquiredImage(trackedImage);
                    return;
                }
                foreach (var trackedImage in args.updated)
                {
                    PlaceOnAquiredImage(trackedImage);
                    return;
                }
            }
        }

        private void PlaceOnAquiredImage(ARTrackedImage trackedImage)
        {
            JsonDeseralize();
            _arTrackedImage = trackedImage;
            if (m_placedObject && m_placedObject != _arTrackedImage.gameObject)
            {
                m_placedObject.transform.SetParent(_arTrackedImage.transform, false);
                PositionOffset = Vector3.zero;
                RotationOffset = Vector3.zero;
            }
            ResetTranformValues();
            AdjustTransform();
            MarkerAquired?.Invoke(true);
        }

        private void SetAxisToWorld()
        {
            if (m_axes)
            {
                if(m_axisToLocalCoroutine != null)
                {
                    StopCoroutine(m_axisToLocalCoroutine);
                    m_axisToLocalCoroutine = null;
                }
                m_axes.ResetOrientation();
            }
        }

        private void SetAxisToLocal()
        {
            if (m_axes && m_placedObject)
            {
                if (m_axisToLocalCoroutine != null)
                {
                    StopCoroutine(m_axisToLocalCoroutine);
                    m_axisToLocalCoroutine = null;
                }
                m_axes.SetOrientation(m_placedObject.transform.forward, Vector3.up);
                m_axisToLocalCoroutine = StartCoroutine(RevertAxesToWorld(0.5f));
            }
        }

        private IEnumerator RevertAxesToWorld(float delay)
        {
            yield return new WaitForSeconds(delay);
            m_axes.ResetOrientation();
            m_axisToLocalCoroutine = null;
        }

        private void AdjustTransform()
        {
            if ((_currentMode == ARTargetMode.Marker && _arTrackedImage != null) || (_currentMode == ARTargetMode.Surface && _arAnchor != null))
            {
                if (m_placedObject)
                {
                    m_placedObject.transform.localPosition = _arAnchor.transform.InverseTransformVector(_positionOffset);
                    m_placedObject.transform.localEulerAngles = _rotationOffset;
                    m_placedObject.transform.localScale = Vector3.one * _scale;
                }
                else
                {
                    transform.localScale = Vector3.one * _scale;

                    transform.parent = null;

                    if (_currentMode == ARTargetMode.Marker)
                    {
                        _arSessionOrigin.MakeContentAppearAt(transform, _arTrackedImage.transform.position + _positionOffset, Quaternion.Euler(transform.eulerAngles + _rotationOffset));
                    }
                    else
                    {
                        _arSessionOrigin.MakeContentAppearAt(transform, _arAnchor.transform.position + _positionOffset, Quaternion.Euler(transform.rotation.eulerAngles + _rotationOffset));
                    }

                    transform.parent = _arSessionOrigin.transform;
                }
            }

            if (ShowLineToSurface)
            {
                ShowSurfaceInfo(m_lineVisibilityDuration);
            }
        }

        private void HideSurfaceInfo()
        {
            if(_currentMode == ARTargetMode.Surface)
            {
                if (m_lineToSurface) { m_lineToSurface.Hide(); }
                EnableSurfacePlanes(false);
            }
            if(m_surfaceInfoCoroutine != null)
            {
                StopCoroutine(m_surfaceInfoCoroutine);
            }
        }

        private void ShowSurfaceInfo(float duration)
        {
            if (m_surfaceInfoCoroutine != null) { StopCoroutine(m_surfaceInfoCoroutine); }
            if (_currentMode == ARTargetMode.Surface)
            {
                if (m_lineToSurface && m_placedObject && _arAnchor)
                {
                    m_lineToSurface.TranformA = m_placedObject.transform;
                    var pointB = m_placedObject.transform.position;
                    pointB.y = _arAnchor.transform.position.y;
                    m_lineToSurface.PointB = pointB;
                    m_lineToSurface.Show();
                }
                EnableSurfacePlanes(true);
                m_surfaceInfoCoroutine = StartCoroutine(ShowSurfaceInfoCoroutine(duration));
            }
        }

        private IEnumerator ShowSurfaceInfoCoroutine(float duration)
        {
            float timeToHide = Time.time + duration;
            while(Time.time < timeToHide)
            {
                if (m_lineToSurface && m_placedObject && _arAnchor)
                {
                    m_lineToSurface.TranformA = m_placedObject.transform;
                    var pointB = m_placedObject.transform.position;
                    pointB.y = _arAnchor.transform.position.y;
                    m_lineToSurface.PointB = pointB;
                }
                yield return null;
            }
            HideSurfaceInfo();
        }

        public void PositionOnSurface(Vector2 touchPosition)
        {
            if (_arRayCastManager.Raycast(touchPosition, hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon))
            {
                var hitPose = hits[0].pose;


                var touchedPlane = _arPlaneManager.GetPlane(hits[0].trackableId);

                if (touchedPlane)
                {
                    if (_arAnchor != null)
                    {
                        Destroy(_arAnchor);
                    }

                    _arAnchor = _arAnchorManager.AttachAnchor(touchedPlane, hitPose);
                    if (m_placedObject && m_placedObject != _arAnchor.gameObject)
                    {
                        m_placedObject.transform.SetParent(_arAnchor.transform, false);
                        PositionOffset = Vector3.zero;
                        RotationOffset = Vector3.zero;
                    }
                }

                JsonDeseralize();
                ResetTranformValues();
                AdjustTransform();
                StopPlaneTracking();

                ObjectSetOnSurface?.Invoke(true);
            }
        }

        public bool TryMoveOnSurface(Vector2 touchPosition, bool isDeltaMove)
        {
            if (TryGetIntersectionPoint(touchPosition, out Vector3 point))
            {
                Vector2 delta;
                if (isDeltaMove)
                {
                    if (!_arAnchor) { return false; }
                    delta = point - _arAnchor.transform.position;
                    delta.y = PositionOffset.y;
                    PositionOffset = delta;
                    return true;
                }

                if(m_lastIntersectionFrame != Time.frameCount - 1)
                {
                    m_lastIntersectionPoint = point;
                }

                PositionOffset += point - m_lastIntersectionPoint;
                m_lastIntersectionPoint = point;
                m_lastIntersectionFrame = Time.frameCount;
                return true;
            }
            return false;
        }

        private bool TryGetIntersectionPoint(Vector2 touchPosition, out Vector3 point)
        {
            if (_arRayCastManager.Raycast(touchPosition, hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon))
            {
                point = hits[0].pose.position;
                return true;
            }
            else if (_arPlaneManager && ARCamera && _arAnchor)
            {
                // Try intersect infinite planes
                var ray = ARCamera.ScreenPointToRay(touchPosition);
                foreach (var plane in _arPlaneManager.trackables)
                {
                    //Destroy(plane.gameObject);
                    if (plane && plane.infinitePlane.Raycast(ray, out float enter))
                    {
                        point = ray.GetPoint(enter);
                        return true;
                    }
                }
            }
            point = default;
            return false;
        }

        public void ResetPosition()
        {
            JsonDeseralize();
            PositionOffset = savedData.positionOffset;
        }

        public void ResetScale()
        {
            JsonDeseralize();
            Scale = savedData.scale > 0 ? savedData.scale : 1;
        }

        public void ResetRotation()
        {
            JsonDeseralize();
            RotationOffset = savedData.rotationOffset;
        }

        private void JsonSeralize()
        {
            SeralizedInfo data = new SeralizedInfo
            {
                position = savedData.positionOffset,
                rotation = savedData.rotationOffset,
                scale = savedData.scale
            };

            string json = JsonUtility.ToJson(data);

            File.WriteAllText(SaveDataPath, json);
        }

        public void SaveRotation()
        {
            savedData.rotationOffset = _rotationOffset;
            JsonSeralize();
        }

        public void SavePosition()
        {
            savedData.positionOffset = _positionOffset;
            JsonSeralize();
        }

        public void SaveScale()
        {
            savedData.scale = _scale;
            JsonSeralize();
        }

        private void JsonDeseralize()
        {
            if (File.Exists(SaveDataPath))
            {
                string json = File.ReadAllText(SaveDataPath);
                SeralizedInfo jsonCatcher = JsonUtility.FromJson<SeralizedInfo>(json);

                savedData.positionOffset = jsonCatcher.position;
                savedData.rotationOffset = jsonCatcher.rotation;
                savedData.scale = jsonCatcher.scale;
            }
            else
            {
                savedData.positionOffset = Vector3.zero;
                savedData.rotationOffset = Vector3.zero;
                savedData.scale = 1;
            }
        }

        private void ResetTranformValues(bool applyToTransform = false)
        {
            _positionOffset = savedData.positionOffset;
            _rotationOffset = savedData.rotationOffset;
            _scale = savedData.scale > 0 ? savedData.scale : 1;

            if (applyToTransform)
            {
                AdjustTransform();
            }
        }

        public class SeralizedInfo
        {
            public Vector3 position;
            public Vector3 rotation;
            public float scale;
        }
        static List<ARRaycastHit> hits = new List<ARRaycastHit>();


        public void SwitchTracking()
        {

            if (CurrentARTargetMode == ARTargetMode.Surface)
                CurrentARTargetMode = ARTargetMode.Marker;
            else if (CurrentARTargetMode == ARTargetMode.Marker)
                CurrentARTargetMode = ARTargetMode.Surface;

            JsonDeseralize();
        }

        public async Task<bool> CheckIfARIsSupported()
        {
            while (!m_isArSupported.HasValue)
            {
                await Task.Yield();
            }
            return m_isArSupported.Value;
        }

        public void StopTracking()
        {
            StopPlaneTracking();
            _arPlaneManager.enabled = false;
            _arTrackedImageManager.enabled = false;
        }

        public void ResumeTracking()
        {
            if (_currentMode == ARTargetMode.Surface)
            {
                //StopImageTracking();
                _arPlaneManager.enabled = true;
                _arTrackedImageManager.enabled = false;
                _arPlaneManager.requestedDetectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Horizontal;
                foreach (var plane in _arPlaneManager.trackables)
                {
                    if (plane && plane.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
                    {
                        plane.enabled = true;
                        plane.gameObject.SetActive(true);
                    }
                    //else
                    //{
                    //    Destroy(plane.gameObject);
                    //}
                }
            }
            else if (_currentMode == ARTargetMode.Marker)
            {
                StopPlaneTracking();
                _arPlaneManager.enabled = false;
                _arTrackedImageManager.enabled = false;
                _arTrackedImageManager.enabled = true;

                //foreach (var image in _arTrackedImageManager.trackables)
                //{
                //    if (image && image.trackingState != UnityEngine.XR.ARSubsystems.TrackingState.None)
                //    {
                //        image.gameObject.SetActive(true);
                //        PlaceOnAquiredImage(image);
                //    }
                //}
            }
        }
#else

        public ARTargetMode CurrentARTargetMode { get; set; }
        public Vector3 PositionOffset { get; set; }
        public Vector3 RotationOffset { get; set; }
        public float Scale { get; set; }

        public GameObject ARTarget { get; set; }
        public bool ShowWorldAxes { get; set; }
            
        public Gradient LineToSurfaceGradient { get; set; }

        public void StopTracking()
        {
            throw new NotImplementedException();
        }

        public void ResumeTracking()
        {
            throw new NotImplementedException();
        }
        
        public bool TryMoveOnSurface(Vector2 touchPosition, bool isDeltaMove)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CheckIfARIsSupported() => new Task<bool>(() => false);

        public void PositionOnSurface(Vector2 touchPosition)
        {
            throw new System.NotImplementedException();
        }

        public void ResetPosition()
        {
            throw new System.NotImplementedException();
        }

        public void ResetRotation()
        {
            throw new System.NotImplementedException();
        }

        public void ResetScale()
        {
            throw new System.NotImplementedException();
        }

        public void SavePosition()
        {
            throw new System.NotImplementedException();
        }

        public void SaveRotation()
        {
            throw new System.NotImplementedException();
        }

        public void SaveScale()
        {
            throw new System.NotImplementedException();
        }

        public void ScalePinch(float touchDelta, float speedTouch0, float speedTouch1)
        {
            throw new System.NotImplementedException();
        }
#endif
    }
}