using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Player.Views
{
    public interface IUserViewModel : IViewModel
    {
        Guid Id { get; }
        string Name { get; set; }
        Texture2D Image { get; }

        IEnumerable<IGroupViewModel> Groups { get; }
    }
}
