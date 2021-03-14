using System;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Controls;
using UnityEditor;

namespace TXT.WEAVR.Procedure
{
    internal class AddItemDataSource : AdvancedDropdownDataSource
    {
        private Descriptor m_root;
        private Action<Descriptor> m_callback;
        private Func<Descriptor, bool> m_filter;
        private Func<Descriptor, bool> m_isSearchableFilter;

        public AddItemDataSource(Descriptor root, Action<Descriptor> callback, 
                                 Func<Descriptor, bool> filterCallback, 
                                 Func<Descriptor, bool> canBeSearchedFilter)
        {
            m_root = root;
            m_callback = callback;
            m_filter = filterCallback ?? (d => true);
            m_isSearchableFilter = canBeSearchedFilter ?? (d => !(d is DescriptorGroup));
        }

        protected override AdvancedDropdownItem FetchData()
        {
            return RebuildTree();
        }

        protected AdvancedDropdownItem RebuildTree()
        {
            m_SearchableElements = new List<AdvancedDropdownItem>();
            AdvancedDropdownItem root = new ItemDropdownItem(m_root, m_callback);

            if (m_root is DescriptorGroup group)
            {
                foreach (var child in group.Children)
                {
                    if (child && m_filter(child))
                    {
                        AddChildrenRecursive(root, child);
                    }
                }
            }

            //root = root.children.Single();
            //root.SetParent(null);
            return root;
        }

        protected virtual void AddChildrenRecursive(AdvancedDropdownItem parent, Descriptor descriptor)
        {
            ItemDropdownItem item = new ItemDropdownItem(descriptor, m_callback);
            item.SetParent(parent);
            parent.AddChild(item);
            if (m_isSearchableFilter(descriptor))
            {
                m_SearchableElements.Add(item);
            }
            if(descriptor is DescriptorGroup group)
            {
                foreach(var child in group.Children)
                {
                    if (child && m_filter(child))
                    {
                        AddChildrenRecursive(item, child);
                    }
                }
            }
        }
    }
}
