using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Common
{
    [System.Serializable]
    public class WorldCollisionEvent : UnityEvent<Collision>
    {

    }

    [ExecuteInEditMode]
    [AddComponentMenu("")]
    public class SpaceOccupier : MonoBehaviour
    {
        [SerializeField]
        [ShowAsReadOnly]
        private BoxCollider m_occupiedSpace;

        public WorldCollisionEvent onWorldCollision;

        private void OnEnable()
        {
            if (m_occupiedSpace == null)
            {
                GetOrCreateOccupancyObject();
            }
        }

        protected virtual void OnCollisionEnter(Collision collision)
        {
            onWorldCollision.Invoke(collision);
        }

        protected virtual void OnCollisionStay(Collision collision)
        {
            onWorldCollision.Invoke(collision);
        }

        public void ResizeOccupancyVolume(Vector3 center, Vector3 size)
        {
            m_occupiedSpace.center = center;
            m_occupiedSpace.size = size;
        }

        private void GetOrCreateOccupancyObject()
        {
            int occupancyLayer = WorldBounds.GetOccupancySpaceActiveLayer();
            foreach (var collider in GetComponentsInChildren<BoxCollider>(true))
            {
                if (collider.gameObject.layer == occupancyLayer)
                {
                    m_occupiedSpace = collider;
                    return;
                }
            }
            GameObject go = new GameObject("Space");
            go.transform.SetParent(transform, false);
            go.layer = occupancyLayer;

            // Search for UI ..
            var rectTransform = GetComponentInChildren<RectTransform>(true);
            if (rectTransform != null)
            {
                m_occupiedSpace = go.AddComponent<BoxCollider>();
                Vector3[] corners = new Vector3[4];
                rectTransform.GetLocalCorners(corners);
                ResizeOccupancyVolume((corners[2] + corners[0]) * 0.5f + rectTransform.localPosition,
                                     new Vector3(Mathf.Abs(corners[2].x - corners[1].x) * rectTransform.lossyScale.x,
                                                 Mathf.Abs(corners[1].y - corners[0].y) * rectTransform.lossyScale.y,
                                                 Mathf.Max(Mathf.Abs(corners[2].z - corners[0].z) * rectTransform.lossyScale.z, 0.01f)));
                return;
            }

            Bounds biggestBounds = new Bounds(transform.position, Vector3.zero);
            bool boundsSet = false;
            // Search for renderers bounds
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                boundsSet = true;
                biggestBounds.Encapsulate(renderer.bounds);
            }

            if (boundsSet)
            {
                m_occupiedSpace = go.AddComponent<BoxCollider>();
                ResizeOccupancyVolume(biggestBounds.center - transform.position, biggestBounds.size);
                return;
            }
            // Search for colliders
            boundsSet = false;

            foreach (var collider in GetComponentsInChildren<Collider>())
            {
                boundsSet = true;
                biggestBounds.Encapsulate(collider.bounds);
            }

            if (boundsSet)
            {
                m_occupiedSpace = go.AddComponent<BoxCollider>();
                ResizeOccupancyVolume(biggestBounds.center - transform.position, biggestBounds.size);
                return;
            }
        }

        private void OnDestroy()
        {
            if (m_occupiedSpace != null)
            {
                if (Application.isEditor) {
                    DestroyImmediate(m_occupiedSpace.gameObject);
                }
                else {
                    Destroy(m_occupiedSpace.gameObject);
                }
            }
        }
    }
}
