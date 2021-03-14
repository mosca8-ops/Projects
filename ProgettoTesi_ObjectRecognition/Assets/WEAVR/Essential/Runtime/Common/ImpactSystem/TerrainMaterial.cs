using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.ImpactSystem
{
    [RequireComponent(typeof(Terrain))]
    [RequireComponent(typeof(TerrainCollider))]
    [AddComponentMenu("WEAVR/Impact System/Terrain Material")]
    public class TerrainMaterial : AbstractObjectMaterial
    {
        [SerializeField]
        [Draggable]
        private TerrainCollider m_collider;
        [SerializeField]
        [Draggable]
        private Terrain m_terrain;
        [SerializeField]
        [ShowAsReadOnly]
        private TerrainData m_data;
        [SerializeField]
        [Draggable]
        private ImpactMaterial m_fallbackMaterial;

        [SerializeField]
        private List<LayerMaterial> m_layers = new List<LayerMaterial>();

        public override IEnumerable<Collider> Colliders => new Collider[] { m_collider };

        public IReadOnlyList<LayerMaterial> Layers => m_layers;

        public ImpactMaterial FallbackMaterial => m_fallbackMaterial;

        protected override void OnValidate()
        {
            base.OnValidate();
            if (!m_collider)
            {
                m_collider = GetComponent<TerrainCollider>();
            }
            if (!m_terrain)
            {
                m_terrain = GetComponent<Terrain>();
            }

            bool updateLayers = false;
            if(m_data != m_collider.terrainData)
            {
                m_data = m_collider.terrainData;
                updateLayers = true;
            }
            else if(m_data.alphamapLayers != m_layers.Count)
            {
                updateLayers = true;
            }
            else
            {
                foreach(var layer in m_layers)
                {
                    if (!layer.layer)
                    {
                        updateLayers = true;
                        break;
                    }
                }
            }
            if (updateLayers)
            {
                UpdateTerrainData();
            }
        }
        
        private int m_alphamapWidth;
        private int m_alphamapHeight;

        private float[,,] m_splatmapData;
        private int m_numTextures;

        void Start()
        {
            GetTerrainProps();
        }

        private void GetTerrainProps()
        {
            m_alphamapWidth = m_data.alphamapWidth;
            m_alphamapHeight = m_data.alphamapHeight;

            m_splatmapData = m_data.GetAlphamaps(0, 0, m_alphamapWidth, m_alphamapHeight);
            m_numTextures = m_splatmapData.Length / (m_alphamapWidth * m_alphamapHeight);
        }

        public void UpdateTerrainData()
        {
            var terrainLayers = m_data.terrainLayers;
            foreach(var layer in m_layers.Where(l => !terrainLayers.Contains(l.layer)).ToArray())
            {
                m_layers.Remove(layer);
            }
            foreach(var layer in terrainLayers)
            {
                if(!m_layers.Any(l => l.layer == layer))
                {
                    m_layers.Add(new LayerMaterial()
                    {
                        layer = layer,
                    });
                }
            }
        }

        public override ImpactMaterial GetMaterial(Vector3 worldPoint)
        {
            int index = GetTerrainAtPosition(worldPoint);
            return 0 <= index && index < m_layers.Count ? m_layers[index].material : m_fallbackMaterial;
        }

        private Vector3 ConvertToSplatMapCoordinate(Vector3 point)
        {
            Terrain ter = m_terrain;
            Vector3 terPosition = ter.transform.position;
            return new Vector3(((point.x - terPosition.x) / ter.terrainData.size.x) * ter.terrainData.alphamapWidth, 
                                0,
                               ((point.z - terPosition.z) / ter.terrainData.size.z) * ter.terrainData.alphamapHeight);
        }

        private int GetActiveTerrainTextureIdx(Vector3 pos)
        {
            Vector3 terrainCordinates = ConvertToSplatMapCoordinate(pos);
            int ret = 0;
            float comp = 0f;
            for (int i = 0; i < m_numTextures; i++)
            {
                float alpha = m_splatmapData[(int)terrainCordinates.z, (int)terrainCordinates.x, i];
                if (comp < alpha)
                {
                    comp = alpha;
                    ret = i;
                }
            }
            return ret;
        }

        public int GetTerrainAtPosition(Vector3 pos)
        {
            int terrainIdx = GetActiveTerrainTextureIdx(pos);
            return terrainIdx;
        }

        [Serializable]
        public class LayerMaterial
        {
            public TerrainLayer layer;
            public ImpactMaterial material;
        }
    }
}
