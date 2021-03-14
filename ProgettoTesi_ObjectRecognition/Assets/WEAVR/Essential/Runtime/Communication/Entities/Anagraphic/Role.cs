using System.Collections.Generic;

namespace TXT.WEAVR.Communication.Entities
{
    public class Role : BaseEntity
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public IEnumerable<UserRole> UsersRoles { get; set; }

        // ---------------
    }
}
