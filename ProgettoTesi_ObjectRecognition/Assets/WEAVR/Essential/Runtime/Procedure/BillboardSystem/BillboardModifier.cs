using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public abstract class BillboardModifier : ProcedureObject, IProgressElement, IRequiresValidation, IPreviewAnimation
    {
        [SerializeField]
        protected int m_elementId;
        [SerializeField]
        protected string m_elementName;
        [SerializeField]
        private bool m_enabled = true;

        public int separator;

        [NonSerialized]
        protected float m_progress;
        [NonSerialized]
        private BillboardAction m_action;
        public BillboardAction Action
        {
            get => m_action;
            set
            {
                if(m_action != value)
                {
                    m_action = value;

                }
            }
        }

        public string ElementName => m_elementName;

        public int ElementId => m_elementId;

        public virtual bool Enabled
        {
            get => m_enabled;
            set
            {
                if(m_enabled != value)
                {
                    BeginChange();
                    m_enabled = value;
                    PropertyChanged(nameof(Enabled));
                }
            }
        }

        public virtual void Prepare(Billboard billboard) { }
        public abstract void Apply(float dt);
        public virtual void FastForward() { }
        public virtual void OnRevert() { }
        public abstract void GetTargetFrom(Billboard billboard);

        public void ResetProgress()
        {
            Progress = 0;
        }

        public virtual void OnValidate()
        {
            
        }

        public abstract void ApplyPreview(GameObject previewGameObject);

        public virtual bool CanPreview() => true;

        public virtual float Progress { get => m_progress; set => m_progress = value; }

        public virtual string Description => string.Empty;
    }

    public abstract class BillboardModifier<T> : BillboardModifier, ITargetingObject where T : Component
    {
        [NonSerialized]
        protected T m_target;

        public UnityEngine.Object Target {
            get => m_target;
            set
            {
                if(value is T t)
                {
                    SetTarget(t);
                }
                else if(value is GameObject go)
                {
                    SetTarget(go.GetComponent<T>());
                }
                else if(value is Component c)
                {
                    SetTarget(c.GetComponent<T>());
                }
                else if(value == null)
                {
                    SetTarget(null);
                }
            }
        }

        private void SetTarget(T newTarget)
        {
            if(m_target != newTarget)
            {
                m_target = newTarget;
                if (newTarget)
                {
                    m_elementId = newTarget.GetComponent<BillboardElement>()?.ID ?? -1;
                    m_elementName = m_elementId >= 0 ? newTarget.name : string.Empty;
                }
                else
                {
                    m_elementId = -1;
                    m_elementName = string.Empty;
                }
            }
        }

        public override void Prepare(Billboard billboard)
        {
            GetTargetFrom(billboard);
            base.Prepare(billboard);
        }

        public sealed override void GetTargetFrom(Billboard billboard)
        {
            if (billboard.Elements.TryGetValue(m_elementId, out BillboardElement elem))
            {
                m_target = elem.GetComponent<T>();
            }
        }

        public override void ApplyPreview(GameObject previewBillboard)
        {
            if(m_elementId < 0) { return; }
            var elem = previewBillboard.GetComponentsInChildren<BillboardElement>().FirstOrDefault(e => e.ID == m_elementId);
            if (elem)
            {
                T previewElem = elem.GetComponent<T>();
                if (previewElem)
                {
                    ApplyPreview(previewElem);
                }
            }
        }

        protected abstract void ApplyPreview(T previewElem);

        public string TargetFieldName => nameof(m_target);

        //public override bool Enabled { get => base.Enabled && m_target; set => base.Enabled = value; }
    }
}
