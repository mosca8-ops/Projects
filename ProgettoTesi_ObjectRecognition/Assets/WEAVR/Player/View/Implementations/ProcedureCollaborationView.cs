using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace TXT.WEAVR.Player.Views
{
    public class ProcedureCollaborationView : BaseView, IProcedureCollaborationPage
    {
        // STATIC PART??

        [Space]
        [Header("Procedure Page Items")]
        [Tooltip("Procedure Image preview")]
        [Draggable]
        public Image procedureImage;
        [Tooltip("Button controls video preview")]
        [Draggable]
        public Button procedurePlayVideoButton; // To decide if used
        [Tooltip("GameObject controls video preview raycast")]
        [Draggable]
        public GameObject procedurePlayVideo; // To decide if used
        [Tooltip("List of online users - panel parent")]
        [Draggable]
        public GameObject onlineUserPanel; // TODO: to add example
        [Tooltip("Example User Image - Prefab")]
        [Draggable]
        public UserImageElement userImageExample; // TODO: to add in panel
        [Tooltip("Object List Online users overflow")]
        [Draggable]
        public GameObject onlineUserOverflowObj; // TODO: to activate/deactivate
        [Tooltip("Object number Online users overflow  - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI onlineUserOverflowNumber; 
        [Tooltip("Collaboration name - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI collaborationName;
        [Tooltip("Procedure name - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI procedureName;
        [Tooltip("Procedure Assigned Group - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI assignedGroupName;
        [Tooltip("Collaboration group users list")]
        [Draggable]
        public GridLayoutGroup collGroupList; // TODO: to add example
        [Tooltip("User name and image example - prefab")]
        [Draggable]
        public UserElement userElementExample; // TODO: to add in panel
        [Tooltip("Overflow group users list")]
        [Draggable]
        public Button moreCollGroupButton;
        [Tooltip("Procedure Overview - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI procedureOverview;
        [Tooltip("Start Button")]
        [Draggable]
        public Button procedureStartButton;
        [Tooltip("Back Button")]
        [Draggable]
        public Button procedureCollBackButton;


        [Header("Stats")]
        [Tooltip("Procedure estimated time - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI procedureEstTime;
        [Tooltip("Procedure number of steps - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI procedureStepNumber;
        [Tooltip("ProcedureCompletedTimes - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI procedureCompletedTimes;

        public Texture2D ProcedureImage { get => procedureImage.sprite.texture; set => procedureImage.sprite = SpriteCache.Instance.Get(value); }
        public string OnlineUserOverflowNumber { get => onlineUserOverflowNumber.text; set => onlineUserOverflowNumber.text = value; }
        public string CollaborationName { get => collaborationName.text; set => collaborationName.text = value; }
        public string ProcedureName { get => procedureName.text; set => procedureName.text = value; }
        public string AssignedGroupName { get => assignedGroupName.text; set => assignedGroupName.text = value; }
        public string ProcedureOverview { get => procedureOverview.text; set => procedureOverview.text = value; }
        public string ProcedureEstTime { get => procedureEstTime.text; set => procedureEstTime.text = value; }
        public string ProcedureStepNumber { get => procedureStepNumber.text; set => procedureStepNumber.text = value; }
        public string ProcedureCompletedTimes { get => procedureCompletedTimes.text; set => procedureCompletedTimes.text = value; }

        public event UnityAction MoreCollGroup
        {
            add => procedureStartButton.onClick.AddListener(value);
            remove => procedureStartButton.onClick.RemoveListener(value);
        }

        public event UnityAction ProcedureStart
        {
            add => procedureStartButton.onClick.AddListener(value);
            remove => procedureStartButton.onClick.RemoveListener(value);
        }

        public event UnityAction ProcedureCollBack
        {
            add => procedureCollBackButton.onClick.AddListener(value);
            remove => procedureCollBackButton.onClick.RemoveListener(value);
        }
    }
}
