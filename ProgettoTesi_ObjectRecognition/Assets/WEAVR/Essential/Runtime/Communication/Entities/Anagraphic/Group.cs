using System.Collections.Generic;

namespace TXT.WEAVR.Communication.Entities
{
    public class Group : BaseEntity
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public IEnumerable<UserGroup> UsersGroups { get; set; }

        // ---------------

    }
}
