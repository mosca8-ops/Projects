using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Utilities/Door State Color")]
    public class DoorStateColor : MonoBehaviour
    {
        [Header("Colors")]
        public Color closeColor = Color.red;
        public Color openColor = Color.green;
        public Color fullyOpenColor = Color.cyan;

        [Header("Components")]
        [Draggable]
        public AbstractDoor door;
        public UnityEngine.UI.Image image;

        private float alpha;

        private void Reset()
        {
            image = GetComponentInChildren<UnityEngine.UI.Image>();
        }

        private void OnValidate()
        {
            if (image != null)
            {
                alpha = image.color.a;
            }
            closeColor.a = alpha;
            openColor.a = alpha;
            fullyOpenColor.a = alpha;
        }

        // Use this for initialization
        void Start()
        {
            if (image != null)
            {
                alpha = image.color.a;
            }
            closeColor.a = alpha;
            openColor.a = alpha;
            fullyOpenColor.a = alpha;

            if (door != null)
            {
                door.OnClosed.AddListener(() => image.color = closeColor);
                door.OnOpening.AddListener(() => image.color = openColor);
                door.OnFullyOpened.AddListener(() => image.color = fullyOpenColor);
            }
        }
    }
}