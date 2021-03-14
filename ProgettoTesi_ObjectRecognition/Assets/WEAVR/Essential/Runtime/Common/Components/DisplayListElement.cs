using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Text Editing/Display List Element")]
    public class DisplayListElement : MonoBehaviour
    {
        [Draggable]
        public List<DisplayTextObject> DisplayTextObjects;
        public bool Empty = false;
        public int SelectedTextIndex= -1;

        public DisplayListElement ExchangeDisplayElement
        {
            get => null;
            set
            {
                ExchangeDisplayElementContent(value);
            }
        }

        public DisplayListElement CopyContentToDisplayElement
        {
            get => null;
            set
            {
                CopyContentToElement(value);
            }
        }

        public string GetValueById(string id)
        {
            foreach (var textObj in DisplayTextObjects)
            {
                if (textObj.Id == id)
                {
                    return textObj.Value;
                }
            }
            return null;
        }

        public void ExchangeDisplayElementContent(DisplayListElement value)
        {
            Dictionary<string, string> tempIdValue = new Dictionary<string, string>();
            foreach (var obj in value.DisplayTextObjects)
            {
                tempIdValue.Add(obj.Id, obj.Text.text);
            }
            foreach (var textObj in value.DisplayTextObjects)
            {
                var obj = DisplayTextObjects.SingleOrDefault(s => s.Id == textObj.Id);
                if (obj != null)
                {
                    textObj.Text.text = obj.Text.text;
                }
            }
            foreach (var textObj in DisplayTextObjects)
            {
                string tempText = "";
                if (tempIdValue.TryGetValue(textObj.Id, out tempText))
                {
                    textObj.Text.text = tempText;
                }
            }
        }


        public void CopyContentToElement(DisplayListElement value)
        {
            foreach (var textObj in value.DisplayTextObjects)
            {
                var obj = DisplayTextObjects.SingleOrDefault(s => s.Id == textObj.Id);
                if (obj != null)
                {
                    textObj.Text.text = obj.Text.text;
                }
            }
        }
    }
}
