using System;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TXT.WEAVR.Interaction
{
    public interface IPointer3D
    {
        Color Color { get; set; }
        Transform PointingLine { get; }
        Transform PointingDot { get; }
    }

    public abstract class WorldPointer : MonoBehaviour
    {
        [Flags]
        public enum RaycastType
        {
            //None = 0,
            World3D = 1 << 0,
            World2D = 1 << 1,
            Canvases = 1 << 2
        }

        [Serializable]
        public struct Events
        {
            public UnityEventGameObject onPointerEnter;
            public UnityEventGameObject onPointerExit;
        }

        [Header("General")]
        [Tooltip("The pointing object")]
        [Draggable]
        public GameObject pointer;
        public float thickness = 0.002f;
        public float maxDistance = 100f;
        public float dragThreshold = 0.01f;
        public bool disableOnStart = true;

        [Header("Raycasting")]
        [Tooltip("The camera to use for screen space raycasts")]
        [Draggable]
        public Camera eventCamera;
        [Tooltip("Whether to search for active canvases on each pointing active or not")]
        public bool dynamicCanvasSearch = true;
        [Tooltip("Point only on WorldSpaceCanvases")]
        public bool onlySpecialCanvases = false;
        [Space]
        [Tooltip("Whether to ignore or not canvases which are faced the other way around")]
        public bool ignoreReversedGraphics = true;
        [Tooltip("The objects to raycast")]
        //[EnumMask]
        public RaycastType raycastObjects = RaycastType.World3D | RaycastType.Canvases;
        [Tooltip("The layers to raycast")]
        public LayerMask raycastLayers = ~0;

        public bool removeConflictingLayers = true;

        [Space]
        [SerializeField]
        private Events m_events;

        public UnityEventGameObject OnPointerEnterEvent => m_events.onPointerEnter;
        public UnityEventGameObject OnPointerExitEvent => m_events.onPointerExit;

        //protected GameObject m_previousContact = null;
        protected GameObject m_currentContact = null;
        protected GameObject m_draggableContact = null;

        //protected RaycastedObject m_focused;
        protected bool m_shouldRefreshCanvases;
        protected bool m_pointerWasDown;
        protected bool m_pointerIsDown;
        protected bool m_draggingStarted;
        protected float m_rayDistance;
        protected Vector3 m_lastPointerWorldPos;
        protected List<Canvas> m_canvases;
        protected IEnumerable<Canvas> m_canvasesToTest;
        protected List<PointerRaycastHit> m_frameHits;

        protected PointerEventData m_pointerData;
        protected AxisEventData m_axisData;

        private static Vector3[] m_corners = new Vector3[4];

        public virtual bool IsVisible {
            get {
                return pointer != null && pointer.activeInHierarchy;
            }
        }

        public virtual float RayDistance
        {
            get
            {
                return m_rayDistance;
            }
            set
            {
                if (m_rayDistance != value)
                {
                    m_rayDistance = Mathf.Clamp(value, 0, maxDistance);

                }
            }
        }

        [Space]
        [SerializeField]
        protected bool m_debug = false;

        private GameObject m_testPoint;
        protected GameObject DebugSphere {
            get {
                if (m_testPoint == null)
                {
                    m_testPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    m_testPoint.hideFlags = HideFlags.HideAndDontSave;
                    m_testPoint.transform.localScale *= 0.05f;
                    Destroy(m_testPoint.GetComponent<Collider>());

                    Material newMaterial = new Material(Shader.Find("Unlit/Color"));
                    newMaterial.SetColor("_Color", Color.red);
                    m_testPoint.GetComponent<MeshRenderer>().material = newMaterial;
                }
                return m_testPoint;
            }
        }

        protected GameObject CurrentTarget {
            get { return m_currentContact; }
            set {
                if (m_currentContact != value)
                {
                    if (m_currentContact)
                    {
                        OnPointerExit(m_currentContact, m_pointerData);
                    }
                    m_currentContact = value;
                    if (value)
                    {
                        if (m_draggableContact != null && !value.transform.IsChildOf(m_draggableContact.transform))
                        {
                            m_draggableContact = null;
                        }

                        //m_pointerData.selectedObject = value;
                        
                        //m_focused.Set(value);
                        OnPointerEnter(value, m_pointerData, m_pointerIsDown);
                    }
                    else
                    {
                        m_draggableContact = null;
                        m_draggingStarted = false;
                        m_pointerData.selectedObject = null;
                        ClearEventData(m_pointerData);
                        //m_pointerIsDown = m_pointerWasDown = false;
                    }
                }
            }
        }

        /// <summary>
        /// Perform a raycast into the screen and collect all graphics underneath it.
        /// </summary>
        [System.NonSerialized]
        protected readonly List<PointerRaycastHit> m_sortedGraphics = new List<PointerRaycastHit>();

        protected virtual void OnValidate()
        {
            if (removeConflictingLayers)
            {
                raycastLayers &= ~(1 << WorldBounds.GetOccupancySpaceActiveLayer());
                raycastLayers &= ~(1 << WorldBounds.GetOccupancySpacePassiveLayer());
            }

            if (Application.isPlaying)
            {
                if (onlySpecialCanvases)
                {
                    m_canvasesToTest = WorldPointerCanvas.Canvases;
                }
                else
                {
                    m_canvasesToTest = m_canvases;
                }
            }
        }

        protected virtual void Reset()
        {
            if (removeConflictingLayers)
            {
                raycastLayers &= ~(1 << WorldBounds.GetOccupancySpaceActiveLayer());
                raycastLayers &= ~(1 << WorldBounds.GetOccupancySpacePassiveLayer());
            }
        }

        protected virtual void OnEnable()
        {

        }

        // Use this for initialization
        protected virtual void Start()
        {
            //m_focused = new RaycastedObject(null);

            pointer = GetPointerObject(pointer);

            m_pointerWasDown = false;

            if (eventCamera == null)
            {
                eventCamera = Camera.allCameras[0];
            }

            dynamicCanvasSearch = !onlySpecialCanvases;

            m_canvases = new List<Canvas>();
            if (onlySpecialCanvases)
            {
                m_canvasesToTest = WorldPointerCanvas.Canvases;
            }
            else
            {
                m_canvasesToTest = m_canvases;
            }
            if (!dynamicCanvasSearch && !onlySpecialCanvases)
            {
                m_canvases.AddRange(FindObjectsOfType<Canvas>());
            }

            m_shouldRefreshCanvases = false;
            m_frameHits = new List<PointerRaycastHit>();

            m_pointerData = new PointerEventData(EventSystem.current);
            m_pointerData.eligibleForClick = true;
            m_pointerData.pointerId = GetInstanceID();

            if (disableOnStart)
            {
                OnPointerDisable();
            }
        }

        // Update is called once per frame
        protected virtual void Update()
        {
            if (!IsValid()) return;

            if (!GetPointerEnabled())
            {
                CurrentTarget = null;

                OnPointerDisable();

                m_shouldRefreshCanvases = true;
                return;
            }

            if (dynamicCanvasSearch && m_shouldRefreshCanvases)
            {
                m_canvases.Clear();
                m_canvases.AddRange(FindObjectsOfType<Canvas>());
            }

            m_shouldRefreshCanvases = false;
            m_rayDistance = m_pointerWasDown ? maxDistance * 2 : maxDistance;

            OnPointerEnable();

            bool pointerHandled = false;
            float pointerLength = 0f;
            float pointerThickness = thickness;

            // Implement continuously targeting elements
            if (/*shouldRaycast && */Raycast(new Ray(transform.position, transform.forward)))
            {
                m_pointerIsDown = GetPointerDown();
                bool pointerUp = m_pointerWasDown && !m_pointerIsDown;

                m_pointerData.Reset();

                GameObject target = null;
                for (int i = 0; i < m_frameHits.Count; i++)
                {
                    target = m_frameHits[i].target;
                    if (m_draggingStarted && m_draggableContact != null && target.transform.IsChildOf(m_draggableContact.transform))
                    {
                        CurrentTarget = target = m_draggableContact;
                    }
                    //m_pointerData.selectedObject = target;
                    m_pointerData.position = eventCamera.WorldToScreenPoint(m_frameHits[i].worldPos);
                    var raycastResult = m_frameHits[i].ConvertToRaycastResult(i + 1);
                    m_pointerData.pointerCurrentRaycast = raycastResult;

                    if (pointerLength < m_frameHits[i].pointerDistance)
                    {
                        pointerLength = m_frameHits[i].pointerDistance;
                    }

                    if (CurrentTarget != target && (ExecuteEvents.CanHandleEvent<IPointerEnterHandler>(target) || ExecuteEvents.CanHandleEvent<IPointerDownHandler>(target)))
                    {
                        var draggable = target.GetComponentInParent<IDragHandler>() as Component;
                        if (draggable)
                        {
                            m_draggableContact = draggable.gameObject;
                            //m_pointerData.selectedObject = target;
                        }
                        else
                        {
                            m_draggableContact = null;
                        }
                        m_draggingStarted = false;

                        m_lastPointerWorldPos = m_frameHits[i].worldPos;
                        pointerLength = m_frameHits[i].pointerDistance;

                        //m_previousContact = target;
                        //m_focused.Reset();
                        CurrentTarget = target;
                        pointerHandled = true;
                        break;
                    }
                    else if (CurrentTarget == target)
                    {
                        if (m_debug)
                        {
                            DebugSphere.transform.position = m_frameHits[i].worldPos;
                        }

                        var worldDelta = m_frameHits[i].worldPos - m_lastPointerWorldPos;
                        m_pointerData.delta = Vector3.ProjectOnPlane(worldDelta, transform.forward);
                        m_pointerData.dragging = m_pointerIsDown && m_draggableContact != null && worldDelta.magnitude > dragThreshold;
                        if (m_pointerIsDown && m_pointerData.dragging)
                        {
                            CurrentTarget = target = m_draggableContact;
                            m_draggingStarted = true;
                        }
                        m_pointerData.scrollDelta = GetScrollDelta(target);

                        //if(m_pointerData.dragging && m_focused.needsDragCheck)
                        //{
                        //    m_focused.needsDragCheck = false;
                        //    var dragger = target.GetComponentInParent<IDragHandler>() as Component;
                        //    if(dragger != null)
                        //    {
                        //        m_focused.isDraggable = true;
                        //        CurrentTarget = dragger.gameObject;
                        //    }
                        //}

                        if (m_pointerIsDown)
                        {
                            pointerThickness = thickness * 5f;
                            pointerLength = m_frameHits[i].pointerDistance;

                            OnPointerDown(target, m_pointerData);
                        }
                        else if (pointerUp)
                        {
                            OnPointerUp(target, m_pointerData);
                            m_shouldRefreshCanvases = true;
                            CurrentTarget = null;
                        }
                        pointerHandled = true;
                        break;
                    }
                }
            }

            m_pointerWasDown = m_pointerIsDown;
            m_pointerIsDown = false;

            if (!pointerHandled && CurrentTarget)
            {
                ClearEventData(m_pointerData);
                CurrentTarget = null;
            }

            if (pointerLength == 0) { pointerLength = m_rayDistance; }
            OnPointerUpdate(pointerLength, pointerThickness);
        }

        private void OnPointerExit(GameObject target, PointerEventData data)
        {
            var lastSelected = data.selectedObject;
            m_pointerData.selectedObject = target;
            bool dragStarted = m_draggingStarted;
            if (m_draggingStarted && m_draggableContact)
            {
                ExecuteEventWithData(m_draggableContact, data, ExecuteEvents.endDragHandler);
                m_draggableContact = null;
                m_draggingStarted = false;
                //m_focused.isDraggable = false;
            }
            var exitHandler = ExecuteEventWithData(target, data, ExecuteEvents.pointerExitHandler);
            if (exitHandler)
            {
                OnPointerExitEvent?.Invoke(exitHandler);
            }
            if (dragStarted && !m_pointerData.dragging)
            {
                ExecuteEventWithData(target, data, ExecuteEvents.pointerUpHandler);
            }
            m_pointerData.selectedObject = lastSelected;
        }

        protected virtual void OnPointerDisable()
        {
            pointer.SetActive(false);
        }
        protected virtual void OnPointerEnable()
        {
            pointer.SetActive(true);
        }

        protected virtual void OnPointerUpdate(float pointerLength, float pointerThickness)
        {
            pointer.transform.localScale = new Vector3(pointerThickness, pointerThickness, pointerLength);
            pointer.transform.localPosition = new Vector3(0f, 0f, pointerLength / 2f);
        }


        protected abstract GameObject GetPointerObject(GameObject startupPointer);

        protected abstract Vector2 GetScrollDelta(GameObject target);

        protected abstract bool IsValid();

        public abstract bool GetPointerEnabled();

        public abstract bool GetPointerDown();

        protected virtual void OnPointerUp(GameObject target, PointerEventData data)
        {
            data.button = PointerEventData.InputButton.Left;

            if (m_draggingStarted && m_draggableContact)
            {
                ExecuteHierarchyEvent(target, data, ExecuteEvents.endDragHandler);
            }
            // TODO: Test if this change is OK
            //if (!ExecuteEventWithData(target, data, ExecuteEvents.pointerClickHandler))
            {
                ExecuteEventWithData(target, data, ExecuteEvents.selectHandler);
                ExecuteEventWithData(target, data, ExecuteEvents.pointerUpHandler);
                ExecuteEventWithData(target, data, ExecuteEvents.pointerClickHandler);
            }

            m_draggableContact = null;
            m_draggingStarted = false;
            CurrentTarget = null;
            ClearEventData(data);
        }

        private static void ClearEventData(PointerEventData data)
        {
            data.selectedObject = null;
            data.pointerEnter = null;
            data.pointerDrag = null;
            data.pointerPress = null;
            data.rawPointerPress = null;
        }

        protected virtual void OnPointerDown(GameObject target, PointerEventData data)
        {
            data.rawPointerPress = target;
            data.pressPosition = data.position;
            data.pointerPressRaycast = data.pointerCurrentRaycast;
            data.button = PointerEventData.InputButton.Left;


            if (data.dragging && m_draggableContact != null)
            {
                if (m_pointerWasDown)
                {
                    target.GetComponentInParent<IDragHandler>();
                    GameObject beginDragHandler = null;
                    if (!m_pointerWasDown)
                    {
                        beginDragHandler = ExecuteHierarchyEvent(target, data, ExecuteEvents.beginDragHandler);
                        data.pointerDrag = beginDragHandler;
                    }
                    if (m_pointerWasDown || !beginDragHandler)
                    {
                        var dragHandler =  ExecuteHierarchyEvent(target, data, ExecuteEvents.dragHandler);
                        if (dragHandler)
                        {
                            data.pointerDrag = dragHandler;
                        }
                        //ExecuteEventWithData(target, data, ExecuteEvents.moveHandler);
                    }
                }
            }
            else
            {
                var pointerDownTarget =  ExecuteEventWithData(target, data, ExecuteEvents.pointerDownHandler);
                if (pointerDownTarget)
                {
                    data.pointerPress = pointerDownTarget;
                }
            }
        }

        protected virtual void OnPointerEnter(GameObject target, PointerEventData data, bool pointerDown)
        {
            var enteredTarget = ExecuteEventWithData(target, data, ExecuteEvents.pointerEnterHandler);
            if (enteredTarget)
            {
                data.pointerEnter = enteredTarget;
                OnPointerEnterEvent?.Invoke(enteredTarget);
            }

            if (pointerDown)
            {
                data.rawPointerPress = target;
                data.pressPosition = data.position;
                data.pointerPressRaycast = data.pointerCurrentRaycast;
                data.button = PointerEventData.InputButton.Left;

                var executedTarget = ExecuteEventWithData(target, data, ExecuteEvents.pointerDownHandler);
                if (executedTarget)
                {
                    data.pointerPress = executedTarget;
                }
                if (m_draggableContact != null)
                {
                    ExecuteHierarchyEvent(m_draggableContact, data, ExecuteEvents.initializePotentialDrag);
                }
            }
        }

        private bool ExecuteEvent<T>(GameObject target, GameObject selected, ExecuteEvents.EventFunction<T> eventFunction) where T : IEventSystemHandler
        {
            if (ExecuteEvents.CanHandleEvent<T>(target))
            {
                PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
                pointerEventData.selectedObject = selected;
                pointerEventData.eligibleForClick = true;
                return ExecuteEvents.Execute(target, pointerEventData, eventFunction);
            }
            return false;
        }

        private GameObject ExecuteEventWithData<T>(GameObject target, PointerEventData data, ExecuteEvents.EventFunction<T> eventFunction) where T : IEventSystemHandler
        {
            return ExecuteEvents.CanHandleEvent<T>(target) ? ExecuteEvents.ExecuteHierarchy(target, data, eventFunction) : null;
        }

        private GameObject ExecuteHierarchyEvent<T>(GameObject target, PointerEventData data, ExecuteEvents.EventFunction<T> eventFunction) where T : IEventSystemHandler
        {
            return ExecuteEvents.ExecuteHierarchy(target, data, eventFunction);
        }

        #region [  OVERLAPPING LOGIC  ]

        //protected bool Overlap(Vector3 center, float radius, bool firstEncountersOnly = true)
        //{

        //    if (eventCamera == null)
        //        return false;

        //    m_frameHits.Clear();

        //    float hitDistance = float.MaxValue;

        //    float dist = Mathf.Min(maxDistance, eventCamera.farClipPlane);

        //    if (raycastObjects != GraphicRaycaster.BlockingObjects.None)
        //    {

        //        if (raycastObjects == GraphicRaycaster.BlockingObjects.ThreeD || raycastObjects == GraphicRaycaster.BlockingObjects.All)
        //        {
        //            RaycastHit hit;
        //            if (Physics.Raycast(ray, out hit, dist, raycastLayers))
        //            {
        //                hitDistance = hit.distance;
        //                m_frameHits.Add(new PointerRaycastHit()
        //                {
        //                    fromMouse = false,
        //                    target = hit.collider.gameObject,
        //                    pointerDistance = hit.distance,
        //                    worldPos = hit.point
        //                });
        //            }
        //        }

        //        if (raycastObjects == GraphicRaycaster.BlockingObjects.TwoD || raycastObjects == GraphicRaycaster.BlockingObjects.All)
        //        {
        //            var hit = Physics2D.GetRayIntersection(ray, dist, raycastLayers);

        //            if (hit && hit.fraction * dist < hitDistance)
        //            {
        //                hitDistance = hit.fraction * dist;
        //                m_frameHits.Add(new PointerRaycastHit()
        //                {
        //                    fromMouse = false,
        //                    target = hit.collider.gameObject,
        //                    pointerDistance = hit.fraction * dist,
        //                    worldPos = hit.point
        //                });
        //            }
        //        }
        //    }

        //    foreach (var canvas in m_canvases)
        //    {
        //        if (!ignoreReversedGraphics || Vector3.Dot(ray.direction, canvas.transform.forward) > 0)
        //        {
        //            GraphicRaycast(canvas, ray, m_frameHits);
        //        }
        //    }

        //    m_frameHits.Sort((h1, h2) => h1.pointerDistance.CompareTo(h2.pointerDistance));

        //    if (firstEncountersOnly && m_frameHits.Count > 0)
        //    {
        //        var lastDistance = m_frameHits[0].pointerDistance;
        //        for (int i = 1; i < m_frameHits.Count; i++)
        //        {
        //            if (m_frameHits[i].pointerDistance - lastDistance > 0.01f || m_frameHits[i].pointerDistance > dist)
        //            {
        //                m_frameHits.RemoveRange(i, m_frameHits.Count - i);
        //                break;
        //            }
        //        }
        //    }
        //    else
        //    {
        //        for (int i = 0; i < m_frameHits.Count; i++)
        //        {
        //            if (m_frameHits[i].pointerDistance > dist)
        //            {
        //                m_frameHits.RemoveRange(i, m_frameHits.Count - i);
        //                break;
        //            }
        //        }
        //    }

        //    return m_frameHits.Count > 0;
        //}

        //private void GraphicRaycast(Canvas canvas, Ray ray, List<PointerRaycastHit> results)
        //{
        //    //This function is based closely on :
        //    // void GraphicRaycaster.Raycast(Canvas canvas, Camera eventCamera, Vector2 pointerPosition, List<Graphic> results)
        //    // But modified to take a Ray instead of a canvas pointer, and also to explicitly ignore
        //    // the graphic associated with the pointer

        //    // Necessary for the event system
        //    var foundGraphics = GraphicRegistry.GetGraphicsForCanvas(canvas);

        //    m_sortedGraphics.Clear();

        //    BaseRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();

        //    for (int i = 0; i < foundGraphics.Count; ++i)
        //    {
        //        Graphic graphic = foundGraphics[i];

        //        // -1 means it hasn't been processed by the canvas, which means it isn't actually drawn
        //        if (graphic.depth == -1 || (pointer == graphic.gameObject))
        //            continue;
        //        Vector3 worldPos;
        //        if ((!ignoreReversedGraphics || Vector3.Dot(ray.direction, canvas.transform.rotation * Vector3.forward) > 0)
        //            && RayIntersectsRectTransform(graphic.rectTransform, ray, out worldPos))
        //        {
        //            //Work out where this is on the screen for compatibility with existing Unity UI code
        //            Vector2 screenPos = eventCamera.WorldToScreenPoint(worldPos);
        //            // mask/image intersection - See Unity docs on eventAlphaThreshold for when this does anything
        //            if (graphic.Raycast(screenPos, eventCamera))
        //            {
        //                var selectable = graphic.GetComponentInParent<Selectable>();
        //                m_sortedGraphics.Add(new PointerRaycastHit()
        //                {
        //                    graphic = graphic,
        //                    worldPos = worldPos,
        //                    fromMouse = false,
        //                    target = selectable ? selectable.gameObject : graphic.gameObject,
        //                    module = raycaster,
        //                    pointerDistance = Vector3.Distance(worldPos, ray.origin)
        //                });
        //            }
        //        }
        //    }

        //    m_sortedGraphics.Sort((g1, g2) => g2.graphic.depth.CompareTo(g1.graphic.depth));

        //    results.AddRange(m_sortedGraphics);
        //}

        ///// <summary>
        ///// Detects whether a ray intersects a RectTransform and if it does also 
        ///// returns the world position of the intersection.
        ///// </summary>
        ///// <param name="rectTransform"></param>
        ///// <param name="ray"></param>
        ///// <param name="worldPos"></param>
        ///// <returns></returns>
        //static bool RayIntersectsRectTransform(RectTransform rectTransform, Vector3 sphereCenter, float radius, ref Vector3 worldPos)
        //{
        //    Vector3[] corners = new Vector3[4];
        //    rectTransform.GetWorldCorners(corners);
        //    Plane plane = new Plane(corners[0], corners[1], corners[2]);

        //    float enter;
        //    Vector3 intersection;
        //    if (!plane.Dis(ray, out enter))
        //    {
        //        worldPos = Vector3.zero;
        //        return false;
        //    }

        //    Vector3 intersection = ray.GetPoint(enter);

        //    Vector3 BottomEdge = corners[3] - corners[0];
        //    Vector3 LeftEdge = corners[1] - corners[0];
        //    float BottomDot = Vector3.Dot(intersection - corners[0], BottomEdge);
        //    float LeftDot = Vector3.Dot(intersection - corners[0], LeftEdge);
        //    if (BottomDot < BottomEdge.sqrMagnitude && // Can use sqrMag because BottomEdge is not normalized
        //        LeftDot < LeftEdge.sqrMagnitude &&
        //            BottomDot >= 0 &&
        //            LeftDot >= 0)
        //    {
        //        worldPos = corners[0] + LeftDot * LeftEdge / LeftEdge.sqrMagnitude + BottomDot * BottomEdge / BottomEdge.sqrMagnitude;
        //        return true;
        //    }
        //    else
        //    {
        //        worldPos = Vector3.zero;
        //        return false;
        //    }
        //}

        #endregion

        #region [   RAYCASTING LOGIC   ]

        protected bool Raycast(Ray ray, bool firstEncountersOnly = true)
        {

            if (eventCamera == null || raycastObjects == 0)
                return false;

            m_frameHits.Clear();

            float hitDistance = float.MaxValue;

            float dist = Mathf.Min(m_rayDistance, eventCamera.farClipPlane);

            if (raycastObjects.HasFlag(RaycastType.World3D))
            {
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, dist, raycastLayers))
                {
                    hitDistance = hit.distance;
                    m_frameHits.Add(new PointerRaycastHit()
                    {
                        fromMouse = false,
                        target = hit.collider.gameObject,
                        pointerDistance = hit.distance,
                        worldPos = hit.point
                    });
                }
            }

            if (raycastObjects.HasFlag(RaycastType.World2D))
            {
                var hit = Physics2D.GetRayIntersection(ray, dist, raycastLayers);

                if (hit && hit.fraction * dist < hitDistance)
                {
                    hitDistance = hit.fraction * dist;
                    m_frameHits.Add(new PointerRaycastHit()
                    {
                        fromMouse = false,
                        target = hit.collider.gameObject,
                        pointerDistance = hit.fraction * dist,
                        worldPos = hit.point
                    });
                }
            }

            if (raycastObjects.HasFlag(RaycastType.Canvases))
            {
                foreach (var canvas in m_canvasesToTest)
                {
                    if (!ignoreReversedGraphics || Vector3.Dot(ray.direction, canvas.transform.forward) > 0)
                    {
                        GraphicRaycast(canvas, ray, m_frameHits);
                    }
                }
            }

            m_frameHits.Sort((h1, h2) => h1.pointerDistance.CompareTo(h2.pointerDistance - 0.000001f));

            if (firstEncountersOnly && m_frameHits.Count > 0)
            {
                var lastDistance = m_frameHits[0].pointerDistance;
                for (int i = 1; i < m_frameHits.Count; i++)
                {
                    if (m_frameHits[i].pointerDistance - lastDistance > 0.01f || m_frameHits[i].pointerDistance > dist)
                    {
                        m_frameHits.RemoveRange(i, m_frameHits.Count - i);
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < m_frameHits.Count; i++)
                {
                    if (m_frameHits[i].pointerDistance > dist)
                    {
                        m_frameHits.RemoveRange(i, m_frameHits.Count - i);
                        break;
                    }
                }
            }

            return m_frameHits.Count > 0;
        }

        private bool IsTargetted(RaycastedObject target, Ray ray, out Vector3 worldPosition)
        {
            RaycastHit hit;
            if (target.collider != null && target.collider.Raycast(ray, out hit, m_rayDistance))
            {
                worldPosition = hit.point;
                return true;
            }
            return RayIntersectsRectTransform(target.rectTransform, ray, out worldPosition);
        }

        private void GraphicRaycast(Canvas canvas, Ray ray, List<PointerRaycastHit> results)
        {
            //This function is based closely on :
            // void GraphicRaycaster.Raycast(Canvas canvas, Camera eventCamera, Vector2 pointerPosition, List<Graphic> results)
            // But modified to take a Ray instead of a canvas pointer, and also to explicitly ignore
            // the graphic associated with the pointer

            // Necessary for the event system
            var foundGraphics = GraphicRegistry.GetGraphicsForCanvas(canvas);

            m_sortedGraphics.Clear();

            BaseRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();

            for (int i = 0; i < foundGraphics.Count; ++i)
            {
                Graphic graphic = foundGraphics[i];

                // -1 means it hasn't been processed by the canvas, which means it isn't actually drawn
                if (graphic.depth == -1 || (pointer == graphic.gameObject) || !graphic.raycastTarget)
                    continue;
                Vector3 worldPos;
                if ((!ignoreReversedGraphics || Vector3.Dot(ray.direction, canvas.transform.rotation * Vector3.forward) > 0)
                    && RayIntersectsRectTransform(graphic.rectTransform, ray, out worldPos))
                {
                    //Work out where this is on the screen for compatibility with existing Unity UI code
                    Vector2 screenPos = eventCamera.WorldToScreenPoint(worldPos);
                    // mask/image intersection - See Unity docs on eventAlphaThreshold for when this does anything
                    if (graphic.Raycast(screenPos, eventCamera))
                    {
                        var selectable = graphic.GetComponentInParent<Selectable>();
                        m_sortedGraphics.Add(new PointerRaycastHit()
                        {
                            graphic = graphic,
                            worldPos = worldPos,
                            fromMouse = false,
                            target = selectable ? selectable.gameObject : graphic.gameObject,
                            module = raycaster,
                            pointerDistance = Vector3.Distance(worldPos, ray.origin)
                        });
                    }
                }
            }

            m_sortedGraphics.Sort((g1, g2) => g2.graphic.depth.CompareTo(g1.graphic.depth));

            results.AddRange(m_sortedGraphics);
        }

        /// <summary>
        /// Detects whether a ray intersects a RectTransform and if it does also 
        /// returns the world position of the intersection.
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <param name="ray"></param>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        private bool RayIntersectsRectTransform(RectTransform rectTransform, Ray ray, out Vector3 worldPos)
        {
            rectTransform.GetWorldCorners(m_corners);
            Plane plane = new Plane(m_corners[0], m_corners[1], m_corners[2]);

            float enter;
            if (!plane.Raycast(ray, out enter) || enter > m_rayDistance)
            {
                worldPos = Vector3.zero;
                return false;
            }

            Vector3 intersection = ray.GetPoint(enter);

            Vector3 BottomEdge = m_corners[3] - m_corners[0];
            Vector3 LeftEdge = m_corners[1] - m_corners[0];
            float BottomDot = Vector3.Dot(intersection - m_corners[0], BottomEdge);
            float LeftDot = Vector3.Dot(intersection - m_corners[0], LeftEdge);
            if (BottomDot < BottomEdge.sqrMagnitude && // Can use sqrMag because BottomEdge is not normalized
                LeftDot < LeftEdge.sqrMagnitude &&
                    BottomDot >= 0 &&
                    LeftDot >= 0)
            {
                //worldPos = m_corners[0] + LeftDot * LeftEdge / LeftEdge.sqrMagnitude + BottomDot * BottomEdge / BottomEdge.sqrMagnitude;
                worldPos = intersection;
                return true;
            }
            else
            {
                worldPos = Vector3.zero;
                return false;
            }
        }


        protected struct PointerRaycastHit
        {
            public Graphic graphic;
            public GameObject target;
            public Vector3 worldPos;
            public float pointerDistance;
            public bool fromMouse;

            public BaseRaycaster module;

            public RaycastResult ConvertToRaycastResult(int index)
            {
                return new RaycastResult()
                {
                    gameObject = target,
                    distance = pointerDistance,
                    index = index,
                    depth = graphic != null ? graphic.depth : 0,
                    module = module,

                    worldPosition = worldPos
                };
            }
        };

        protected class RaycastedObject
        {
            public GameObject target;
            public RectTransform rectTransform;
            public Collider collider;
            public Graphic graphic;
            public Selectable selectable;

            public PointerRaycastHit pointerHit;
            public GameObject draggable;

            public bool isPointedDown;
            public bool isDraggable;
            public bool needsDragCheck;

            public RaycastedObject(GameObject gameObject)
            {
                Set(gameObject);
            }

            public void Reset()
            {
                target = null;
                rectTransform = null;
                collider = null;
                graphic = null;
                selectable = null;
                draggable = null;

                isPointedDown = false;
                isDraggable = false;
                needsDragCheck = true;
            }

            public void ResetFlags()
            {
                isPointedDown = false;
                isDraggable = false;
                needsDragCheck = true;
            }

            public void Set(GameObject gameObject)
            {
                if (gameObject != null)
                {
                    target = gameObject;
                    rectTransform = gameObject.transform as RectTransform;
                    //draggable = (gameObject.GetComponentInParent<IDragHandler>() as Component)?.gameObject;
                    collider = gameObject.GetComponentInChildren<Collider>();
                    graphic = gameObject.GetComponent<Graphic>();
                    selectable = gameObject.GetComponent<Selectable>();
                }
                else
                {
                    Reset();
                }
            }

            public void Set(GameObject gameObject, Collider collider, UIBehaviour uiElement)
            {
                if (gameObject != null)
                {
                    target = gameObject;
                    rectTransform = gameObject.transform as RectTransform;
                    this.collider = collider;
                    this.graphic = uiElement as Graphic;
                    this.selectable = uiElement as Selectable;
                }
                else
                {
                    Reset();
                }
            }

            public void Set(GameObject gameObject, Collider collider)
            {
                if (gameObject != null)
                {
                    target = gameObject;
                    rectTransform = gameObject.transform as RectTransform;
                    this.collider = collider;
                    this.graphic = null;
                    this.selectable = null;
                }
                else
                {
                    Reset();
                }
            }

            public void Set(GameObject gameObject, UIBehaviour uiElement)
            {
                if (gameObject != null)
                {
                    target = gameObject;
                    rectTransform = gameObject.transform as RectTransform;
                    this.collider = null;
                    this.graphic = uiElement as Graphic;
                    this.selectable = uiElement as Selectable;
                }
                else
                {
                    Reset();
                }
            }
        }

        #endregion
    }
}
