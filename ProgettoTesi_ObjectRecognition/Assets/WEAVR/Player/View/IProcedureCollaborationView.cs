using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Player.Views
{
    internal interface IProcedureCollaborationPage : IView
    {
        Texture2D ProcedureImage { get; set; }

        string OnlineUserOverflowNumber { get; set; }

        string CollaborationName { get; set; }

        string ProcedureName { get; set; }

        string AssignedGroupName { get; set; }

        event UnityAction MoreCollGroup;

        string ProcedureOverview { get; set; }


        event UnityAction ProcedureStart;


        event UnityAction ProcedureCollBack;

        string ProcedureEstTime { get; set; }

        string ProcedureStepNumber { get; set; }

        string ProcedureCompletedTimes { get; set; }

    }
}