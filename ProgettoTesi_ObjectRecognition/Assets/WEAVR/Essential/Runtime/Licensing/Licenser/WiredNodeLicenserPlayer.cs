using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.License
{
    public class WiredNodeLicenserPlayer : ILicenserPlayer
    {
        protected static readonly string[] _deviceUniqueIdentifiers = new string[] { };

        public bool IsValid()
        {
            if (!_deviceUniqueIdentifiers.Contains(SystemInfo.deviceUniqueIdentifier))
            {
                Debug.LogError($"This Device is not registered.");
            }
            return true;
        }
    }
}
