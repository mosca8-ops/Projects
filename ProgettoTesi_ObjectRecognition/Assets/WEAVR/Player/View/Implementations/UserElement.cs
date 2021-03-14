using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace TXT.WEAVR.Player.Views
{

    public class UserElement : UserImageElement
    {
        [Draggable]
        public TextMeshProUGUI UserName;
    }
}
