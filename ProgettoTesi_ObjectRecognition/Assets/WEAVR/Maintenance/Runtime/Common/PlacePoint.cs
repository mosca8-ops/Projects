namespace TXT.WEAVR.Maintenance
{
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Animation;
    using TXT.WEAVR.Common;
    using TXT.WEAVR.Core;
    using TXT.WEAVR.Interaction;
    using UnityEngine;

    [AddComponentMenu("")]
    public class PlacePoint : MonoBehaviour
    {
        [Draggable]
        public GameObject placedObject;
        public bool animatePlacing = false;

        [SerializeField]
        private float m_placementRadius = 0.02f;
        [SerializeField]
        private float m_placementDelay = 1f;
        private AbstractPlaceable m_placeable;

        private Transform m_placedTransform;

        private Transform m_placePosition;
        private float m_nextTimeCheck;

        public AbstractPlaceManager Manager { get; internal set; }

        public bool IsFree { get { return !placedObject; } }

        private void Start()
        {
            m_placePosition = new GameObject("PlacePosition").transform;
            m_placePosition.gameObject.hideFlags = HideFlags.HideAndDontSave;
            m_placePosition.SetParent(transform, false);
            m_placePosition.localPosition = Vector3.zero;
        }

        public void PlaceObject(GameObject newGameObject, float placementVelocity) {
            if(m_placedTransform) { return; }
            if (animatePlacing && placementVelocity > 0) {
                newGameObject.Animate(AnimationFactory.MoveAndRotate(transform, placementVelocity), (g, a) => SetPlacedObject(g, null));
            }
            else {
                newGameObject.transform.SetPositionAndRotation(transform.position, transform.rotation);
                SetPlacedObject(newGameObject, null);
            }
        }

        public void PlaceObject(GameObject newGameObject, Transform relativePlacePoint, float placementVelocity)
        {
            if (m_placedTransform) { return; }
            if (animatePlacing && placementVelocity > 0)
            {
                newGameObject.Animate(AnimationFactory.MoveAndRotate(transform, relativePlacePoint, placementVelocity)
                                      , (g, a) => SetPlacedObject(g, relativePlacePoint));
            }
            else
            {
                newGameObject.RepositionWithOffset(relativePlacePoint, transform);
                SetPlacedObject(newGameObject, relativePlacePoint);
            }
        }

        private void SetPlacedObject(GameObject go, Transform offset) {
            placedObject = go;
            if (m_placeable) { m_placeable.RemovePlacement(this); }
            m_placeable = go.GetComponentInChildren<AbstractPlaceable>();
            m_placedTransform = offset != null ? offset : go.transform;
            if(m_placeable) {
                m_placeable.SetPlacement(this);
            }
            m_placePosition.position = go.transform.position;
            m_nextTimeCheck = Time.time + m_placementDelay;
        }

        public bool IsInPlaceRange(GameObject gameObject, Transform offset = null)
        {
            var testTransform = offset != null ? offset : gameObject.transform;
            return Vector3.Distance(testTransform.position, transform.position) < m_placementRadius;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (placedObject) { return; }
            var placeable = other.GetComponentInChildren<AbstractPlaceable>();
            if(placeable && placeable.CanBePlaced(this) && placeable.autoPlace && placeable.autoPlaceIfInRadius)
            {
                if (placeable.PlacementOffset)
                {
                    PlaceObject(placeable.gameObject, placeable.PlacementOffset, placeable.instantPlacement ? 0 : placeable.placementSpeed);
                }
                else
                {
                    PlaceObject(placeable.gameObject, placeable.instantPlacement ? 0 : placeable.placementSpeed);
                }
            }
        }

        private void Update() {
            if(m_placedTransform && Time.time < m_nextTimeCheck)
            {
                m_placePosition.position = m_placedTransform.position;
            }
            else if(m_placedTransform && Vector3.Distance(m_placedTransform.position, m_placePosition.position) > m_placementRadius) {
                placedObject = null;
                m_placedTransform = null;
                m_placePosition.localPosition = Vector3.zero;
                if(m_placeable) {
                    m_placeable.RemovePlacement(this);
                }
                m_placeable = null;
            }
        }
    }
}
