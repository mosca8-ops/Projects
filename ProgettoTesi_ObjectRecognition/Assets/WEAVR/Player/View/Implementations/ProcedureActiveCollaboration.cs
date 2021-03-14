using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;


namespace TXT.WEAVR.Player.Views
{
    public class ProcedureActiveCollaboration : ProcedurePreview
    {
        [Space]
        [Header("Collaboration Procedure Items")]
        [Draggable]
        public TextMeshProUGUI CollaborationName;
        [Draggable]
        public Button JoinProcedureButton;
        [Draggable]
        public GameObject OnlineUserPanel;
        [Draggable]
        public GameObject OnlineUserOverflowObj;
        [Draggable]
        public TextMeshProUGUI OnlineUserOverflowNumber;
        [Draggable]
        public GameObject GroupUserPanel;
        [Draggable]
        public UserImageElement UserImageExample;
        [Draggable]
        public TextMeshProUGUI GroupUserOverflowNumber;
    }
}
