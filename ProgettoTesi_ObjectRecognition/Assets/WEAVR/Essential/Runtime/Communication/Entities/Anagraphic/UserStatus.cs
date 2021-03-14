

using System;

namespace TXT.WEAVR.Communication.Entities
{
    [Serializable]
    public class UserStatus : BaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
