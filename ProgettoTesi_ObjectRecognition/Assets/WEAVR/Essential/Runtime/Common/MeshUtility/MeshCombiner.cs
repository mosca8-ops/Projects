using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Utilities/Mesh Combiner")]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class MeshCombiner : MonoBehaviour
    {
        public enum MeshSize { 
            Small = 0, 
            Big = 1, 
            Auto = 2, 
        }

        public MeshSize meshSize = MeshSize.Auto;

        private MeshFilter[] m_meshFilters;

        void Start()
        {
            MergeMeshes();
        }

        public void MergeMeshes()
        {
            if(m_meshFilters?.Length > 0)
            {
                foreach (var m in m_meshFilters)
                {
                    m.gameObject.SetActive(false);
                }
                GetComponent<Renderer>().enabled = true;
                return;
            }

            var rotation = transform.rotation;
            transform.rotation = Quaternion.identity;
            var meshFilters = GetComponentsInChildren<MeshFilter>().Where(m => m.gameObject != gameObject).ToArray();
            var meshData = meshFilters.Select(m => new SmartMeshData(m.sharedMesh,
                                                    m.GetComponent<Renderer>().sharedMaterials,
                                                    m.transform.position - transform.position,
                                                    Quaternion.RotateTowards(transform.rotation, m.transform.rotation, 180),
                                                    m.transform.lossyScale));

            var indexFormat = meshSize == MeshSize.Auto ? (meshData.Sum(d => d.mesh.vertexCount) > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16) : (IndexFormat)meshSize;


            if (Application.isPlaying)
            {
                GetComponent<MeshFilter>().mesh.CombineMeshesSmart(meshData.ToArray(), out Material[] materials, indexFormat);
                GetComponent<Renderer>().materials = materials;
            }
            else
            {
                GetComponent<MeshFilter>().sharedMesh.CombineMeshesSmart(meshData.ToArray(), out Material[] materials, indexFormat);
                GetComponent<Renderer>().sharedMaterials = materials;
            }

            m_meshFilters = meshFilters;

            foreach (var m in meshFilters)
            {
                m.gameObject.SetActive(false);
            }

            GetComponent<Renderer>().enabled = true;

            transform.rotation = rotation;
        }

        public void UnmergeMeshes()
        {
            if (m_meshFilters?.Length > 0)
            {
                foreach (var m in m_meshFilters)
                {
                    m.gameObject.SetActive(true);
                }
                GetComponent<Renderer>().enabled = false;
            }
        }

        public (Mesh mesh, Material[] materials) CombineChildren()
        {
            var rotation = transform.rotation;
            transform.rotation = Quaternion.identity;
            var meshFilters = GetComponentsInChildren<MeshFilter>().Where(m => m.gameObject != gameObject).ToArray();
            var meshData = meshFilters.Select(m => new SmartMeshData(m.sharedMesh,
                                                    m.GetComponent<Renderer>().sharedMaterials,
                                                    m.transform.position - transform.position,
                                                    Quaternion.RotateTowards(transform.rotation, m.transform.rotation, 180),
                                                    m.transform.lossyScale));

            var indexFormat = meshSize == MeshSize.Auto ? (meshData.Sum(d => d.mesh.vertexCount) > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16) : (IndexFormat)meshSize;

            Mesh mesh = new Mesh();
            mesh.CombineMeshesSmart(meshData.ToArray(), out Material[] materials, indexFormat);

            transform.rotation = rotation;

            return (mesh, materials);
        }
    }
}
