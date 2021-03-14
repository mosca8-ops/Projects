using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    public class ActionDescriptor : Descriptor, IHasDescription
    {
        [SerializeField]
        private string m_description;
        [SerializeField]
        private int m_variant;
        [SerializeField]
        protected BaseAction m_defaultAction;
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

        public BaseAction Sample
        {
            get => m_defaultAction;
            set
            {
                if(m_defaultAction != value)
                {
                    m_defaultAction = value;
                    if (m_defaultAction)
                    {
                        Name = m_defaultAction.GetType().Name;
                        m_sampleTypename = m_defaultAction.GetType().Name;
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

        public virtual BaseAction Create()
        {
            if (m_defaultAction)
            {
                var newAction = m_defaultAction.Clone() as BaseAction;
                newAction.Variant = Variant;
                newAction.name = m_defaultAction.name;
                newAction.Guid = Guid.NewGuid().ToString();
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
            if(m_defaultAction && !string.IsNullOrEmpty(m_sampleTypename))
            {
                m_sampleTypename = m_defaultAction.GetType().Name;
            }
        }

        public static ActionDescriptor CreateDescriptor(Type actionType)
        {
            if (!actionType.IsSubclassOf(typeof(BaseAction))) {
                Debug.LogError($"Type {actionType.FullName} is not a subclass of {typeof(BaseAction).FullName}");
                return null;
            }

            var descriptor = CreateInstance<ActionDescriptor>();
            descriptor.Sample = CreateInstance(actionType) as BaseAction;

            return descriptor;
        }

        public override void Clear()
        {
            base.Clear();
            DestroyImmediate(Sample, true);
        }
    }
}
