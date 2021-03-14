using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Component Location/Mesh Descriptor")]
    public class MeshDescriptor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {

        public enum ColliderType { Box, Mesh, ConvexMesh }

        public ColliderType colliderType = ColliderType.Mesh;
        public string description;
        [SerializeField]
        private bool m_preferMeshName = false;
        [SerializeField]
        [Draggable]
        private Renderer m_target;
        [SerializeField]
        //[ShowAsReadOnly]
        [Draggable]
        [Button(nameof(MakeCollider), "Make")]
        private Collider m_collider;

        [Space]
        public UnityEvent OnHoverEnter;
        public UnityEvent OnHoverExit;

        public Renderer Target
        {
            get
            {
                if (!m_target)
                {
                    m_target = GetComponentInChildren<Renderer>(true);
                }
                return m_target;
            }
        }

        private MeshDescriptorGroup m_group;

        public MeshDescriptorGroup Group
        {
            get
            {
                if (!m_group)
                {
                    m_group = GetComponentInParent<MeshDescriptorGroup>();
                }
                return m_group;
            }
        }

        private void Reset()
        {
            m_target = GetComponentInChildren<Renderer>(true);
            m_collider = GetComponentInChildren<Collider>();
        }

        private void OnValidate()
        {
            if (!m_target)
            {
                m_target = GetComponentInChildren<Renderer>(true);
            }
        }

        [ContextMenu("Make Collider")]
        private void MakeCollider()
        {
            if (m_collider && !m_collider.transform.IsChildOf(Target.transform)) { return; }

            var boxCollider = Target.GetComponent<BoxCollider>();
            var meshCollider = Target.GetComponent<MeshCollider>();
            if (colliderType == ColliderType.Box)
            {
                if (meshCollider)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(meshCollider);
                    }
                    else
                    {
                        DestroyImmediate(meshCollider);
                    }
                }
                if (!boxCollider)
                {
                    boxCollider = Target.gameObject.AddComponent<BoxCollider>();
                }
                m_collider = boxCollider;
            }
            else
            {
                if (boxCollider)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(boxCollider);
                    }
                    else
                    {
                        DestroyImmediate(boxCollider);
                    }
                }
                if (!meshCollider)
                {
                    meshCollider = Target.gameObject.AddComponent<MeshCollider>();
                }
                meshCollider.convex = colliderType == ColliderType.ConvexMesh;
                m_collider = meshCollider;
            }
        }

        private void Start()
        {
            Initialize();
            MeshDescriptionManager.Instance.Register(this);
        }

        private void OnDestroy()
        {
            MeshDescriptionManager.Instance?.Unregister(this);
        }

        protected void Initialize()
        {
            MakeCollider();
        }

        private void OnEnable()
        {
            if (m_collider) { m_collider.enabled = true; }
        }

        private void OnDisable()
        {
            if (m_collider) { m_collider.enabled = false; }
        }

        public string ToJson() => JsonUtility.ToJson(CreateDescription());

        public Description CreateDescription() => new Description() { mesh = m_preferMeshName ? Target.GetComponent<MeshFilter>().sharedMesh.name : gameObject.name, colliderType = colliderType, description = description };

        public void FromJson(string json)
        {
            var descr = JsonUtility.FromJson<Description>(json);
            colliderType = descr.colliderType;
            description = descr.description;

            Initialize();
        }

        public void Apply(Description descr, bool nameOriginatesFromMesh)
        {
            colliderType = descr.colliderType;
            description = descr.description;

            m_preferMeshName = nameOriginatesFromMesh;
            Initialize();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnHoverEnter?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnHoverExit?.Invoke();
        }

        [Serializable]
        public class Description
        {
            public string mesh;
            public string description;
            public ColliderType colliderType;
        }
    }
}