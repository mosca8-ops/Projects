using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.ScreenGrab
{

    [AddComponentMenu("WEAVR/Advanced/DXGI Skybox")]
    public class DXGI_Skybox : MonoBehaviour, IDXGI_GroupDataClient
    {
        private static readonly (string texName, string face)[] k_texNames =
        {
            ("_FrontTex", "Front"),
            ("_BackTex", "Back"),
            ("_LeftTex", "Left"),
            ("_RightTex", "Right"),
            ("_UpTex", "Up"),
            ("_DownTex", "Down")
        };

        private Dictionary<string, string> m_textureNames;
        public IReadOnlyDictionary<string, string> TextureNames
        {
            get
            {
                if(m_textureNames == null)
                {
                    m_textureNames = new Dictionary<string, string>();
                    for (int i = 0; i < k_texNames.Length; i++)
                    {
                        m_textureNames[k_texNames[i].face] = k_texNames[i].texName;
                    }
                }
                return m_textureNames;
            }
        }

        [SerializeField]
        private Material m_skyboxMaterial;
        [SerializeField]
        private DXGI_Data[] m_data;

        private DXGI_GroupData m_groupData;
        public string Id => "Skybox";
        public DXGI_GroupData GroupData {
            get => m_groupData;
            set
            {
                if(m_groupData != value)
                {
                    m_groupData = value;
                    if(m_groupData != null)
                    {
                        m_data = m_groupData.data;
                        UpdateMaterial();
                    }
                }
            }
        }

        private void UpdateMaterial()
        {
            if (!Application.isPlaying) { return; }
            ValidateData();

            foreach(var data in m_data)
            {
                var texture = DXGI.Instance.GetTexture(data.monitor);
                var texName = TextureNames[data.id];
                m_skyboxMaterial.SetTexture(texName, texture);
                m_skyboxMaterial.SetTextureOffset(texName, data.NormalizedFlippedOffset(texture));
                m_skyboxMaterial.SetTextureScale(texName, data.NormalizedFlippedSize(texture));
            }
        }

        private void ValidateData()
        {
            if (m_data == null || m_data.Length < Mathf.Min(4, k_texNames.Length))
            {
                m_data = new DXGI_Data[k_texNames.Length];
                for (int i = 0; i < k_texNames.Length; i++)
                {
                    m_data[i] = new DXGI_Data()
                    {
                        id = k_texNames[i].face,
                        monitor = 0,
                        offset = Vector2.zero,
                        size = Vector2.zero,
                        flipAxis = Vector2Int.zero
                    };
                }
                if(m_groupData == null)
                {
                    m_groupData = new DXGI_GroupData()
                    {
                        groupId = Id
                    };
                }
                m_groupData.data = m_data;
            }
        }

        private void OnValidate()
        {
            UpdateMaterial();
        }


        void Start()
        {
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
