using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class DescriptorGroup : Descriptor
    {
        [SerializeField]
        private List<Descriptor> m_children;

        public IReadOnlyList<Descriptor> Children => m_children;

        protected override void OnEnable()
        {
            base.OnEnable();
            if(m_children == null)
            {
                m_children = new List<Descriptor>();
            }
        }

        public void Add(Descriptor child)
        {
            if (m_children.Contains(child)) { return; }

            child.FullPath = FullPath + "/" + child.Name;
            if (child.Color == Color.clear)
            {
                child.Color = Color;
            }
            child.Depth = Depth + 1;
            m_children.Add(child);
        }

        public void Remove(Descriptor child)
        {
            if (m_children.Remove(child))
            {
                child.FullPath = RemoveParentPathFromChild(FullPath, child);
                if(child is DescriptorGroup)
                {
                    (child as DescriptorGroup).Clear();
                }
                DestroyImmediate(child, true);
            }
        }

        public int IndexOf(Descriptor child)
        {
            return m_children.IndexOf(child);
        }

        public void Insert(int index, Descriptor child)
        {
            if (m_children.Contains(child)) {
                m_children.Remove(child);
            }

            child.FullPath = FullPath + "/" + child.Name;
            if (child.Color == Color.clear)
            {
                child.Color = Color;
            }
            child.Depth = Depth + 1;
            m_children.Insert(index, child);
        }

        public override void Clear()
        {
            base.Clear();
            foreach(var child in Children)
            {
                if (child != null)
                {
                    child.Clear();
                    DestroyImmediate(child, true);
                }
            }
            m_children.Clear();
        }

        private string RemoveParentPathFromChild(string path, Descriptor child)
        {
            if (child.FullPath.StartsWith(path))
            {
                return child.FullPath.Remove(0, path.Length);
            }
            return child.FullPath;
        }

        public void UpdateFullPaths()
        {
            FullPath = FullPath.TrimStart('/');
            foreach(var descriptor in m_children)
            {
                if (descriptor)
                {
                    descriptor.FullPath = FullPath + "/" + descriptor.Name;
                    if (descriptor is DescriptorGroup)
                    {
                        (descriptor as DescriptorGroup).UpdateFullPaths();
                    }
                }
            }
        }

        internal void PropagateColor()
        {
            foreach(var child in m_children)
            {
                child.Color = Color;
                if(child is DescriptorGroup)
                {
                    (child as DescriptorGroup).PropagateColor();
                }
            }
        }
    }
}
