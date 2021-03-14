using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [CreateAssetMenu(fileName = "AnimationBlocksCatalogue", menuName = "WEAVR/Actions Catalogue")]
    [DefaultExecutionOrder(-28499)]
    public class AnimationsCatalogue : BaseCatalogue<AnimationDescriptor>
    {
        
        protected override string RootName => "Actions";

        protected override AnimationDescriptor GetDescriptorFor(ProcedureObject obj)
        {
            if (obj is BaseAnimationBlock block && m_descriptors.TryGetValue(obj.GetType(), out List<AnimationDescriptor> descriptors))
            {
                return block.Variant < descriptors.Count ? descriptors[block.Variant] : descriptors[0];
            }
            return null;
        }

        protected override void UpdateFrom(AnimationDescriptor descriptor)
        {
            if (!descriptor.Sample)
            {
                DestroyImmediate(descriptor, true);
                Debug.LogError($"[{name}]: Animation description uses non existing descriptor with type {descriptor.SampleType}");
                return;
            }
            if (!m_descriptors.TryGetValue(descriptor.Sample.GetType(), out List<AnimationDescriptor> descriptors))
            {
                descriptors = new List<AnimationDescriptor>();
                m_descriptors.Add(descriptor.Sample.GetType(), descriptors);
            }
            if (!descriptors.Contains(descriptor))
            {
                descriptor.Variant = descriptors.Count;
                descriptors.Add(descriptor);
            }
            while (m_descriptorsByPath.ContainsKey(descriptor.FullPath))
            {
                descriptor.Name += "_";
            }
            m_descriptorsByPath.Add(descriptor.FullPath, descriptor);
        }
    }
}
