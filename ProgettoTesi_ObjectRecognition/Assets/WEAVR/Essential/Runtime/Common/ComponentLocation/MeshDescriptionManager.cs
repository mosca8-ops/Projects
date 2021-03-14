using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TXT.WEAVR.Common;
using TXT.WEAVR.UI;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Component Location/Mesh Description Manager")]
    public class MeshDescriptionManager : MonoBehaviour
    {
        public interface IPointer
        {
            bool Active { get; }
            bool TriggerDown { get; }
            Ray GetRay();
            void SetRayDistance(float maxRayDistance);
        }

        public enum RevealType { OnHover, OnClick }


        private static MeshDescriptionManager s_instance;
        public static MeshDescriptionManager Instance => s_instance;

        private HashSet<MeshDescriptor> m_descriptors = new HashSet<MeshDescriptor>();
        private Dictionary<IPointer, MeshDescriptor> m_currentDescriptors = new Dictionary<IPointer, MeshDescriptor>();
        private HashSet<IPointer> m_pointers = new HashSet<IPointer>();

        public GameObject[] renderersRoots;
        public RevealType revealType = RevealType.OnHover;
        [SerializeField]
        private LayerMask m_layer;
        [SerializeField]
        [Range(1, 30)]
        private float m_maxRayDistance = 20;

        [Header("Actions")]
        [SerializeField]
        private bool m_showBillboard = true;
        [SerializeField]
        private bool m_outline = true;
        [SerializeField]
        private Color m_outlineColor = Color.green;
        [SerializeField]
        private bool m_playAudio = false;
        [SerializeField]
        private AudioSource m_audioSource;
        [SerializeField]
        private bool m_enableLOD = false;
        [SerializeField]
        private MegaLODGroup m_megaLOD;

        private void Awake()
        {
            if (s_instance && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }
            s_instance = this;
        }

        private void Start()
        {
            LoadDataFromJson();
            if (!m_audioSource)
            {
                m_audioSource = GetComponentInChildren<AudioSource>(true);
            }
            foreach (var meshDescriptor in m_descriptors.ToArray())
            {
                if (string.IsNullOrEmpty(meshDescriptor.description))
                {
                    Unregister(meshDescriptor);
                }
            }
            if (m_megaLOD)
            {
                m_megaLOD.SetVisibility(m_enableLOD);
            }
        }

        [ContextMenu("Save Descriptions")]
        private void SaveCurrentDescriptions()
        {
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }
            string filepath = Path.Combine(Application.streamingAssetsPath, "mesh_descriptions.json");
            var descriptors = m_descriptors != null && m_descriptors.Count > 0 ? m_descriptors : renderersRoots.SelectMany(r => r.GetComponentsInChildren<MeshDescriptor>(true));
            File.WriteAllText(filepath, JsonUtility.ToJson(new MeshDescriptions()
            {
                pointerLength = m_maxRayDistance,
                playAudio = m_playAudio,
                enableLOD = m_enableLOD,
                outlineColor = m_outline ? "#" + ColorUtility.ToHtmlStringRGBA(m_outlineColor).ToLower() : string.Empty,
                groups = descriptors.Select(m => m.Group).Distinct().Select(g => g.CreateDescriptionGroup()).ToArray()
            }, true));
        }

        [ContextMenu("Load From Json")]
        private void LoadDataFromJson()
        {
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }
            string filepath = Path.Combine(Application.streamingAssetsPath, "mesh_descriptions.json");
            if (File.Exists(filepath))
            {
                MeshDescriptions descriptions = JsonUtility.FromJson<MeshDescriptions>(File.ReadAllText(filepath));
                if (descriptions != null && descriptions.groups.Length > 0)
                {
                    Debug.Log($"Loading mesh descriptions [{descriptions.groups.Length}] from file");
                    m_maxRayDistance = Mathf.Clamp(descriptions.pointerLength, 1, 30);
                    m_playAudio = descriptions.playAudio;
                    m_outline = ColorUtility.TryParseHtmlString(descriptions.outlineColor, out m_outlineColor);
                    m_enableLOD = descriptions.enableLOD;
                    Dictionary<string, MeshFilter> meshFilters = new Dictionary<string, MeshFilter>();
                    Dictionary<string, MeshDescriptor> meshDescriptors = new Dictionary<string, MeshDescriptor>();
                    foreach (var root in renderersRoots)
                    {
                        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
                        {
                            if (mf.sharedMesh)
                            {
                                meshFilters[mf.sharedMesh.name.ToLower()] = mf;
                            }
                        }
                        foreach (var md in root.GetComponentsInChildren<MeshDescriptor>(true))
                        {
                            if (md)
                            {
                                meshDescriptors[md.gameObject.name.ToLower()] = md;
                            }
                        }
                    }


                    MeshFilter filter;
                    MeshDescriptor meshDescriptor;
                    for (int i = 0; i < descriptions.groups.Length; i++)
                    {
                        var group = descriptions.groups[i];

                        for (int j = 0; j < group.meshes.Length; j++)
                        {
                            var descr = group.meshes[j];
                            if (meshDescriptors.TryGetValue(descr.mesh.ToLower(), out meshDescriptor))
                            {
                                meshDescriptor.Apply(descr, false);
                                Register(meshDescriptor);
                            }
                            else if (meshFilters.TryGetValue(descr.mesh.ToLower(), out filter))
                            {
                                meshDescriptor = filter.GetComponentInParent<MeshDescriptor>();
                                if (!meshDescriptor || !meshDescriptor.Target || meshDescriptor.Target.gameObject != filter.gameObject)
                                {
                                    meshDescriptor = filter.gameObject.AddComponent<MeshDescriptor>();
                                }
                                meshDescriptor.Apply(descr, true);
                                Register(meshDescriptor);
                            }
                        }
                    }
                }

                foreach (var pointer in m_pointers)
                {
                    pointer.SetRayDistance(m_maxRayDistance);
                }
            }
        }

        private void OnDisable()
        {
            foreach (var pair in m_currentDescriptors)
            {
                if (pair.Value)
                {
                    //BillboardManager.Instance.HideBillboardOn(pair.Value.Target.gameObject);
                    ResetAction(pair.Value);
                }
            }
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }
        }

        private void Update()
        {
            foreach (var pointer in m_pointers)
            {
                var currentDescriptor = m_currentDescriptors[pointer];
                if (!pointer.Active)
                {
                    if (currentDescriptor)
                    {
                        ResetAction(currentDescriptor);
                        m_currentDescriptors[pointer] = null;
                    }
                    continue;
                }

                RaycastHit hitInfo;
                if (Physics.Raycast(pointer.GetRay(), out hitInfo, m_maxRayDistance, m_layer))
                {
                    var descriptor = hitInfo.transform.GetComponentInParent<MeshDescriptor>();
                    if (descriptor != currentDescriptor)
                    {
                        if (currentDescriptor)
                        {
                            ResetAction(currentDescriptor);
                            m_currentDescriptors[pointer] = null;
                        }
                        m_currentDescriptors[pointer] = descriptor;

                        if (revealType == RevealType.OnHover)
                        {
                            ApplyAction(descriptor);
                        }
                    }

                    if (revealType == RevealType.OnClick && pointer.TriggerDown)
                    {
                        ApplyAction(descriptor);
                    }
                }
            }
        }

        private void ResetAction(MeshDescriptor descriptor)
        {
            BillboardManager.Instance.HideBillboardOn(descriptor.Target.gameObject);
            Outliner.RemoveOutline(descriptor.Target.gameObject);
        }

        private void ApplyAction(MeshDescriptor descriptor)
        {
            if (m_showBillboard && descriptor.Target)
            {
                BillboardManager.Instance.ShowBillboardOn(descriptor.Target.gameObject, descriptor.description);
            }
            if (m_outline)
            {
                Outliner.Outline(descriptor.Target.gameObject, m_outlineColor);
            }
            if (m_playAudio && m_audioSource)
            {
                m_audioSource.Play();
            }
        }

        public void Register(IPointer pointer)
        {
            if (!m_currentDescriptors.ContainsKey(pointer))
            {
                m_currentDescriptors[pointer] = null;
            }
            m_pointers.Add(pointer);
            pointer.SetRayDistance(m_maxRayDistance);
        }

        public void Unregister(IPointer pointer)
        {
            m_currentDescriptors.Remove(pointer);
            m_pointers.Remove(pointer);
        }

        public void Register(MeshDescriptor descriptor)
        {
            if (string.IsNullOrEmpty(descriptor.description))
            {
                descriptor.enabled = false;
            }
            else
            {
                m_descriptors.Add(descriptor);
                descriptor.enabled = true;
            }
            //descriptor.Target.gameObject.layer = LayerMask.NameToLayer(m_layer.ToString());
        }

        public void Unregister(MeshDescriptor descriptor)
        {
            m_descriptors.Remove(descriptor);
            descriptor.enabled = false;
        }

        [Serializable]
        private class MeshDescriptions
        {
            public float pointerLength;
            public bool playAudio;
            public bool enableLOD;
            public string outlineColor;
            public MeshDescriptorGroup.DescriptionGroup[] groups;
        }
    }
}
