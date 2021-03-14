using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DrawArrow : MonoBehaviour
{
    public static void SetArrow(OnGUICanvasRelativeDrawer relativeDrawer,
        string label, 
        YOLOHandler.ResultBox box,
        Color col,
        string labelProcedure)
    {
        //frecce
        RawImage arrowLeft = GameObject.FindGameObjectWithTag("LeftArrow").GetComponent<RawImage>();
        RawImage arrowFront = GameObject.FindGameObjectWithTag("FrontArrow").GetComponent<RawImage>();
        RawImage arrowUp = GameObject.FindGameObjectWithTag("UpArrow").GetComponent<RawImage>();
        //frecce

        switch (labelProcedure.ToLower())
        {
            case "ruota":
                relativeDrawer.DrawLabel(label, box, col, arrowFront, labelProcedure);
                break;
            case "rimuovicover":
                relativeDrawer.DrawLabel(label, box, col, arrowLeft, labelProcedure);
                break;
            case "rimuovibatteria":
                relativeDrawer.DrawLabel(label, box, col, arrowUp, labelProcedure);
                break;
            default:
                relativeDrawer.DrawLabel(label, box, col);
                break;
        }
    }

    public static Rect SetRectArrow(string nameArrow,
        Vector2 anchor,
        Vector2 position,
        YOLOHandler.ResultBox box,
        RectTransform relative)
    {
        box.rect.width = box.rect.x + box.rect.width > relative.rect.width ? relative.rect.width - box.rect.x : box.rect.width;
        box.rect.height = box.rect.y + box.rect.height > relative.rect.height ? relative.rect.height - box.rect.y : box.rect.height;

        //modificare il canvas in base a come lo visualizzi nel game windows
        switch (nameArrow)
        {
            case "RawImageFrontArrow":
                //front
                return new Rect((anchor + position) +
                    new Vector2(- box.rect.width/8  * relative.rect.width,
                    box.rect.height / 15 * relative.rect.height),
                    new Vector2(box.rect.size.x * relative.rect.size.x,
                    box.rect.size.y * relative.rect.size.y)); 
            case "RawImageLeftArrow":
                //left
                return new Rect((anchor + position) + 
                     new Vector2(box.rect.width * relative.rect.width, 
                     box.rect.height / 8 * relative.rect.height),
                     new Vector2(box.rect.size.x * relative.rect.size.x,
                     box.rect.size.y * relative.rect.size.y)); 
            case "RawImageUpArrow":
                //up
                return new Rect((anchor + position) + 
                        new Vector2(box.rect.x / 10 * relative.rect.width,
                        box.rect.height * relative.rect.height),
                        new Vector2(box.rect.size.x * relative.rect.size.x,
                        box.rect.size.y * relative.rect.size.y)); 
            default:
                return new Rect(0, 0, 0, 0);
        }
    }

    public static void DrawAll(List<OnGUICanvasRelativeDrawer.Label> labels,  List<OnGUICanvasRelativeDrawer.Arrow> arrows, string labelProcedure, GUIStyle style)
    {
        for (int i = 0; i < labels.Count; i++)
        {
            style.normal.textColor = labels[i].color;
            if (labelProcedure.Equals("Ruota"))
            {
                GUI.Label(labels[i].rect, labels[i].text, style);

                if (labels[i].text.Contains("Fronte S2"))
                {
                    GUI.Label(arrows[i].rectArrow, arrows[i].arrow.texture);
                }
            }
            else if (labelProcedure.Equals("RimuoviCover"))
            {
                GUI.Label(labels[i].rect, labels[i].text, style);

                if (labels[i].text.Contains("Retro S2"))
                {
                    GUI.Label(arrows[i].rectArrow, arrows[i].arrow.texture);
                }
            }
            else
            {
                GUI.Label(labels[i].rect, labels[i].text, style);

                if (labels[i].text.Contains("Batteria"))
                {
                    GUI.Label(arrows[i].rectArrow, arrows[i].arrow.texture);
                }
            }
        }
    }
}
