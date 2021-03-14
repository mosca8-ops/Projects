using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Player.Model
{
    public delegate void OnModelChangedDelegate(IModel model);

    public interface IModel
    {
        IModel Clone();
        event OnModelChangedDelegate OnChanged;
    }
}
