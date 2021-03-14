using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Player.Model
{

    public interface IModelManager
    {
        T GetModel<T>() where T : IModel;
    }
}
