using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TXT.WEAVR.Common;

namespace TXT.WEAVR.Procedure
{
    public class ProcedureMaterialData : ScriptableObject
    {
        public List<MaterialData> MaterialDatas => materialDatas;

        [SerializeField]
        private List<MaterialData> materialDatas = new List<MaterialData>();

        public int Snapshot(Material _material)
        {
            var id = -1;
            if (_material == null)
                return id;

            var matData = materialDatas.FirstOrDefault(m => m.Material == _material);
            if (matData == null)
            {
                matData = materialDatas.FirstOrDefault(m => m.Name == _material.name);
                if (matData == null)
                    id = AddMaterial(_material);
                else
                    id = matData.ID;
            }
            else
            {
                if (!matData.EqualToMaterial(_material))
                    id = AddMaterial(_material);
                else
                    id = matData.ID;
            }
            return id;
        }

        private int AddMaterial(Material _material)
        {
            var id = materialDatas.Count;
            materialDatas.Add(new MaterialData(id, _material));
            return id;
        }

        public Material Restore(int _id)
        {
            var materialData = materialDatas.FirstOrDefault(m => m.ID == _id);
            return materialData.Restore();
        }

        [System.Serializable]
        public class MaterialData
        {
            public int ID => m_id;
            public Material Material => m_material;
            public string Name => Material.name;

            [ShowAsReadOnly]
            [SerializeField]
            private int m_id;
            [ShowAsReadOnly]
            [SerializeField]
            private Material m_material;
            [ShowAsReadOnly]
            [SerializeField]
            private int m_renderQueue;
            [ShowAsReadOnly]
            [SerializeField]
            private Color m_color;
            [ShowAsReadOnly]
            [SerializeField]
            private Vector2 m_mainTextureScale;
            [ShowAsReadOnly]
            [SerializeField]
            private Vector2 m_mainTextureOffset;
            [ShowAsReadOnly]
            [SerializeField]
            private Texture m_mainTexture;

            public MaterialData(int _id, Material _material)
            {
                m_id = _id;
                m_material = _material;
                m_renderQueue = m_material.renderQueue;
                m_color = m_material.color;
                m_mainTextureScale = m_material.mainTextureScale;
                m_mainTextureOffset = m_material.mainTextureOffset;
                m_mainTexture = m_material.mainTexture;
            }

            public Material Restore()
            {
                var matToReturn = new Material(m_material);
                matToReturn.renderQueue = m_renderQueue;
                matToReturn.color = m_color;
                matToReturn.mainTextureScale = m_mainTextureScale;
                matToReturn.mainTextureOffset = m_mainTextureOffset;
                matToReturn.mainTexture = m_mainTexture;
                return matToReturn;
            }

            public bool EqualToMaterial(Material _material)
            {
                if (m_material.name == _material.name &&
                    m_mainTexture == _material.mainTexture &&
                    m_color == _material.color &&
                    m_renderQueue == _material.renderQueue &&
                    m_mainTextureScale == _material.mainTextureScale &&
                    m_mainTextureOffset == _material.mainTextureOffset)
                {
                    return true;
                }
                return false;
            }

        }
    }
}
