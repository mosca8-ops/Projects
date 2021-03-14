using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    public class AnimationDescriptor : Descriptor, IHasDescription
    {
        [SerializeField]
        private string m_description;
        [SerializeField]
        private int m_variant;
        [SerializeField]
        private BaseAnimationBlock m_defaultBlock;
        [SerializeField]
        private string m_sampleTypename;
        [SerializeField]
        private List<string> m_hiddenProperties;

        public string Description
        {
            get => m_description;
            set
            {
                if(m_description != value)
                {
                    m_description = value;
                }
            }
        }

        public BaseAnimationBlock Sample
        {
            get => m_defaultBlock;
            set
            {
                if(m_defaultBlock != value)
                {
                    m_defaultBlock = value;
                    if (m_defaultBlock)
                    {
                        Name = m_defaultBlock.GetType().Name;
                        m_sampleTypename = m_defaultBlock.GetType().Name;
                    }
                    else
                    {
                        m_sampleTypename = string.Empty;
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
                if(m_variant != value)
                {
                    m_variant = value;
                }
            }
        }

        public List<string> HiddenProperties => m_hiddenProperties;

        public BaseAnimationBlock Create()
        {
            if (m_defaultBlock)
            {
                var newAction = Instantiate(m_defaultBlock);
                newAction.Variant = Variant;
                newAction.name = m_defaultBlock.name;
                return newAction;
            }
            return null;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if(m_hiddenProperties == null)
            {
                m_hiddenProperties = new List<string>();
            }
            if(m_defaultBlock && !string.IsNullOrEmpty(m_sampleTypename))
            {
                m_sampleTypename = m_defaultBlock.GetType().Name;
            }
        }

        public static AnimationDescriptor CreateDescriptor(Type blockType)
        {
            if (!blockType.IsSubclassOf(typeof(BaseAnimationBlock))) {
                Debug.LogError($"Type {blockType.FullName} is not a subclass of {typeof(BaseAnimationBlock).FullName}");
                return null;
            }

            var descriptor = CreateInstance<AnimationDescriptor>();
            descriptor.Sample = CreateInstance(blockType) as BaseAnimationBlock;

            return descriptor;
        }

        public override void Clear()
        {
            base.Clear();
            DestroyImmediate(Sample, true);
        }
    }
}
