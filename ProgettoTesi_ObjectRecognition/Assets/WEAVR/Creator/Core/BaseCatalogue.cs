using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    public abstract class BaseCatalogue<T> : BaseCatalogue where T : Descriptor
    {
        protected Dictionary<Type, List<T>> m_descriptors;
        protected Dictionary<string, T> m_descriptorsByPath;

        public IReadOnlyDictionary<Type, List<T>> Descriptors => m_descriptors;
        public IReadOnlyDictionary<string, T> DescriptorsByPath => m_descriptorsByPath;

        protected override void OnEnable()
        {
            base.OnEnable();
            Refresh();
        }

        public T GetDescriptor(ProcedureObject obj)
        {
            if (m_descriptors == null)
            {
                Refresh();
            }
            return GetDescriptorFor(obj);
        }

        protected abstract T GetDescriptorFor(ProcedureObject obj);
        //{
        //    if (m_descriptors.TryGetValue(action.GetType(), out List<T> descriptors))
        //    {
        //        return action.Variant < descriptors.Count ? descriptors[action.Variant] : descriptors[0];
        //    }
        //    return null;
        //}

        public void Refresh()
        {
            if(m_descriptors == null)
            {
                m_descriptors = new Dictionary<Type, List<T>>();
            }
            if(m_descriptorsByPath == null)
            {
                m_descriptorsByPath = new Dictionary<string, T>();
            }

            m_descriptors.Clear();
            m_descriptorsByPath.Clear();

            UpdateFromDescriptor(m_root);
        }

        private void UpdateFromActionGroup(DescriptorGroup group)
        {
            foreach(var descriptor in group.Children)
            {
                UpdateFromDescriptor(descriptor);
            }
        }

        private void UpdateFromDescriptor(Descriptor descriptor)
        {
            if (descriptor is DescriptorGroup)
            {
                UpdateFromActionGroup(descriptor as DescriptorGroup);
            }
            else if (descriptor is T t)
            {
                UpdateFrom(t);
            }
        }

        protected abstract void UpdateFrom(T descriptor);
    }

    public abstract class BaseCatalogue : ScriptableObject, IDescriptorCatalogue
    {
        [SerializeField]
        protected DescriptorGroup m_root;

        public DescriptorGroup Root => m_root;
        
        protected virtual string RootName => "Catalogue";

        protected virtual void OnEnable()
        {
            if (m_root == null)
            {
                m_root = CreateInstance<DescriptorGroup>();
                m_root.Depth = -1;
                m_root.Name = RootName;
            }
        }
    }
}
