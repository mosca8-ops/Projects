
using global::TXT.WEAVR.Player.Controller;
using global::TXT.WEAVR.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TXT.WEAVR.Player.Views
{
    public class HomeView : BaseView
    {
        // STATIC PART??

        [Space]
        [Header("Home Page Items")]

        [Tooltip("User Name - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI UserName;

        [Tooltip("Canvas Panel for the Recently Opened Procedures")]
        [Draggable]
        public GameObject RecentlyOpenedPanel;

        [Tooltip("Example of the procedure object to create Recently Opened list")]
        [Draggable]
        public ProcedurePreview RecentlyOpenedProcExample;

        [Tooltip("Button to open Library Page")]
        [Draggable]
        public Button LibraryButton;

        [Tooltip("Canvas Panel for the Recently Assigned Procedures")]
        [Draggable]
        public GameObject RecentlyAssignedPanel;

        [Tooltip("Example of the procedure object to create Recently Assigned list")]
        [Draggable]
        public ProcedurePreview RecentlyAssignedProcExample;

    }
}
