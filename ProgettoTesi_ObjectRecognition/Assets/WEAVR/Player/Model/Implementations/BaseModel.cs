using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Player.Model
{

    public abstract class BaseModel : MonoBehaviour, IModel
    {
        public event OnModelChangedDelegate OnChanged;

        protected void MarkChanged() => OnChanged?.Invoke(this);

        public abstract IModel Clone();
    }
}
