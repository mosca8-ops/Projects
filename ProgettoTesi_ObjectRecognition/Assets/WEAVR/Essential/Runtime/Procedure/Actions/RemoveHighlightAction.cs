using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class RemoveHighlightAction : BaseAction, ITargetingObject
    {
        [Serializable]
        public class OptionalBillboardRemoval : Optional<BillboardRemoval>
        {
            public static implicit operator OptionalBillboardRemoval(BillboardRemoval value)
            {
                return new OptionalBillboardRemoval()
                {
                    enabled = true,
                    value = value
                };
            }

            public static implicit operator BillboardRemoval(OptionalBillboardRemoval optional)
            {
                return optional.value;
            }
        }

        public enum BillboardRemoval
        {
            All,
            BySampleType,
            LastOne,
        }

        [SerializeField]
        [Tooltip("The object to remove the highlight from")]
        [Draggable]
        private ValueProxyGameObject m_target;
        [SerializeField]
        [Tooltip("Which kind of billboard to remove")]
        private OptionalBillboardRemoval m_removeBillboard;
        [SerializeField]
        [Tooltip("The billboard sample to remove from the target")]
        [ShowIf(nameof(ShowBillboardSample))]
        private ValueProxyBillboard m_billboardSample;
        [SerializeField]
        [Tooltip("Whether to remove or not the outline from the target")]
        private bool m_removeOutline;
        [SerializeField]
        [Tooltip("Whether to remove or not the navigation the target location")]
        private bool m_removeNavigation;

        public UnityEngine.Object Target {
            get => m_target;
            set => m_target.Value = value is GameObject go ? go : value is Component c ? c.gameObject : value == null ? null : m_target.Value;
        }

        public string TargetFieldName => nameof(Target);

        public BillboardRemoval? RemoveBillboard
        {
            get => m_removeBillboard.enabled ? m_removeBillboard.value : (BillboardRemoval?)null;
            set
            {
                if(m_removeBillboard.enabled != value.HasValue || (value.HasValue && m_removeBillboard.value != value.Value))
                {
                    BeginChange();
                    m_removeBillboard.enabled = value.HasValue;
                    m_removeBillboard.value = value ?? m_removeBillboard.value;
                    PropertyChanged(nameof(RemoveBillboard));
                }
            }
        }

        public bool RemoveOutline
        {
            get => m_removeOutline;
            set
            {
                if(m_removeOutline != value)
                {
                    BeginChange();
                    m_removeOutline = value;
                    PropertyChanged(nameof(RemoveOutline));
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (m_removeBillboard == null)
            {
                m_removeBillboard = new OptionalBillboardRemoval();
            }
        }

        private bool ShowBillboardSample()
        {
            return m_removeBillboard.enabled && m_removeBillboard.value == BillboardRemoval.BySampleType;
        }

        public override bool Execute(float dt)
        {
            ApplyData();
            return true;
        }

        private void ApplyData()
        {
            var target = m_target.Value;
            if (m_removeOutline)
            {
                Outliner.RemoveOutline(target);
            }
            if(m_removeNavigation && NavigationArrow.Current && NavigationArrow.Current.Target == target)
            {
                NavigationArrow.Current.Target = null;
            }
            if (m_removeBillboard.enabled)
            {
                switch (m_removeBillboard.value)
                {
                    case BillboardRemoval.All:
                        BillboardManager.Instance.HideBillboardOn(target);
                        break;
                    case BillboardRemoval.LastOne:
                        BillboardManager.Instance.HideLastBillboardOn(target);
                        break;
                    case BillboardRemoval.BySampleType:
                        BillboardManager.Instance.HideBillboardOn(target, m_billboardSample);
                        break;
                }
            }
        }

        public override void FastForward()
        {
            base.FastForward();
            ApplyData();
        }

        public override string GetDescription()
        {
            string target = m_target.IsVariable ? $"[{m_target.VariableName}]" : m_target.Value ? m_target.Value.name : "[ ? ]";
            string removeBillboard = string.Empty;
            if (m_removeBillboard.enabled)
            {
                switch (m_removeBillboard.value)
                {
                    case BillboardRemoval.All:
                        removeBillboard = "all billboards";
                        break;
                    case BillboardRemoval.LastOne:
                        removeBillboard = "last billboard";
                        break;
                    case BillboardRemoval.BySampleType:
                        removeBillboard = "sample billboard";
                        break;
                }
            }
            string removeOutline = m_removeOutline ? "outline" : string.Empty;
            return removeBillboard == string.Empty && removeOutline == string.Empty ?
                $"No highlight to remove from {target}" :
                removeOutline == string.Empty ? $"Remove {removeBillboard} from {target}" :
                removeBillboard == string.Empty ? $"Remove {removeOutline} from {target}" :
                $"Remove {removeBillboard} and {removeOutline} from {target}";
        }
    }
}
