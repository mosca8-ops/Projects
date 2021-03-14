namespace TXT.WEAVR.UI
{
    using TXT.WEAVR.Common;
    using UnityEngine;

    [ExecuteInEditMode]
    [AddComponentMenu("")]
    [RequireComponent(typeof(LineRenderer))]
    public class LineRenderingPopup3D : Popup3D
    {
        [Space]
        [SerializeField]
        protected bool m_dynamicLineScaling;
        [SerializeField]
        protected float m_linePixelWidth = 2;
        protected LineRenderer m_lineRenderer;

        public bool DynamicLineScaling {
            get {
                return m_dynamicLineScaling;
            }
            set {
                if (m_dynamicLineScaling != value)
                {
                    m_dynamicLineScaling = value;
                }
            }
        }

        [Tooltip("Trace the line from the origin to the closest point on canvas")]
        [SerializeField]
        protected bool m_closestPoint = true;
        [SerializeField]
        [Draggable]
        protected Transform[] m_canvasTargetPoints;

        [SerializeField]
        [Tooltip("The sprite sample to apply on target")]
        [Draggable]
        protected Sprite m_targetPointSprite;

        [SerializeField]
        [Tooltip("The size in pixels of the target sprite")]
        protected Vector2 m_targetPointSize = new Vector2(16, 16);

        [SerializeField]
        [Draggable]
        [CanBeGenerated("TargetPoint", Relationship.Child)]
        protected SpriteRenderer m_targetPoint;

        public override void Hide()
        {
            base.Hide();
            m_lineRenderer.enabled = false;
            if (m_targetPoint != null)
            {
                m_targetPoint.gameObject.SetActive(false);
            }
        }

        protected override void InternalShowFixed(Transform point, Transform context)
        {
            base.InternalShowFixed(point, context);
            UpdateLineRenderingPoints();

            ApplyLineWidth();
            InternalUpdate(Time.deltaTime);
        }

        protected override void InternalShow(Vector3 point, Transform context)
        {
            base.InternalShow(point, context);
            UpdateLineRenderingPointsLite();

            ApplyLineWidth();
            InternalUpdate(Time.deltaTime);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (m_targetPointSprite != null && m_targetPoint != null && m_targetPoint.sprite != m_targetPointSprite)
            {
                m_targetPoint.sprite = m_targetPointSprite;
                m_targetPoint.drawMode = SpriteDrawMode.Sliced;
                m_targetPoint.size = Vector2.one;
                m_targetPoint.transform.localScale = Vector3.one;
                if (m_lineRenderer != null && m_lineRenderer.sharedMaterial != null)
                {
                    m_targetPoint.sharedMaterial = m_lineRenderer.sharedMaterial;
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_lineRenderer = GetComponent<LineRenderer>();
            m_lineRenderer.positionCount = m_canvasTargetPoints.Length + 1;

            m_lineRenderer.useWorldSpace = true;
            if (!Application.isEditor && m_targetPointSprite != null && m_targetPoint == null)
            {
                GameObject targetPoint = new GameObject("TargetPoint");
                targetPoint.transform.SetParent(m_mainCanvas.transform, false);
                m_targetPoint = targetPoint.AddComponent<SpriteRenderer>();
                m_targetPoint.sprite = m_targetPointSprite;
            }
        }


        protected override void UpdateOccupancyVolume()
        {
            base.UpdateOccupancyVolume();
            if (m_mainCanvas == null) return;

            UpdateLineRenderingPoints();
        }

        private void UpdateLineRenderingPoints()
        {
            if (m_lineRenderer == null) { return; }

            if (m_closestPoint && m_canvasTargetPoints.Length > 0)
            {
                m_lineRenderer.positionCount = 2;
                var closestPoint = m_canvasTargetPoints[0].position;
                float distance = Vector3.Distance(m_showPoint, m_canvasTargetPoints[0].position);
                for (int i = 1; i < m_canvasTargetPoints.Length; i++)
                {
                    float newDistance = Vector3.Distance(m_showPoint, m_canvasTargetPoints[i].position);
                    if (newDistance < distance)
                    {
                        distance = newDistance;
                        closestPoint = m_canvasTargetPoints[i].position;
                    }
                }
                m_lineRenderer.SetPosition(1, closestPoint);
            }
            else
            {
                m_lineRenderer.positionCount = m_canvasTargetPoints.Length + 1;
                if (m_canvasTargetPoints.Length == 1)
                {
                    m_lineRenderer.SetPosition(1, m_canvasTargetPoints[0].position);
                }
                else if (m_canvasTargetPoints.Length == 2)
                {
                    float firstPointDistance = Vector3.Distance(m_showPoint, m_canvasTargetPoints[0].position);
                    float lastPointDistance = Vector3.Distance(m_showPoint, m_canvasTargetPoints[1].position);
                    if (firstPointDistance < lastPointDistance)
                    {
                        m_lineRenderer.SetPosition(1, m_canvasTargetPoints[0].position);
                        m_lineRenderer.SetPosition(2, m_canvasTargetPoints[1].position);
                    }
                    else
                    {
                        m_lineRenderer.SetPosition(1, m_canvasTargetPoints[1].position);
                        m_lineRenderer.SetPosition(2, m_canvasTargetPoints[0].position);
                    }
                }
                else
                {
                    for (int i = 0; i < m_canvasTargetPoints.Length; i++)
                    {
                        m_lineRenderer.SetPosition(i + 1, m_canvasTargetPoints[i].position);
                    }
                }
            }

            m_lineRenderer.enabled = Application.isPlaying;
            if (m_targetPoint != null)
            {
                m_targetPoint.gameObject.SetActive(Application.isPlaying);
                UpdateTargetPoint();
            }
        }

        private void UpdateLineRenderingPointsLite()
        {
            m_lineRenderer.SetPosition(0, m_showPoint);

            if (m_closestPoint && m_canvasTargetPoints.Length > 0)
            {
                m_lineRenderer.positionCount = 2;
                var closestPoint = m_canvasTargetPoints[0].position;
                float distance = Vector3.Distance(m_showPoint, m_canvasTargetPoints[0].position);
                for (int i = 1; i < m_canvasTargetPoints.Length; i++)
                {
                    float newDistance = Vector3.Distance(m_showPoint, m_canvasTargetPoints[i].position);
                    if (newDistance < distance)
                    {
                        distance = newDistance;
                        closestPoint = m_canvasTargetPoints[i].position;
                    }
                }
                m_lineRenderer.SetPosition(1, closestPoint);
            }
            else
            {
                m_lineRenderer.positionCount = m_canvasTargetPoints.Length + 1;
                if (m_canvasTargetPoints.Length == 1)
                {
                    m_lineRenderer.SetPosition(1, m_canvasTargetPoints[0].position);
                }
                else if (m_canvasTargetPoints.Length == 2)
                {
                    float firstPointDistance = Vector3.Distance(m_showPoint, m_canvasTargetPoints[0].position);
                    float lastPointDistance = Vector3.Distance(m_showPoint, m_canvasTargetPoints[1].position);
                    if (firstPointDistance < lastPointDistance)
                    {
                        m_lineRenderer.SetPosition(1, m_canvasTargetPoints[0].position);
                        m_lineRenderer.SetPosition(2, m_canvasTargetPoints[1].position);
                    }
                    else
                    {
                        m_lineRenderer.SetPosition(1, m_canvasTargetPoints[1].position);
                        m_lineRenderer.SetPosition(2, m_canvasTargetPoints[0].position);
                    }
                }
                else
                {
                    for (int i = 0; i < m_canvasTargetPoints.Length; i++)
                    {
                        m_lineRenderer.SetPosition(i + 1, m_canvasTargetPoints[i].position);
                    }
                }
            }
        }

        protected override void InternalUpdate(float dt)
        {
            base.InternalUpdate(dt);
            if (m_dynamicLineScaling)
            {
                ApplyLineWidth();
            }
            if (m_dynamicSize)
            {
                UpdateLineRenderingPointsLite();
            }
            else
            {
                m_lineRenderer.SetPosition(0, m_lineRenderer.transform.InverseTransformPoint(m_showPoint));
            }

            if (m_targetPoint != null)
            {
                UpdateTargetPoint();
            }
        }

        private void UpdateTargetPoint()
        {
            m_targetPoint.size = Vector3.one * WeavrUIHelper.GetLengthOfPixelsAt(m_showPoint, m_canvasCamera, m_targetPointSize.x);
            m_targetPoint.transform.position = m_showPoint;
            m_targetPoint.transform.LookAt(m_canvasCamera.transform);
        }

        private void ApplyLineWidth()
        {
            float pixelsWidth = WeavrUIHelper.GetLengthOfPixelsAt(m_showPoint, m_canvasCamera, m_linePixelWidth);
            m_lineRenderer.widthMultiplier = pixelsWidth;
        }
    }
}