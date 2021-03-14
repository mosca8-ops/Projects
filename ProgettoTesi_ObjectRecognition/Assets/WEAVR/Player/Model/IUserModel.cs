using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Player.Communication.Auth;
using TXT.WEAVR.Communication.Entities;
using UnityEngine;

namespace TXT.WEAVR.Player.Model
{

    public interface IUserModel : IModel
    {
        User CurrentUser { get; set; }
        AuthUser AuthUser { get; set; }

    }
}
