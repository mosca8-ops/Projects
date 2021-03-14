using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("")]
public class ChangeExecutionModeButton : MonoBehaviour
{
    [Draggable]
    public Text TextExecutionMode;
    [Draggable]
    public Button ExecutionModeButton;

    public void SetButtonContent(string text)
    {
        TextExecutionMode.text = text;
    }
}
