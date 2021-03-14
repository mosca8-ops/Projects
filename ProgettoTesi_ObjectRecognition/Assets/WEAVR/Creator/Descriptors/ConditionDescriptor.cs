using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    public class ConditionDescriptor : Descriptor, IHasDescription
    {
        [SerializeField]
        private string m_description;
        [SerializeField]
        private int m_variant;
        [SerializeField]
        private BaseCondition m_defaultCondition;
        [SerializeField]
        private string m_sampleTypename;
        [SerializeField]
        private List<string> m_hiddenProperties;

        public string Description
        {
            get => m_description;
            set
            {
                if (m_description != value)
                {
                    m_description = value;
                }
            }
        }

        public BaseCondition Sample
        {
            get => m_defaultCondition;
            set
            {
                if (m_defaultCondition != value)
                {
                    m_defaultCondition = value;
                    if (m_defaultCondition)
                    {
                        Name = m_defaultCondition.GetType().Name;
                        m_sampleTypename = m_defaultCondition.GetType().Name;
                    }
                }
            }
        }

        public string SampleType => m_sampleTypename;

        public int Variant
        {
            get => m_variant;
            set
            {
                if (m_variant != value)
                {
                    m_variant = value;
                }
            }
        }

        public List<string> HiddenProperties => m_hiddenProperties;

        public BaseCondition Create()
        {
            if (m_defaultCondition)
            {
                var newCondition = Instantiate(m_defaultCondition);
                newCondition.Variant = Variant;
                newCondition.name = m_defaultCondition.name;
                return newCondition;
            }
            return null;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (m_hiddenProperties == null)
            {
                m_hiddenProperties = new List<string>();
            }
            if(m_defaultCondition && !string.IsNullOrEmpty(m_sampleTypename))
            {
                m_sampleTypename = m_defaultCondition.GetType().Name;
            }
        }

        public static ConditionDescriptor CreateDescriptor(Type conditionType)
        {
            if (!conditionType.IsSubclassOf(typeof(BaseCondition)))
            {
                Debug.LogError($"Type {conditionType.FullName} is not a subclass of {typeof(BaseCondition).FullName}");
                return null;
            }

            var descriptor = CreateInstance<ConditionDescriptor>();
            descriptor.Sample = CreateInstance(conditionType) as BaseCondition;

            return descriptor;
        }

        public override void Clear()
        {
            base.Clear();
            DestroyImmediate(Sample, true);
        }
    }
}
