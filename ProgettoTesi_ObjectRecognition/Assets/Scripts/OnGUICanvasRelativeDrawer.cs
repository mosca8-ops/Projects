using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OnGUICanvasRelativeDrawer : MonoBehaviour
{
    public struct Label
    {
        public string text;
        public Rect rect;
        public Color color;
    }

    public struct Arrow
    {
        public RawImage arrow;
        public Rect rectArrow;
    }

    [HideInInspector]
    public RectTransform relativeObject
    {
        set
        {
            relative = value;
            rootCanvas = GetRootCanvas(value);
        }
    }

    private RectTransform relative;
    private RectTransform rootCanvas;
    private List<Label> labels = new List<Label>();
    private List<Arrow> arrows = new List<Arrow>();
    private GUIStyle style;
    private Vector2 anchor;
    private string labelProcedure;

    /// <summary>
    /// Draws label on screen above relative object
    /// </summary>
    /// <param name="text">Text to draw</param>
    /// <param name="position">Normalized position of label</param>

    //original
    public void DrawLabel(string text, YOLOHandler.ResultBox box, Color col)
    {
        Rect rectBox = box.rect;
        int width = (int) (box.classes[box.bestClassIdx] / 0.5f);
        Vector2 positionNew = new Vector2(rectBox.x * relative.rect.width, rectBox.y * relative.rect.height);

        Rect rect;
        anchor = GetAnchorPosition();

        if (WebCamDetector.android)
        {
            positionNew = TextureDrawingUtils.Rotate(positionNew, 90);
        }

        rect = new Rect(anchor + positionNew, new Vector2(rectBox.width * relative.rect.width + width, width));

        labels.Add(new Label { text = text, rect = rect, color = col });

    }

    public void DrawLabel(string text, YOLOHandler.ResultBox box, Color col, RawImage arrow, string labelProcedure)
    {
        this.labelProcedure = labelProcedure;
        Rect rect;
        Rect rectArrow;
        Rect rectBox = box.rect;
        Vector2 positionNew = new Vector2(rectBox.x * relative.rect.width, rectBox.y * relative.rect.height);

        anchor = GetAnchorPosition();
        int width = (int)(box.classes[box.bestClassIdx] / 0.5f);

        if (WebCamDetector.android)
        {
            positionNew = TextureDrawingUtils.Rotate(positionNew, 90);
        }

        rect = new Rect(anchor + positionNew + new Vector2(0, -28), new Vector2(rectBox.width /** relative.rect.width*/ + width, width));
        rectArrow = DrawArrow.SetRectArrow(arrow.name, anchor, positionNew, box, relative);

        labels.Add(new Label { text = text, rect = rect, color = col });
        arrows.Add(new Arrow { arrow = arrow, rectArrow = rectArrow });

    }

    /// <summary>
    /// Remove all previous draws
    /// </summary>
    public void Clear()
    {
        labels.Clear();
        arrows.Clear();
    }

    private void Start()
    {
        style = new GUIStyle { fontSize = 25, normal = new GUIStyleState { textColor = Color.white } };
    }

    private void OnGUI()
    {
        if (WebCamDetector.android)
        {
            style.fontSize = 40;
        }

        if (labelProcedure == null )
        {
            for(int i=0;i < labels.Count; i++)
            {
                style.normal.textColor = labels[i].color;
                GUI.Label(labels[i].rect, labels[i].text, style);
            }
        }
        else
        {
            DrawArrow.DrawAll(labels, arrows, labelProcedure, style);
        }
    }

    private Vector2 GetAnchorPosition()
    {
        return new Vector2(relative.localPosition.x, -relative.localPosition.y) + rootCanvas.rect.size / 2 - relative.rect.size / 2;
    }

    private RectTransform GetRootCanvas(RectTransform rectTransform)
    {
        Transform parent = rectTransform.transform;
        if (parent.parent != null && parent.parent.GetComponent<RectTransform>() != null)
        {
            return GetRootCanvas(parent.parent.GetComponent<RectTransform>());
        }
        else
        {
            return parent.GetComponent<RectTransform>();
        }
    }

    // original
    //private void OnGUI()
    //{
    //    if (WebCamDetector.android)
    //    {
    //        foreach (var label in labels)
    //        {
    //            style.normal.textColor = label.color;
    //            style.fontSize = 40;
    //            GUI.Label(label.rect, label.text, style); //new GUIStyle {fontSize = 60, normal = new GUIStyleState {textColor = label.color}});
    //        }
    //    }
    //    else
    //    {
    //        foreach (var label in labels)
    //        {
    //            style.normal.textColor = label.color;
    //            GUI.Label(label.rect, label.text, style); //new GUIStyle { fontSize = 30, normal = new GUIStyleState { textColor = label.color } });
    //        }
    //    }
    //}

}
