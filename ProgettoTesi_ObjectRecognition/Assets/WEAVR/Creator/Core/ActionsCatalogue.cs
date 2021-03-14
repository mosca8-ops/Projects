using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [CreateAssetMenu(fileName = "ActionsCatalogue", menuName = "WEAVR/Actions Catalogue")]
    [DefaultExecutionOrder(-28500)]
    public class ActionsCatalogue : BaseCatalogue<ActionDescriptor>
    {
        
        protected override string RootName => "Actions";

        protected override ActionDescriptor GetDescriptorFor(ProcedureObject obj)
        {
            if (obj is BaseAction action && m_descriptors.TryGetValue(obj.GetType(), out List<ActionDescriptor> descriptors))
            {
                return action.Variant < descriptors.Count ? descriptors[action.Variant] : descriptors[0];
            }
            return null;
        }

        protected override void UpdateFrom(ActionDescriptor descriptor)
        {
            if (!descriptor.Sample)
            {
                DestroyImmediate(descriptor, true);
                Debug.LogError($"[{name}]: Action description uses non existing descriptor with type {descriptor.SampleType}");
                return;
            }
            if (!m_descriptors.TryGetValue(descriptor.Sample.GetType(), out List<ActionDescriptor> descriptors))
            {
                descriptors = new List<ActionDescriptor>();
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
