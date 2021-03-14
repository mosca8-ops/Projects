using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Component Location/Mesh Descriptor Group")]
    public class MeshDescriptorGroup : MonoBehaviour
    {
        [Draggable]
        public MeshDescriptor[] descriptors;

        private void OnValidate()
        {
            descriptors = GetComponentsInChildren<MeshDescriptor>(true);
        }

        private void Reset()
        {
            descriptors = GetComponentsInChildren<MeshDescriptor>(true);
        }

        private void Start()
        {
            if (descriptors == null || descriptors.Length == 0)
            {
                descriptors = GetComponentsInChildren<MeshDescriptor>(true);
            }
        }

        public DescriptionGroup CreateDescriptionGroup() => new DescriptionGroup() { group = gameObject.name, meshes = descriptors.Select(m => m.CreateDescription()).ToArray() };

        [Serializable]
        public class DescriptionGroup
        {
            public string group;
            public MeshDescriptor.Description[] meshes;
        }
    }
}
