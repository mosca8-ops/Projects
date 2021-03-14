using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Player.Model
{

    public interface ISettingsModel : IModel
    {
        bool AutoUpdateProcedures { get; set; }
        IList<(string setting, object value)> GetSettings();
        void Set(string setting, object value);
        object Get(string setting);
    }
}
