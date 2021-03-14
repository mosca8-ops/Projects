using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TXT.WEAVR.Common
{

    [SelectionBase]
    [AddComponentMenu("WEAVR/Components/Screen Feed")]
    public class ScreenFeed : MonoBehaviour
    {
        [SerializeField]
        [Draggable]
        private Material m_screenMaterial;
        [SerializeField]
        [HiddenBy(nameof(m_graphic), hiddenWhenTrue: true)]
        [Button(nameof(TryGetRenderer), "Find", nameof(FindRendererIsActive))]
        private Renderer m_renderer;
        [SerializeField]
        [HiddenBy(nameof(m_renderer), hiddenWhenTrue: true)]
        [Button(nameof(TryGetGraphic), "Find", nameof(FindGraphicIsActive))]
        private Graphic m_graphic;
        [SerializeField]
        [Tooltip("If true, the turned off screen will turn on when changing screen")]
        private bool m_autoSwitchOn = true;
        [SerializeField]
        [Tooltip("If true, the screen can change its texture even when off")]
        private bool m_allowChangeWhenOff = false;
        [RangeFrom(-1, nameof(m_screens))]
        [SerializeField]
        private int m_initialScreen = -1;
        [SerializeField]
        private Color m_switchOffColor = Color.black;
        private bool m_isOn;



        [SerializeField]
        [Draggable]
        private Texture[] m_screens;

        [Space]
        [SerializeField]
        private Events events;

        public UnityEvent OnScreenSwitchedOn => events.onScreenSwitchedOn;
        public UnityEvent OnScreenSwitchedOff => events.onScreenSwitchedOff;
        public UnityEventBoolean OnScreenOnOff => events.onScreenOnOff;
        public UnityEventInt OnScreenIndexChanged => events.onScreenIndexChanged;
        public UnityEventTexture OnScreenChanged => events.onScreenChanged;

        public bool IsOn {
            get { return m_isOn; }
            set {
                if (m_isOn != value)
                {
                    if (value)
                    {
                        m_isOn = value;
                        CurrentScreenIndex = m_lastScreenIndex;
                        OnScreenSwitchedOn.Invoke();
                    }
                    else
                    {
                        CurrentScreenIndex = -1;
                        m_isOn = value;
                        OnScreenSwitchedOff.Invoke();
                    }
                    OnScreenOnOff.Invoke(value);
                }
            }
        }

        private int m_currentScreenIndex;
        private int m_lastScreenIndex;

        public int CurrentScreenIndex {
            get { return m_currentScreenIndex; }
            set {
                if (m_currentScreenIndex != value && value < m_screens.Length)
                {
                    if (value < 0)
                    {
                        m_currentScreenIndex = -1;
                        SetMaterialTexture(m_screenMaterial, null);
                        SetMaterialColor(m_screenMaterial, m_switchOffColor);
                    }
                    else if (IsOn || m_autoSwitchOn)
                    {
                        m_isOn = true;
                        m_currentScreenIndex = value;
                        m_lastScreenIndex = m_currentScreenIndex;
                        SetMaterialTexture(m_screenMaterial, m_screens[value]);
                        SetMaterialColor(m_screenMaterial, Color.white);

                        OnScreenIndexChanged.Invoke(value);
                        OnScreenChanged.Invoke(m_screens[value]);
                    }
                    else if (m_allowChangeWhenOff)
                    {
                        m_currentScreenIndex = value;
                        m_lastScreenIndex = m_currentScreenIndex;
                        SetMaterialTexture(m_screenMaterial, m_screens[value]);
                        SetMaterialColor(m_screenMaterial, Color.white);
                    }
                }
            }
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
            else
            {
                material.color = color;
            }
        }

        private static void SetMaterialTexture(Material material, Texture texture)
        {
            material.mainTexture = texture;
            material.SetTexture("_BaseMap", texture);
        }

        public Texture CurrentScreen => m_screenMaterial.mainTexture;

        private void OnValidate()
        {
            if (m_renderer)
            {
                if (!m_screenMaterial)
                {
                    m_screenMaterial = RendererMaterial;
                }
                else if (m_screenMaterial != RendererMaterial)
                {
                    RendererMaterial = m_screenMaterial;
                }
                m_graphic = null;
            }
            else if (m_graphic)
            {
                m_renderer = null;
                if (!m_screenMaterial)
                {
                    m_screenMaterial = UIGraphicMaterial;
                }
                else
                {
                    UIGraphicMaterial = m_screenMaterial;
                }
            }
            if (m_screenMaterial && !Application.isPlaying)
            {
                if (m_initialScreen < 0)
                {
                    SetMaterialTexture(m_screenMaterial, null);
                    SetMaterialColor(m_screenMaterial, m_switchOffColor);
                }
                else if(m_initialScreen < m_screens.Length)
                {
                    SetMaterialTexture(m_screenMaterial, m_screens[m_initialScreen]);
                    SetMaterialColor(m_screenMaterial, Color.white);
                }
            }
        }

        public Material RendererMaterial
        {
            get => m_renderer ? (Application.isPlaying ? m_renderer.material : m_renderer.sharedMaterial) : null;
            set
            {
                if (m_renderer)
                {
                    if (Application.isPlaying) { m_renderer.material = value; }
                    else { m_renderer.sharedMaterial = value; }
                }
            }
        }

        public Material UIGraphicMaterial
        {
            get => m_graphic ? m_graphic.material : null;
            set
            {
                if (m_graphic)
                {
                    m_graphic.material = value;
                }
            }
        }

        private void TryGetRenderer()
        {
            if (!m_renderer)
            {
                m_renderer = GetComponentInChildren<Renderer>(true);
            }
        }

        private bool FindRendererIsActive() => !m_renderer;

        private void TryGetGraphic()
        {
            if (!m_graphic)
            {
                m_graphic = GetComponentInChildren<RawImage>();
            }
            if (!m_graphic)
            {
                m_graphic = GetComponentInChildren<Image>();
            }
            if (!m_graphic)
            {
                m_graphic = GetComponentInChildren<Graphic>();
            }
        }

        private bool FindGraphicIsActive() => !m_graphic;

        public void SetScreen(int index)
        {
            CurrentScreenIndex = index;
        }

        public void SetScreen(Texture screenTexture)
        {
            if (screenTexture == null)
            {
                CurrentScreenIndex = -1;
                return;
            }
            for (int i = 0; i < m_screens.Length; i++)
            {
                if (screenTexture == m_screens[i])
                {
                    CurrentScreenIndex = i;
                    return;
                }
            }
            if (IsOn || m_autoSwitchOn)
            {
                m_currentScreenIndex = -1;
                SetMaterialTexture(m_screenMaterial, screenTexture);
                SetMaterialColor(m_screenMaterial, Color.white);
            }
        }

        public void SwitchOff()
        {
            IsOn = false;
        }

        public void SwitchOn()
        {
            IsOn = true;
        }

        // Use this for initialization
        void Start()
        {
            if (!m_screenMaterial)
            {
                m_screenMaterial = RendererMaterial ? RendererMaterial : UIGraphicMaterial ? UIGraphicMaterial : GetComponentInChildren<Renderer>().material;
            }
            m_currentScreenIndex = -1;
            if (m_initialScreen >= 0 && m_autoSwitchOn)
            {
                SwitchOn();
            }
            CurrentScreenIndex = m_initialScreen;
        }

        [Serializable]
        private class Events
        {
            public UnityEvent onScreenSwitchedOn;
            public UnityEvent onScreenSwitchedOff;
            public UnityEventBoolean onScreenOnOff;
            public UnityEventInt onScreenIndexChanged;
            public UnityEventTexture onScreenChanged;
        }
    }
}