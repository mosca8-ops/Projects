using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TXT.WEAVR.LayoutSystem
{

    [ExecuteInEditMode]
    [AddComponentMenu("WEAVR/Layout System/Layout Container")]
    public class LayoutContainer : MonoBehaviour
    {
        #region [  STATIC PART  ]

        private static List<LayoutContainer> s_containers = new List<LayoutContainer>();
        public static IReadOnlyList<LayoutContainer> ContainersInScene => s_containers;

        private static void RegisterContainer(LayoutContainer container)
        {
            if(s_containers == null)
            {
                s_containers = new List<LayoutContainer>();
            }
            if (!s_containers.Contains(container))
            {
                s_containers.Add(container);
            }
        }

        private static void UnregisterContainer(LayoutContainer container)
        {
            s_containers.Remove(container);
        }

        public static void RefreshAvailableContainers()
        {
            Canvas.ForceUpdateCanvases();
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach(var container in root.GetComponentsInChildren<LayoutContainer>(true))
                {
                    if (container.enabled)
                    {
                        RegisterContainer(container);
                        container.RefreshCanvas();
                    }
                }
            }
        }

        #endregion
        [SerializeField]
        [Draggable]
        [Button(nameof(OnValidate), "Fix")]
        [ShowAsReadOnly]
        protected Canvas m_canvas;
        [Header("Layout Preview")]
        [Draggable]
        public Texture2D layoutPreview;
        [SerializeField]
        protected GUIStyle m_style;
        [Space]
        [Draggable]
        public Texture2D defaultElementPreview;
        
        public bool showElementNames = true;
        [Draggable]
        public Texture2D labelPreview;
        [Draggable]
        public Texture2D imagePreview;
        [Draggable]
        public Texture2D buttonPreview;
        [Draggable]
        public Texture2D inputFieldPreview;

        [Space]
        [SerializeField]
        [Button(nameof(UpdateList), "Refresh")]
        [ShowAsReadOnly]
        private int m_totalItems;
        
        [SerializeField]
        [Draggable]
        //[ReorderableList]
        private List<BaseLayoutItem> m_layoutItems;

        private RectTransform m_rectTransform;

        private Texture2D m_fallbackElementPreview;

        public GUIStyle Style
        {
            get
            {
                if(m_style == null)
                {
                    m_style = new GUIStyle();
                }
                return m_style;
            }
        }

        public Texture2D ElementPreviewTexture
        {
            get
            {
                if(defaultElementPreview == null)
                {
                    if(m_fallbackElementPreview == null)
                    {
                        m_fallbackElementPreview = new GUIStyle("Box").normal.background;
                    }
                    return m_fallbackElementPreview;
                }
                return defaultElementPreview;
            }
        }

        public RectTransform RectTransform
        {
            get
            {
                if (m_rectTransform == null)
                {
                    m_rectTransform = transform as RectTransform;
                }
                return m_rectTransform;
            }
        }

        public Canvas Canvas => m_canvas;

        public IReadOnlyList<BaseLayoutItem> LayoutItems => m_layoutItems;

        private GUIContent m_dummyContent;
        private GUIContent DummyContent
        {
            get
            {
                if(m_dummyContent == null)
                {
                    m_dummyContent = new GUIContent();
                }
                return m_dummyContent;
            }
        }

        public bool IsCurrentlyActive {
            get { return gameObject.activeInHierarchy; }
            set
            {
                if (value)
                {
                    foreach(var container in LayoutContainer.ContainersInScene)
                    {
                        if(container != this && container != null)
                        {
                            container.gameObject.SetActive(false);
                        }
                    }
                    //ClearAllItems();
                    gameObject.SetActive(true);
                    if (!Application.isPlaying)
                    {
                        Canvas.ForceUpdateCanvases();
                    }
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }

        public void ResetToDefaults()
        {
            foreach(var elem in m_layoutItems)
            {
                elem.ResetToDefaults();
            }
            Canvas.ForceUpdateCanvases();
        }

        public void Refresh()
        {
            if (!gameObject.activeInHierarchy)
            {
                var wasActive = gameObject.activeSelf;
                gameObject.SetActive(true);
                if (!Application.isPlaying)
                {
                    Canvas.ForceUpdateCanvases();
                }
                gameObject.SetActive(wasActive);
            }
        }

        private void Reset()
        {
            m_canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
            UpdateList();
        }

        private void OnValidate()
        {
            if(m_canvas == null)
            {
                m_canvas = GetComponent<Canvas>();
                m_canvas = m_canvas == null ? GetComponentInParent<Canvas>() : m_canvas;
            }
        }

        public void UpdateList()
        {
            if(m_layoutItems == null)
            {
                m_layoutItems = new List<BaseLayoutItem>(GetComponentsInChildren<BaseLayoutItem>(true));
            }
            else
            {
                foreach(var item in m_layoutItems)
                {
                    item.container = null;
                }
                m_layoutItems.Clear();
                m_layoutItems.AddRange(GetComponentsInChildren<BaseLayoutItem>(true));
            }
            foreach (var item in m_layoutItems)
            {
                item.container = this;
            }

            m_totalItems = m_layoutItems.Count;
        }

        public void ClearAllItems()
        {
            foreach(var item in m_layoutItems)
            {
                item.Clear();
            }
        }

        // Use this for initialization
        void Start()
        {
            RegisterContainer(this);
        }

        private void OnEnable()
        {
            m_dummyContent = new GUIContent();
            RegisterContainer(this);
        }

        private void OnDestroy()
        {
            UnregisterContainer(this);
        }

        private void OnDisable()
        {
            ResetToDefaults();
        }

        private void RefreshCanvas()
        {
            if (m_canvas != null)
            {
                bool wasEnabled = m_canvas.gameObject.activeInHierarchy;
                m_canvas.gameObject.SetActive(false);
                m_canvas.gameObject.SetActive(true);
                //LayoutRebuilder.ForceRebuildLayoutImmediate(canvas.transform as RectTransform);
                m_canvas.gameObject.SetActive(wasEnabled);
            }
        }

        private IEnumerator UpdateCanvas(Canvas canvas, bool wasEnabled)
        {
            Debug.Log("Coroutine Started!!");
            yield return new WaitForSeconds(5f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(canvas.transform as RectTransform);
            canvas.gameObject.SetActive(wasEnabled);
            Debug.Log("Coroutine Ended!!");
        }

        private IEnumerator RefreshCanvasCoroutine()
        {
            Debug.Log("Coroutine Started!!");
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                bool wasEnabled = canvas.gameObject.activeInHierarchy;
                canvas.gameObject.SetActive(false);
                canvas.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.1f);
                LayoutRebuilder.ForceRebuildLayoutImmediate(canvas.transform as RectTransform);
                canvas.gameObject.SetActive(wasEnabled);
            }
        }

        public void OnGUIDraw(Rect pos)
        {
            if(layoutPreview != null)
            {
                GUI.DrawTexture(pos, layoutPreview);
                return;
            }

            Vector2 scalingFactor = new Vector2(pos.size.x / RectTransform.rect.size.x, pos.size.y / RectTransform.rect.size.y);
            if (!m_canvas.gameObject.activeInHierarchy)
            {
                //scalingFactor = new Vector2(pos.size.x / RectTransform.rect.size.x, pos.size.y / RectTransform.rect.size.y);
                scalingFactor *= 2;
            }
            Vector2 offset = pos.size * 0.5f;

            foreach (var item in m_layoutItems)
            {
                var rect = item.RectTransform.rect;
                var position = new Vector2(item.RectTransform.position.x - transform.position.x, -item.RectTransform.position.y + transform.position.y);
                rect.size = Vector2.Scale(rect.size, scalingFactor);
                rect.center = Vector2.Scale(position, scalingFactor) + pos.position + offset;

                DrawElement(item, rect);
            }
        }

        private void DrawElement(BaseLayoutItem item, Rect rect)
        {
            if(item is LayoutLabel)
            {
                DrawElementInternally(rect, item.layoutPreviewImage != null ? item.layoutPreviewImage : labelPreview, item.name, "Label");
            }
            else if(item is LayoutImage)
            {
                DrawElementInternally(rect, item.layoutPreviewImage != null ? item.layoutPreviewImage : imagePreview, item.name, "Image");
            }
            else if(item is LayoutButton)
            {
                DrawElementInternally(rect, item.layoutPreviewImage != null ? item.layoutPreviewImage : buttonPreview, item.name, "Button");
            }
            else if(item is LayoutInputField)
            {
                DrawElementInternally(rect, item.layoutPreviewImage != null ? item.layoutPreviewImage : inputFieldPreview, item.name, "InputField");
            }
            else
            {
                DrawElementInternally(rect, item.layoutPreviewImage, item.name, "Unknown");
            }
        }

        private void DrawElementInternally(Rect rect, Texture2D texture, string text, string type)
        {
            if (texture != null)
            {
                GUI.DrawTexture(rect, texture);
                if (showElementNames)
                {
                    var center = rect.center;
                    DummyContent.text = text;
                    rect.size = Style.CalcSize(m_dummyContent);
                    rect.center = center;
                    GUI.Label(rect, m_dummyContent, Style);
                }
            }
            else
            {
                GUI.DrawTexture(rect, ElementPreviewTexture);
                var center = rect.center;
                DummyContent.text = $"{text}:{type}";
                rect.size = Style.CalcSize(m_dummyContent);
                rect.center = center;
                GUI.Label(rect, m_dummyContent, Style);
            }
        }
    }
}
