using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Player.Communication.Auth;
using TXT.WEAVR.Player.Model;
using TXT.WEAVR.Communication.Entities;
using UnityEngine;

namespace TXT.WEAVR.Player.Model
{

    public class UsersModel : IUserModel
    {
        public User CurrentUser { get => AuthUser.User; set => AuthUser.User = value; }
        public AuthUser AuthUser { get; set; }

        public event OnModelChangedDelegate OnChanged;

        public IModel Clone()
        {
            return new UsersModel()
            {
                AuthUser = AuthUser,
            };
        }
    }
}
