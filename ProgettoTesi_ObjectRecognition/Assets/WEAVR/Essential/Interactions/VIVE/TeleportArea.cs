using UnityEngine;
using TXT.WEAVR.Interaction;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TXT.WEAVR.Interaction
{
    [AddComponentMenu("WEAVR/VR/Interactions/Teleport Area")]
    public class TeleportArea
#if WEAVR_VR
        : Valve.VR.InteractionSystem.TeleportMarkerBase
#else
        : MonoBehaviour
#endif
    {
        //Public properties
        public Bounds meshBounds { get; private set; }

        public string areaName;
        public GameObject areaDescription;
        public bool lookAtPlayer = true;

        //Private data
        private Text m_textComponent;
        private float m_textVisibilityThreshold;

        private MeshRenderer areaMesh;
        private int tintColorId = 0;
        private Color visibleTintColor = Color.clear;
        private Color highlightedTintColor = Color.clear;
        private Color lockedTintColor = Color.clear;
        private bool highlighted = false;

        private Text TextComponent {
            get {
                if (!m_textComponent && areaDescription)
                {
                    m_textComponent = GetComponentInChildren<Text>(true);
                }
                return m_textComponent;
            }
        }

#if WEAVR_VR

        //-------------------------------------------------
        public void Awake()
        {
            areaMesh = GetComponent<MeshRenderer>();

            tintColorId = Shader.PropertyToID("_TintColor");

            CalculateBounds();
        }


        //-------------------------------------------------
        public void Start()
        {
            if (Valve.VR.InteractionSystem.Teleport.instance == null) { return; }
            visibleTintColor = Valve.VR.InteractionSystem.Teleport.instance.areaVisibleMaterial.GetColor(tintColorId);
            highlightedTintColor = Valve.VR.InteractionSystem.Teleport.instance.areaHighlightedMaterial.GetColor(tintColorId);
            lockedTintColor = Valve.VR.InteractionSystem.Teleport.instance.areaLockedMaterial.GetColor(tintColorId);
        }


        //-------------------------------------------------
        public override bool ShouldActivate(Vector3 playerPosition)
        {
            if (areaDescription)
            {
                areaDescription.SetActive(Vector3.Distance(playerPosition, areaDescription.transform.position) > m_textVisibilityThreshold);
                if (lookAtPlayer)
                {
                    areaDescription.transform.LookAt(playerPosition);
                }
            }
            return true;
        }


        //-------------------------------------------------
        public override bool ShouldMovePlayer()
        {
            return true;
        }


        //-------------------------------------------------
        public override void Highlight(bool highlight)
        {
            if (!locked)
            {
                highlighted = highlight;

                if (highlight)
                {
                    areaMesh.material = Valve.VR.InteractionSystem.Teleport.instance.areaHighlightedMaterial;
                }
                else
                {
                    areaMesh.material = Valve.VR.InteractionSystem.Teleport.instance.areaVisibleMaterial;
                }

                UpdateText();
            }
        }

        private void UpdateText()
        {
            if (TextComponent)
            {
                m_textComponent.text = areaName;
                m_textComponent.color = GetTintColor();
            }
        }


        //-------------------------------------------------
        public override void SetAlpha(float tintAlpha, float alphaPercent)
        {
            Color tintedColor = GetTintColor();
            tintedColor.a *= alphaPercent;
            areaMesh.material.SetColor(tintColorId, tintedColor);

            UpdateText();
        }


        //-------------------------------------------------
        public override void UpdateVisuals()
        {
            if (locked)
            {
                areaMesh.material = Valve.VR.InteractionSystem.Teleport.instance.areaLockedMaterial;
            }
            else
            {
                areaMesh.material = Valve.VR.InteractionSystem.Teleport.instance.areaVisibleMaterial;
            }

            UpdateText();
        }


        //-------------------------------------------------
        public void UpdateVisualsInEditor()
        {
            areaMesh = GetComponent<MeshRenderer>();

            if (locked)
            {
                areaMesh.sharedMaterial = Valve.VR.InteractionSystem.Teleport.instance.areaLockedMaterial;
            }
            else
            {
                areaMesh.sharedMaterial = Valve.VR.InteractionSystem.Teleport.instance.areaVisibleMaterial;
            }

            UpdateText();
        }


        //-------------------------------------------------
        private bool CalculateBounds()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                return false;
            }

            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null)
            {
                return false;
            }

            meshBounds = mesh.bounds;
            m_textVisibilityThreshold = meshBounds.extents.magnitude;
            return true;
        }


        //-------------------------------------------------
        private Color GetTintColor()
        {
            if (locked)
            {
                return lockedTintColor;
            }
            else
            {
                if (highlighted)
                {
                    return highlightedTintColor;
                }
                else
                {
                    return visibleTintColor;
                }
            }
        }

#endif // UNITY_STANDALONE_WIN
    }
}

#if WEAVR_VR
#if UNITY_EDITOR
//-------------------------------------------------------------------------
[CustomEditor(typeof(TeleportArea))]
public class TeleportAreaEditor : Editor
{
    //-------------------------------------------------
    void OnEnable()
    {
        if (Selection.activeTransform != null)
        {
            TeleportArea teleportArea = Selection.activeTransform.GetComponent<TeleportArea>();
            if (teleportArea != null)
            {
                teleportArea.UpdateVisualsInEditor();
            }
        }
    }


    //-------------------------------------------------
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (Selection.activeTransform != null)
        {
            TeleportArea teleportArea = Selection.activeTransform.GetComponent<TeleportArea>();
            if (GUI.changed && teleportArea != null)
            {
                teleportArea.UpdateVisualsInEditor();
            }
        }
    }
}
#endif // UNITY_EDITOR

#endif // UNITY_STANDALONE_WIN
