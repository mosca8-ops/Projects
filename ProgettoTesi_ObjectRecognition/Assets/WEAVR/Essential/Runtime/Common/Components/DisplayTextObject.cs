using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Text Editing/Display Text Object")]
    public class DisplayTextObject : MonoBehaviour
    {
        public string Id;
        public string Value;
        [Draggable]
        public Text Text;
        public List<string> IgnoredCharList = new List<string>();

        public void SetContent(string value)
        {
            Value = value;
            
            var editor = GetComponent<TextEditor>();
            if(editor)
            {
                foreach(var ignoredChar in IgnoredCharList)
                {
                   value = value.Replace(ignoredChar, "");
                }
                editor.Text = value;
            }
            if (Text)
            {
                Text.text = Value;
            }
        }
    }
}
