using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class ShowHideObjectAction : BaseReversibleProgressAction, ITargetingObject, ISerializedNetworkProcedureObject
    {
        [SerializeField]
        [Tooltip("The target object to show or hide")]
        [Draggable]
        private ValueProxyGameObject m_target;
        [SerializeField]
        [Tooltip("Whether to show or hide the object")]
        [Reversible]
        private AnimatedBool m_show;

        #region [  ISerializedNetworkProcedureObject IMPLEMENTATION  ]

        [SerializeField]
        private bool m_isGlobal = true;
        public string IsGlobalFieldName => nameof(m_isGlobal);
        public bool IsGlobal => m_isGlobal;

        #endregion

        [NonSerialized]
        private bool m_prevValue;
        private List<(Material material, float originalAlpha, int mode)> m_allMaterials;

        public UnityEngine.Object Target {
            get => m_target;
            set => m_target.Value = value is GameObject go ? go : value is Component c ? c.gameObject : value == null ? null : m_target.Value; }

        public string TargetFieldName => nameof(m_target);

        public bool Show
        {
            get => m_show.TargetValue;
            set
            {
                if(m_show.TargetValue != value)
                {
                    BeginChange();
                    m_show.TargetValue = value;
                    PropertyChanged(nameof(Show));
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if(m_allMaterials == null)
            {
                m_allMaterials = new List<(Material material, float originalAlpha, int mode)>();
            }
        }

        public override void OnStart(ExecutionFlow flow, ExecutionMode executionMode)
        {
            base.OnStart(flow, executionMode);
            var target = m_target.Value;

            m_prevValue = target.activeSelf;
            if (m_show.IsAnimated)
            {
                target.SetActive(true);
                if (m_allMaterials == null)
                {
                    m_allMaterials = new List<(Material, float, int)>();
                }
                else
                {
                    m_allMaterials.Clear();
                }

                foreach(var renderer in target.GetComponentsInChildren<Renderer>())
                {
                    var originals = renderer.materials;
                    for (int i = 0; i < originals.Length; i++)
                    {
                        var material = originals[i];
                        var alpha = 1f;
                        if(!material.HasProperty("_Mode") || material.GetInt("_Mode") == 0)
                        {
                            MakeTransparent(material);
                            m_allMaterials.Add((material, alpha, 0));
                            ChangeOpacity(material, (m_show.TargetValue ? 0 : 1) * alpha);
                        }
                        else if(material.HasProperty("_Color") || material.HasProperty("_color"))
                        {
                            alpha = material.color.a;
                            m_allMaterials.Add((material, alpha, material.HasProperty("_Mode") ? material.GetInt("_Mode") : -1));
                            ChangeOpacity(material, (m_show.TargetValue ? 0 : 1) * alpha);
                        }
                    }
                }
                m_show.Start(m_prevValue);

            }
        }

        public override bool Execute(float dt)
        {
            var target = m_target.Value;

            if (!m_show.IsAnimated) {
                target.SetActive(m_show.TargetValue);
                Progress = 1;
                return true;
            }

            m_show.Update(dt);
            
            if (m_show.HasFinished)
            {
                Progress = 1;
                RestoreMaterials();
                target.SetActive(m_show.CurrentTargetValue);
                return true;
            }

            float alpha = Mathf.Clamp01(m_show.Interpolation);
            Progress = m_show.Progress;
            ChangeOpacity(alpha);
            return false;
        }

        private void RestoreMaterials()
        {
            foreach (var (mat, a, mode) in m_allMaterials)
            {
                ChangeOpacity(mat, a);
                if (mode == 0)
                {
                    MakeOpaque(mat);
                }
            }
        }

        public override void OnContextExit(ExecutionFlow flow)
        {
            if (RevertOnExit)
            {
                if (m_show.IsAnimated)
                {
                    m_target.Value.SetActive(true);
                    foreach (var (mat, alpha, mode) in m_allMaterials)
                    {
                        if (mode == 0)
                        {
                            MakeTransparent(mat);
                        }
                        //ChangeOpacity(mat, m_prevValue ? 0 : 1);
                    }
                    m_show.AutoAnimate(m_prevValue, ReverseCallback);
                    return;
                }
                else
                {
                    m_target.Value.SetActive(m_prevValue);
                }
            }
        }

        public override void FastForward()
        {
            base.FastForward();
            Progress = 1;
            if (!m_show.IsAnimated)
            {
                m_target.Value.SetActive(m_show.TargetValue);
                return;
            }

            RestoreMaterials();
            m_target.Value.SetActive(m_show.TargetValue);
        }

        public override void OnStop()
        {
            base.OnStop();
            RestoreMaterials();
            m_target.Value.SetActive(m_prevValue);
        }

        public override string GetDescription()
        {
            string action = m_show.TargetValue ? "Enable" : "Disable";
            return $"{action} {m_target} ";
        }

        private void ReverseCallback(bool value)
        {
            if (m_show.HasFinished)
            {
                Progress = 1;
                RestoreMaterials();
                m_target.Value.SetActive(value);
                return;
            }

            float alpha = Mathf.Clamp01(m_show.Interpolation);
            Progress = m_show.Progress;
            ChangeOpacity(alpha);
        }

        private static void ChangeOpacity(Material material, float alpha)
        {
            if (material.HasProperty("_Color") || material.HasProperty("_BaseColor"))
            {
                var color = material.color;
                color.a = alpha;
                material.color = color;
            }
        }

        private void ChangeOpacity(float alpha)
        {
            foreach (var (mat, a, _) in m_allMaterials)
            {
                ChangeOpacity(mat, alpha * a);
            }
        }

        private static void MakeTransparent(Material material)
        {
            if (!material.HasProperty("_Mode")) { return; }
            if (material.GetInt("_Mode") == 0 || material.GetInt("_Mode") == 1)
            {
                material.SetInt("_Mode", 2);

                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }
        }

        private static void MakeOpaque(Material material)
        {
            if (material.HasProperty("_Mode") && material.GetInt("_Mode") == 2)
            {
                material.SetInt("_Mode", 0);

                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = -1;
            }
        }

        private class MaterialPair
        {
            public Material original;
            public Material clone;

            public MaterialPair(Material material)
            {
                original = material;
                clone = new Material(material);
            }
        }
    }
}