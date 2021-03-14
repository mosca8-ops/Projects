namespace TXT.WEAVR.Player.View
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    [AddComponentMenu("")]
    public class PlayModeStartButton : MonoBehaviour
    {
        [Draggable]
        public Text ModeText;
        [Draggable]
        public Button StartModeButtton;
    }
}
