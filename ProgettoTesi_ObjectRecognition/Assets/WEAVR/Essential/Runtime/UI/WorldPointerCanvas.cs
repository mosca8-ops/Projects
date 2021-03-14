using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Interaction
{
    [RequireComponent(typeof(Canvas))]
    [AddComponentMenu("WEAVR/UI/World Pointer Canvas")]
    public class WorldPointerCanvas : MonoBehaviour
    {
        private static readonly List<Canvas> s_canvases = new List<Canvas>();
        public static IReadOnlyList<Canvas> Canvases => s_canvases;

        private Canvas m_canvas;
        
        // Use this for initialization
        void Awake()
        {
            m_canvas = GetComponent<Canvas>();
            s_canvases.Add(m_canvas);
        }

        private void OnEnable()
        {
            if (!s_canvases.Contains(m_canvas))
            {
                s_canvases.Add(m_canvas);
            }
        }

        private void OnDisable()
        {
            s_canvases.Remove(m_canvas);
        }
    }
}
