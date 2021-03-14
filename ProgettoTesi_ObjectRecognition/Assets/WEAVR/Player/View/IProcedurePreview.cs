using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Player.Views
{
    public interface IProcedurePreview : IView
    {
        string Name { get; set; }
        string Description { get; set; }
        string AssignedGroupName { get; set; }
        Texture2D Image { get; set; }
        ProcedureFlags Status { get; set; }

        event ViewDelegate<IProcedurePreview> OnSelected;
        event ViewDelegate<IProcedurePreview> OnAction;


        void Clear();
    }
}
