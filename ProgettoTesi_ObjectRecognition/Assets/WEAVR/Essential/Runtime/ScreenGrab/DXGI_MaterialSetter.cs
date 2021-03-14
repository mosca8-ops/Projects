using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.ScreenGrab
{

    [AddComponentMenu("WEAVR/Advanced/DXGI Material Setter")]
    public class DXGI_MaterialSetter : MonoBehaviour, IDXGI_DataClient
    {
        [SerializeField]
        private Material m_material;

        [SerializeField]
        private int m_monitor;
        [SerializeField]
        private Vector2 m_offset;
        [SerializeField]
        private Vector2 m_size;
        [SerializeField]
        private bool m_flipX;
        [SerializeField]
        private bool m_flipY;

        public string Id => name;

        private DXGI_Data m_data;
        public DXGI_Data Data {
            get => m_data;
            set
            {
                if(m_data != value)
                {
                    m_data = value;
                    if(m_data != null)
                    {
                        m_monitor = m_data.monitor;
                        m_offset = m_data.offset;
                        m_size = m_data.size;
                        m_flipX = m_data.flipAxis.x != 0;
                        m_flipY = m_data.flipAxis.y != 0;

                        UpdateMaterial();
                    }
                }
            }
        }

        private void Reset()
        {
            var r = GetComponent<Renderer>();
            if (r)
            {
                m_material = Application.isPlaying ? r.material : r.sharedMaterial;
            }
        }

        private void OnValidate()
        {
            if (!m_material)
            {
                var r = GetComponent<Renderer>();
                if (r)
                {
                    m_material = Application.isPlaying ? r.material : r.sharedMaterial;
                }
            }
            if(m_data != null)
            {
                m_data.id = Id;
                m_data.monitor = m_monitor;
                m_data.offset = m_offset;
                m_data.size = m_size;
                m_data.flipAxis = new Vector2Int(m_flipX ? 1 : 0, m_flipY ? 1 : 0);

                UpdateMaterial();
            }
        }

        private void UpdateMaterial()
        {
            if(m_data == null || !Application.isPlaying) { return; }
            var texture = DXGI.Instance.GetTexture(m_data.monitor);
            m_material.mainTexture = texture;
            m_material.mainTextureOffset = m_data.NormalizedFlippedOffset(texture);
            m_material.mainTextureScale = m_data.NormalizedFlippedSize(texture);
        }

        void Start()
        {
            var r = GetComponent<Renderer>();
            if (!m_material)
            {
                if (r)
                {
                    m_material = r.material;
                }
            }

            r.material = m_material = new Material(m_material);

            DXGI.Instance.RegisterClient(this);
            DXGI.Instance.Changed -= UpdateMaterial;
            DXGI.Instance.Changed += UpdateMaterial;
            UpdateMaterial();
        }

        private void OnDestroy()
        {
            var unsafeInstance = DXGI.UnsafeInstance;
            if (unsafeInstance)
            {
                unsafeInstance.Changed -= UpdateMaterial;
                unsafeInstance.UnregisterClient(this);
            }
        }
    }
}
