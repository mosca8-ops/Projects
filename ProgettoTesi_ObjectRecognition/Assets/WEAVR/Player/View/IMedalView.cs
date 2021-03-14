using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace TXT.WEAVR.Player.Views
{
    internal interface IMedalView : IView
    {
        string UserNameCongrats { get; set; }

        string ProcedureName { get; set; }

        event UnityAction MedalFinish;

        event UnityAction MedalRestart;

        string UserTime { get; set; }

        string UserScore { get; set; }

    }
}

