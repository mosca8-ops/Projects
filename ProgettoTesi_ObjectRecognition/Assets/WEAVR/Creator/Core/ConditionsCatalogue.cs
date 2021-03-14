using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [CreateAssetMenu(fileName = "ConditionsCatalogue", menuName = "WEAVR/Conditions Catalogue")]
    [DefaultExecutionOrder(-28498)]
    public class ConditionsCatalogue : BaseCatalogue<ConditionDescriptor>
    {
        protected override string RootName => "Conditions";

        protected override ConditionDescriptor GetDescriptorFor(ProcedureObject obj)
        {
            if (obj is BaseCondition condition && m_descriptors.TryGetValue(obj.GetType(), out List<ConditionDescriptor> descriptors))
            {
                return condition.Variant < descriptors.Count ? descriptors[condition.Variant] : descriptors[0];
            }
            return null;
        }

        protected override void UpdateFrom(ConditionDescriptor descriptor)
        {
            if (!descriptor.Sample)
            {
                DestroyImmediate(descriptor, true);
                Debug.LogError($"[{name}]: Condition description uses non existing descriptor with type {descriptor.SampleType}");
                return;
            }
            if (!m_descriptors.TryGetValue(descriptor.Sample.GetType(), out List<ConditionDescriptor> descriptors))
            {
                descriptors = new List<ConditionDescriptor>();
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
