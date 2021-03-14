using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Maintenance
{
    [AddComponentMenu("WEAVR/Utilities/Switch State Canvas")]
    public class SwitchStateCanvas : MonoBehaviour
    {

        [Draggable]
        public Text textComponent;
        public Color defaultColor = Color.clear;

        private void OnValidate()
        {
            if (textComponent == null)
            {
                textComponent = GetComponentInChildren<Text>();
            }
            if (defaultColor == Color.clear)
            {
                defaultColor = textComponent.color;
            }
        }

        public void SetText(string text)
        {
            SetText(text, defaultColor);
        }

        public void SetText(string text, Color color)
        {
            if (textComponent != null)
            {
                textComponent.text = text;
                textComponent.color = color;
            }
        }

        public void SetState(SwitchState state)
        {
            SetText(state.displayName, state.displayColor);
        }
    }
}