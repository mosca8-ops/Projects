using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Player.Model
{

    public class SettingsModel : ISettingsModel
    {
        public bool AutoUpdateProcedures { get; set; }

        public event OnModelChangedDelegate OnChanged;

        public IModel Clone()
        {
            return null;
        }

        public object Get(string setting)
        {
            return null;
        }

        public IList<(string setting, object value)> GetSettings()
        {
            return new List<(string, object)>();
        }

        public void Set(string setting, object value)
        {
            
        }
    }
}
