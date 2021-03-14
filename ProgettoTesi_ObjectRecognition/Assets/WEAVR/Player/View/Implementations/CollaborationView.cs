using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace TXT.WEAVR.Player.Views
{
    public class CollaborationView : BaseView, ICollaborationPage
    {
        // STATIC PART??

        [Space]
        [Header("Collaboration Page Items")]
        [Tooltip("Canvas Panel for the Active Colaboration Procedures List")]
        [Draggable]
        public GameObject CollaborationProcedurePanel; // TODO: to add examples
        [Tooltip("Example of the procedure object to create the list")]
        [Draggable]
        public ProcedurePreview CollaborationProcedureExample;  // TODO: to add in the panel
        [Tooltip("Scrollbar for the Active Collaborations List")]
        [Draggable]
        public Scrollbar CollaborationListScrollbar; // TODO: VR only
        [Tooltip("Button up arrow of scrollbar for the Active Collaborations List")]
        [Draggable]
        public Button UpArrowCollaborationScrollbar;  // TODO: VR only
        [Tooltip("Button down arrow of scrollbar for the Active Collaborations List")]
        [Draggable]
        public Button DownArrowCollaborationScrollbar; // TODO: VR only

    }
}
