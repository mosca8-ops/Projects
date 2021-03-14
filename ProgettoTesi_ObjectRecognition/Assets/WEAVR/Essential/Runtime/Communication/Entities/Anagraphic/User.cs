using System;
using System.Collections.Generic;

namespace TXT.WEAVR.Communication.Entities
{
    public class User : BaseEntity
    {
        public Guid AuthUserId { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string FullName => $"{FirstName} {LastName}";

        public string Department { get; set; }


        public DateTime? LastLoginAt { get; set; }


        public Guid StatusId { get; set; }
        public UserStatus Status { get; set; }

        public IEnumerable<UserGroup> UsersGroups { get; set; }
        public IEnumerable<UserRole> UsersRoles { get; set; }


        public Guid? UserPhotoId { get; set; }
        public virtual UserPhoto UserPhoto { get; set; }

        // ---------------

    }
}
