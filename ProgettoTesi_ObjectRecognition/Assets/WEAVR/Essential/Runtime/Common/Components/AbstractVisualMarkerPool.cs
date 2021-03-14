using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    public abstract class AbstractVisualMarkerPool<T> : MonoBehaviour where T : IVisualMarker
    {
        
        [SerializeField]
        [Draggable]
        protected Transform m_container;

        private Dictionary<T, List<T>> m_markers = new Dictionary<T, List<T>>();
        private Dictionary<T, T> m_markerSamples = new Dictionary<T, T>();
        protected ICollection<T> AllPooledMarkers => m_markerSamples.Keys;
        protected ICollection<T> AllSamples => m_markers.Keys;
        public abstract T DefaultSample { get; }

        protected virtual void OnValidate()
        {
            if (!m_container) { m_container = transform; }
        }

        public T GetMarker(T sample)
        {
            sample = sample != null ? sample : DefaultSample;
            if (m_markers.TryGetValue(sample, out List<T> markers) && markers.Count > 0)
            {
                var result = markers[0];
                markers.RemoveAt(0);
                result.CopyValuesFrom(sample);
                OnUnpooled(result);
                return result;
            }

            var marker = InstantiateNewMarker(sample);
            if(marker == null) { return default; }

            if (!m_markers.ContainsKey(sample))
            {
                m_markers[sample] = new List<T>();
            }

            m_markerSamples[marker] = sample;

            marker.Released -= Marker_Released;
            marker.Released += Marker_Released;

            marker.CopyValuesFrom(sample);
            OnUnpooled(marker);

            return marker;
        }

        protected virtual void Marker_Released(IVisualMarker marker)
        {
            if (marker is T t)
            {
                OnMarkerReleased(t);
            }
        }

        private void OnMarkerReleased(T marker)
        {
            if (m_markerSamples.TryGetValue(marker, out T sample) && m_markers.TryGetValue(sample, out List<T> markers) && !markers.Contains(marker))
            {
                markers.Add(marker);
            }
            OnPooled(marker);
        }

        protected abstract T InstantiateNewMarker(T sample);

        protected virtual void OnPooled(T marker)
        {
            if (marker is Component c)
            {
                c.transform.SetParent(m_container ? m_container : transform, false);
                if (!marker.AutoDisableOnRelease)
                {
                    c.gameObject.SetActive(false);
                }
            }
        }

        protected virtual void OnUnpooled(T result)
        {
            if (result is Component c)
            {
                c.gameObject.SetActive(true);
            }
        }
    }
}
