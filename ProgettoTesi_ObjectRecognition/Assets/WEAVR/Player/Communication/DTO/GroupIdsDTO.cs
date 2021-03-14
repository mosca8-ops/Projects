using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace TXT.WEAVR.Player.Communication.DTO
{
    [Serializable]
    public class GroupIdsDTO
    {
        public Guid[] groupIds;
    }
    
}